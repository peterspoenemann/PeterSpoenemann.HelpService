# PeterSpoenemann HelpService Preview

Diese VS-Code-Extension zeigt die Markdown-Hilfedateien von
`PeterSpoenemann.HelpService` mit derselben .NET-/Markdig-Pipeline wie die Anwendung an.

Unterstützt werden insbesondere:

- `!include`, auch verschachtelt und mit Leerzeichen in `<...>`
- Themenüberschriften der Form `# id | Titel`
- lokale Bilder relativ zur jeweiligen Quelldatei
- Markdigs `UseAdvancedExtensions()`, darunter Tabellen, Fußnoten, Aufgabenlisten,
  Definitionslisten, Auto-Links, Überschriften-IDs und Markdown-Alerts
- `topic:`-Links innerhalb der Vorschau
- Herkunftsanzeige als Tooltip mit relativem Dateipfad und Originalzeile
- natives Kontextmenü **Quelldatei öffnen**, auch für Inhalte aus verschachtelten Includes

## Verwendung

Eine `ContextHelp*.md`-Datei oder eine von ihr eingebundene Markdown-Datei öffnen und
**HelpService: Vorschau öffnen** ausführen. Alternativ steht rechts oben im Editor das
Vorschau-Symbol zur Verfügung. Die Ansicht wird beim Speichern einer Markdown-Datei
aktualisiert. Das ist wichtig, weil auch verschachtelte Includes vom Dateisystem gelesen werden.

Die Extension erkennt die zugehörige `ContextHelp*.md`-Wurzeldatei anhand der
Include-Kette. Für gemeinsam eingebundene Dateien kann unter
`helpService.preview.rootFile` eine feste Wurzel angegeben werden. Relative Pfade
beziehen sich auf den Workspace. `helpService.preview.language` legt bei Bedarf die
Sprache fest.

Jeder gerenderte Markdown-Block zeigt beim Darüberfahren seine ursprüngliche Datei und
Zeilennummer an. Über einen Rechtsklick und **HelpService: Quelldatei öffnen** wird die
tatsächliche Markdown-Datei im ursprünglichen Editorbereich geöffnet und der Cursor an
der entsprechenden Zeile positioniert. Bei `!include` ist dies die eingebundene Datei.

Voraussetzung ist das .NET-10-Runtime-Paket beziehungsweise SDK.

## Installieren

Die jeweils aktuelle fertige VSIX steht im GitHub Release bereit:

[PeterSpoenemann.HelpService.Preview.vsix herunterladen](https://github.com/PeterSpoenemann/PeterSpoenemann.HelpService/releases/latest/download/PeterSpoenemann.HelpService.Preview.vsix)

Anschließend kann sie auf der Kommandozeile installiert werden:

```powershell
code --install-extension .\PeterSpoenemann.HelpService.Preview.vsix
```

## Entwickeln und paketieren

```powershell
cd vscode-extension
npm ci
npm run build:renderer
npm run package
```

`build:renderer` veröffentlicht den .NET-Renderer nach `vscode-extension/renderer`.
`npm run package` erzeugt anschließend eine installierbare `.vsix`-Datei.

Unter Windows genügt alternativ ein Aufruf von `Build.cmd` im Extension-Ordner. Das
Skript baut den aktuellen Core und Renderer und erzeugt anschließend die `.vsix`-Datei.

Beim Push eines Tags wie `v0.4.0` setzt der GitHub-Workflow die Extension-Version automatisch
auf `0.4.0`, baut die VSIX unter dem stabilen Namen
`PeterSpoenemann.HelpService.Preview.vsix` und hängt sie an das zugehörige GitHub Release an.
