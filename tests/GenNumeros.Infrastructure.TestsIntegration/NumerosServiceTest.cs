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

        // Un tirage qui rend toujours la meme chose : de quoi forcer la
        // collision que le hasard ne produirait presque jamais.
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
            //Etant donne une demande complete
            NumerosService service = Service();

            //Lorsque
            NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "employe.limoilou");
            _base.Oublier();

            //Alors le numero suit la forme attendue et arrive en base
            Assert.NotNull(attribue);
            string[] tranches = attribue!.NumeroCompte.Split('-');
            Assert.Equal(new[] { 3, 2, 5, 6 }, tranches.Select(t => t.Length));
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
            //Etant donne un tirage quelconque
            NumerosService service = Service(tirage: new Random(graine));

            //Lorsque vingt numeros sont attribues
            for (int demande = 0; demande < 20; demande++)
            {
                NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "employe.limoilou");

                //Alors chacun se termine par un nombre pair
                Assert.NotNull(attribue);
                Assert.True(NumeroDeCompte.SeTermineParUnNombrePair(attribue!.NumeroCompte),
                    attribue.NumeroCompte);
            }
        }

        [Fact]
        public async Task GenererUnNumero_PoseLeStatutNeufEtLaDateDuJour()
        {
            //Etant donne une demande
            NumerosService service = Service();

            //Lorsque
            NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "  employe.limoilou  ");

            //Alors
            Assert.Equal("Nouveau", attribue?.Statut);
            Assert.Equal(DateTime.Today, attribue?.DateCreation.Date);
            Assert.Equal("employe.limoilou", attribue?.IdDemandeur);
        }

        [Fact]
        public async Task GenererUnNumero_LeNumeroDeLaBanqueVientDeLaConfiguration()
        {
            //Etant donne une banque qui a change de numero
            NumerosService service = Service(banque: "200");

            //Lorsque
            NumeroDossier? attribue = await service.GenererUnNumero("12", "45400", "employe.limoilou");

            //Alors
            Assert.StartsWith("200-12-45400-", attribue!.NumeroCompte, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("1", "45400", "employe")]
        [InlineData("123", "45400", "employe")]
        [InlineData("1a", "45400", "employe")]
        [InlineData("12", "4540", "employe")]
        [InlineData("12", "454000", "employe")]
        [InlineData("12", "4540a", "employe")]
        [InlineData("12", "45400", "")]
        [InlineData("12", "45400", "   ")]
        public async Task GenererUnNumero_RefuseCeQuiNEstPasAuFormat(string systeme, string succursale, string demandeur)
        {
            //Etant donne une demande mal formee
            NumerosService service = Service();

            //Alors rien n'est attribue, et rien n'est ecrit
            Assert.Null(await service.GenererUnNumero(systeme, succursale, demandeur));
            Assert.Empty(await _numeros.ListAsync());
        }

        [Fact]
        public async Task GenererUnNumero_NAttribueJamaisDeuxFoisLeMemeNumero()
        {
            //Etant donne un tirage qui rend toujours le meme numero
            NumerosService service = Service(tirage: new TirageFige(42));
            NumeroDossier? premier = await service.GenererUnNumero("12", "45400", "employe.limoilou");
            _base.Oublier();

            //Lorsqu'une deuxieme demande arrive
            NumeroDossier? second = await service.GenererUnNumero("12", "45400", "employe.beauport");

            //Alors la bibliotheque prefere ne rien attribuer plutot que de
            //donner deux fois le meme numero
            Assert.NotNull(premier);
            Assert.Null(second);
            Assert.Single(await _numeros.ListAsync());
        }

        [Fact]
        public async Task LaBaseRefuseElleMemeUnNumeroDejaAttribue()
        {
            //Etant donne un numero deja en base
            await _numeros.AddAsync(new NumeroDossier
            {
                NumeroCompte = "145-12-45400-123456",
                IdDemandeur = "employe.limoilou",
                Statut = "Nouveau",
                DateCreation = DateTime.Now
            });
            _base.Oublier();

            //Lorsqu'un deuxieme dossier pretend porter le meme numero
            Task Ecrire() => _numeros.AddAsync(new NumeroDossier
            {
                NumeroCompte = "145-12-45400-123456",
                IdDemandeur = "employe.beauport",
                Statut = "Nouveau",
                DateCreation = DateTime.Now
            });

            //Alors l'index unique de la base refuse, sans compter sur le service
            await Assert.ThrowsAsync<DbUpdateException>(Ecrire);
        }

        [Fact]
        public async Task ObtenirTousLesNumeros_RendLePlusRecentEnPremier()
        {
            //Etant donne trois numeros attribues a trois moments
            foreach ((string numero, int jour) in new[] { ("premier", 5), ("dernier", 7), ("deuxieme", 6) })
            {
                await _numeros.AddAsync(new NumeroDossier
                {
                    NumeroCompte = numero,
                    IdDemandeur = "employe.limoilou",
                    Statut = "Nouveau",
                    DateCreation = new DateTime(2026, 1, jour)
                });
            }

            _base.Oublier();

            //Lorsque
            IEnumerable<NumeroDossier> numeros = await Service().ObtenirTousLesNumeros();

            //Alors
            Assert.Equal(new[] { "dernier", "deuxieme", "premier" }, numeros.Select(n => n.NumeroCompte));
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
