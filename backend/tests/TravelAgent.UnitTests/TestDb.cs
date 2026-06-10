using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TravelAgent.Infrastructure.Persistence;

namespace TravelAgent.UnitTests;

/// <summary>
/// SQLite in-memory database that lives for one test. Keeps the connection
/// open (closing it drops the database) and disposes both together.
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    public TravelAgentDbContext Context { get; }

    public TestDb()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TravelAgentDbContext>()
            .UseSqlite(_connection)
            .Options;
        Context = new TravelAgentDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
