using AccountNumbers.ApplicationCore.Entities;
using AccountNumbers.ApplicationCore.Interfaces;
using AccountNumbers.ApplicationCore.Services;
using AccountNumbers.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AccountNumbers.Infrastructure.IntegrationTests
{
    public class AccountNumberServiceTest : IDisposable
    {
        private readonly FreshDatabase _database = new FreshDatabase();
        private readonly AsyncRepository<AccountNumber> _numeros;

        public AccountNumberServiceTest()
        {
            _numeros = new AsyncRepository<AccountNumber>(_database.Context);
        }

        // A draw that always answers the same thing: enough to force the
        // collision that chance would almost never produce.
        private sealed class FrozenDraw : Random
        {
            private readonly int _valeur;

            public FrozenDraw(int value) => _valeur = value;

            public override int Next(int minValue, int maxValue) => _valeur % maxValue;
        }

        private static IConfiguration Configuration(string? bank = null)
        {
            var valeurs = new Dictionary<string, string?>();
            if (bank != null)
            {
                valeurs["Bank:Number"] = bank;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(valeurs).Build();
        }

        private AccountNumberService Service(string? bank = null, Random? tirage = null)
        {
            return tirage == null
                ? new AccountNumberService(_numeros, Configuration(bank))
                : new AccountNumberService(_numeros, Configuration(bank), tirage);
        }

        [Fact]
        public async Task GenerateAsync_WritesASixteenDigitNumberInFourSlices()
        {
            //Given a complete request
            AccountNumberService service = Service();

            //When
            AccountNumber? given = await service.GenerateAsync("12", "45400", "employee.limoilou");
            _database.Forget();

            //Then the number follows the expected shape and reaches the database
            Assert.NotNull(given);
            string[] slices = given!.Number.Split('-');
            Assert.Equal("3-2-5-6", string.Join('-', slices.Select(t => t.Length)));
            Assert.Equal(16, given.Number.Replace("-", string.Empty, StringComparison.Ordinal).Length);
            Assert.Equal("145", slices[0]);
            Assert.Equal("12", slices[1]);
            Assert.Equal("45400", slices[2]);
            Assert.Single(await _numeros.ListAsync());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(1234)]
        public async Task GenerateAsync_TheLastTwoDigitsFormAnEvenNumber(int seed)
        {
            //Given any draw at all
            AccountNumberService service = Service(tirage: new Random(seed));

            //When twenty numbers are given out
            for (int request = 0; request < 20; request++)
            {
                AccountNumber? given = await service.GenerateAsync("12", "45400", "employee.limoilou");

                //Then each one ends with an even number
                Assert.NotNull(given);
                Assert.True(AccountNumberFormat.EndsWithAnEvenNumber(given!.Number),
                    given.Number);
            }
        }

        [Fact]
        public async Task GenerateAsync_SetsTheNewStatusAndTodaysDate()
        {
            //Given a request
            AccountNumberService service = Service();

            //When
            AccountNumber? given = await service.GenerateAsync("12", "45400", "  employee.limoilou  ");

            //Then
            Assert.Equal("New", given?.Status);
            Assert.Equal(DateTime.Today, given?.AttributedOn.Date);
            Assert.Equal("employee.limoilou", given?.RequestedBy);
        }

        [Fact]
        public async Task GenerateAsync_TheBankNumberComesFromTheConfiguration()
        {
            //Given a bank that changed its number
            AccountNumberService service = Service(bank: "200");

            //When
            AccountNumber? given = await service.GenerateAsync("12", "45400", "employee.limoilou");

            //Then
            Assert.StartsWith("200-12-45400-", given!.Number, StringComparison.Ordinal);
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
        public async Task GenerateAsync_RejectsWhatIsOutsideTheFormat(string systeme, string branch, string requester)
        {
            //Given a malformed request
            AccountNumberService service = Service();

            //Then nothing is given out, and nothing is written
            Assert.Null(await service.GenerateAsync(systeme, branch, requester));
            Assert.Empty(await _numeros.ListAsync());
        }

        [Fact]
        public async Task GenerateAsync_NeverGivesOutTheSameNumberTwice()
        {
            //Given a draw that always answers the same number
            AccountNumberService service = Service(tirage: new FrozenDraw(42));
            AccountNumber? premier = await service.GenerateAsync("12", "45400", "employee.limoilou");
            _database.Forget();

            //When a second request arrives
            AccountNumber? second = await service.GenerateAsync("12", "45400", "employee.beauport");

            //Then the service would rather give out nothing than hand out
            //the same number twice
            Assert.NotNull(premier);
            Assert.Null(second);
            Assert.Single(await _numeros.ListAsync());
        }

        [Fact]
        public async Task TheDatabaseItselfRefusesANumberAlreadyGivenOut()
        {
            //Given a number already in the database
            await _numeros.AddAsync(new AccountNumber
            {
                Number = "145-12-45400-123456",
                RequestedBy = "employee.limoilou",
                Status = "New",
                AttributedOn = DateTime.Now
            });
            _database.Forget();

            //When a second file claims the same number
            Task Ecrire() => _numeros.AddAsync(new AccountNumber
            {
                Number = "145-12-45400-123456",
                RequestedBy = "employee.beauport",
                Status = "New",
                AttributedOn = DateTime.Now
            });

            //Then the unique index refuses it, without relying on the service
            await Assert.ThrowsAsync<DbUpdateException>(Ecrire);
        }

        [Fact]
        public async Task GetAllAsync_AnswersTheMostRecentFirst()
        {
            //Given three numbers given out at three moments
            foreach ((string number, int jour) in new[] { ("first", 5), ("last", 7), ("second", 6) })
            {
                await _numeros.AddAsync(new AccountNumber
                {
                    Number = number,
                    RequestedBy = "employee.limoilou",
                    Status = "New",
                    AttributedOn = new DateTime(2026, 1, jour)
                });
            }

            _database.Forget();

            //When
            IEnumerable<AccountNumber> numbers = await Service().GetAllAsync();

            //Then
            Assert.Equal("last, second, first", string.Join(", ", numbers.Select(n => n.Number)));
        }

        [Fact]
        public async Task GetAllAsync_AnswersEmptyWhenNothingWasGivenOut()
        {
            Assert.Empty(await Service().GetAllAsync());
        }

        public void Dispose()
        {
            _database.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
