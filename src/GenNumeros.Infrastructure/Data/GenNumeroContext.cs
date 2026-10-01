using GenNumeros.ApplicationCore.Entites;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            // La banque ne redonne jamais un numero : la base le garantit,
            // pas seulement le service qui le tire.
            modelBuilder.Entity<NumeroDossier>()
                .HasIndex(n => n.NumeroCompte)
                .IsUnique();
        }
    }
}
