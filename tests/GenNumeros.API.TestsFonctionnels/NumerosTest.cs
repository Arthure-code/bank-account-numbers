using System.Net;
using System.Net.Http.Json;
using GenNumeros.ApplicationCore.DTOs;

namespace GenNumeros.API.TestsFonctionnels
{
    public class NumerosTest : IDisposable
    {
        private readonly ApplicationDeTest _application = new ApplicationDeTest();

        private static DemandeDeNumeroDto UneDemande(string succursale = "45400",
            string demandeur = "employee.limoilou") => new DemandeDeNumeroDto
            {
                SystemeAppelant = "12",
                Succursale = succursale,
                IdDemandeur = demandeur
            };

        [Fact]
        public async Task Get_RendUneListeVideSurUneBaseNeuve()
        {
            //Given a database nobody has used yet
            HttpClient client = _application.CreateClient();

            //Then
            HttpResponseMessage reponse = await client.GetAsync("/api/Numeros");
            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
            Assert.Empty((await reponse.Content.ReadFromJsonAsync<List<NumeroDossierDto>>())!);
        }

        [Fact]
        public async Task Post_AttribueUnNumeroEtLeRendDansLaListe()
        {
            //Given a complete request
            HttpClient client = _application.CreateClient();

            //When
            HttpResponseMessage reponse = await client.PostAsJsonAsync("/api/Numeros", UneDemande());

            //Then the number is created, and the list carries it
            Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
            NumeroDossierDto? attribue = await reponse.Content.ReadFromJsonAsync<NumeroDossierDto>();
            Assert.Equal("New", attribue?.Statut);
            Assert.Equal("employee.limoilou", attribue?.IdDemandeur);

            List<NumeroDossierDto> liste = (await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!;
            Assert.Equal(attribue!.NumeroCompte, Assert.Single(liste).NumeroCompte);
        }

        [Fact]
        public async Task Post_LeNumeroSuitLaFormeAttendue()
        {
            //Given a request for the Limoilou branch
            HttpClient client = _application.CreateClient();

            //When
            NumeroDossierDto? attribue = await (await client.PostAsJsonAsync("/api/Numeros", UneDemande("45401")))
                .Content.ReadFromJsonAsync<NumeroDossierDto>();

            //Then sixteen digits in four slices, the last two even
            string[] tranches = attribue!.NumeroCompte.Split('-');
            Assert.Equal("145-12-45401", string.Join('-', tranches[..3]));
            Assert.Equal(6, tranches[3].Length);
            Assert.Equal(0, int.Parse(tranches[3][^2..], System.Globalization.CultureInfo.InvariantCulture) % 2);
        }

        [Fact]
        public async Task Post_CentDemandesDonnentCentNumerosDifferents()
        {
            //Given a hundred requests in a row
            HttpClient client = _application.CreateClient();

            for (int demande = 0; demande < 100; demande++)
            {
                await client.PostAsJsonAsync("/api/Numeros", UneDemande());
            }

            //Then no number was given out twice
            List<NumeroDossierDto> liste = (await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!;
            Assert.Equal(100, liste.Count);
            Assert.Equal(100, liste.Select(n => n.NumeroCompte).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public async Task Get_RendLePlusRecentEnPremier()
        {
            //Given three numbers given out one after the other
            HttpClient client = _application.CreateClient();
            var attribues = new List<string>();

            for (int demande = 0; demande < 3; demande++)
            {
                NumeroDossierDto? numero = await (await client.PostAsJsonAsync("/api/Numeros", UneDemande()))
                    .Content.ReadFromJsonAsync<NumeroDossierDto>();
                attribues.Add(numero!.NumeroCompte);
            }

            //Then the list answers them in reverse order
            List<NumeroDossierDto> liste = (await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!;
            attribues.Reverse();
            Assert.Equal(attribues, liste.Select(n => n.NumeroCompte));
        }

        [Theory]
        [InlineData("1", "45400", "employee")]
        [InlineData("12", "4540", "employee")]
        [InlineData("12", "45400", "")]
        [InlineData("ab", "45400", "employee")]
        public async Task Post_RefuseUneDemandeMalFormee(string systeme, string succursale, string demandeur)
        {
            //Given a request outside the format
            HttpClient client = _application.CreateClient();

            //When
            HttpResponseMessage reponse = await client.PostAsJsonAsync("/api/Numeros", new DemandeDeNumeroDto
            {
                SystemeAppelant = systeme,
                Succursale = succursale,
                IdDemandeur = demandeur
            });

            //Then it is rejected before it reaches the service, and
            //nothing is recorded
            Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<List<NumeroDossierDto>>("/api/Numeros"))!);
        }

        [Fact]
        public async Task Post_RefuseUnCorpsAbsent()
        {
            //Given a request with no body
            HttpClient client = _application.CreateClient();

            //Then
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
