using AccountNumbers.ApplicationCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccountNumbers.Infrastructure.Data
{
    public class AccountNumberContext : DbContext
    {
        public AccountNumberContext(DbContextOptions<AccountNumberContext> options) : base(options)
        {
        }

        public DbSet<AccountNumber> AccountNumbers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            // The bank never gives a number twice: the database guarantees
            // it, not only the service that draws it.
            modelBuilder.Entity<AccountNumber>()
                .HasIndex(n => n.Number)
                .IsUnique();
        }
    }
}
