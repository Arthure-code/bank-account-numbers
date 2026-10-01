using System.Reflection;
using System.Text.Json.Serialization;
using AccountNumbers.ApplicationCore.DTOs;
using AccountNumbers.ApplicationCore.Interfaces;
using AccountNumbers.ApplicationCore.Services;
using AccountNumbers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace AccountNumbers.API
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
                        Name = builder.Configuration["Documentation:License"],
                        Url = Endpoint(builder.Configuration["Documentation:LicenseUrl"])
                    },
                    Contact = new OpenApiContact
                    {
                        Name = builder.Configuration["Documentation:Author"],
                        Url = Endpoint(builder.Configuration["Documentation:AuthorUrl"])
                    }
                });

                // The comments in the code become the documentation of
                // every exposed method, and those of the core the
                // documentation of the schemas it exchanges.
                Assembly[] assemblies = { typeof(Program).Assembly, typeof(NumberRequestDto).Assembly };

                foreach (Assembly assembly in assemblies)
                {
                    string documentation = Path.Combine(AppContext.BaseDirectory, assembly.GetName().Name + ".xml");

                    if (File.Exists(documentation))
                    {
                        c.IncludeXmlComments(documentation);
                    }
                }
            });

            builder.Services.AddDbContext<AccountNumberContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped(typeof(IAsyncRepository<>), typeof(AsyncRepository<>));
            builder.Services.AddScoped<IAccountNumberService, AccountNumberService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // The API owns the schema: it runs its migrations on startup,
            // which EnsureCreated would not do.
            using (IServiceScope scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AccountNumberContext>();
                await context.Database.MigrateAsync();
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }

        // A missing address simply leaves the link empty on the page.
        private static Uri? Endpoint(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : new Uri(value);
        }
    }
}
