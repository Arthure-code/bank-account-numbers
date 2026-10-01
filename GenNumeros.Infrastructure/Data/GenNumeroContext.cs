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
    }
}
