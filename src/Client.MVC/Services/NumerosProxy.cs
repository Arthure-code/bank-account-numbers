using System.Net.Http.Json;
using Client.MVC.Interfaces;
using Client.MVC.Models;

namespace Client.MVC.Services
{
    public class NumerosProxy : INumerosProxy
    {
        private const string Adresse = "api/Numeros";

        private readonly HttpClient _client;

        public NumerosProxy(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<NumeroDossier>> ObtenirTousLesNumeros()
        {
            // An empty list beats nothing at all: the caller is not left
            // wondering what to do with an absence.
            return await _client.GetFromJsonAsync<List<NumeroDossier>>(Adresse) ?? new List<NumeroDossier>();
        }

        public async Task<NumeroDossier?> DemanderUnNumero(DemandeDeNumero demande)
        {
            ArgumentNullException.ThrowIfNull(demande);

            HttpResponseMessage reponse = await _client.PostAsJsonAsync(Adresse, new
            {
                demande.SystemeAppelant,
                demande.Succursale,
                demande.IdDemandeur
            });

            if (!reponse.IsSuccessStatusCode)
            {
                return null;
            }

            return await reponse.Content.ReadFromJsonAsync<NumeroDossier>();
        }
    }
}
