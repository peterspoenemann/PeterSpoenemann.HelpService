# reports | Auswertung

## Auswertung erstellen

Die Registerkarte erzeugt im Sample eine einfache Vorschau. In einer echten Anwendung könnte hier ein Bericht, eine Tabelle oder ein Diagramm erscheinen.

### Ablauf

- [x] Zeitraum und Datenquelle bestimmen
- [x] Option **Details einschließen** wählen
- [ ] Vorschau prüfen
- [ ] Ergebnis exportieren

> [!WARNING]
> Eine neu erzeugte Vorschau ersetzt in diesem Beispiel eine bereits angezeigte Vorschau. Speichern Sie benötigte Ergebnisse vorher.

Ein möglicher Aufruf aus dem Anwendungscode sieht so aus:

```csharp
helpService.ShowHelp("reports", this);
```

Die aufgerufene Themen-ID muss mit der Überschrift `# reports | Auswertung` übereinstimmen.[^topic-id]

[^topic-id]: Themen-IDs werden ohne Beachtung der Groß-/Kleinschreibung gesucht.

Zurück zu den [Einstellungen](topic:settings). Allgemeine Informationen zu WPF finden Sie in der [Microsoft-Dokumentation](https://learn.microsoft.com/dotnet/desktop/wpf/).

!include ../Shared/Tastatur.md
