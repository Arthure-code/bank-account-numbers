using GenNumeros.ApplicationCore.Entites;
using Microsoft.EntityFrameworkCore;

namespace GenNumeros.Infrastructure.Data
{
    public class GenNumeroContext : DbContext
    {
        public GenNumeroContext(DbContextOptions<GenNumeroContext> options) : base(options)
        {
        }

        public DbSet<NumeroDossier> NumeroDossiers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            // The bank never gives a number twice: the database guarantees
            // it, not only the service that draws it.
            modelBuilder.Entity<NumeroDossier>()
                .HasIndex(n => n.NumeroCompte)
                .IsUnique();
        }
    }
}
