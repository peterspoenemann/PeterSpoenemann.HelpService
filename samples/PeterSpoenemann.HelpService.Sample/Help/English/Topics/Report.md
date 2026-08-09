# reports | Report

## Create a report

The sample creates a simple preview on this tab. A real application could display a report, table, or chart here.

### Workflow

- [x] Select the time period and data source
- [x] Choose **Include details**
- [ ] Review the preview
- [ ] Export the result

> [!WARNING]
> In this sample, creating a new preview replaces the currently displayed preview. Save any results you still need first.

The application can open this topic as follows:

```csharp
helpService.ShowHelp("reports", this);
```

The requested topic ID must match the heading `# reports | Report`.[^topic-id]

[^topic-id]: Topic IDs are matched without regard to upper- or lower-case spelling.

Return to [Settings](topic:settings). General WPF information is available in the [Microsoft documentation](https://learn.microsoft.com/dotnet/desktop/wpf/).

!include ../Shared/Keyboard.md
