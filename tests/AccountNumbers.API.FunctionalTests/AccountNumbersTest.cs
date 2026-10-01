using System.Net;
using System.Net.Http.Json;
using AccountNumbers.ApplicationCore.DTOs;

namespace AccountNumbers.API.FunctionalTests
{
    public class AccountNumbersTest : IDisposable
    {
        private readonly TestApplication _application = new TestApplication();

        private static NumberRequestDto ARequest(string branch = "45400",
            string requester = "employee.limoilou") => new NumberRequestDto
            {
                CallingSystem = "12",
                Branch = branch,
                RequestedBy = requester
            };

        [Fact]
        public async Task Get_AnswersAnEmptyListOnAFreshDatabase()
        {
            //Given a database nobody has used yet
            HttpClient client = _application.CreateClient();

            //Then
            HttpResponseMessage response = await client.GetAsync("/api/AccountNumbers");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Empty((await response.Content.ReadFromJsonAsync<List<AccountNumberDto>>())!);
        }

        [Fact]
        public async Task Post_GivesOutANumberAndListsIt()
        {
            //Given a complete request
            HttpClient client = _application.CreateClient();

            //When
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/AccountNumbers", ARequest());

            //Then the number is created, and the list carries it
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            AccountNumberDto? given = await response.Content.ReadFromJsonAsync<AccountNumberDto>();
            Assert.Equal("New", given?.Status);
            Assert.Equal("employee.limoilou", given?.RequestedBy);

            List<AccountNumberDto> list = (await client.GetFromJsonAsync<List<AccountNumberDto>>("/api/AccountNumbers"))!;
            Assert.Equal(given!.Number, Assert.Single(list).Number);
        }

        [Fact]
        public async Task Post_TheNumberFollowsTheExpectedShape()
        {
            //Given a request for the Limoilou branch
            HttpClient client = _application.CreateClient();

            //When
            AccountNumberDto? given = await (await client.PostAsJsonAsync("/api/AccountNumbers", ARequest("45401")))
                .Content.ReadFromJsonAsync<AccountNumberDto>();

            //Then sixteen digits in four slices, the last two even
            string[] slices = given!.Number.Split('-');
            Assert.Equal("145-12-45401", string.Join('-', slices[..3]));
            Assert.Equal(6, slices[3].Length);
            Assert.Equal(0, int.Parse(slices[3][^2..], System.Globalization.CultureInfo.InvariantCulture) % 2);
        }

        [Fact]
        public async Task Post_AHundredRequestsGiveAHundredDifferentNumbers()
        {
            //Given a hundred requests in a row
            HttpClient client = _application.CreateClient();

            for (int request = 0; request < 100; request++)
            {
                await client.PostAsJsonAsync("/api/AccountNumbers", ARequest());
            }

            //Then no number was given out twice
            List<AccountNumberDto> list = (await client.GetFromJsonAsync<List<AccountNumberDto>>("/api/AccountNumbers"))!;
            Assert.Equal(100, list.Count);
            Assert.Equal(100, list.Select(n => n.Number).Distinct(StringComparer.Ordinal).Count());
        }

        [Fact]
        public async Task Get_AnswersTheMostRecentFirst()
        {
            //Given three numbers given out one after the other
            HttpClient client = _application.CreateClient();
            var given = new List<string>();

            for (int request = 0; request < 3; request++)
            {
                AccountNumberDto? number = await (await client.PostAsJsonAsync("/api/AccountNumbers", ARequest()))
                    .Content.ReadFromJsonAsync<AccountNumberDto>();
                given.Add(number!.Number);
            }

            //Then the list answers them in reverse order
            List<AccountNumberDto> list = (await client.GetFromJsonAsync<List<AccountNumberDto>>("/api/AccountNumbers"))!;
            given.Reverse();
            Assert.Equal(given, list.Select(n => n.Number));
        }

        [Theory]
        [InlineData("1", "45400", "employee")]
        [InlineData("12", "4540", "employee")]
        [InlineData("12", "45400", "")]
        [InlineData("ab", "45400", "employee")]
        public async Task Post_RejectsAMalformedRequest(string systeme, string branch, string requester)
        {
            //Given a request outside the format
            HttpClient client = _application.CreateClient();

            //When
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/AccountNumbers", new NumberRequestDto
            {
                CallingSystem = systeme,
                Branch = branch,
                RequestedBy = requester
            });

            //Then it is rejected before it reaches the service, and
            //nothing is recorded
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Empty((await client.GetFromJsonAsync<List<AccountNumberDto>>("/api/AccountNumbers"))!);
        }

        [Fact]
        public async Task Post_RejectsAMissingBody()
        {
            //Given a request with no body
            HttpClient client = _application.CreateClient();

            //Then
            HttpResponseMessage response = await client.PostAsJsonAsync<NumberRequestDto?>("/api/AccountNumbers", null);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        public void Dispose()
        {
            _application.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
