using System.Net.Http.Headers;

using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using MudBlazor.Services;

using QT.PizzaOrdering.UI.Components;
using QT.PizzaOrdering.UI.Shared;
using QT.PizzaOrdering.UI.Shared.Configs;
using QT.PizzaOrdering.UI.Shared.ShoppingCart;

using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

using ILogger = Serilog.ILogger;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console(
        theme: AnsiConsoleTheme.Code,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();
ILogger log = Log.ForContext<Program>();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
    log.Information("********************Start*******************");
    log.Information("Initializing {AppName} in environment {Environment}", AppDomain.CurrentDomain.FriendlyName, builder.Environment.EnvironmentName);
    builder.Host.UseSerilog((hostingContext, loggingConfig) => loggingConfig.ReadFrom.Configuration(hostingContext.Configuration), writeToProviders: true);
    builder.Services.AddRazorComponents().AddInteractiveServerComponents();
    builder.Services.AddMudServices(configuration =>
    {
        configuration.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
        configuration.SnackbarConfiguration.ShowCloseIcon = true;
        configuration.SnackbarConfiguration.NewestOnTop = true;
        configuration.SnackbarConfiguration.PreventDuplicates = false;
        configuration.SnackbarConfiguration.VisibleStateDuration = 4000;
        configuration.SnackbarConfiguration.HideTransitionDuration = 500;
        configuration.SnackbarConfiguration.ShowTransitionDuration = 500;
        configuration.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
    });
    builder.Services.AddScoped<ProtectedSessionStorage>();
    builder.Services.AddScoped<ICartStorage, ProtectedSessionCartStorage>();
    builder.Services.AddScoped<CartState>();
    builder.Services.AddOptions<OrderStatusConfig>().Bind(builder.Configuration.GetSection(OrderStatusConfig.SectionName)).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddOptions<KitchenConfig>().Bind(builder.Configuration.GetSection(KitchenConfig.SectionName)).ValidateDataAnnotations().ValidateOnStart();
    builder.Services.AddHttpClient<IPizzaOrderingHttpClient, PizzaOrderingHttpClient>(client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["BaseAddresses:PizzaOrderingApi"]
            ?? throw new InvalidOperationException("BaseAddresses:PizzaOrderingApi not configured."));
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    });
    WebApplication app = builder.Build();
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }
    // Unknown addresses (a typo, or a cut-off order link) would otherwise get an empty 404 that the
    // browser shows as its own error page. Re-running them as /not-found keeps the 404 status but
    // serves the app, whose router then shows the Not Found page.
    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAntiforgery();
    app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}
