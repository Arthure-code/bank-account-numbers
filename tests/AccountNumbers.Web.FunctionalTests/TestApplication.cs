using AccountNumbers.Web.Controllers;
using AccountNumbers.Web.Interfaces;
using AccountNumbers.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace AccountNumbers.Web.FunctionalTests
{
    // The whole application, its routing, its model binding, its
    // validation and its views, brought up for a single test. The API
    // becomes a double: what is tested here is the application, not the
    // network. The type given to the factory only names the assembly.
    public sealed class TestApplication : WebApplicationFactory<AccountNumbersController>
    {
        public Mock<IAccountNumberProxy> Api { get; } = new Mock<IAccountNumberProxy>();

        public TestApplication()
        {
            Api.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<AccountNumber>());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);

            builder.ConfigureServices(services =>
            {
                foreach (ServiceDescriptor previous in services
                    .Where(s => s.ServiceType == typeof(IAccountNumberProxy)).ToList())
                {
                    services.Remove(previous);
                }

                services.AddScoped(_ => Api.Object);
            });
        }
    }
}
