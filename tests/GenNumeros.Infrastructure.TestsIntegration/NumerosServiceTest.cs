using GenNumeros.ApplicationCore.Entites;
using GenNumeros.ApplicationCore.Interfaces;
using GenNumeros.ApplicationCore.Services;
using GenNumeros.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GenNumeros.Infrastructure.TestsIntegration
{
    public class NumerosServiceTest : IDisposable
    {
        private readonly BaseNeuve _base = new BaseNeuve();
        private readonly AsyncRepository<NumeroDossier> _numeros;

        public NumerosServiceTest()
        {
            _numeros = new AsyncRepository<NumeroDossier>(_base.Context);
        }

        // A draw that always answers the same thing: enough to force the
        // collision that chance would almost never produce.
        private sealed class TirageFige : Random
        {
            private readonly int _valeur;

            public TirageFige(int valeur) => _valeur = valeur;

            public override int Next(int minValue, int maxValue) => _valeur % maxValue;
        }

        private static IConfiguration Configuration(string? banque = null)
        {
            var valeurs = new Dictionary<string, string?>();
            if (banque != null)
            {
                valeurs["Banque:Numero"] = banque;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valeurs).Build();
        }

        private NumerosService Service(string? banque = null, Random? tirage = null)
        {
            return tirage == null
                ? new NumerosService(_numeros, Configuration(banque))
                : new NumerosService(_numeros, Configuration(banque), tirage);
        }

        [Fact]
        public async Task GenererUnNumero_EcritUnNumeroDeSeizeChiffresEnQuatreTranches()
        {
            //Given a complete request
            NumerosService service = Service();

            //When
            NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "employee.limoilou");
            _base.Oublier();

            //Then the number follows the expected shape and reaches the database
            Assert.NotNull(attribue);
            string[] tranches = attribue!.NumeroCompte.Split('-');
            Assert.Equal("3-2-5-6", string.Join('-', tranches.Select(t => t.Length)));
            Assert.Equal(16, attribue.NumeroCompte.Replace("-", string.Empty, StringComparison.Ordinal).Length);
            Assert.Equal("145", tranches[0]);
            Assert.Equal("12", tranches[1]);
            Assert.Equal("45400", tranches[2]);
            Assert.Single(await _numeros.ListAsync());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(1234)]
        public async Task GenererUnNumero_LesDeuxDerniersChiffresFormentUnNombrePair(int graine)
        {
            //Given any draw at all
            NumerosService service = Service(tirage: new Random(graine));

            //When twenty numbers are given out
            for (int demande = 0; demande < 20; demande++)
            {
                NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "employee.limoilou");

                //Then each one ends with an even number
                Assert.NotNull(attribue);
                Assert.True(NumeroDeCompte.SeTermineParUnNombrePair(attribue!.NumeroCompte),
                    attribue.NumeroCompte);
            }
        }

        [Fact]
        public async Task GenererUnNumero_PoseLeStatutNeufEtLaDateDuJour()
        {
            //Given a request
            NumerosService service = Service();

            //When
            NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "  employee.limoilou  ");

            //Then
            Assert.Equal("New", attribue?.Statut);
            Assert.Equal(DateTime.Today, attribue?.DateCreation.Date);
            Assert.Equal("employee.limoilou", attribue?.IdDemandeur);
        }

        [Fact]
        public async Task GenererUnNumero_LeNumeroDeLaBanqueVientDeLaConfiguration()
        {
            //Given a bank that changed its number
            NumerosService service = Service(banque: "200");

            //When
            NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "employee.limoilou");

            //Then
            Assert.StartsWith("200-12-45400-", attribue!.NumeroCompte, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("1", "45400", "employee")]
        [InlineData("123", "45400", "employee")]
        [InlineData("1a", "45400", "employee")]
        [InlineData("12", "4540", "employee")]
        [InlineData("12", "454000", "employee")]
        [InlineData("12", "4540a", "employee")]
        [InlineData("12", "45400", "")]
        [InlineData("12", "45400", "   ")]
        public async Task GenererUnNumero_RefuseCeQuiNEstPasAuFormat(string systeme, string succursale, string demandeur)
        {
            //Given a malformed request
            NumerosService service = Service();

            //Then nothing is given out, and nothing is written
            Assert.Null(await service.GenererUnNumero(systeme, succursale, demandeur));
            Assert.Empty(await _numeros.ListAsync());
        }

        [Fact]
        public async Task GenererUnNumero_NAttribueJamaisDeuxFoisLeMemeNumero()
        {
            //Given a draw that always answers the same number
            NumerosService service = Service(tirage: new TirageFige(42));
            NumeroDossier? premier = await service.GenererUnNumero("12", "45400", "employee.limoilou");
            _base.Oublier();

            //When a second request arrives
            NumeroDossier? second = await service.GenererUnNumero("12", "45400", "employee.beauport");

            //Then the service would rather give out nothing than hand out
            //the same number twice
            Assert.NotNull(premier);
            Assert.Null(second);
            Assert.Single(await _numeros.ListAsync());
        }

        [Fact]
        public async Task LaBaseRefuseElleMemeUnNumeroDejaAttribue()
        {
            //Given a number already in the database
            await _numeros.AddAsync(new NumeroDossier
            {
                NumeroCompte = "145-12-45400-123456",
                IdDemandeur = "employee.limoilou",
                Statut = "New",
                DateCreation = DateTime.Now
            });
            _base.Oublier();

            //When a second file claims the same number
            Task Ecrire() => _numeros.AddAsync(new NumeroDossier
            {
                NumeroCompte = "145-12-45400-123456",
                IdDemandeur = "employee.beauport",
                Statut = "New",
                DateCreation = DateTime.Now
            });

            //Then the unique index refuses it, without relying on the service
            await Assert.ThrowsAsync<DbUpdateException>(Ecrire);
        }

        [Fact]
        public async Task ObtenirTousLesNumeros_RendLePlusRecentEnPremier()
        {
            //Given three numbers given out at three moments
            foreach ((string numero, int jour) in new[] { ("first", 5), ("last", 7), ("second", 6) })
            {
                await _numeros.AddAsync(new NumeroDossier
                {
                    NumeroCompte = numero,
                    IdDemandeur = "employee.limoilou",
                    Statut = "New",
                    DateCreation = new DateTime(2026, 1, jour)
                });
            }

            _base.Oublier();

            //When
            IEnumerable<NumeroDossier> numeros = await Service().ObtenirTousLesNumeros();

            //Then
            Assert.Equal("last, second, first", string.Join(", ", numeros.Select(n => n.NumeroCompte)));
        }

        [Fact]
        public async Task ObtenirTousLesNumeros_RendVideQuandRienNAEteAttribue()
        {
            Assert.Empty(await Service().ObtenirTousLesNumeros());
        }

        public void Dispose()
        {
            _base.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
