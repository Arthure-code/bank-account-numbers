using GenNumeros.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenNumeros.API.TestsFonctionnels
{
    // The whole API, its routing, its validation and its serialisation,
    // brought up for a single test. Only the database is replaced: an
    // in-memory SQLite that dies with the test, and that the API migrates
    // itself on startup.
    public sealed class ApplicationDeTest : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connexion = new SqliteConnection("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ConfigureServices(services =>
            {
                ServiceDescriptor? ancien = services.SingleOrDefault(
                    s => s.ServiceType == typeof(DbContextOptions<GenNumeroContext>));

                if (ancien != null)
                {
                    services.Remove(ancien);
                }

                _connexion.Open();
                services.AddDbContext<GenNumeroContext>(options => options.UseSqlite(_connexion));
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
