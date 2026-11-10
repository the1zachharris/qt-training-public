# Neighborhood Pizza Requirements Gaps Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the pass-1 `QT.PizzaOrdering` app in line with `requirements.md`: the shop's real menu and prices, pickup-only ordering with just a name, and a kitchen screen with New/Ready statuses that survive restarts.

**Architecture:** Same shape as pass 1 (Core / Infrastructure / API / UI, one handler per operation, `IValidator<T>`, repository/query split). The work is mostly subtraction (crust, sauce, cheese, delivery, phone, tax, fees, the quote endpoint) plus one new slice: kitchen orders. The schema moves from `EnsureCreated` to EF Core migrations so orders are never lost to a schema change.

**Tech Stack:** C# / .NET 10, ASP.NET Core minimal API, EF Core 10 + SQLite, Blazor Server + MudBlazor 9.11, xUnit v3 + NSubstitute.

**Spec:** `requirements.md` (repo root; copied from the `pass-2` branch). All work stays on branch `ZH-pass1`.

## Gap Analysis (pass 1 vs. requirements)

| Requirement | Pass 1 today | Gap |
|---|---|---|
| Menu: Small 10" $10.00, Large 14" $14.00 | Four sizes at $9.99 to $18.99 | Wrong sizes and prices |
| Toppings: Pepperoni, Mushrooms, Onions, Green peppers, $2.00 each regardless of size | 14 toppings, $1.25 to $2.00 depending on size | Wrong list and pricing rule |
| Size + toppings only (section 1) | Also crust (with upcharges), sauce, cheese (extra cheese is charged) | Options and charges that are not on the menu |
| AC-1.1 to AC-1.3, AC-2.1 to AC-2.3 | Builder dialog with live price; cart with quantity, edit, remove | Met once the menu is corrected |
| AC-3.1 name required, empty order blocked | Name **and phone** required | Phone must go: a name alone has to be enough |
| AC-3.2 confirmation: order number, name, items, total | Present | Met (number format changes, see Decisions) |
| AC-3.3 order reaches the kitchen | Order is saved, nothing shows it | **Missing** |
| AC-4.1 kitchen sees orders | No kitchen screen, no list endpoint | **Missing** |
| AC-4.2 New → Ready | Orders have no status | **Missing** |
| AC-4.3 orders and statuses survive restart | SQLite file, but created with `EnsureCreated` and a working-directory-relative path | Fragile: a schema change or a different launch folder loses orders |
| Section 5: pickup only, no delivery | Pickup/Delivery choice, address, delivery fee | Remove |
| Section 5: no taxes or fees | 8.5% tax, $3.99 delivery fee | Remove |
| Section 5: customers know the order was submitted | Confirmation page | Met |

## Decisions (mine, for your review)

1. **The requirements menu replaces the pass-1 menu.** Crust, sauce, and cheese choices are removed, not hidden: they are not on the shop's menu and two of them add charges the requirements rule out.
2. **Presets stay, as shortcuts.** You asked for presets in pass 1 and no requirement conflicts with them. They become named topping combinations priced by the same $2.00 rule: Cheese (no toppings), Pepperoni, Veggie (mushrooms, onions, green peppers), The Works (all four). If you would rather drop presets to match the requirements literally, delete Task 4 Step 3's cards and the `PresetPizza` type; nothing else depends on them.
3. **Phone and notes are removed.** The requirements ask only for a name; collecting less is simpler and keeps personal data out of the database.
4. **Order numbers become short and sequential (`#1001`, `#1002`, ...).** Staff call these out at the counter, and the kitchen list needs a stable order. Pass 1's `PZ-3FA9C1` is neither. The confirmation URL keeps the Guid so one customer cannot page through other customers' orders.
5. **Ready is one-way.** The requirements describe New → Ready only. Marking an already-Ready order Ready again succeeds without change (two kitchen screens can tap the same order).
6. **The kitchen screen has no login.** The requirements rule out customer accounts and say nothing about staff sign-in, so `/kitchen` is an open URL that shows customer names. That is acceptable for a first release only if the shop agrees; flagging it rather than deciding it.

## Global Constraints

- Prices, exactly: Small pizza (10") $10.00; Large pizza (14") $14.00; each topping $2.00 per pizza regardless of size.
- Toppings, exactly: Pepperoni, Mushrooms, Onions, Green peppers.
- Order total = sum of pizza line totals. No tax, no fees, anywhere.
- Pickup only. No delivery, online payment, accounts, coupons, or loyalty.
- Statuses are named exactly **New** and **Ready**.
- Build stays at 0 warnings (`TreatWarningsAsErrors`). US English spellings. "QuikTrip" has no "c".
- Money is formatted with `PizzaText.ToCurrency()` (en-US), never the server culture.
- xUnit v3: every awaited call that takes a token passes `TestContext.Current.CancellationToken`.
- Stay on `ZH-pass1`. Commit after each task.

## Review Focus

1. **A database file left over from pass 1.** Expected: the app starts and works. Pinned by Task 3 (new file name, so the old file is ignored, plus a startup test).
2. **A cart saved in the browser by the pass-1 build.** Expected: it is discarded, not shown with blank or wrong pizzas. Pinned by Task 4 (storage key change + test).
3. **Two customers ordering at the same moment.** Expected: two different order numbers, both orders saved. Pinned by Task 2 (concurrent placement test).
4. **The app launched from a different folder** (Visual Studio vs. `dotnet run` vs. the built exe). Expected: the same orders appear. Pinned by Task 3 (path anchored to the content root + test).
5. **A second kitchen screen marks an order Ready that is already Ready, or an order id that does not exist.** Expected: no error for the first, a clear "not found" for the second, and neither screen goes stale. Pinned by Task 2 (handler tests) and Task 5 (list refresh after every action).

---

### Task 1: Menu, pricing, and order model to spec (Core, Infrastructure seed, API)

After this task the API projects and `QT.PizzaOrdering.Test` build and pass. The UI will not compile until Task 4; verify with the test project, not the whole solution.

**Files:**
- Modify: `QT.PizzaOrdering.Core/Features/Menu/Models/MenuCatalog.cs`, `Core/Features/Orders/PizzaPricer.cs`, `Core/Features/Orders/OrderPricer.cs`, `Core/Features/Orders/Models/Order.cs`, `Core/Features/Orders/Models/OrderLine.cs`, `Core/Features/Orders/Requests/OrderRequests.cs`, `Core/Features/Orders/Responses/OrderResponses.cs`, `Core/Abstractions/IPizzaOrderingHttpClient.cs`
- Create: `Core/Features/Orders/Enums/OrderStatus.cs`
- Delete: `Core/Features/Menu/Enums/CheeseLevel.cs`, `Core/Features/Menu/Enums/ToppingCategory.cs`, `Core/Features/Orders/Enums/FulfillmentType.cs`, `Core/Features/Orders/Models/OrderPricingRules.cs`, `API/Configs/OrderPricingConfig.cs`, `API/Features/Orders/QuoteOrderHandler.cs`
- Modify: `QT.PizzaOrdering.Infrastructure/Seeding/MenuSeed.cs`, `Infrastructure/Configurations/Menu/MenuConfigurations.cs`, `Infrastructure/Configurations/Orders/OrderConfigurations.cs`, `Infrastructure/PizzaOrderingDbContext.cs`, `Infrastructure/DbContextRegistration.cs` (seed only sizes, toppings, presets)
- Modify: `QT.PizzaOrdering.API/Program.cs`, `API/DIConfiguration.cs`, `API/appsettings.json` (drop `OrderPricingConfig`), `API/Adapters/Data/MenuQueries.cs`, `API/Endpoints/OrderEndpoints.cs` (drop `/orders/quote`), `API/Features/Orders/PlaceOrderHandler.cs`, `API/Features/Orders/GetOrderHandler.cs`, `API/Features/Orders/OrderRequestRules.cs`
- Test: rewrite `QT.PizzaOrdering.Test/Core/PizzaPricerTests.cs`, `Core/OrderPricerTests.cs`, `Core/OrderTests.cs`, `Validators/PlaceOrderRequestValidatorTests.cs`, `Handlers/PlaceOrderHandlerTests.cs`, `Handlers/QuoteAndGetOrderHandlerTests.cs` (rename to `GetOrderHandlerTests.cs`, quote tests deleted), `Endpoints/PizzaOrderingEndpointsTests.cs`, `Shared/Pizzas.cs`

**Interfaces (Produces):**

```csharp
// Menu
record MenuCatalog(IReadOnlyList<PizzaSize> Sizes, IReadOnlyList<Topping> Toppings, IReadOnlyList<PresetPizza> PresetPizzas);
record PizzaSize(int Id, string Name, int Inches, decimal Price);
record Topping(int Id, string Name, decimal Price);
record PresetPizza(int Id, string Name, string Description, List<int> ToppingIds);

// Orders
enum OrderStatus { New = 0, Ready = 1 }   // [JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
record PizzaSelection(int? PresetPizzaId, int SizeId, IReadOnlyList<int> ToppingIds, int Quantity);
record PlaceOrderRequest(string CustomerName, IReadOnlyList<PizzaSelection> Pizzas);
record PricedPizza(string Name, string Size, IReadOnlyList<string> Toppings, int Quantity, decimal UnitPrice, decimal LineTotal);
record OrderDetails(Guid Id, int OrderNumber, string CustomerName, DateTimeOffset PlacedAt, OrderStatus Status,
                    DateTimeOffset? ReadyAt, IReadOnlyList<PricedPizza> Pizzas, decimal Total);   // replaces OrderConfirmation; static From(Order)

static Result<PricedPizza> PizzaPricer.Price(MenuCatalog menu, PizzaSelection selection, string path = "Pizza");
static Result<IReadOnlyList<PricedPizza>> OrderPricer.Price(MenuCatalog menu, IReadOnlyList<PizzaSelection> pizzas);   // replaces Quote

// Order aggregate
static Order Order.Place(TimeProvider timeProvider, PlaceOrderRequest request, IReadOnlyList<PricedPizza> pizzas);  // Status = New, Total = sum of LineTotal, OrderNumber = 0 until assigned
void Order.AssignOrderNumber(int orderNumber);   // throws ArgumentOutOfRangeException if orderNumber < 1; may be called again (the repository renumbers on a collision)
void Order.MarkReady(TimeProvider timeProvider); // New -> Ready and sets ReadyAt; no change if already Ready
```

`Order` loses `Phone`, `Fulfillment`, `DeliveryAddress`, `Notes`, `Subtotal`, `Tax`, `DeliveryFee`; `OrderNumber` becomes `int`; gains `Status`, `ReadyAt`. `OrderLine` loses `Crust`, `Sauce`, `Cheese`. `IPizzaOrderingHttpClient` loses `QuoteOrderAsync`; `PlaceOrderAsync` and `GetOrderAsync` return `ClientResult<OrderDetails>`.

Seed (`MenuSeed.Create()`), ids fixed:

| Sizes | Toppings | Presets (topping ids) |
|---|---|---|
| 1 Small, 10", 10.00 | 1 Pepperoni, 2 Mushrooms, 3 Onions, 4 Green peppers, all 2.00 | 1 Cheese `[]`, 2 Pepperoni `[1]`, 3 Veggie `[2,3,4]`, 4 The Works `[1,2,3,4]` |
| 2 Large, 14", 14.00 | | |

Pricing rule: `UnitPrice = size.Price + sum(selected topping.Price)`. `PricedPizza.Size` reads `Small (10")`. Name: no preset and no toppings → `Cheese Pizza`; no preset with toppings → `Build Your Own`; preset with the same topping set → preset name; otherwise `Custom {preset name}`. Remove `PizzaPricer.MaxToppings` (unreachable with four toppings); keep "each topping once", "topping must be on the menu", and `MaxQuantity = 20`.

- [ ] **Step 1: Rewrite the pricing tests first** in `PizzaPricerTests.cs` / `OrderPricerTests.cs` with the spec's numbers:

```csharp
// Price_SmallPlain_Is10            -> UnitPrice 10.00m, Name "Cheese Pizza"
// Price_LargePlain_Is14            -> UnitPrice 14.00m
// Price_EachToppingAdds2_OnEitherSize (Theory): (Small,1 topping)=12.00, (Large,1)=16.00, (Small,4)=18.00, (Large,4)=22.00
// Price_RemovingATopping_Drops2    -> price([1,2]) - price([1]) == 2.00m
// Price_Quantity3LargePepperoni    -> LineTotal 48.00m
// Price_PresetUnchanged / PresetCustomized -> "Veggie" / "Custom Veggie"
// Price_UnknownSizeOrTopping, Price_RepeatedTopping, Price_QuantityOutOfRange (0, -1, 21) -> Error.Validation with keyed failures
// OrderPricer.Price_TwoDifferentPizzas -> 2 lines; sum of LineTotal == 12.00 + 22.00
// OrderPricer.Price_InvalidPizza_ReportsByPosition -> key "Pizzas[1].SizeId"
```

- [ ] **Step 2: Run** `dotnet test QT.PizzaOrdering.Test` — expected: compile errors (types not reshaped yet).
- [ ] **Step 3: Reshape Core** to the Interfaces block above and delete the listed files.
- [ ] **Step 4: Rewrite `OrderTests.cs`:** `Place` sets `Status == OrderStatus.New`, `Total == 34.00m` for (Small+1 topping) + (Large+4 toppings), trims the name; `Place` with no pizzas throws `ArgumentException`; `MarkReady` sets `Ready` and `ReadyAt == now`; calling `MarkReady` twice keeps the first `ReadyAt`; `AssignOrderNumber(0)` throws.
- [ ] **Step 5: Update Infrastructure** (seed table above; drop Crust/Sauce sets and configurations; `Order.Status` stored as string, max length 20; unique index on `Order.OrderNumber`).
- [ ] **Step 6: Update the API.** `PlaceOrderRequestValidator` keeps only: name required, name ≤ 100 characters, cart not empty, ≤ 20 lines. `PlaceOrderHandler` prices with `OrderPricer.Price`, places, saves, returns `Ok<OrderDetails>`. Remove the quote route, handler, validator, DI registrations, and `OrderPricingConfig`.
- [ ] **Step 7: Update the remaining tests.** Validator tests for AC-3.1: empty name, whitespace name, null name, empty cart, null cart each return an error; a name plus one pizza is valid. Endpoint tests: `GET api/v1/menu` returns 2 sizes, 4 toppings, 4 presets; `POST api/v1/orders` with only `customerName` and one pizza returns 200 with `status == "New"` and `total == 12.00`; a body with `customerName: ""` returns 400.
- [ ] **Step 8: Run** `dotnet test QT.PizzaOrdering.Test` — expected: all pass, 0 warnings.
- [ ] **Step 9: Commit** `Menu, prices, and orders match requirements: two sizes, $2 toppings, pickup only, no tax or fees`.

### Task 2: Kitchen orders API (AC-3.3, AC-4.1, AC-4.2)

**Files:**
- Modify: `QT.PizzaOrdering.Core/Features/Orders/Interfaces/IOrderRepository.cs`, `Core/Features/Orders/Responses/OrderResponses.cs`
- Modify: `QT.PizzaOrdering.API/Adapters/Data/OrderRepository.cs`, `API/Endpoints/OrderEndpoints.cs`, `API/DIConfiguration.cs`
- Create: `QT.PizzaOrdering.API/Features/Orders/GetKitchenOrdersHandler.cs`, `API/Features/Orders/MarkOrderReadyHandler.cs`
- Test: `QT.PizzaOrdering.Test/Handlers/MarkOrderReadyHandlerTests.cs`, `Test/Endpoints/KitchenEndpointsTests.cs`

**Interfaces:**
- Consumes: `Order`, `OrderDetails`, `OrderStatus`, `Order.MarkReady`, `Order.AssignOrderNumber` from Task 1.
- Produces:

```csharp
record KitchenOrders(IReadOnlyList<OrderDetails> New, IReadOnlyList<OrderDetails> Ready);

interface IOrderRepository
{
    Task AddAsync(Order order, CancellationToken ct = default);                       // assigns the next order number, then saves
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);          // tracked, with Lines
    Task<IReadOnlyList<Order>> GetNewAsync(CancellationToken ct = default);           // every New order, oldest first (OrderNumber ascending)
    Task<IReadOnlyList<Order>> GetRecentReadyAsync(int take, CancellationToken ct = default); // newest first (OrderNumber descending)
    Task SaveChangesAsync(CancellationToken ct = default);
}

// GET   /api/v1/kitchen/orders               -> Ok<KitchenOrders>   (Ready capped at 50)
// PATCH /api/v1/kitchen/orders/{orderId:guid}/ready -> Ok<OrderDetails> | ProblemHttpResult 404 (title "OrderNotFound")
```

Order numbers: the first order is 1001. `AddAsync` reads `MAX(OrderNumber)` (1000 when there are no orders), assigns the next number, and saves. If the unique index rejects the save because another order took that number, it re-reads the max, assigns the next number to the same order, and saves again, up to 5 attempts; after that the exception propagates. Sort by `OrderNumber`, never by `PlacedAt`/`ReadyAt`: EF Core's SQLite provider cannot order by `DateTimeOffset`.

- [ ] **Step 1: Write `MarkOrderReadyHandlerTests`** (NSubstitute repository, `FakeTimeProvider`): New order → `Ok<OrderDetails>` with `Status == Ready`, `SaveChangesAsync` received once; already-Ready order → `Ok`, `ReadyAt` unchanged; unknown id → `ProblemHttpResult` 404, `SaveChangesAsync` not received.
- [ ] **Step 2: Write `KitchenEndpointsTests`** (`PizzaOrderingWebAppFactory`, a fresh factory per test so order numbers start clean):

```csharp
// PlacedOrder_AppearsUnderNew_WithNameNumberPizzasAndTotal  (AC-3.3, AC-4.1)
//   place "Pat Sample", Large + Pepperoni + Mushrooms x2 -> GET kitchen/orders -> New has 1 item:
//   OrderNumber 1001, CustomerName "Pat Sample", Pizzas[0].Toppings ["Pepperoni","Mushrooms"], Quantity 2, Total 36.00
// MarkReady_MovesTheOrderFromNewToReady                      (AC-4.2)
// NewOrders_AreOldestFirst_ReadyOrders_AreNewestFirst        place 3, ready #1001 and #1002 -> New [1003], Ready [1002, 1001]
// MarkReady_UnknownOrder_Returns404
// OrdersPlacedAtTheSameTime_GetDifferentNumbers              20 x Task.WhenAll(POST) -> all 200, 20 distinct OrderNumbers  (Review Focus 3)
```

- [ ] **Step 3: Run** `dotnet test QT.PizzaOrdering.Test --filter "FullyQualifiedName~Kitchen|FullyQualifiedName~MarkOrderReady"` — expected: fail to compile.
- [ ] **Step 4: Implement** the repository methods, both handlers (same shape as `GetOrderHandler`, with `[LoggerMessage]` logging), the two routes under tag `Kitchen`, and DI registrations.
- [ ] **Step 5: Run** `dotnet test QT.PizzaOrdering.Test` — expected: all pass.
- [ ] **Step 6: Commit** `Kitchen API: list New and Ready orders, mark an order Ready`.

### Task 3: Orders survive restarts and upgrades (AC-4.3)

**Files:**
- Modify: `Directory.Packages.props` (add `Microsoft.EntityFrameworkCore.Design` 10.0.13), `QT.PizzaOrdering.Infrastructure/QT.PizzaOrdering.Infrastructure.csproj`, `QT.PizzaOrdering.API/QT.PizzaOrdering.API.csproj` (Design reference with `PrivateAssets=all`, as ShiftAudit does)
- Modify: `QT.PizzaOrdering.Infrastructure/DbContextRegistration.cs`, `QT.PizzaOrdering.API/DIConfiguration.cs`, `QT.PizzaOrdering.API/appsettings.json`
- Create: `QT.PizzaOrdering.Infrastructure/Migrations/*` (generated)
- Test: `QT.PizzaOrdering.Test/Infrastructure/OrderPersistenceTests.cs`, `Test/Infrastructure/DatabasePathTests.cs`; modify `Test/Shared/PizzaOrderingWebAppFactory.cs`

**Interfaces (Produces):**

```csharp
// Infrastructure/DbContextRegistration.cs
static string ResolveSqliteConnectionString(string connectionString, string contentRootPath);
// A relative "Data Source" is made absolute against contentRootPath; absolute paths and ":memory:" are returned unchanged.
static Task InitializePizzaOrderingDatabaseAsync(this IServiceProvider services, CancellationToken ct = default);
// Now: Database.MigrateAsync, then seed the menu if PizzaSize is empty.
```

Connection string becomes `Data Source=neighborhood-pizza.db`. The new file name is deliberate: a `pizza-ordering.db` left by pass 1 was built without migrations and cannot be migrated, so it is left alone and ignored.

`PizzaOrderingWebAppFactory` gains an optional constructor argument `string? databasePath`: null keeps the in-memory database; a path uses that SQLite file, so two factories can share one file.

- [ ] **Step 1: Write `DatabasePathTests`:** relative `Data Source=neighborhood-pizza.db` with content root `C:\apps\pizza` resolves to `C:\apps\pizza\neighborhood-pizza.db`; an absolute path is unchanged; `Data Source=:memory:` is unchanged.
- [ ] **Step 2: Write `OrderPersistenceTests`** against a temp-folder database file (deleted in `Dispose`):

```csharp
// OrdersAndStatuses_SurviveARestart                                  (AC-4.3)
//   factory A: place 2 orders, mark #1001 Ready; dispose A.
//   factory B on the same file: GET kitchen/orders -> New [1002], Ready [1001], names/pizzas/totals intact;
//   place a third order -> OrderNumber 1003 (numbering continues, no reuse).
// Startup_IsRepeatable                                               start, dispose, start again on the same file -> menu still 2 sizes / 4 toppings / 4 presets (no duplicate seed)
```

- [ ] **Step 3: Run** the two new test classes — expected: fail.
- [ ] **Step 4: Add the migration.** From the repo root:

```bash
dotnet ef migrations add Init --project QT.PizzaOrdering.Infrastructure --startup-project QT.PizzaOrdering.API
```

The installed global `dotnet-ef` is 9.0.8; it warns about being older than the 10.0 runtime but normally works. If it refuses, run `dotnet new tool-manifest` and `dotnet tool install dotnet-ef --version "10.*"`, then repeat with `dotnet ef`.
- [ ] **Step 5: Implement** `ResolveSqliteConnectionString` (use `SqliteConnectionStringBuilder`), call it from `AddDbContexts` with `builder.Environment.ContentRootPath`, switch the initializer to `MigrateAsync`, rename the database in `appsettings.json`.
- [ ] **Step 6: Run** `dotnet test QT.PizzaOrdering.Test` — expected: all pass. Confirm `git status` shows no `.db` files staged.
- [ ] **Step 7: Commit** `Persist orders with EF migrations and a fixed database location`.

### Task 4: Customer UI to spec (sections 1 to 3, section 5)

After this task the whole solution builds again.

**Files:**
- Modify: `QT.PizzaOrdering.UI/Components/Pages/PizzaMenu/MenuPage.razor(.cs)`, `PresetPizzaCard.razor`, `Dialogs/PizzaBuilderDialog.razor(.cs)`, `Components/Pages/ShoppingCart/CartPage.razor(.cs)`, `Components/Pages/Checkout/CheckoutPage.razor(.cs)`, `Components/Pages/Orders/OrderConfirmationPage.razor`, `Components/Shared/PizzaSummary.razor`, `Components/Layout/MainLayout.razor`, `Components/_Imports.razor`
- Modify: `QT.PizzaOrdering.UI/Shared/PizzaText.cs`, `Shared/PizzaSelections.cs`, `Shared/Clients/PizzaOrderingHttpClient.cs`, `Shared/ShoppingCart/ProtectedSessionCartStorage.cs`
- Delete: `QT.PizzaOrdering.UI/Components/Shared/OrderTotals.razor`
- Test: `QT.PizzaOrdering.UI.Tests/UI/CartStateTests.cs`, `UI/PizzaOrderingHttpClientTests.cs`; create `UI/PizzaSelectionsTests.cs`

**Interfaces:**
- Consumes: Task 1 types; `IPizzaOrderingHttpClient` without `QuoteOrderAsync`.
- Produces: `PizzaSelections.BuildYourOwn(MenuCatalog)` → first size, no toppings, quantity 1; `PizzaSelections.FromPreset(PresetPizza, int sizeId)`; `PizzaText.Toppings(PricedPizza)` → `"Plain cheese"` when there are none, else the names joined with `", "`; `ProtectedSessionCartStorage.Key == "Cart.v2"`.

- [ ] **Step 1: Update the UI tests.** `CartStateTests` use the new `PizzaSelection`/`PricedPizza` shapes with spec prices (Large + Pepperoni = 16.00; quantity 3 → 48.00). `PizzaOrderingHttpClientTests`: menu JSON in the new shape; quote tests deleted. New `PizzaSelectionsTests`: `BuildYourOwn` picks the first size and no toppings; `FromPreset` copies the preset's toppings. Add `ProtectedSessionCartStorage_UsesAVersionedKey` asserting the key is `"Cart.v2"` (Review Focus 2: a pass-1 cart under `"Cart"` is never read).
- [ ] **Step 2: Builder dialog (AC-1.1 to AC-1.3).** Size as two choices (`MudRadioGroup`), each labeled with its price (`Small (10") - $10.00`). Toppings as one chip set of the four, with the heading `Toppings · $2.00 each`. Quantity field. The price in the dialog footer recalculates on every change (it already binds to `PizzaPricer.Price`). Remove crust, sauce, cheese, and the topping-limit text.
- [ ] **Step 3: Menu page.** Banner text: `Choose a size, then add any toppings you like. Toppings are $2.00 each.` Preset cards show name, description, toppings, a size choice, and the price for the chosen size. Remove the Vegetarian chip (it depended on `ToppingCategory`).
- [ ] **Step 4: Cart (AC-2.1 to AC-2.3).** Each row: name, size, toppings, quantity stepper, each, line total, edit, remove. Footer shows `Total` (not "Subtotal") and drops the "tax and delivery" caption.
- [ ] **Step 5: Checkout (AC-3.1, section 5).** One field: `Name for the order` (required, max 100). Order summary lists each pizza and `Total`, taken from `Cart.Subtotal`. Notice: `Pickup only. Pay at the counter when you arrive.` Place Order is disabled while the name is blank, the cart is empty, or a submit is in flight. Remove phone, pickup/delivery, address, notes, and the quote call. A validation response from the API shows under the name field as it does today.
- [ ] **Step 6: Confirmation (AC-3.2).** Shows `Order #1001`, the customer's name, each pizza with quantity and line total, and `Total`. Message: `Thanks, {name}! Your order is in.` and `Pay at the counter when you pick up.`
- [ ] **Step 7: Branding.** `AppName` and page titles read `Neighborhood Pizza`.
- [ ] **Step 8: Run** `dotnet build QT.PizzaOrdering.slnx` then `dotnet test QT.PizzaOrdering.slnx` — expected: 0 warnings, all tests pass.
- [ ] **Step 9: Commit** `Customer UI matches requirements: size and toppings, name-only pickup checkout`.

### Task 5: Kitchen screen (AC-4.1, AC-4.2)

**Files:**
- Modify: `QT.PizzaOrdering.Core/Abstractions/IPizzaOrderingHttpClient.cs`, `QT.PizzaOrdering.UI/Shared/Clients/PizzaOrderingHttpClient.cs`, `UI/Components/Layout/MainLayout.razor` (nav link `Kitchen`, icon `Icons.Material.Filled.SoupKitchen`), `UI/Program.cs`, `UI/appsettings.json`
- Create: `QT.PizzaOrdering.UI/Components/Pages/Kitchen/KitchenPage.razor`, `KitchenPage.razor.cs`, `KitchenOrderCard.razor`, `QT.PizzaOrdering.UI/Shared/Configs/KitchenConfig.cs`
- Test: `QT.PizzaOrdering.UI.Tests/UI/PizzaOrderingHttpClientTests.cs`

**Interfaces:**
- Consumes: `KitchenOrders`, `OrderDetails` and the two kitchen routes from Task 2.
- Produces:

```csharp
Task<ClientResult<KitchenOrders>> GetKitchenOrdersAsync(CancellationToken ct = default);          // GET api/v1/kitchen/orders
Task<ClientResult<OrderDetails>> MarkOrderReadyAsync(Guid orderId, CancellationToken ct = default); // PATCH api/v1/kitchen/orders/{id}/ready

class KitchenConfig { const string SectionName = "KitchenConfig"; [Range(2, 300)] int RefreshSeconds { get; set; } = 10; }
```

Page `/kitchen`: two columns, **New** (count in the heading, oldest first) and **Ready** (newest first). Each `KitchenOrderCard` shows `#1001`, the customer's name, the time placed, every pizza as `2 × Large (14") · Pepperoni, Mushrooms`, and the total. New cards have a `Mark Ready` button (success color); Ready cards show a `Ready` chip and the time it was marked. Empty states: `No orders waiting.` / `Nothing ready yet.`

The list reloads on a `PeriodicTimer` every `RefreshSeconds`, immediately after a `Mark Ready` (success or failure, so a card another screen already moved does not linger), and from a `Refresh` button. A failed reload keeps the last list on screen and shows a warning with the time of the last successful update; it never blanks the board. The timer is disposed with the page.

- [ ] **Step 1: Add client tests:** `GetKitchenOrdersAsync` requests `/api/v1/kitchen/orders` and reads `new`/`ready` arrays with `status` as `"New"`/`"Ready"`; `MarkOrderReadyAsync` sends `PATCH` to `/api/v1/kitchen/orders/{id}/ready`; a 404 problem response returns a failure with `Status == NotFound`.
- [ ] **Step 2: Run** `dotnet test QT.PizzaOrdering.UI.Tests` — expected: fail to compile.
- [ ] **Step 3: Implement** the two client methods, `KitchenConfig` (bound with `ValidateDataAnnotations().ValidateOnStart()`), the page, the card, and the nav link.
- [ ] **Step 4: Run** `dotnet test QT.PizzaOrdering.slnx` — expected: all pass, 0 warnings.
- [ ] **Step 5: Commit** `Kitchen screen: New and Ready orders with Mark Ready`.

### Task 6: Walk every acceptance criterion in the running app

**Files:** none changed unless a check fails.

- [ ] **Step 1: Start both apps** (`dotnet run --project QT.PizzaOrdering.API --launch-profile http`, same for `QT.PizzaOrdering.UI`) against a fresh `neighborhood-pizza.db`.
- [ ] **Step 2: Check each line in a browser** and record pass/fail:

| AC | Check | Expected |
|---|---|---|
| 1.1 | Build Small with no toppings; build Large with all four | Both can be added |
| 1.2, 1.3 | In the builder, Large, add Pepperoni, add Mushrooms, remove Pepperoni | $14.00 → $16.00 → $18.00 → $16.00, before adding to the cart |
| 2.1, 2.2 | Add Small plain and Large with two toppings | Cart shows both with details, $10.00 and $18.00, Total $28.00 |
| 2.3 | Set the Small to quantity 2, then remove the Large | Total $38.00, then $20.00 |
| 3.1 | Open checkout with an empty cart; with a cart but a blank name | Cannot place either |
| 3.2 | Place the order as "Pat Sample" | Confirmation shows `#1001`, Pat Sample, 2 × Small, Total $20.00 |
| 3.3, 4.1 | Open `/kitchen` in a second tab | Order `#1001` is under New with name, pizzas, and total, without a manual refresh within 10 seconds |
| 4.2 | Click Mark Ready | The order moves to Ready |
| 4.3 | Stop both apps, start them again, open `/kitchen` | `#1001` is still Ready; a new order gets `#1002` and is New |
| 5 | Look for tax, fees, delivery, phone, payment fields | None anywhere |

- [ ] **Step 3: Fix anything that failed** in the task that owns it, re-run `dotnet test QT.PizzaOrdering.slnx`, and commit.
- [ ] **Step 4: Stop the apps and delete the local test database** so no sample orders are left behind.
