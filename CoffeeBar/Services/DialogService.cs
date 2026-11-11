namespace CoffeeBar.Services;

public enum DialogKind {
    Primary,
    Danger
}

public record DialogDetail(string Label, string Value);

public class DialogOptions {
    public string Title { get; init; } = "";
    public string? Message { get; init; }
    public List<DialogDetail> Details { get; init; } = new();
    // Shown below the details, e.g. a total.
    public DialogDetail? Summary { get; init; }
    public string ConfirmText { get; init; } = "OK";
    // No cancel button when null (simple message box).
    public string? CancelText { get; init; } = "Cancel";
    public DialogKind Kind { get; init; } = DialogKind.Primary;
}

// In-app replacement for the browser alert/confirm boxes.
// Scoped: every Blazor circuit (browser tab) has its own dialog, shown by the DialogHost in the MainLayout.
public class DialogService {

    private TaskCompletionSource<bool>? _pending;

    public DialogOptions? Current { get; private set; }

    public event Action? OnChange;

    // Returns true when the user confirmed, false when cancelled.
    public Task<bool> ConfirmAsync(DialogOptions options) {
        // Only one dialog at a time, an open one counts as cancelled.
        _pending?.TrySetResult(false);

        _pending = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Current = options;
        OnChange?.Invoke();
        return _pending.Task;
    }

    public Task AlertAsync(string title, string message) {
        return ConfirmAsync(new DialogOptions { Title = title, Message = message, CancelText = null });
    }

    public void Close(bool confirmed) {
        var pending = _pending;
        _pending = null;
        Current = null;
        OnChange?.Invoke();
        pending?.TrySetResult(confirmed);
    }
}
