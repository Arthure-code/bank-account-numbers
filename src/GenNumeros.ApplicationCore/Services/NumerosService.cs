using GenNumeros.ApplicationCore.Entites;
using GenNumeros.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GenNumeros.ApplicationCore.Services
{
    public class NumerosService : INumerosService
    {
        public const string StatutALaCreation = "New";

        // The bank number can change: it is read from the configuration,
        // and stays 145 until somebody changes it.
        private const string BanqueParDefaut = "145";

        // A draw can land on a number already given out. After this many
        // attempts, answering that nothing was attributed beats spinning
        // forever.
        private const int EssaisMaximum = 50;

        private readonly IAsyncRepository<NumeroDossier> _numeros;
        private readonly IConfiguration _configuration;
        private readonly Random _hasard;

        public NumerosService(IAsyncRepository<NumeroDossier> numeros, IConfiguration configuration)
            : this(numeros, configuration, Random.Shared)
        {
        }

        // A known draw makes the rule verifiable.
        public NumerosService(IAsyncRepository<NumeroDossier> numeros, IConfiguration configuration, Random hasard)
        {
            _numeros = numeros;
            _configuration = configuration;
            _hasard = hasard;
        }

        public async Task<IEnumerable<NumeroDossier>> ObtenirTousLesNumeros()
        {
            IEnumerable<NumeroDossier> numeros = await _numeros.ListAsync();

            return numeros.OrderByDescending(n => n.DateCreation).ThenByDescending(n => n.Id);
        }

        public async Task<NumeroDossier?> GenererUnNumero(string systemeAppelant, string succursale, string idDemandeur)
        {
            if (!NumeroDeCompte.EstUneTranche(systemeAppelant, NumeroDeCompte.LongueurDuSystemeAppelant)
                || !NumeroDeCompte.EstUneTranche(succursale, NumeroDeCompte.LongueurDeLaSuccursale)
                || string.IsNullOrWhiteSpace(idDemandeur))
            {
                return null;
            }

            string banque = _configuration["Banque:Numero"] ?? BanqueParDefaut;

            string? numeroCompte = await TirerUnNumeroLibre(banque, systemeAppelant, succursale);
            if (numeroCompte == null)
            {
                return null;
            }

            var dossier = new NumeroDossier
            {
                NumeroCompte = numeroCompte,
                IdDemandeur = idDemandeur.Trim(),
                Statut = StatutALaCreation,
                DateCreation = DateTime.Now
            };

            await _numeros.AddAsync(dossier);

            return dossier;
        }

        private async Task<string?> TirerUnNumeroLibre(string banque, string systemeAppelant, string succursale)
        {
            IEnumerable<NumeroDossier> dejaAttribues = await _numeros.ListAsync();
            HashSet<string> pris = dejaAttribues
                .Select(n => n.NumeroCompte)
                .Where(n => n != null)
                .ToHashSet(StringComparer.Ordinal);

            for (int essai = 0; essai < EssaisMaximum; essai++)
            {
                string candidat = NumeroDeCompte.Assembler(banque, systemeAppelant, succursale,
                    NumeroDeCompte.TirerUnCompte(_hasard));

                if (pris.Add(candidat))
                {
                    return candidat;
                }
            }

            return null;
        }
    }
}
