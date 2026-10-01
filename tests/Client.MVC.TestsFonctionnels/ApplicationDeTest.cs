using Client.MVC.Controllers;
using Client.MVC.Interfaces;
using Client.MVC.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Client.MVC.TestsFonctionnels
{
    // The whole application, its routing, its model binding, its
    // validation and its views, brought up for a single test. The API
    // becomes a double: what is tested here is the application, not the
    // network. The type given to the factory only names the assembly.
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
