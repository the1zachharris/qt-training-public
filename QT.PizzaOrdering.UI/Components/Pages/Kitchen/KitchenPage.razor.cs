using System.Net;

using Microsoft.Extensions.Options;

using QT.PizzaOrdering.Core.Features.Orders.Responses;
using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Shared;
using QT.PizzaOrdering.UI.Shared.Configs;

namespace QT.PizzaOrdering.UI.Components.Pages.Kitchen;

/// <summary>
/// The kitchen's board: orders to prepare on the left, orders waiting for pickup on the right.
/// It reloads itself on a timer so new orders show up without anyone touching the screen.
/// </summary>
public partial class KitchenPage : IDisposable
{
    private readonly CancellationTokenSource _disposal = new();
    private readonly HashSet<Guid> _marking = [];

    // Cards in the New column and the column's heading, so focus can follow Mark Ready (WCAG 2.4.3).
    private readonly Dictionary<Guid, KitchenOrderCard> _newCards = [];
    private ElementReference _newHeading;

    /// <summary>
    /// The New orders to try, in order, once the board has re-rendered after a Mark Ready. Only
    /// Mark Ready sets this; the timed refresh never moves focus.
    /// </summary>
    private List<Guid>? _focusAfterMarkReady;

    [Inject] private IPizzaOrderingHttpClient Client { get; set; } = default!;
    [Inject] private IOptions<KitchenConfig> KitchenOptions { get; set; } = default!;

    private KitchenOrders? Orders { get; set; }
    private bool Loading { get; set; } = true;
    private bool Refreshing { get; set; }
    private DateTimeOffset? LastUpdated { get; set; }

    /// <summary>
    /// A short note for screen readers when a refresh brings in orders that were not on the board,
    /// e.g. "New order #1005 arrived." It only changes when that happens, so it is not read out on
    /// every refresh (WCAG 4.1.3).
    /// </summary>
    private string? Arrivals { get; set; }

    /// <summary>Shown while the list on screen may be out of date.</summary>
    private StatusMessage? RefreshProblem { get; set; }

    /// <summary>Shown when the last Mark Ready did not go through.</summary>
    private StatusMessage? ActionProblem { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
        Loading = false;
        _ = RefreshOnTimerAsync();
    }

    private async Task RefreshOnTimerAsync()
    {
        using PeriodicTimer timer = new(TimeSpan.FromSeconds(KitchenOptions.Value.RefreshSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(_disposal.Token))
            {
                await RefreshAsync();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException)
        {
            // The page was closed; stop refreshing.
        }
    }

    /// <summary>
    /// Reloads both lists. When the reload fails, the orders already on screen stay there with a
    /// warning: a board that goes blank mid-shift is worse than one that is a few seconds old.
    /// </summary>
    private async Task RefreshAsync()
    {
        Refreshing = true;
        ClientResult<KitchenOrders> result = await Client.GetKitchenOrdersAsync(_disposal.Token);
        Refreshing = false;

        if (_disposal.IsCancellationRequested)
            return;

        if (result.IsSuccess)
        {
            AnnounceArrivals(Orders, result.Data!);
            Orders = result.Data;
            LastUpdated = DateTimeOffset.Now;
            RefreshProblem = null;
        }
        else if (Orders is null)
        {
            RefreshProblem = StatusMessage.Error($"Orders could not be loaded. Trying again every {KitchenOptions.Value.RefreshSeconds} seconds. {result.Error?.Message}");
        }
        else
        {
            RefreshProblem = StatusMessage.Warning($"Orders could not be refreshed, so this list may be out of date (last updated {LastUpdated:h:mm:ss tt}). Trying again every {KitchenOptions.Value.RefreshSeconds} seconds. {result.Error?.Message}");
        }
    }

    private void AnnounceArrivals(KitchenOrders? before, KitchenOrders after)
    {
        // The first load is the board itself, not news.
        if (before is null)
            return;

        HashSet<Guid> known = [.. before.New.Select(o => o.Id), .. before.Ready.Select(o => o.Id)];
        List<string> arrived = after.New.Where(o => !known.Contains(o.Id)).Select(o => PizzaText.OrderNumber(o.OrderNumber)).ToList();
        if (arrived.Count == 0)
            return;

        Arrivals = arrived.Count == 1
            ? $"New order {arrived[0]} arrived."
            : $"{arrived.Count} new orders arrived: {string.Join(", ", arrived)}.";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusAfterMarkReady is not { } candidates || Orders is null)
            return;

        _focusAfterMarkReady = null;
        foreach (Guid id in _newCards.Keys.Except(Orders.New.Select(o => o.Id)).ToList())
            _newCards.Remove(id);

        foreach (Guid id in candidates)
        {
            if (_newCards.TryGetValue(id, out KitchenOrderCard? card) && await card.FocusMarkReadyAsync())
                return;
        }

        await _newHeading.FocusAsync();
    }

    private async Task MarkReadyAsync(OrderDetails order)
    {
        if (!_marking.Add(order.Id))
            return;

        // Where focus should go afterwards: this order again if it is still New (the request
        // failed), else the next New order, else the one before it.
        List<Guid> before = Orders?.New.Select(o => o.Id).ToList() ?? [];
        int index = before.IndexOf(order.Id);
        List<Guid> candidates = [order.Id, .. before.Skip(index + 1), .. Enumerable.Reverse(before.Take(Math.Max(index, 0)))];

        ActionProblem = null;
        ClientResult<OrderDetails> result = await Client.MarkOrderReadyAsync(order.Id, _disposal.Token);
        if (result.IsFailure)
        {
            string orderNumber = PizzaText.OrderNumber(order.OrderNumber);
            ActionProblem = result.Error?.Status == HttpStatusCode.NotFound
                ? StatusMessage.Warning($"Order {orderNumber} no longer exists. The list has been refreshed.")
                : StatusMessage.Error($"Order {orderNumber} was not marked Ready. Try again. {result.Error?.Message}");
        }

        // Reload either way: on success the order moves to Ready, and on failure another screen
        // may already have moved it.
        await RefreshAsync();
        _marking.Remove(order.Id);

        if (Orders is not null)
        {
            // Orders that arrived since are still better than the heading.
            candidates.AddRange(Orders.New.Select(o => o.Id).Except(candidates));
            _focusAfterMarkReady = candidates;
            StateHasChanged();
        }
    }

    private bool IsMarking(Guid orderId) => _marking.Contains(orderId);

    public void Dispose()
    {
        _disposal.Cancel();
        _disposal.Dispose();
    }
}
