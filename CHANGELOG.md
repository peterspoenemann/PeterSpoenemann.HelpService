# Changelog

Alle wesentlichen Änderungen an diesem Projekt werden in dieser Datei dokumentiert.
Das Format orientiert sich an [Keep a Changelog](https://keepachangelog.com/de/1.1.0/),
die Versionierung folgt [Semantic Versioning](https://semver.org/lang/de/).

## [Unreleased]

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
