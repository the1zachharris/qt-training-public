using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using QT.PizzaOrdering.API.Adapters.Data;
using QT.PizzaOrdering.Infrastructure;
using QT.PizzaOrdering.Test.Shared;

namespace QT.PizzaOrdering.Test.Infrastructure;

/// <summary>
/// The database must be the same file no matter how or from where the app is launched, or the
/// kitchen would open an empty order list after starting the app a different way.
/// </summary>
public class DatabasePathTests
{
    private static readonly string DataDirectory = Path.Combine(Path.GetTempPath(), "apps", "pizza-data");

    private static IConfiguration Configuration(params (string Key, string Value)[] settings) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(setting => KeyValuePair.Create(setting.Key, (string?)setting.Value)))
            .Build();

    private static string DataSourceOf(string connectionString) => new SqliteConnectionStringBuilder(connectionString).DataSource;

    [Fact]
    public void ResolveSqliteConnectionString_RelativeFile_IsAnchoredToTheDataDirectory()
    {
        string resolved = DbContextRegistration.ResolveSqliteConnectionString("Data Source=neighborhood-pizza.db", DataDirectory);

        Assert.Equal($"Data Source={Path.Combine(DataDirectory, "neighborhood-pizza.db")}", resolved);
    }

    [Fact]
    public void ResolveSqliteConnectionString_AbsoluteFile_IsUnchanged()
    {
        string absolute = Path.Combine(Path.GetTempPath(), "elsewhere", "orders.db");

        string resolved = DbContextRegistration.ResolveSqliteConnectionString($"Data Source={absolute}", DataDirectory);

        Assert.Equal($"Data Source={absolute}", resolved);
    }

    [Fact]
    public void ResolveSqliteConnectionString_InMemory_IsUnchanged()
    {
        string resolved = DbContextRegistration.ResolveSqliteConnectionString("Data Source=:memory:", DataDirectory);

        Assert.Equal("Data Source=:memory:", resolved);
    }

    [Fact]
    public void BuildConnectionString_ByDefault_UsesAFolderThatDoesNotDependOnWhereTheAppIsLaunchedFrom()
    {
        IConfiguration configuration = Configuration(("ConnectionStrings:PizzaOrdering", "Data Source=neighborhood-pizza.db"));

        string dataSource = DataSourceOf(DatabaseConfiguration.BuildConnectionString(configuration));

        string expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeighborhoodPizza", "neighborhood-pizza.db");
        Assert.Equal(expected, dataSource);
        // Neither the working directory nor the build output folder: both change with how the app is started.
        Assert.False(dataSource.StartsWith(Environment.CurrentDirectory, StringComparison.OrdinalIgnoreCase));
        Assert.False(dataSource.StartsWith(AppContext.BaseDirectory, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BuildConnectionString_WithAConfiguredDataDirectory_UsesIt()
    {
        IConfiguration configuration = Configuration(
            ("ConnectionStrings:PizzaOrdering", "Data Source=neighborhood-pizza.db"),
            (DatabaseConfiguration.DataDirectoryKey, DataDirectory));

        string dataSource = DataSourceOf(DatabaseConfiguration.BuildConnectionString(configuration));

        Assert.Equal(Path.Combine(DataDirectory, "neighborhood-pizza.db"), dataSource);
    }

    [Fact]
    public void BuildConnectionString_WithoutAConnectionString_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => DatabaseConfiguration.BuildConnectionString(Configuration()));
    }

    [Fact]
    public async Task ShippedSettings_PointAtTheNeighborhoodPizzaFile_NotThePassOneFile()
    {
        // pizza-ordering.db was created without migrations by the first version of this app and
        // cannot be upgraded, so the app's own settings must resolve to a different file.
        await using PizzaOrderingWebAppFactory factory = new();
        IConfiguration shipped = factory.Services.GetRequiredService<IConfiguration>();

        string dataSource = DataSourceOf(DatabaseConfiguration.BuildConnectionString(shipped));

        Assert.Equal("neighborhood-pizza.db", Path.GetFileName(dataSource));
        Assert.True(Path.IsPathRooted(dataSource));
    }
}
