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
        private static NumeroDossier UnDossier(int identifiant, string numero, string demandeur = "employee.limoilou")
            => new NumeroDossier
            {
                Id = identifiant,
                NumeroCompte = numero,
                IdDemandeur = demandeur,
                Statut = "New",
                DateCreation = new DateTime(2026, 1, 5, 9, 30, 0)
            };

        private static DemandeDeNumeroDto UneDemande() => new DemandeDeNumeroDto
        {
            SystemeAppelant = "12",
            Succursale = "45400",
            IdDemandeur = "employee.limoilou"
        };

        [Fact]
        public async Task Get_RendChaqueNumeroAttribue()
        {
            //Given two numbers already given out
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnDossier(1, "145-12-45400-123456"),
                UnDossier(2, "145-12-45401-654320")
            });
            var controleur = new NumerosController(numeros.Object);

            //When
            IEnumerable<NumeroDossierDto> rendus = await controleur.Get();

            //Then
            Assert.Equal(2, rendus.Count());
            numeros.Verify(s => s.ObtenirTousLesNumeros(), Times.Once);
        }

        [Fact]
        public async Task Get_RecopieChaqueChampDuDossierDansSonDto()
        {
            //Given one number given out
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnDossier(7, "145-12-45400-123456", "marie.tremblay")
            });
            var controleur = new NumerosController(numeros.Object);

            //When
            NumeroDossierDto rendu = (await controleur.Get()).Single();

            //Then
            Assert.Equal(7, rendu.Id);
            Assert.Equal("145-12-45400-123456", rendu.NumeroCompte);
            Assert.Equal("marie.tremblay", rendu.IdDemandeur);
            Assert.Equal("New", rendu.Statut);
            Assert.Equal(new DateTime(2026, 1, 5, 9, 30, 0), rendu.DateCreation);
        }

        [Fact]
        public async Task Get_RendUneListeVideQuandAucunNumeroNAEteAttribue()
        {
            //Given no number at all
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>());
            var controleur = new NumerosController(numeros.Object);

            //Then
            Assert.Empty(await controleur.Get());
        }

        [Fact]
        public async Task Post_RefuseUneDemandeAbsente()
        {
            //Given no request body
            var numeros = new Mock<INumerosService>();
            var controleur = new NumerosController(numeros.Object);

            //When
            ActionResult<NumeroDossierDto> resultat = await controleur.Post(null!);

            //Then nothing is given out
            Assert.IsType<BadRequestObjectResult>(resultat.Result);
            numeros.Verify(s => s.GenererUnNumero(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Post_AttribueLeNumeroEtDonneSonAdresse()
        {
            //Given a service that gives out a number
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.GenererUnNumero("12", "45400", "employee.limoilou"))
                .ReturnsAsync(UnDossier(9, "145-12-45400-123456"));
            var controleur = new NumerosController(numeros.Object);

            //When
            ActionResult<NumeroDossierDto> resultat = await controleur.Post(UneDemande());

            //Then the number comes back with the address to read it again
            var cree = Assert.IsType<CreatedAtActionResult>(resultat.Result);
            NumeroDossierDto rendu = Assert.IsType<NumeroDossierDto>(cree.Value);
            Assert.Equal("145-12-45400-123456", rendu.NumeroCompte);
            Assert.Equal(9, cree.RouteValues!["id"]);
        }

        [Fact]
        public async Task Post_PasseAuServiceCeQueLaDemandePorte()
        {
            //Given a complete request
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.GenererUnNumero(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(UnDossier(1, "145-12-45400-123456"));
            var controleur = new NumerosController(numeros.Object);

            //When
            await controleur.Post(UneDemande());

            //Then the three values arrive untouched
            numeros.Verify(s => s.GenererUnNumero("12", "45400", "employee.limoilou"), Times.Once);
        }

        [Fact]
        public async Task Post_RepondConflitQuandAucunNumeroNEstLibre()
        {
            //Given a service that finds nothing to give out
            var numeros = new Mock<INumerosService>();
            numeros.Setup(s => s.GenererUnNumero(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((NumeroDossier?)null);
            var controleur = new NumerosController(numeros.Object);

            //When
            ActionResult<NumeroDossierDto> resultat = await controleur.Post(UneDemande());

            //Then
            Assert.IsType<ConflictObjectResult>(resultat.Result);
        }
    }
}
