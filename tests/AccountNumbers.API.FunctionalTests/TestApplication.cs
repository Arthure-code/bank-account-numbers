using AccountNumbers.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountNumbers.API.FunctionalTests
{
    // The whole API, its routing, its validation and its serialisation,
    // brought up for a single test. Only the database is replaced: an
    // in-memory SQLite that dies with the test, and that the API migrates
    // itself on startup.
    public sealed class TestApplication : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connexion = new SqliteConnection("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ConfigureServices(services =>
            {
                ServiceDescriptor? previous = services.SingleOrDefault(
                    s => s.ServiceType == typeof(DbContextOptions<AccountNumberContext>));

                if (previous != null)
                {
                    services.Remove(previous);
                }

                _connexion.Open();
                services.AddDbContext<AccountNumberContext>(options => options.UseSqlite(_connexion));
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _connexion.Dispose();
            }
        }
    }
}
