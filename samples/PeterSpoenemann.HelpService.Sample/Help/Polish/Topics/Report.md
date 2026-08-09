# reports | Raport

## Tworzenie raportu

Na tej karcie przykładowa aplikacja tworzy prosty podgląd. Rzeczywista aplikacja mogłaby w tym miejscu wyświetlać raport, tabelę lub wykres.

### Przebieg pracy

- [x] Wybierz okres i źródło danych
- [x] Zaznacz opcję **Uwzględnij szczegóły**
- [ ] Sprawdź podgląd
- [ ] Wyeksportuj wynik

> [!WARNING]
> W tym przykładzie utworzenie nowego podglądu zastępuje aktualnie wyświetlany podgląd. Najpierw zapisz wyniki, których nadal potrzebujesz.

Aplikacja może otworzyć ten temat w następujący sposób:

```csharp
helpService.ShowHelp("reports", this);
```

Żądany identyfikator tematu musi odpowiadać nagłówkowi `# reports | Raport`.[^topic-id]

[^topic-id]: Wielkość liter w identyfikatorach tematów nie ma znaczenia.

Wróć do [ustawień](topic:settings). Ogólne informacje o WPF są dostępne w [dokumentacji firmy Microsoft](https://learn.microsoft.com/dotnet/desktop/wpf/).

!include ../Shared/Keyboard.md
