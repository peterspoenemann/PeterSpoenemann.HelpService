const vscode = require('vscode');
const path = require('path');
const fs = require('fs');
const { execFile } = require('child_process');

let currentPanel;
let currentDocument;
let renderTimer;
let selectedSource;
let sourceViewColumn = vscode.ViewColumn.One;

function activate(context) {
  context.subscriptions.push(vscode.commands.registerCommand('helpService.openPreview', uri => openPreview(context, uri)));
  context.subscriptions.push(vscode.commands.registerCommand('helpService.openSource', openSelectedSource));
  context.subscriptions.push(vscode.workspace.onDidSaveTextDocument(document => {
    if (currentPanel && document.languageId === 'markdown') scheduleRender(context);
  }));
  context.subscriptions.push(vscode.workspace.onDidChangeConfiguration(event => {
    if (currentPanel && event.affectsConfiguration('helpService.preview')) scheduleRender(context);
  }));
}

async function openPreview(context, uri) {
  const editorUri = uri || vscode.window.activeTextEditor?.document.uri;
  if (!editorUri || path.extname(editorUri.fsPath).toLowerCase() !== '.md') {
    vscode.window.showWarningMessage('Öffnen Sie zuerst eine Markdown-Hilfedatei.');
    return;
  }

  currentDocument = editorUri;
  sourceViewColumn = vscode.window.activeTextEditor?.viewColumn || sourceViewColumn;
  if (!currentPanel) {
    currentPanel = vscode.window.createWebviewPanel(
      'helpServicePreview',
      'HelpService-Vorschau',
      vscode.ViewColumn.Beside,
      { enableScripts: true, retainContextWhenHidden: true }
    );
    currentPanel.onDidDispose(() => {
      currentPanel = undefined;
      currentDocument = undefined;
      selectedSource = undefined;
    });
    currentPanel.webview.onDidReceiveMessage(message => {
      if (message.type === 'openExternal') vscode.env.openExternal(vscode.Uri.parse(message.href));
      else if (message.type === 'selectSource') {
        selectedSource = typeof message.file === 'string' && Number.isInteger(message.line)
          ? { file: message.file, line: message.line }
          : undefined;
      }
    });
  } else {
    currentPanel.reveal(vscode.ViewColumn.Beside, true);
  }
  await render(context);
}

async function openSelectedSource() {
  if (!selectedSource) {
    vscode.window.showWarningMessage('Für diesen Inhalt ist keine Markdown-Quelldatei hinterlegt.');
    return;
  }

  try {
    const document = await vscode.workspace.openTextDocument(vscode.Uri.file(selectedSource.file));
    const editor = await vscode.window.showTextDocument(document, {
      viewColumn: sourceViewColumn,
      preview: false,
      preserveFocus: false
    });
    const line = Math.max(0, Math.min(selectedSource.line - 1, document.lineCount - 1));
    const position = new vscode.Position(line, 0);
    editor.selection = new vscode.Selection(position, position);
    editor.revealRange(new vscode.Range(position, position), vscode.TextEditorRevealType.InCenterIfOutsideViewport);
  } catch (error) {
    vscode.window.showErrorMessage(`Quelldatei konnte nicht geöffnet werden: ${error.message || String(error)}`);
  }
}

function scheduleRender(context) {
  clearTimeout(renderTimer);
  renderTimer = setTimeout(() => render(context), 250);
}

async function render(context) {
  if (!currentPanel || !currentDocument) return;
  selectedSource = undefined;
  currentPanel.webview.html = loadingHtml();
  try {
    const result = await invokeRenderer(context, currentDocument);
    if (!currentPanel) return;
    currentPanel.title = `Hilfe: ${path.basename(result.rootFile)}`;
    currentPanel.webview.html = previewHtml(result, currentPanel.webview.cspSource);
  } catch (error) {
    if (currentPanel) currentPanel.webview.html = errorHtml(error.message || String(error));
  }
}

function invokeRenderer(context, documentUri) {
  const configuration = vscode.workspace.getConfiguration('helpService.preview', documentUri);
  const dotnet = configuration.get('dotnetPath', 'dotnet');
  const root = configuration.get('rootFile', '').trim();
  const language = configuration.get('language', 'auto');
  const workspace = vscode.workspace.getWorkspaceFolder(documentUri)?.uri.fsPath || path.dirname(documentUri.fsPath);
  const packagedDll = path.join(context.extensionPath, 'renderer', 'PeterSpoenemann.HelpService.VsCodeRenderer.dll');
  const developmentProject = path.join(context.extensionPath, '..', 'src', 'HelpService.VsCodeRenderer', 'PeterSpoenemann.HelpService.VsCodeRenderer.csproj');
  const args = fs.existsSync(packagedDll)
    ? [packagedDll]
    : ['run', '--project', developmentProject, '--'];
  args.push('--document', documentUri.fsPath, '--workspace', workspace);
  if (root) args.push('--root', root);
  if (language !== 'auto') args.push('--language', language);

  return new Promise((resolve, reject) => {
    execFile(dotnet, args, { cwd: workspace, windowsHide: true, maxBuffer: 16 * 1024 * 1024 }, (error, stdout, stderr) => {
      if (error) {
        reject(new Error((stderr || stdout || error.message).trim()));
        return;
      }
      try { resolve(JSON.parse(stdout)); }
      catch (parseError) { reject(new Error(`Ungültige Renderer-Antwort: ${parseError.message}`)); }
    });
  });
}

function previewHtml(result, cspSource) {
  const nonce = randomNonce();
  const topics = result.topics.map(topic => `
    <article id="topic-${escapeAttribute(topic.id)}" data-topic="${escapeAttribute(topic.id)}">
      <h1>${escapeHtml(topic.title)}</h1>${namespaceHtml(topic.html, topic.id)}
    </article>`).join('');
  const navigation = result.topics.map(topic =>
    `<a href="#topic-${escapeAttribute(topic.id)}">${escapeHtml(topic.title)}</a>`).join('');
  const messages = result.messages.length ? `<aside class="messages">${result.messages.map(message =>
    `<div><strong>${escapeHtml(message.level)}:</strong> ${escapeHtml(message.message)}</div>`).join('')}</aside>` : '';
  return `<!DOCTYPE html><html lang="${escapeAttribute(result.language)}"><head>
    <meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
    <meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data: ${cspSource} https:; style-src 'nonce-${nonce}'; script-src 'nonce-${nonce}'">
    <style nonce="${nonce}">${styles()}</style></head><body>
    <nav><div class="nav-title">Inhalt</div>${navigation}</nav>
    <main>${messages}${topics || '<p>Keine gültigen Hilfethemen gefunden.</p>'}</main>
    <script nonce="${nonce}">
      const vscode = acquireVsCodeApi();
      document.addEventListener('click', event => {
        const link = event.target.closest('a'); if (!link) return;
        const href = link.getAttribute('href') || '';
        if (href.startsWith('topic:')) { event.preventDefault(); location.hash = 'topic-' + encodeURIComponent(href.slice(6)); }
        else if (/^https?:/i.test(href)) { event.preventDefault(); vscode.postMessage({ type: 'openExternal', href }); }
      });
      document.querySelectorAll('[data-source-file]').forEach(element => {
        element.dataset.vscodeContext = JSON.stringify({ webviewSection: 'source' });
      });
      document.addEventListener('contextmenu', event => {
        const source = event.target.closest('[data-source-file]');
        vscode.postMessage(source ? {
          type: 'selectSource',
          file: source.dataset.sourceFile,
          line: Number.parseInt(source.dataset.sourceLine, 10)
        } : { type: 'selectSource' });
      }, true);
    </script></body></html>`;
}

function styles() { return `
  :root { color: var(--vscode-editor-foreground); background: var(--vscode-editor-background); font-family: var(--vscode-font-family); }
  body { margin: 0; display: grid; grid-template-columns: minmax(150px, 230px) 1fr; min-height: 100vh; }
  nav { position: sticky; top: 0; height: 100vh; overflow: auto; box-sizing: border-box; padding: 18px 12px; border-right: 1px solid var(--vscode-panel-border); background: var(--vscode-sideBar-background); }
  nav a { display: block; padding: 5px 7px; color: var(--vscode-sideBar-foreground); text-decoration: none; border-radius: 3px; }
  nav a:hover { background: var(--vscode-list-hoverBackground); }
  .nav-title { font-weight: 700; margin: 0 7px 10px; text-transform: uppercase; font-size: 11px; }
  main { max-width: 900px; padding: 14px 32px 60px; line-height: 1.55; min-width: 0; }
  article { padding-bottom: 28px; border-bottom: 1px solid var(--vscode-panel-border); }
  article > h1, h2, h3 { color: var(--vscode-textLink-foreground); line-height: 1.25; }
  article > h1 { margin-top: 1.2em; } h2 { margin-top: 1.4em; border-bottom: 1px solid var(--vscode-panel-border); padding-bottom: .25em; }
  p { margin: 0 0 .8em; } ul, ol { padding-left: 1.6em; } li { margin: .2em 0; }
  table { width: 100%; border-collapse: collapse; margin: .8em 0 1.2em; } th, td { border: 1px solid var(--vscode-panel-border); padding: 7px 9px; text-align: left; vertical-align: top; }
  th { background: var(--vscode-editorWidget-background); } code { background: var(--vscode-textCodeBlock-background); border-radius: 3px; padding: .12em .3em; }
  a { color: var(--vscode-textLink-foreground); } img { max-width: 100%; height: auto; }
  blockquote { margin: .8em 0 1em; padding: 8px 14px; border-left: 4px solid var(--vscode-panel-border); background: var(--vscode-textBlockQuote-background); }
  .markdown-alert { border: 1px solid; border-left-width: 5px; border-radius: 4px; padding: 11px 13px; margin: .8em 0 1em; }
  .markdown-alert-title { font-weight: 700; margin-bottom: .35em; } .markdown-alert > :last-child { margin-bottom: 0; }
  .markdown-alert-tip, .markdown-alert-note { border-color: var(--vscode-charts-yellow); } .markdown-alert-warning, .markdown-alert-important, .markdown-alert-caution { border-color: var(--vscode-errorForeground); }
  .task-list-item { list-style: none; } .footnotes { margin-top: 1.5em; border-top: 1px solid var(--vscode-panel-border); font-size: .92em; }
  .messages { padding: 10px 12px; border-left: 4px solid var(--vscode-errorForeground); background: var(--vscode-inputValidation-warningBackground); margin-bottom: 18px; }
  @media (max-width: 650px) { body { display: block; } nav { position: static; width: auto; height: auto; border-right: 0; border-bottom: 1px solid var(--vscode-panel-border); } main { padding: 12px 18px 40px; } }
`; }

function loadingHtml() { return '<!DOCTYPE html><html><body><p>HelpService-Vorschau wird erzeugt …</p></body></html>'; }
function errorHtml(message) { return `<!DOCTYPE html><html><body><h2>Vorschau konnte nicht erzeugt werden</h2><pre>${escapeHtml(message)}</pre></body></html>`; }
function namespaceHtml(html, topicId) {
  const prefix = `help-${encodeURIComponent(topicId)}-`;
  return html
    .replace(/\bid="([^"]+)"/g, (_match, id) => `id="${prefix}${id}"`)
    .replace(/\bhref="#([^"]+)"/g, (_match, id) => `href="#${prefix}${id}"`);
}
function escapeHtml(value) { return String(value).replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[char]); }
function escapeAttribute(value) { return escapeHtml(value); }
function randomNonce() { return [...Array(24)].map(() => Math.random().toString(36)[2]).join(''); }

function deactivate() { clearTimeout(renderTimer); selectedSource = undefined; }
module.exports = { activate, deactivate };
