namespace QT.PizzaOrdering.UI.Shared;

/// <summary>
/// Wording shared by customer pages when the ordering system cannot be reached. Technical details
/// (host names, ports, socket errors) go to the log, not to the customer.
/// </summary>
public static class CustomerMessages
{
    public const string TryAgainSoon = "Please try again in a few minutes, or call the shop.";

    public const string MenuUnavailable = $"The menu could not be loaded. {TryAgainSoon}";
}
