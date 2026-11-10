using QT.PizzaOrdering.Infrastructure;

namespace QT.PizzaOrdering.API.Adapters.Data;

public static class DatabaseConfiguration
{
    /// <summary>
    /// Name of the connection string in appsettings.
    /// </summary>
    public const string ConnectionString = "PizzaOrdering";

    /// <summary>
    /// Optional appsettings key naming the folder the database file lives in.
    /// </summary>
    public const string DataDirectoryKey = "DataDirectory";

    /// <summary>
    /// Where the database goes when <see cref="DataDirectoryKey"/> is not set: a folder under the
    /// signed-in Windows user's local application data. Unlike the working directory or the build
    /// output folder, it is the same however the app is started (Visual Studio, <c>dotnet run</c>,
    /// or the built executable) and is not removed by cleaning or rebuilding the solution.
    /// </summary>
    public static string DefaultDataDirectory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeighborhoodPizza");

    /// <summary>
    /// The connection string from configuration, with a relative database file placed in the data
    /// directory so every launch opens the same orders.
    /// </summary>
    public static string BuildConnectionString(IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(ConnectionString)
            ?? throw new InvalidOperationException($"Cannot find connection string '{ConnectionString}' in appsettings.");

        string? configured = configuration[DataDirectoryKey];
        string dataDirectory = string.IsNullOrWhiteSpace(configured) ? DefaultDataDirectory : Path.GetFullPath(configured);

        return DbContextRegistration.ResolveSqliteConnectionString(connectionString, dataDirectory);
    }
}
