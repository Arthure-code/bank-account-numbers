using GenNumeros.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace GenNumeros.Infrastructure.TestsIntegration
{
    // Une base SQLite en memoire, montee pour un seul test et fermee avec
    // lui. xUnit construit une instance de la classe par test, donc rien ne
    // circule d'un test a l'autre.
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

        // Ce qu'une deuxieme requete verrait : sans cela, le suivi d'entites
        // rendrait l'objet garde en memoire au lieu de ce qui est ecrit.
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
