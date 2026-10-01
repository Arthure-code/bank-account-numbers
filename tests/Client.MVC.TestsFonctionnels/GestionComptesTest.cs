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

        // Sans cela, le client suit la redirection et on ne voit plus que la
        // page d'arrivee, jamais la reponse du formulaire.
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
                IdDemandeur = "employe.limoilou",
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
            //Etant donne deux numeros neufs et un numero deja utilise
            _application.Api.Setup(p => p.ObtenirTousLesNumeros()).ReturnsAsync(new List<NumeroDossier>
            {
                UnNumero(1, "145-12-45400-111110", "Nouveau", 5),
                UnNumero(2, "145-12-45401-222220", "Attribue", 6),
                UnNumero(3, "145-12-45402-333330", "Nouveau", 7)
            });
            HttpClient navigateur = Navigateur();

            //Lorsque
            string page = await navigateur.GetStringAsync("/GestionComptes");

            //Alors le numero utilise ne parait pas, et le plus recent passe
            //devant
            Assert.DoesNotContain("222220", page, StringComparison.Ordinal);
            Assert.True(page.IndexOf("333330", StringComparison.Ordinal)
                < page.IndexOf("111110", StringComparison.Ordinal));
        }

        [Fact]
        public async Task DemanderNumero_LaListeDeroulantePorteLesSuccursalesDuFichier()
        {
            //Etant donne les trois succursales de la configuration
            HttpClient navigateur = Navigateur();

            //Lorsque
            string page = await navigateur.GetStringAsync("/GestionComptes/DemanderNumero");

            //Alors
            Assert.Contains("value=\"45400\"", page, StringComparison.Ordinal);
            Assert.Contains("value=\"45401\"", page, StringComparison.Ordinal);
            Assert.Contains("value=\"45402\"", page, StringComparison.Ordinal);
            Assert.Contains("Lebourneuf", page, StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderNumero_TransmetLaDemandeEtRevientALaListe()
        {
            //Etant donne une API qui attribue
            DemandeDeNumero? envoyee = null;
            _application.Api.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .Callback<DemandeDeNumero>(d => envoyee = d)
                .ReturnsAsync(UnNumero(1, "145-12-45400-123456", "Nouveau", 5));
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //Lorsque le formulaire part
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = "45402",
                    ["IdDemandeur"] = "marie.tremblay"
                }));

            //Alors l'application ajoute son numero de systeme appelant, et
            //renvoie le visiteur a la liste
            Assert.Equal(HttpStatusCode.Found, reponse.StatusCode);
            Assert.Equal("/GestionComptes", reponse.Headers.Location?.OriginalString);
            Assert.Equal("12", envoyee?.SystemeAppelant);
            Assert.Equal("45402", envoyee?.Succursale);
            Assert.Equal("marie.tremblay", envoyee?.IdDemandeur);
        }

        [Fact]
        public async Task DemanderNumero_UnIdentifiantManquantRevientAvecSonMessage()
        {
            //Etant donne un formulaire sans identifiant
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //Lorsque
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = "45402",
                    ["IdDemandeur"] = string.Empty
                }));

            //Alors la page revient en le disant, et rien n'est demande
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("identifiant est requis", await reponse.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
            _application.Api.Verify(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()), Times.Never);
        }

        [Fact]
        public async Task DemanderNumero_UneSuccursaleManquanteRevientAvecSonMessage()
        {
            //Etant donne un formulaire ou aucune succursale n'est choisie
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //Lorsque
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = string.Empty,
                    ["IdDemandeur"] = "marie.tremblay"
                }));

            //Alors
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("Choisissez une succursale", await reponse.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task DemanderNumero_ExpliqueQuandLApiNAttribueRien()
        {
            //Etant donne une API qui ne rend aucun numero
            _application.Api.Setup(p => p.DemanderUnNumero(It.IsAny<DemandeDeNumero>()))
                .ReturnsAsync((NumeroDossier?)null);
            HttpClient navigateur = Navigateur();
            string jeton = await Jeton(navigateur, "/GestionComptes/DemanderNumero");

            //Lorsque
            HttpResponseMessage reponse = await navigateur.PostAsync("/GestionComptes/DemanderNumero",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = jeton,
                    ["Succursale"] = "45402",
                    ["IdDemandeur"] = "marie.tremblay"
                }));

            //Alors le visiteur lit pourquoi, au lieu d'une page blanche
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Contains("n&#x27;a pas pu attribuer", await reponse.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }

        public void Dispose()
        {
            _application.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
