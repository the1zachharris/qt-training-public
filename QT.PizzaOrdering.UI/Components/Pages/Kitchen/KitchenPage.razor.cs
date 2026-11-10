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

    [Inject] private IPizzaOrderingHttpClient Client { get; set; } = default!;
    [Inject] private IOptions<KitchenConfig> KitchenOptions { get; set; } = default!;

    private KitchenOrders? Orders { get; set; }
    private bool Loading { get; set; } = true;
    private bool Refreshing { get; set; }
    private DateTimeOffset? LastUpdated { get; set; }

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

    private async Task MarkReadyAsync(OrderDetails order)
    {
        if (!_marking.Add(order.Id))
            return;

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
    }

    private bool IsMarking(Guid orderId) => _marking.Contains(orderId);

    public void Dispose()
    {
        _disposal.Cancel();
        _disposal.Dispose();
    }
}
