# PeterSpoenemann.HelpService

[![Build and test](https://github.com/PeterSpoenemann/PeterSpoenemann.HelpService/actions/workflows/ci.yml/badge.svg)](https://github.com/PeterSpoenemann/PeterSpoenemann.HelpService/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/PeterSpoenemann.HelpService.svg)](https://www.nuget.org/packages/PeterSpoenemann.HelpService)

`PeterSpoenemann.HelpService` ist eine wiederverwendbare WPF-Hilfebibliothek. Sie liest
Markdown-Themen mit `!include`-Unterstützung, rendert das aktive Thema mit Markdig
als statisches HTML und zeigt es in einem WebView2-Hilfefenster an. Das Fenster
enthält Inhaltsverzeichnis, Volltextsuche sowie Zurück-/Vorwärts-Navigation.

## Voraussetzungen

- Windows und eine WPF-Anwendung auf .NET 10
- installierte [Microsoft Edge WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

## Installation

Nach der Veröffentlichung auf NuGet.org:

```powershell
dotnet add package PeterSpoenemann.HelpService
```

## Einbindung

Das WPF-Anwendungsprojekt referenziert `PeterSpoenemann.HelpService` und registriert die
Dienste im DI-Container:

```csharp
services.AddPeterSpoenemannHelpService(options =>
{
    options.RootHelpFile = Path.Combine("Help", "ContextHelp.de.md");
    options.ApplicationName = "MeineAnwendung";
});
```

Die bisherige Eigenschaft `RootHelpFile` bleibt vollständig unterstützt und konfiguriert die
deutsche Hilfe. Deutsch (`de`) ist die Standardsprache.

### Mehrsprachige Hilfe

Für Deutsch, Englisch und Polnisch werden mehrere Wurzeldateien nach Sprachcode registriert:

```csharp
services.AddPeterSpoenemannHelpService(options =>
{
    options.RootHelpFiles[HelpLanguageCodes.German] =
        Path.Combine("Help", "ContextHelp.de.md");
    options.RootHelpFiles[HelpLanguageCodes.English] =
        Path.Combine("Help", "ContextHelp.en.md");
    options.RootHelpFiles[HelpLanguageCodes.Polish] =
        Path.Combine("Help", "ContextHelp.pl.md");
    options.Language = HelpLanguageCodes.German;
    options.ApplicationName = "MeineAnwendung";
});
```

Die Sprache kann anschließend ohne Neustart gewechselt werden. Ein bereits geöffnetes
Hilfefenster aktualisiert dabei Oberfläche, Inhaltsverzeichnis und aktives Hilfethema:

```csharp
var languageService = serviceProvider.GetRequiredService<IHelpLanguageService>();

languageService.SetLanguage(HelpLanguageCodes.English);
languageService.SetLanguage(HelpLanguageCodes.Polish);
languageService.SetLanguage(HelpLanguageCodes.German);
```

`IHelpLanguageService.CurrentLanguage` enthält die aktive Sprache und
`SupportedLanguages` die tatsächlich konfigurierten Sprachen. Über `LanguageChanged`
kann die einbindende Anwendung ihre eigene Oberfläche gleichzeitig aktualisieren.

Relative Wurzelpfade werden gegen `AppContext.BaseDirectory` aufgelöst. Die
Anwendung muss ihre Hilfequellen in die Ausgabe kopieren, beispielsweise:

```xml
<ItemGroup>
  <None Update="Help\**\*">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

Anwendungscode verwendet `IContextHelpService.ShowHelp(topicId, owner)` zum Öffnen
und `IHelpContentProvider.HasTopic(topicId)` zum Ein-/Ausblenden kontextbezogener
Hilfe-Schaltflächen. F1 und die konkrete Gestaltung der Hilfe-Schaltfläche bleiben
Aufgabe der einbindenden Anwendung.

## Syntax der Programmhilfe

Die Hilfetexte werden mit **Markdig** und dessen `UseAdvancedExtensions()`-Pipeline in HTML umgewandelt. Damit stehen CommonMark und die unten beschriebenen erweiterten Markdown-Elemente zur Verfügung. Eingebettetes Roh-HTML ist aus Sicherheitsgründen deaktiviert und wird als Text angezeigt.

## Themen und Inhaltsverzeichnis

Die Wurzeldatei ist `ContextHelp.de.md`. Sie enthält ausschließlich `!include`-Zeilen, eine je Themendatei unter `Topics/`. Jedes eigentliche Hilfethema beginnt mit einer Themen-ID und einem Titel:

```md
# collection | Sammlung
```

Eine Themendatei kann mehrere Themen enthalten (mehrere `# id | Titel`-Überschriften hintereinander). Diese Überschrift ist Metadaten-Syntax der Anwendung: Sie bestimmt Themen-ID und Navigationstitel und wird nicht an Markdig übergeben. Inhaltliche Überschriften innerhalb eines Themas beginnen daher mit `##`.

Die Themen-ID entspricht üblicherweise einem Navigationseintrag, einer Seite oder
einem Dialog der einbindenden Anwendung und wird dort an `ShowHelp` übergeben.

## Verweise auf andere Themen

Ein Verweis auf ein anderes Thema (z. B. im Abschnitt "Siehe auch") wird als normaler Markdown-Link mit dem Schema `topic:` geschrieben:

```md
Siehe [Sammlung](topic:collection).
```

Der `topic:`-Präfix ist keine echte URL, sondern wird von `WebView2Html` (Behaviors) über `NavigationStarting` abgefangen, bevor Chromium versucht, sie aufzulösen: Ein Klick wechselt im selben Hilfefenster direkt zum angegebenen Thema, statt eine (fehlschlagende) Browser-Navigation auszulösen. Externe `http(s)`-Links werden dagegen normal im Standardbrowser geöffnet.

## Markdown-Syntax

Die wichtigsten unterstützten Elemente sind:

```md
Normaler Absatz mit **fettem**, *kursivem* und ***fett-kursivem*** Text.
Auch ~~durchgestrichener~~ Text und `Quelltext` werden von Markdig gerendert.

## Abschnitt {#stabile-abschnitts-id}

- Erster Punkt
- Zweiter Punkt

- [x] Erledigter Punkt
- [ ] Offener Punkt

1. Erster Schritt
2. Zweiter Schritt

| Spalte A | Spalte B |
|----------|----------|
| Wert 1   | Wert 2   |

Begriff
:   Eine Definition in einer Definitionsliste.

Ein Text mit Fußnote.[^hinweis]

[^hinweis]: Der zugehörige Fußnotentext.

Eine normale URL wird automatisch verlinkt: https://example.org

> Ein normales Zitat bleibt ein normales Zitat.

> [!TIP]
> Ein hervorgehobener Hinweis mit **Formatierung**.

> [!WARNING]
> Ein hervorgehobener Hinweis auf tatsächliches Risiko, z. B. Datenverlust oder einen irreführenden Scan-Stand.

![Beschreibung](images/beispiel.png)

!include ../Shared/weiterer-text.md
!include <../Shared/datei mit leerzeichen.md>
```

Markdig unterstützt die Alert-Typen `NOTE`, `TIP`, `IMPORTANT`, `WARNING` und `CAUTION`. Für Hinweise und Risiken sollen bevorzugt `TIP` beziehungsweise `WARNING` verwendet werden.

Überschriften erhalten durch Markdig automatisch stabile HTML-IDs. Mit `{#eigene-id}` kann eine ID explizit festgelegt werden. Darüber hinaus aktiviert die Pipeline unter anderem Aufgabenlisten, Definitionslisten, Fußnoten, automatische Links, Pipe- und Grid-Tabellen sowie zusätzliche Hervorhebungen.

## Anwendungsspezifische Erweiterungen

`!include` und die Themenüberschrift `# id | Titel` sind keine Markdig-Syntax, sondern werden vor dem Markdown-Rendering vom `HelpContentProvider` verarbeitet. Markdig selbst bietet keine Dateieinbindung oder Verwaltung anwendungsspezifischer Themen-IDs.

Include- und Bildpfade werden relativ zu der Markdown-Datei aufgelöst, in der sie stehen. Auch verschachtelte Includes sind möglich. Alle eingebundenen Dateien müssen innerhalb des `Help`-Ordners liegen. Lokale Bilder werden beim HTML-Rendering über Markdigs `LinkRewriter` als Data-URI eingebettet; fehlt ein Bild, zeigt der Browser dessen Alternativtext an.

## Ordnerstruktur

```text
Help/
    ContextHelp.de.md   -- deutsche !include-Liste
    ContextHelp.en.md   -- englische !include-Liste
    ContextHelp.pl.md   -- polnische !include-Liste
    Topics/
        Start.md
        Einstellungen.md
```

Für Inhalte, die von mehreren Themen gemeinsam genutzt werden (z. B. ein Glossar
oder eine Fehlerbehebungs-Tabelle), kann ein `Shared/`-Unterordner angelegt und per
`!include` in die betroffenen Themendateien eingebunden werden, statt Inhalte zu
duplizieren.

## Fehlertolerantes Laden

Jede `!include`-Zeile der Wurzeldatei bildet eine eigene Fehlergrenze. Schlägt eine Themendatei fehl (fehlende Datei, zirkuläres Include, Pfad außerhalb von `Help/`, ungültige Themenüberschrift, doppelte Themen-ID), werden **nur deren Themen** übersprungen - alle anderen Themendateien bleiben vollständig verfügbar. Der Fehler wird mit Datei- und Zeilenangabe protokolliert (`HelpContentProvider`, Anwendungsprotokoll).

Eine Themen-ID, für die zur Laufzeit kein passender Eintrag existiert (z. B. wegen eines fehlgeschlagenen Includes oder eines fehlenden Themas), zeigt einen generischen Hinweistext statt eines Fehlers. Der Hilfe-Button in der Anwendung wird für eine Seite ohne hinterlegtes Thema ausgeblendet (`IHelpContentProvider.HasTopic`).

## Lokal bauen und testen

Benötigt wird das .NET 10 SDK:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet pack src/PeterSpoenemann.HelpService.csproj --configuration Release --no-build --output artifacts
```

Die erzeugten `.nupkg`- und `.snupkg`-Dateien liegen danach in `artifacts`.

## Sample-Anwendung

Unter [`samples/PeterSpoenemann.HelpService.Sample`](samples/PeterSpoenemann.HelpService.Sample)
liegt eine kleine WPF-Anwendung mit zwei Registerkarten und kontextsensitiver Hilfe.
Die aktive Hilfeseite wird über die Schaltfläche am unteren Fensterrand oder mit `F1` geöffnet.
Über die Sprachauswahl kann die komplette Sample-Oberfläche einschließlich eines bereits
geöffneten Hilfefensters zur Laufzeit zwischen Deutsch, Englisch und Polnisch wechseln.

```powershell
dotnet run --project samples/PeterSpoenemann.HelpService.Sample/PeterSpoenemann.HelpService.Sample.csproj
```

## Lizenz

Dieses Projekt steht unter der [MIT-Lizenz](LICENSE). Copyright © 2026 Peter Spönemann.
