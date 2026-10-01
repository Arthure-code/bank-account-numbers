using AccountNumbers.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AccountNumbers.Infrastructure.IntegrationTests
{
    // An in-memory SQLite database, brought up for a single test and closed
    // with it. xUnit builds one instance of the class per test, so nothing
    // travels from one test to the next.
    public sealed class FreshDatabase : IDisposable
    {
        private readonly SqliteConnection _connexion;

        public AccountNumberContext Context { get; }

        public FreshDatabase()
        {
            _connexion = new SqliteConnection("DataSource=:memory:");
            _connexion.Open();

            DbContextOptions<AccountNumberContext> options =
                new DbContextOptionsBuilder<AccountNumberContext>()
                    .UseSqlite(_connexion)
                    .Options;

            Context = new AccountNumberContext(options);
            Context.Database.EnsureCreated();
        }

        // What a second request would see: without this, change tracking
        // would answer the object held in memory instead of what was written.
        public void Forget()
        {
            Context.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            Context.Dispose();
            _connexion.Dispose();
        }
    }
}
