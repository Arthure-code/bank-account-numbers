using System.Reflection;
using System.Text.Json.Serialization;
using GenNumeros.ApplicationCore.Interfaces;
using GenNumeros.ApplicationCore.Services;
using GenNumeros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace GenNumeros.API
{
    public class Program
    {
        // La classe ne sert qu'a porter le point d'entree, mais les tests
        // fonctionnels la designent : elle ne peut pas etre statique.
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
                    Title = "API de generation des numeros de compte",
                    Version = "v1",
                    Description = "Generation et reservation des numeros de compte bancaire.",
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

                // Les commentaires du code deviennent la documentation de
                // chaque methode exposee.
                string documentation = Assembly.GetExecutingAssembly().GetName().Name + ".xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, documentation));
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

            // L'API porte le schema : elle joue ses migrations au demarrage,
            // ce qu'EnsureCreated ne ferait pas.
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

        // Une adresse absente laisse simplement le lien vide dans la fiche.
        private static Uri? Adresse(string? valeur)
        {
            return string.IsNullOrWhiteSpace(valeur) ? null : new Uri(valeur);
        }
    }
}
