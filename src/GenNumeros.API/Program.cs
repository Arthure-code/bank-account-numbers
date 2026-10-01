using System.Reflection;
using System.Text.Json.Serialization;
using GenNumeros.ApplicationCore.DTOs;
using GenNumeros.ApplicationCore.Interfaces;
using GenNumeros.ApplicationCore.Services;
using GenNumeros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace GenNumeros.API
{
    public class Program
    {
        // The class only carries the entry point, but the functional
        // tests name it, so it cannot be static.
        protected Program()
        {
        }

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Account number generation API",
                    Version = "v1",
                    Description = "Generation and reservation of bank account numbers.",
                    License = new OpenApiLicense
                    {
                        Name = builder.Configuration["Documentation:Licence"],
                        Url = Adresse(builder.Configuration["Documentation:LicenceUrl"])
                    },
                    Contact = new OpenApiContact
                    {
                        Name = builder.Configuration["Documentation:Auteur"],
                        Url = Adresse(builder.Configuration["Documentation:AuteurUrl"])
                    }
                });

                // The comments in the code become the documentation of
                // every exposed method, and those of the core the
                // documentation of the schemas it exchanges.
                Assembly[] assemblages = { typeof(Program).Assembly, typeof(DemandeDeNumeroDto).Assembly };

                foreach (Assembly assemblage in assemblages)
                {
                    string documentation = Path.Combine(AppContext.BaseDirectory, assemblage.GetName().Name + ".xml");

                    if (File.Exists(documentation))
                    {
                        c.IncludeXmlComments(documentation);
                    }
                }
            });

            builder.Services.AddDbContext<GenNumeroContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(AsyncRepository<>));
            builder.Services.AddScoped<INumerosService, NumerosService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // The API owns the schema: it runs its migrations on startup,
            // which EnsureCreated would not do.
            using (IServiceScope portee = app.Services.CreateScope())
            {
                var context = portee.ServiceProvider.GetRequiredService<GenNumeroContext>();
                await context.Database.MigrateAsync();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }

        // A missing address simply leaves the link empty on the page.
        private static Uri? Adresse(string? valeur)
        {
            return string.IsNullOrWhiteSpace(valeur) ? null : new Uri(valeur);
        }
    }
}
