using System.Net;
using System.Text.RegularExpressions;
using Client.MVC.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace Client.MVC.TestsFonctionnels
{
    public class GestionComptesTest : IDisposable
    {
        private readonly ApplicationDeTest _application = new ApplicationDeTest();

        // Without this, the client follows the redirect and only the
        // landing page is seen, never the answer to the form.
        private HttpClient Navigateur() => _application.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        private static async Task<string> Jeton(HttpClient navigateur, string adresse)
        {
            string page = await navigateur.GetStringAsync(adresse);
            System.Text.RegularExpressions.Match jeton = Regex.Match(page,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
                RegexOptions.None, TimeSpan.FromSeconds(5));

            return jeton.Groups[1].Value;
        }

        private static NumeroDossier UnNumero(int identifiant, string numero, string statut, int jour)
            => new NumeroDossier
            {
                Id = identifiant,
                NumeroCompte = numero,
                IdDemandeur = "employee.limoilou",
                Statut = statut,
                DateCreation = new DateTime(2026, 1, jour)
            };

        [Theory]
        [InlineData("/GestionComptes")]
        [InlineData("/GestionComptes/DemanderNumero")]
        public async Task LesPagesRepondent(string adresse)
        {
            HttpClient navigateur = Navigateur();

            Assert.Equal(HttpStatusCode.OK, (await navigateur.GetAsync(adresse)).StatusCode);
        }

        [Fact]
        public async Task Index_NeMontreQueLesNumerosNeufsDuPlusRecentAuPlusAncien()
        {
            //Given two new numbers and one already in use
            _application.Api.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnNumero(1, "145-12-45400-111110", "New", 5),
                UnNumero(2, "145-12-45401-222220", "Attribue", 6),
                UnNumero(3, "145-12-45402-333330", "New", 7)
            });
            HttpClient navigateur = Navigateur();

            //When
            string page = await navigateur.GetStringAsync("/GestionComptes");

            //Then the used number is absent, and the most recent one
            //comes first
            Assert.DoesNotContain("222220", page, StringComparison.Ordinal);
            Assert.True(page.IndexOf("333330", StringComparison.Ordinal)
                < page.IndexOf("111110", StringComparison.Ordinal));
        }

        [Fact]
        public async Task DemanderNumero_LaListeDeroulantePorteLesSuccursalesDuFichier()
        {
            //Given the three branches from the configuration
            HttpClient navigateur = Navigateur();

            //When
            string page = await navigateur.GetStringAsync("/GestionComptes/DemanderNumero");

            //Then
            Assert.Contains("value=\"45400\"", page, StringComparison.Ordinal);
            Assert.Contains("value=\"45401\"", page, StringComparison.Ordinal);
            Assert.Contains("value=\"45402\"", page, StringComparison.Ordinal);
            Assert.Contains("Lebourneuf", page, StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderNumero_TransmetLaDemandeEtRevientALaListe()
        {
            //Given an API that gives out a number
            DemandeDeNumero? envoyee = null;
            _application.Api.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .Callback<DemandeDeNumero>(d => envoyee = d)
                .ReturnsAsync(UnNumero(1, "145-12-45400-123456", "New", 5));
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //When the form is sent
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = "45402",
                    ["IdDemandeur"] = "marie.tremblay"
                }));

            //Then the application adds its own calling system number, and
            //sends the visitor back to the list
            Assert.Equal(HttpStatusCode.Found, reponse.StatusCode);
            Assert.Equal("/GestionComptes", reponse.Headers.Location?.OriginalString);
            Assert.Equal("12", envoyee?.SystemeAppelant);
            Assert.Equal("45402", envoyee?.Succursale);
            Assert.Equal("marie.tremblay", envoyee?.IdDemandeur);
        }

        [Fact]
        public async Task DemanderNumero_UnIdentifiantManquantRevientAvecSonMessage()
        {
            //Given a form with no identifier
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //When
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = "45402",
                    ["IdDemandeur"] = string.Empty
                }));

            //Then the page comes back saying so, and nothing is asked for
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("identifier is required", await reponse.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
            _application.Api.Verify(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()), Times.Never);
        }

        [Fact]
        public async Task DemanderNumero_UneSuccursaleManquanteRevientAvecSonMessage()
        {
            //Given a form where no branch is chosen
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //When
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = string.Empty,
                    ["IdDemandeur"] = "marie.tremblay"
                }));

            //Then
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("Choose a branch", await reponse.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderNumero_ExpliqueQuandLApiNAttribueRien()
        {
            //Given an API that answers no number
            _application.Api.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .ReturnsAsync((NumeroDossier?)null);
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //When
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = "45402",
                    ["IdDemandeur"] = "marie.tremblay"
                }));

            //Then the visitor reads why, instead of a blank page
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("could not give out a number", await reponse.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }

        public void Dispose()
        {
            _application.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
