using CoffeeBar.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoffeeBar.Tests;

// In-memory SQLite database built from the real migrations.
// EF Core throws on Migrate() if the model has changes without a migration,
// so every test run also checks that the migrations are complete.
public sealed class TestDbContextFactory : IDbContextFactory<CoffeesContext>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DbContextOptions<CoffeesContext> _options;

    public TestDbContextFactory()
    {
        _connection.Open();
        _options = new DbContextOptionsBuilder<CoffeesContext>().UseSqlite(_connection).Options;
        using var context = CreateDbContext();
        context.Database.Migrate();
    }

    public CoffeesContext CreateDbContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}
