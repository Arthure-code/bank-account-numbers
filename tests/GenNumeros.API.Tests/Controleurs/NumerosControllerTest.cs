using GenNumeros.API.Controllers;
using GenNumeros.ApplicationCore.DTOs;
using GenNumeros.ApplicationCore.Entites;
using GenNumeros.ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GenNumeros.API.Tests.Controleurs
{
    public class NumerosControllerTest
    {
        private static NumeroDossier UnDossier(int identifiant, string numero, string demandeur = "employe.limoilou")
            => new NumeroDossier
            {
                Id = identifiant,
                NumeroCompte = numero,
                IdDemandeur = demandeur,
                Statut = "Nouveau",
                DateCreation = new DateTime(2026, 1, 5, 9, 30, 0)
            };

        private static DemandeDeNumeroDto UneDemande() => new DemandeDeNumeroDto
        {
            SystemeAppelant = "12",
            Succursale = "45400",
            IdDemandeur = "employe.limoilou"
        };

        [Fact]
        public async Task Get_RendChaqueNumeroAttribue()
        {
            //Etant donne deux numeros deja attribues
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnDossier(1, "145-12-45400-123456"),
                UnDossier(2, "145-12-45401-654320")
            });
            var controleur = new NumerosController(numeros.Object);

            //Lorsque
            IEnumerable<NumeroDossierDto> rendus = await controleur.Get();

            //Alors
            Assert.Equal(2, rendus.Count());
            numeros.Verify(s => s.ObtenirTousLesNumeros(), Times.Once);
        }

        [Fact]
        public async Task Get_RecopieChaqueChampDuDossierDansSonDto()
        {
            //Etant donne un numero attribue
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnDossier(7, "145-12-45400-123456", "marie.tremblay")
            });
            var controleur = new NumerosController(numeros.Object);

            //Lorsque
            NumeroDossierDto rendu = (await controleur.Get()).Single();

            //Alors
            Assert.Equal(7, rendu.Id);
            Assert.Equal("145-12-45400-123456", rendu.NumeroCompte);
            Assert.Equal("marie.tremblay", rendu.IdDemandeur);
            Assert.Equal("Nouveau", rendu.Statut);
            Assert.Equal(new DateTime(2026, 1, 5, 9, 30, 0), rendu.DateCreation);
        }

        [Fact]
        public async Task Get_RendUneListeVideQuandAucunNumeroNAEteAttribue()
        {
            //Etant donne aucun numero
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>());
            var controleur = new NumerosController(numeros.Object);

            //Alors
            Assert.Empty(await controleur.Get());
        }

        [Fact]
        public async Task Post_RefuseUneDemandeAbsente()
        {
            //Etant donne aucun corps de requete
            var numeros = new Mock<INumerosService>();
            var controleur = new NumerosController(numeros.Object);

            //Lorsque
            ActionResult<NumeroDossierDto> resultat = await controleur.Post(null!);

            //Alors rien n'est attribue
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            numeros.Verify(s => s.GenererUnNumero(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Post_AttribueLeNumeroEtDonneSonAdresse()
        {
            //Etant donne un service qui attribue
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.GenererUnNumero("12", "45400", "employe.limoilou"))
                .ReturnsAsync(UnDossier(9, "145-12-45400-123456"));
            var controleur = new NumerosController(numeros.Object);

            //Lorsque
            ActionResult<NumeroDossierDto> resultat = await controleur.Post(UneDemande());

            //Alors le numero revient avec l'adresse ou le relire
            var cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            NumeroDossierDto rendu = Assert.IsType<NumeroDossierDto>(cree.Value);
            Assert.Equal("145-12-45400-123456", rendu.NumeroCompte);
            Assert.Equal(9, cree.RouteValues!["id"]);
        }

        [Fact]
        public async Task Post_PasseAuServiceCeQueLaDemandePorte()
        {
            //Etant donne une demande complete
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.GenererUnNumero(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(UnDossier(1, "145-12-45400-123456"));
            var controleur = new NumerosController(numeros.Object);

            //Lorsque
            await controleur.Post(UneDemande());

            //Alors les trois valeurs arrivent telles quelles
            numeros.Verify(s => s.GenererUnNumero("12", "45400", "employe.limoilou"), Times.Once);
        }

        [Fact]
        public async Task Post_RepondConflitQuandAucunNumeroNEstLibre()
        {
            //Etant donne un service qui ne trouve rien a attribuer
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.GenererUnNumero(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((NumeroDossier?)null);
            var controleur = new NumerosController(numeros.Object);

            //Lorsque
            ActionResult<NumeroDossierDto> resultat = await controleur.Post(UneDemande());

            //Alors
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }
    }
}
