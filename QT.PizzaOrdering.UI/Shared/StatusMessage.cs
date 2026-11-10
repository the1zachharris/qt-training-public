namespace QT.PizzaOrdering.UI.Shared;

public class StatusMessage
{
    public Severity Severity { get; set; }
    public string Message { get; set; } = string.Empty;

    public static StatusMessage Info(string message) =>
        new() { Severity = Severity.Info, Message = message };

    public static StatusMessage Warning(string message) =>
        new() { Severity = Severity.Warning, Message = message };

    public static StatusMessage Error(string message) =>
        new() { Severity = Severity.Error, Message = message };
}
