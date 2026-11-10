using Microsoft.AspNetCore.Http.Features;

using QT.PizzaOrdering.API;
using QT.PizzaOrdering.API.Endpoints;
using QT.PizzaOrdering.API.Middleware;
using QT.PizzaOrdering.Infrastructure;

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

    builder.Services.AddOpenApi("v1", options =>
    {
        options.ShouldInclude = (desc) => desc.RelativePath?.StartsWith("api/v1", StringComparison.OrdinalIgnoreCase) == true;
    });

    // Features
    builder.AddMenuHandlers();
    builder.AddOrderHandlers();
    builder.AddDbContexts();

    // GlobalExceptionHandler
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);
        context.ProblemDetails.Extensions.TryAdd("traceId", context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity?.Id);
    });

    // Tests build many hosts in one process; Serilog's static logger is registered outside Test only.
    if (!builder.Environment.IsTest())
        builder.AddNonTestServices();

    // TimeProvider
    builder.Services.AddSingleton(TimeProvider.System);

    WebApplication app = builder.Build();

    app.UseHttpsRedirection();
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi("/openapi/{documentName}.json");
        app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "PizzaOrderingApi v1"));
    }

    if (!builder.Environment.IsTest())
    {
        app.UseSerilogRequestLogging();
    }

    await app.Services.InitializePizzaOrderingDatabaseAsync();

    // Endpoints
    app.MapMethods("/", ["GET", "HEAD"], () => Results.Ok());
    app.MapMenuEndpoints("v1");
    app.MapOrderEndpoints("v1");
    app.MapKitchenEndpoints("v1");
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
