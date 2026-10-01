using Client.MVC.Controllers;
using Client.MVC.Interfaces;
using Client.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Client.MVC.Tests.Controleurs
{
    public class GestionComptesControllerTest
    {
        // The framework's own configuration, filled in memory: it is not
        // a dependency of the project, so it is not mocked.
        private static IConfiguration Configuration(params (string Cle, string Valeur)[] valeurs)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(valeurs.ToDictionary(v => v.Cle, v => (string?)v.Valeur))
                .Build();
        }

        private static IConfiguration ConfigurationAvecSuccursales()
        {
            return Configuration(
                ("SystemeAppelant", "12"),
                ("Succursales:0:Nom", "Lebourneuf"),
                ("Succursales:0:Numero", "45400"),
                ("Succursales:1:Nom", "Limoilou"),
                ("Succursales:1:Numero", "45401"));
        }

        private static NumeroDossier UnNumero(int identifiant, string numero, string statut, DateTime creation)
            => new NumeroDossier
            {
                Id = identifiant,
                NumeroCompte = numero,
                IdDemandeur = "employee.limoilou",
                Statut = statut,
                DateCreation = creation
            };

        [Fact]
        public async Task Index_NeMontreQueLesNumerosNeufs()
        {
            //Given one new number and one already in use
            var proxy = new Mock<INumerosProxy>();
            proxy.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnNumero(1, "145-12-45400-123456", "New", new DateTime(2026, 1, 5)),
                UnNumero(2, "145-12-45401-654320", "Attribue", new DateTime(2026, 1, 6))
            });
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //When
            ActionResult resultat = await controleur.Index();

            //Then
            var montres = Assert.IsAssignableFrom<IEnumerable<NumeroDossier>>(
                Assert.IsType<ViewResult>(resultat).Model);
            Assert.Equal("145-12-45400-123456", Assert.Single(montres).NumeroCompte);
        }

        [Fact]
        public async Task Index_MontreLePlusRecentEnPremier()
        {
            //Given three new numbers given out at three moments
            var proxy = new Mock<INumerosProxy>();
            proxy.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnNumero(1, "first", "New", new DateTime(2026, 1, 5)),
                UnNumero(3, "last", "New", new DateTime(2026, 1, 7)),
                UnNumero(2, "second", "New", new DateTime(2026, 1, 6))
            });
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //When
            ActionResult resultat = await controleur.Index();

            //Then
            var montres = Assert.IsAssignableFrom<IEnumerable<NumeroDossier>>(
                Assert.IsType<ViewResult>(resultat).Model).ToList();
            Assert.Equal("last, second, first", string.Join(", ", montres.Select(n => n.NumeroCompte)));
        }

        [Fact]
        public void DemanderNumero_ProposeLesSuccursalesDeLaConfiguration()
        {
            //Given two branches in the configuration file
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(),
                new Mock<INumerosProxy>().Object);

            //When the page opens
            ActionResult resultat = controleur.DemanderNumero();

            //Then the drop-down carries them, number on five digits
            Assert.IsType<ViewResult>(resultat);
            var succursales = Assert.IsType<List<SelectListItem>>(controleur.ViewBag.Succursales);
            Assert.Equal(2, succursales.Count);
            Assert.Equal("45400", succursales[0].Value);
            Assert.Contains("Lebourneuf", succursales[0].Text, StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderNumero_NeDemandeRienQuandLeFormulaireEstInvalide()
        {
            //Given a missing identifier
            var proxy = new Mock<INumerosProxy>();
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);
            controleur.ModelState.AddModelError("IdDemandeur", "Votre identifiant est requis.");

            //When
            ActionResult resultat = await controleur.DemanderNumero(new DemandeDeNumero());

            //Then the page comes back with its branches, and nothing is asked for
            Assert.IsType<ViewResult>(resultat);
            Assert.NotNull(controleur.ViewBag.Succursales);
            proxy.Verify(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()), Times.Never);
        }

        [Fact]
        public async Task DemanderNumero_PoseLeNumeroDeSystemeAppelantDeLApplication()
        {
            //Given a request where the visitor filled in only their
            //branch and their identifier
            var proxy = new Mock<INumerosProxy>();
            DemandeDeNumero? envoyee = null;
            proxy.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .Callback<DemandeDeNumero>(d => envoyee = d)
                .ReturnsAsync(UnNumero(1, "145-12-45400-123456", "New", DateTime.Now));
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //When
            ActionResult resultat = await controleur.DemanderNumero(new DemandeDeNumero
            {
                Succursale = "45400",
                IdDemandeur = "marie.tremblay"
            });

            //Then the application adds its own number before calling
            Assert.Equal("12", envoyee?.SystemeAppelant);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DemanderNumero_LeNumeroDeSystemeAppelantSeLitDansLaConfiguration()
        {
            //Given an application registered under another number
            var proxy = new Mock<INumerosProxy>();
            DemandeDeNumero? envoyee = null;
            proxy.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .Callback<DemandeDeNumero>(d => envoyee = d)
                .ReturnsAsync(UnNumero(1, "145-99-45400-123456", "New", DateTime.Now));
            var controleur = new GestionComptesController(
                Configuration(("SystemeAppelant", "99")), proxy.Object);

            //When
            await controleur.DemanderNumero(new DemandeDeNumero
            {
                Succursale = "45400",
                IdDemandeur = "marie.tremblay"
            });

            //Then the one from the configuration is the one that goes
            Assert.Equal("99", envoyee?.SystemeAppelant);
        }

        [Fact]
        public async Task DemanderNumero_RevientSurLaPageQuandLApiNAttribueRien()
        {
            //Given an API that answers no number
            var proxy = new Mock<INumerosProxy>();
            proxy.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .ReturnsAsync((NumeroDossier?)null);
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //When
            ActionResult resultat = await controleur.DemanderNumero(new DemandeDeNumero
            {
                Succursale = "45400",
                IdDemandeur = "marie.tremblay"
            });

            //Then the visitor reads why, instead of a blank page
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("could not give out a number", controleur.ModelState[string.Empty]!.Errors[0].ErrorMessage,
                StringComparison.Ordinal);
        }

        [Fact]
        public void DemanderNumero_SupporteUneConfigurationSansSuccursale()
        {
            //Given a configuration file with no list of branches
            var controleur = new GestionComptesController(Configuration(("SystemeAppelant", "12")),
                new Mock<INumerosProxy>().Object);

            //Then the page still opens, with an empty list
            Assert.IsType<ViewResult>(controleur.DemanderNumero());
            Assert.Empty(Assert.IsType<List<SelectListItem>>(controleur.ViewBag.Succursales));
        }
    }
}
