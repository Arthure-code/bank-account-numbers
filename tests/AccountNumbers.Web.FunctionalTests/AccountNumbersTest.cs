using System.Net;
using System.Text.RegularExpressions;
using AccountNumbers.Web.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace AccountNumbers.Web.FunctionalTests
{
    public class AccountNumbersTest : IDisposable
    {
        private readonly TestApplication _application = new TestApplication();

        // Without this, the client follows the redirect and only the
        // landing page is seen, never the answer to the form.
        private HttpClient Browser() => _application.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        private static async Task<string> Token(HttpClient browser, string address)
        {
            string page = await browser.GetStringAsync(address);
            System.Text.RegularExpressions.Match token = Regex.Match(page,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"",
                RegexOptions.None, TimeSpan.FromSeconds(5));

            return token.Groups[1].Value;
        }

        private static AccountNumber ANumber(int identifier, string number, string status, int day)
            => new AccountNumber
            {
                Id = identifier,
                Number = number,
                RequestedBy = "employee.limoilou",
                Status = status,
                AttributedOn = new DateTime(2026, 1, day)
            };

        [Theory]
        [InlineData("/AccountNumbers")]
        [InlineData("/AccountNumbers/Ask")]
        public async Task ThePagesAnswer(string address)
        {
            HttpClient browser = Browser();

            Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync(address)).StatusCode);
        }

        [Fact]
        public async Task Index_ShowsOnlyTheNewNumbersFromTheMostRecentToTheOldest()
        {
            //Given two new numbers and one already in use
            _application.Api.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<AccountNumber>
            {
                ANumber(1, "145-12-45400-111110", "New", 5),
                ANumber(2, "145-12-45401-222220", "Used", 6),
                ANumber(3, "145-12-45402-333330", "New", 7)
            });
            HttpClient browser = Browser();

            //When
            string page = await browser.GetStringAsync("/AccountNumbers");

            //Then the used number is absent, and the most recent one
            //comes first
            Assert.DoesNotContain("222220", page, StringComparison.Ordinal);
            Assert.True(page.IndexOf("333330", StringComparison.Ordinal)
                < page.IndexOf("111110", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Ask_TheDropDownCarriesTheBranchesFromTheFile()
        {
            //Given the three branches from the configuration
            HttpClient browser = Browser();

            //When
            string page = await browser.GetStringAsync("/AccountNumbers/Ask");

            //Then
            Assert.Contains("value=\"45400\"", page, StringComparison.Ordinal);
            Assert.Contains("value=\"45401\"", page, StringComparison.Ordinal);
            Assert.Contains("value=\"45402\"", page, StringComparison.Ordinal);
            Assert.Contains("Lebourneuf", page, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Ask_SendsTheRequestAndComesBackToTheList()
        {
            //Given an API that gives out a number
            NumberRequest? sent = null;
            _application.Api.Setup(p => p.AskForOneAsync(It.IsAny<NumberRequest>()))
                .Callback<NumberRequest>(d => sent = d)
                .ReturnsAsync(ANumber(1, "145-12-45400-123456", "New", 5));
            HttpClient browser = Browser();
            string token = await Token(browser, "/AccountNumbers/Ask");

            //When the form is sent
            HttpResponseMessage response = await browser.PostAsync("/AccountNumbers/Ask",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["Branch"] = "45402",
                    ["RequestedBy"] = "marie.tremblay"
                }));

            //Then the application adds its own calling system number, and
            //sends the visitor back to the list
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("/AccountNumbers", response.Headers.Location?.OriginalString);
            Assert.Equal("12", sent?.CallingSystem);
            Assert.Equal("45402", sent?.Branch);
            Assert.Equal("marie.tremblay", sent?.RequestedBy);
        }

        [Fact]
        public async Task Ask_AMissingIdentifierComesBackWithItsMessage()
        {
            //Given a form with no identifier
            HttpClient browser = Browser();
            string token = await Token(browser, "/AccountNumbers/Ask");

            //When
            HttpResponseMessage response = await browser.PostAsync("/AccountNumbers/Ask",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["Branch"] = "45402",
                    ["RequestedBy"] = string.Empty
                }));

            //Then the page comes back saying so, and nothing is asked for
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("identifier is required", await response.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
            _application.Api.Verify(p => p.AskForOneAsync(It.IsAny<NumberRequest>()), Times.Never);
        }

        [Fact]
        public async Task Ask_AMissingBranchComesBackWithItsMessage()
        {
            //Given a form where no branch is chosen
            HttpClient browser = Browser();
            string token = await Token(browser, "/AccountNumbers/Ask");

            //When
            HttpResponseMessage response = await browser.PostAsync("/AccountNumbers/Ask",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["Branch"] = string.Empty,
                    ["RequestedBy"] = "marie.tremblay"
                }));

            //Then
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Choose a branch", await response.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }

        [Fact]
        public async Task Ask_ExplainsWhenTheApiGivesOutNothing()
        {
            //Given an API that answers no number
            _application.Api.Setup(p => p.AskForOneAsync(It.IsAny<NumberRequest>()))
                .ReturnsAsync((AccountNumber?)null);
            HttpClient browser = Browser();
            string token = await Token(browser, "/AccountNumbers/Ask");

            //When
            HttpResponseMessage response = await browser.PostAsync("/AccountNumbers/Ask",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["__RequestVerificationToken"] = token,
                    ["Branch"] = "45402",
                    ["RequestedBy"] = "marie.tremblay"
                }));

            //Then the visitor reads why, instead of a blank page
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("could not give out a number", await response.Content.ReadAsStringAsync(),
                StringComparison.Ordinal);
        }

        public void Dispose()
        {
            _application.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
