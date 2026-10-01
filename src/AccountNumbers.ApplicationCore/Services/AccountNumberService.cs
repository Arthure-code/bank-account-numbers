using AccountNumbers.ApplicationCore.Entities;
using AccountNumbers.ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AccountNumbers.ApplicationCore.Services
{
    public class AccountNumberService : IAccountNumberService
    {
        public const string StatusAtCreation = "New";

        // The bank number can change: it is read from the configuration,
        // and stays 145 until somebody changes it.
        private const string DefaultBankNumber = "145";

        // A draw can land on a number already given out. After this many
        // attempts, answering that nothing was attributed beats spinning
        // forever.
        private const int MaximumAttempts = 50;

        private readonly IAsyncRepository<AccountNumber> _numeros;
        private readonly IConfiguration _configuration;
        private readonly Random _hasard;

        public AccountNumberService(IAsyncRepository<AccountNumber> numbers, IConfiguration configuration)
            : this(numbers, configuration, Random.Shared)
        {
        }

        // A known draw makes the rule verifiable.
        public AccountNumberService(IAsyncRepository<AccountNumber> numbers, IConfiguration configuration, Random random)
        {
            _numeros = numbers;
            _configuration = configuration;
            _hasard = random;
        }

        public async Task<IEnumerable<AccountNumber>> GetAllAsync()
        {
            IEnumerable<AccountNumber> numbers = await _numeros.ListAsync();

            return numbers.OrderByDescending(n => n.AttributedOn).ThenByDescending(n => n.Id);
        }

        public async Task<AccountNumber?> GenerateAsync(string callingSystem, string branch, string requestedBy)
        {
            if (!AccountNumberFormat.IsSlice(callingSystem, AccountNumberFormat.CallingSystemLength)
                || !AccountNumberFormat.IsSlice(branch, AccountNumberFormat.BranchLength)
                || string.IsNullOrWhiteSpace(requestedBy))
            {
                return null;
            }

            string bank = _configuration["Bank:Number"] ?? DefaultBankNumber;

            string? number = await DrawFreeNumber(bank, callingSystem, branch);
            if (number == null)
            {
                return null;
            }

            var file = new AccountNumber
            {
                Number = number,
                RequestedBy = requestedBy.Trim(),
                Status = StatusAtCreation,
                AttributedOn = DateTime.Now
            };

            await _numeros.AddAsync(file);

            return file;
        }

        private async Task<string?> DrawFreeNumber(string bank, string callingSystem, string branch)
        {
            IEnumerable<AccountNumber> alreadyGiven = await _numeros.ListAsync();
            HashSet<string> taken = alreadyGiven
                .Select(n => n.Number)
                .Where(n => n != null)
                .ToHashSet(StringComparer.Ordinal);

            for (int attempt = 0; attempt < MaximumAttempts; attempt++)
            {
                string candidate = AccountNumberFormat.Compose(bank, callingSystem, branch,
                    AccountNumberFormat.DrawAccount(_hasard));

                if (taken.Add(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
