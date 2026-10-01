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
        // La configuration reelle du cadriciel, remplie en memoire : ce n'est
        // pas une dependance du projet, donc on ne la moque pas.
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
                IdDemandeur = "employe.limoilou",
                Statut = statut,
                DateCreation = creation
            };

        [Fact]
        public async Task Index_NeMontreQueLesNumerosNeufs()
        {
            //Etant donne un numero neuf et un numero deja utilise
            var proxy = new Mock<INumerosProxy>();
            proxy.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnNumero(1, "145-12-45400-123456", "Nouveau", new DateTime(2026, 1, 5)),
                UnNumero(2, "145-12-45401-654320", "Attribue", new DateTime(2026, 1, 6))
            });
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //Lorsque
            ActionResult resultat = await controleur.Index();

            //Alors
            var montres = Assert.IsAssignableFrom<IEnumerable<NumeroDossier>>(
                Assert.IsType<ViewResult>(resultat).Model);
            Assert.Equal("145-12-45400-123456", Assert.Single(montres).NumeroCompte);
        }

        [Fact]
        public async Task Index_MontreLePlusRecentEnPremier()
        {
            //Etant donne trois numeros neufs attribues a trois moments
            var proxy = new Mock<INumerosProxy>();
            proxy.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnNumero(1, "premier", "Nouveau", new DateTime(2026, 1, 5)),
                UnNumero(3, "dernier", "Nouveau", new DateTime(2026, 1, 7)),
                UnNumero(2, "deuxieme", "Nouveau", new DateTime(2026, 1, 6))
            });
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //Lorsque
            ActionResult resultat = await controleur.Index();

            //Alors
            var montres = Assert.IsAssignableFrom<IEnumerable<NumeroDossier>>(
                Assert.IsType<ViewResult>(resultat).Model).ToList();
            Assert.Equal(new[] { "dernier", "deuxieme", "premier" }, montres.Select(n => n.NumeroCompte));
        }

        [Fact]
        public void DemanderNumero_ProposeLesSuccursalesDeLaConfiguration()
        {
            //Etant donne deux succursales au fichier de configuration
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(),
                new Mock<INumerosProxy>().Object);

            //Lorsque la page s'ouvre
            ActionResult resultat = controleur.DemanderNumero();

            //Alors la liste deroulante les porte, numero sur cinq chiffres
            Assert.IsType<ViewResult>(resultat);
            var succursales = Assert.IsType<List<SelectListItem>>(controleur.ViewBag.Succursales);
            Assert.Equal(2, succursales.Count);
            Assert.Equal("45400", succursales[0].Value);
            Assert.Contains("Lebourneuf", succursales[0].Text, StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderNumero_NeDemandeRienQuandLeFormulaireEstInvalide()
        {
            //Etant donne un identifiant manquant
            var proxy = new Mock<INumerosProxy>();
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);
            controleur.ModelState.AddModelError("IdDemandeur", "Votre identifiant est requis.");

            //Lorsque
            ActionResult resultat = await controleur.DemanderNumero(new DemandeDeNumero());

            //Alors la page revient, avec ses succursales, et rien n'est demande
            Assert.IsType<ViewResult>(resultat);
            Assert.NotNull(controleur.ViewBag.Succursales);
            proxy.Verify(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()), Times.Never);
        }

        [Fact]
        public async Task DemanderNumero_PoseLeNumeroDeSystemeAppelantDeLApplication()
        {
            //Etant donne une demande ou le visiteur n'a rempli que sa
            //succursale et son identifiant
            var proxy = new Mock<INumerosProxy>();
            DemandeDeNumero? envoyee = null;
            proxy.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .Callback<DemandeDeNumero>(d => envoyee = d)
                .ReturnsAsync(UnNumero(1, "145-12-45400-123456", "Nouveau", DateTime.Now));
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //Lorsque
            ActionResult resultat = await controleur.DemanderNumero(new DemandeDeNumero
            {
                Succursale = "45400",
                IdDemandeur = "marie.tremblay"
            });

            //Alors l'application ajoute son propre numero avant d'appeler
            Assert.Equal("12", envoyee?.SystemeAppelant);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(resultat).ActionName);
        }

        [Fact]
        public async Task DemanderNumero_LeNumeroDeSystemeAppelantSeLitDansLaConfiguration()
        {
            //Etant donne une application enregistree sous un autre numero
            var proxy = new Mock<INumerosProxy>();
            DemandeDeNumero? envoyee = null;
            proxy.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .Callback<DemandeDeNumero>(d => envoyee = d)
                .ReturnsAsync(UnNumero(1, "145-99-45400-123456", "Nouveau", DateTime.Now));
            var controleur = new GestionComptesController(
                Configuration(("SystemeAppelant", "99")), proxy.Object);

            //Lorsque
            await controleur.DemanderNumero(new DemandeDeNumero
            {
                Succursale = "45400",
                IdDemandeur = "marie.tremblay"
            });

            //Alors c'est celui de la configuration qui part
            Assert.Equal("99", envoyee?.SystemeAppelant);
        }

        [Fact]
        public async Task DemanderNumero_RevientSurLaPageQuandLApiNAttribueRien()
        {
            //Etant donne une API qui ne rend aucun numero
            var proxy = new Mock<INumerosProxy>();
            proxy.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .ReturnsAsync((NumeroDossier?)null);
            var controleur = new GestionComptesController(ConfigurationAvecSuccursales(), proxy.Object);

            //Lorsque
            ActionResult resultat = await controleur.DemanderNumero(new DemandeDeNumero
            {
                Succursale = "45400",
                IdDemandeur = "marie.tremblay"
            });

            //Alors le visiteur lit pourquoi, au lieu d'une page blanche
            Assert.IsType<ViewResult>(resultat);
            Assert.Contains("n'a pas pu attribuer", controleur.ModelState[string.Empty]!.Errors[0].ErrorMessage,
                StringComparison.Ordinal);
        }

        [Fact]
        public void DemanderNumero_SupporteUneConfigurationSansSuccursale()
        {
            //Etant donne un fichier de configuration sans liste de succursales
            var controleur = new GestionComptesController(Configuration(("SystemeAppelant", "12")),
                new Mock<INumerosProxy>().Object);

            //Alors la page s'ouvre quand meme, avec une liste vide
            Assert.IsType<ViewResult>(controleur.DemanderNumero());
            Assert.Empty(Assert.IsType<List<SelectListItem>>(controleur.ViewBag.Succursales));
        }
    }
}
