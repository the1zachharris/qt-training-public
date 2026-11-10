using Microsoft.Data.Sqlite;

namespace QT.PizzaOrdering.Test.Shared;

/// <summary>
/// A SQLite database file in its own temp folder, removed when the test finishes.
/// </summary>
internal sealed class TempDatabase : IDisposable
{
    private readonly string _folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"pizza-ordering-tests-{Guid.NewGuid():N}");

    public TempDatabase()
    {
        Directory.CreateDirectory(_folder);
        Path = System.IO.Path.Combine(_folder, "orders.db");
    }

    public string Path { get; }

    public void Dispose()
    {
        // Pooled connections keep the file locked on Windows until the pools are cleared.
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, recursive: true);
    }
}
