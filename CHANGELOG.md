# Changelog

Alle wesentlichen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.
Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung folgt [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

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
