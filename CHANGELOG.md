# Changelog

Alle wesentlichen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.
Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung folgt [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

## [0.6.0] - 2026-08-10

### Added

- Der Core bettet wahlweise ein helles, dunkles oder systemabhängiges Theme direkt in jedes
  erzeugte HTML-Dokument ein.
- WPF-Anwendungen können über `HelpServiceOptions.AdditionalStyleSheetPath` eine zusätzliche
  CSS-Datei einbetten und damit das gewählte Theme anpassen oder überschreiben.
- Über `IHelpThemeService` lässt sich das HTML-Theme zur Laufzeit wechseln; das Sample demonstriert
  die Auswahl zwischen System-, hellem und dunklem Theme.
- Das gewählte Theme wird auch auf Navigation, Inhaltsverzeichnis und Bedienelemente des
  WPF-Hilfedialogs angewendet.
- Das Sample kann die vollständige HTML-Hilfeseite erzeugen und im Standardbrowser öffnen.
- Der Core kann über `IHelpPageBuilder` eine eigenständige HTML-Hilfeseite mit gruppiertem
  Inhaltsverzeichnis sowie internen Topic- und Abschnittsverweisen erzeugen.

## [0.5.0] - 2026-08-10

### Added

- Die VS-Code-Vorschau zeigt für gerenderte Markdown-Blöcke Quelldatei und Originalzeile
  als Tooltip und öffnet die zugehörige Datei über ein natives Kontextmenü direkt an dieser Stelle.
- Der Core stellt über `IHelpSourceMapProvider` die Herkunft aufgelöster Markdown-Zeilen bereit,
  einschließlich genauer Zuordnung für verschachtelte `!include`-Dateien.

## [0.4.0] - 2026-08-10

### Added

- VS-Code-Extension zur originalgetreuen Vorschau der Hilfedateien mit `!include`,
  Themenüberschriften, relativen Bildern und Markdig Advanced Extensions.
- Automatische Erkennung der zugehörigen `ContextHelp*.md`-Wurzeldatei sowie Navigation
  zwischen den enthaltenen Hilfethemen.
- Reproduzierbarer VSIX-Build über `vscode-extension/Build.cmd` und eine festgeschriebene
  npm-Lockdatei.

### Changed

- Der tagbasierte Veröffentlichungsworkflow erzeugt neben den NuGet-Paketen ein GitHub Release
  und stellt die VS-Code-Extension dort unter einem stabilen Downloadnamen bereit.

## [0.3.0] - 2026-08-09

### Added

- Plattformneutrales Assembly und NuGet-Paket `PeterSpoenemann.HelpService.Core` zum Laden von
  Markdown-Hilfethemen, Bereitstellen der Inhaltsverzeichnisdaten und Erzeugen vollständiger HTML-Dokumente.
- Direkte `HelpContentProvider`-Konstruktoren ohne verpflichtende Logging-Konfiguration für die Nutzung
  beispielsweise in Webanwendungen.

### Changed

- Das WPF-Paket verwendet den ausgelagerten Core intern und behält seine bisherige öffentliche API über
  unveränderte Namespaces und Typweiterleitungen bei.
- Die Quellcodes von Core und WPF liegen wartungsfreundlich in den unabhängigen Projektverzeichnissen
  `src/HelpService.Core` und `src/HelpService` ohne verlinkte Dateien oder nachträgliche Compile-Ausschlüsse.

## [0.2.0] - 2026-08-09

### Fixed

- Die Schließen-Schaltfläche des Hilfefensters bleibt aktiv und schließt das nichtmodale Fenster zuverlässig.

### Added

- Deutsche, englische und polnische Hilfethemen, Oberflächentexte und Meldungen.
- Mehrere sprachgebundene Hilfe-Wurzeldateien über `HelpServiceOptions.RootHelpFiles`.
- Laufzeit-Sprachwechsel über `IHelpLanguageService` mit `de` als Standard.
- Dreisprachige Sample-Oberfläche mit Umschaltung zur Laufzeit.

### Compatibility

- `HelpServiceOptions.RootHelpFile`, `IContextHelpService.ShowHelp(...)` und
  `IHelpDocumentBuilder.BuildHtml(string)` bleiben unverändert nutzbar.

## [0.1.0] - 2026-08-09

### Added

- Erste eigenständig paketierbare Version der WPF-Hilfebibliothek.
- Markdown-Hilfethemen mit Includes, Suche, Navigation und WebView2-Anzeige.
- NuGet-Paketmetadaten, Symbolpaket, Unit-Tests und GitHub-Actions-Workflows.
