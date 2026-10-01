using System.Net.Http.Json;
using AccountNumbers.Web.Interfaces;
using AccountNumbers.Web.Models;

namespace AccountNumbers.Web.Services
{
    public class AccountNumberProxy : IAccountNumberProxy
    {
        private const string Endpoint = "api/AccountNumbers";

        private readonly HttpClient _client;

        public AccountNumberProxy(HttpClient client)
        {
            _client = client;
        }

        public async Task<List<AccountNumber>> GetAllAsync()
        {
            // An empty list beats nothing at all: the caller is not left
            // wondering what to do with an absence.
            return await _client.GetFromJsonAsync<List<AccountNumber>>(Endpoint) ?? new List<AccountNumber>();
        }

        public async Task<AccountNumber?> AskForOneAsync(NumberRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            HttpResponseMessage response = await _client.PostAsJsonAsync(Endpoint, new
            {
                request.CallingSystem,
                request.Branch,
                request.RequestedBy
            });

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AccountNumber>();
        }
    }
}
