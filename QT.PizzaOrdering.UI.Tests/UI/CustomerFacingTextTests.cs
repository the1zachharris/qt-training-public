using System.Net;

using QT.PizzaOrdering.Core.Shared;
using QT.PizzaOrdering.UI.Shared;

namespace QT.PizzaOrdering.UI.Tests.UI;

public class CustomerFacingTextTests
{
    [Fact]
    public void AppNavigation_OffersTheMenuTheOrderAndTheKitchen()
    {
        // The kitchen is listed so it is easy to reach while testing; see AppNavigation.
        Assert.Equal(["/", "/cart", "/kitchen"], AppNavigation.Items.Select(item => item.Href));
    }

    [Fact]
    public void OrderFailed_WhenTheApiRejectedTheOrder_SaysItWasNotPlaced()
    {
        ClientError rejected = new("A name is required to place an order.", HttpStatusCode.BadRequest);

        string message = CheckoutMessages.OrderFailed(rejected);

        Assert.Equal("Your order was not placed. A name is required to place an order.", message);
    }

    [Fact]
    public void OrderFailed_WhenNoAnswerCameBack_DoesNotClaimTheOrderWasNotPlaced()
    {
        // A dropped connection or timeout: the order may or may not have reached the kitchen.
        // Saying "not placed" here invites a second, duplicate order.
        ClientError noAnswer = new("The operation was canceled.");

        string message = CheckoutMessages.OrderFailed(noAnswer);

        Assert.DoesNotContain("was not placed", message);
        Assert.Contains("could not confirm", message);
        Assert.Contains("call", message);
    }

    [Fact]
    public void OrderFailed_WhenTheServerFailed_DoesNotClaimTheOrderWasNotPlaced()
    {
        ClientError serverError = new("Something went wrong on our end. Please try again.", HttpStatusCode.InternalServerError);

        Assert.Contains("could not confirm", CheckoutMessages.OrderFailed(serverError));
    }
}
