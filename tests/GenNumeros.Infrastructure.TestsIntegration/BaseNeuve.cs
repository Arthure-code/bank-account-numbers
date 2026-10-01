using GenNumeros.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GenNumeros.Infrastructure.TestsIntegration
{
    // An in-memory SQLite database, brought up for a single test and closed
    // with it. xUnit builds one instance of the class per test, so nothing
    // travels from one test to the next.
    public sealed class BaseNeuve : IDisposable
    {
        private readonly SqliteConnection _connexion;

        public GenNumeroContext Context { get; }

        public BaseNeuve()
        {
            _connexion = new SqliteConnection("DataSource=:memory:");
            _connexion.Open();

            DbContextOptions<GenNumeroContext> options =
                new DbContextOptionsBuilder<GenNumeroContext>()
                    .UseSqlite(_connexion)
                    .Options;

            Context = new GenNumeroContext(options);
            Context.Database.EnsureCreated();
        }

        // What a second request would see: without this, change tracking
        // would answer the object held in memory instead of what was written.
        public void Oublier()
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
