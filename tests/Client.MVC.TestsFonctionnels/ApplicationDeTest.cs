using Client.MVC.Controllers;
using Client.MVC.Interfaces;
using Client.MVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Client.MVC.TestsFonctionnels
{
    // L'application entiere, son routage, sa liaison de modele, sa
    // validation et ses vues, montee pour un seul test. L'API devient un
    // double : c'est l'application qu'on eprouve ici, pas le reseau.
    // Le type passe a la fabrique ne sert qu'a designer l'assemblage.
    public sealed class ApplicationDeTest : WebApplicationFactory<GestionComptesController>
    {
        public Mock<INumerosProxy> Api { get; } = new Mock<INumerosProxy>();

        public ApplicationDeTest()
        {
            Api.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ConfigureServices(services =>
            {
                foreach (ServiceDescriptor ancien in services
                    .Where(s => s.ServiceType == typeof(INumerosProxy)).ToList())
                {
                    services.Remove(ancien);
                }

                services.AddScoped(_ => Api.Object);
            });
        }
    }
}
