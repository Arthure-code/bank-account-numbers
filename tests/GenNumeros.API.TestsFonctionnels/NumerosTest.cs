using System.Net;
using System.Net.Http.Json;
using GenNumeros.ApplicationCore.DTOs;

namespace GenNumeros.API.TestsFonctionnels
{
    public class NumerosTest : IDisposable
    {
        private readonly ApplicationDeTest _application = new ApplicationDeTest();

        private static DemandeDeNumeroDto UneDemande(string succursale = "45400",
            string demandeur = "employe.limoilou") => new DemandeDeNumeroDto
            {
                SystemeAppelant = "12",
                Succursale = succursale,
                IdDemandeur = demandeur
            };

        [Fact]
        public async Task Get_RendUneListeVideSurUneBaseNeuve()
        {
            //Etant donne une base que personne n'a encore servie
            HttpClient client = _application.CreateClient();

            //Alors
            HttpResponseMessage reponse = await client.GetAsync("/api/Numeros");
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Empty((await reponse.Content.ReadFromJsonAsync<List<NumeroDossierDto>>())!);
        }

        [Fact]
        public async Task Post_AttribueUnNumeroEtLeRendDansLaListe()
        {
            //Etant donne une demande complete
            HttpClient client = _application.CreateClient();

            //Lorsque
            HttpResponseMessage reponse = await client.PostAsJsonAsync("/api/Numeros", UneDemande());

            //Alors le numero est cree, et la liste le porte
            Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
            NumeroDossierDto? attribue = await reponse.Content.ReadFromJsonAsync<NumeroDossierDto>();
            Assert.Equal("Nouveau", attribue?.Statut);
            Assert.Equal("employe.limoilou", attribue?.IdDemandeur);

            List<NumeroDossierDto> liste = (await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!;
            Assert.Equal(attribue!.NumeroCompte, Assert.Single(liste).NumeroCompte);
        }

        [Fact]
        public async Task Post_LeNumeroSuitLaFormeAttendue()
        {
            //Etant donne une demande pour la succursale de Limoilou
            HttpClient client = _application.CreateClient();

            //Lorsque
            NumeroDossierDto? attribue = await (await client.PostAsJsonAsync("/api/Numeros", UneDemande("45401")))
                .Content.ReadFromJsonAsync<NumeroDossierDto>();

            //Alors seize chiffres en quatre tranches, les deux derniers pairs
            string[] tranches = attribue!.NumeroCompte.Split('-');
            Assert.Equal("145-12-45401", string.Join('-', tranches[..3]));
            Assert.Equal(6, tranches[3].Length);
            Assert.Equal(0, int.Parse(tranches[3][^2..], System.Globalization.CultureInfo.InvariantCulture) % 2);
        }

        [Fact]
        public async Task Post_CentDemandesDonnentCentNumerosDifferents()
        {
            //Etant donne cent demandes de suite
            HttpClient client = _application.CreateClient();

            for (int demande = 0; demande < 100; demande++)
            {
                await client.PostAsJsonAsync("/api/Numeros", UneDemande());
            }

            //Alors aucun numero n'a ete donne deux fois
            List<NumeroDossierDto> liste = (await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!;
            Assert.Equal(100, liste.Count);
            Assert.Equal(100, liste.Select(n => n.NumeroCompte).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public async Task Get_RendLePlusRecentEnPremier()
        {
            //Etant donne trois numeros attribues l'un apres l'autre
            HttpClient client = _application.CreateClient();
            var attribues = new List<string>();

            for (int demande = 0; demande < 3; demande++)
            {
                NumeroDossierDto? numero = await (await client.PostAsJsonAsync("/api/Numeros", UneDemande()))
                    .Content.ReadFromJsonAsync<NumeroDossierDto>();
                attribues.Add(numero!.NumeroCompte);
            }

            //Alors la liste les rend dans l'ordre inverse
            List<NumeroDossierDto> liste = (await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!;
            attribues.Reverse();
            Assert.Equal(attribues, liste.Select(n => n.NumeroCompte));
        }

        [Theory]
        [InlineData("1", "45400", "employe")]
        [InlineData("12", "4540", "employe")]
        [InlineData("12", "45400", "")]
        [InlineData("ab", "45400", "employe")]
        public async Task Post_RefuseUneDemandeMalFormee(string systeme, string succursale, string demandeur)
        {
            //Etant donne une demande hors format
            HttpClient client = _application.CreateClient();

            //Lorsque
            HttpResponseMessage reponse = await client.PostAsJsonAsync("/api/Numeros", new DemandeDeNumeroDto
            {
                SystemeAppelant = systeme,
                Succursale = succursale,
                IdDemandeur = demandeur
            });

            //Alors elle est refusee avant d'atteindre le service, et rien
            //n'est enregistre
            Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!);
        }

        [Fact]
        public async Task Post_RefuseUnCorpsAbsent()
        {
            //Etant donne une requete sans corps
            HttpClient client = _application.CreateClient();

            //Alors
            HttpResponseMessage reponse = await client.PostAsJsonAsync<DemandeDeNumeroDto?>("/api/Numeros", null);
            Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
        }

        public void Dispose()
        {
            _application.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
