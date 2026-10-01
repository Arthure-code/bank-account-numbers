using GenNumeros.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenNumeros.API.TestsFonctionnels
{
    // L'API entiere, son routage, sa validation et sa serialisation, montee
    // pour un seul test. Seule la base est remplacee : une SQLite en memoire
    // qui meurt avec le test, et que le demarrage de l'API migre lui-meme.
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
