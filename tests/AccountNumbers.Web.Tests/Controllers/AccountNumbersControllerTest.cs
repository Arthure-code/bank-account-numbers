using AccountNumbers.Web.Controllers;
using AccountNumbers.Web.Interfaces;
using AccountNumbers.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AccountNumbers.Web.Tests.Controllers
{
    public class AccountNumbersControllerTest
    {
        // The framework's own configuration, filled in memory: it is not
        // a dependency of the project, so it is not mocked.
        private static IConfiguration Configuration(params (string Key, string Value)[] values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values.ToDictionary(v => v.Key, v => (string?)v.Value))
                .Build();
        }

        private static IConfiguration ConfigurationWithBranches()
        {
            return Configuration(
                ("CallingSystem", "12"),
                ("Branches:0:Name", "Lebourneuf"),
                ("Branches:0:Number", "45400"),
                ("Branches:1:Name", "Limoilou"),
                ("Branches:1:Number", "45401"));
        }

        private static AccountNumber ANumber(int identifier, string number, string status, DateTime createdOn)
            => new AccountNumber
            {
                Id = identifier,
                Number = number,
                RequestedBy = "employee.limoilou",
                Status = status,
                AttributedOn = createdOn
            };

        [Fact]
        public async Task Index_ShowsOnlyTheNewNumbers()
        {
            //Given one new number and one already in use
            var proxy = new Mock<IAccountNumberProxy>();
            proxy.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<AccountNumber>
            {
                ANumber(1, "145-12-45400-123456", "New", new DateTime(2026, 1, 5)),
                ANumber(2, "145-12-45401-654320", "Used", new DateTime(2026, 1, 6))
            });
            var controller = new AccountNumbersController(ConfigurationWithBranches(), proxy.Object);

            //When
            ActionResult result = await controller.Index();

            //Then
            var shown = Assert.IsAssignableFrom<IEnumerable<AccountNumber>>(
                Assert.IsType<ViewResult>(result).Model);
            Assert.Equal("145-12-45400-123456", Assert.Single(shown).Number);
        }

        [Fact]
        public async Task Index_ShowsTheMostRecentFirst()
        {
            //Given three new numbers given out at three moments
            var proxy = new Mock<IAccountNumberProxy>();
            proxy.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<AccountNumber>
            {
                ANumber(1, "first", "New", new DateTime(2026, 1, 5)),
                ANumber(3, "last", "New", new DateTime(2026, 1, 7)),
                ANumber(2, "second", "New", new DateTime(2026, 1, 6))
            });
            var controller = new AccountNumbersController(ConfigurationWithBranches(), proxy.Object);

            //When
            ActionResult result = await controller.Index();

            //Then
            var shown = Assert.IsAssignableFrom<IEnumerable<AccountNumber>>(
                Assert.IsType<ViewResult>(result).Model).ToList();
            Assert.Equal("last, second, first", string.Join(", ", shown.Select(n => n.Number)));
        }

        [Fact]
        public void Ask_OffersTheBranchesFromTheConfiguration()
        {
            //Given two branches in the configuration file
            var controller = new AccountNumbersController(ConfigurationWithBranches(),
                new Mock<IAccountNumberProxy>().Object);

            //When the page opens
            ActionResult result = controller.Ask();

            //Then the drop-down carries them, number on five digits
            Assert.IsType<ViewResult>(result);
            var branches = Assert.IsType<List<SelectListItem>>(controller.ViewBag.Branches);
            Assert.Equal(2, branches.Count);
            Assert.Equal("45400", branches[0].Value);
            Assert.Contains("Lebourneuf", branches[0].Text, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Ask_AsksForNothingWhenTheFormIsInvalid()
        {
            //Given a missing identifier
            var proxy = new Mock<IAccountNumberProxy>();
            var controller = new AccountNumbersController(ConfigurationWithBranches(), proxy.Object);
            controller.ModelState.AddModelError("RequestedBy", "Votre identifier est requis.");

            //When
            ActionResult result = await controller.Ask(new NumberRequest());

            //Then the page comes back with its branches, and nothing is asked for
            Assert.IsType<ViewResult>(result);
            Assert.NotNull(controller.ViewBag.Branches);
            proxy.Verify(p => p.AskForOneAsync(It.IsAny<NumberRequest>()), Times.Never);
        }

        [Fact]
        public async Task Ask_SetsTheApplicationCallingSystemNumber()
        {
            //Given a request where the visitor filled in only their
            //branch and their identifier
            var proxy = new Mock<IAccountNumberProxy>();
            NumberRequest? sent = null;
            proxy.Setup(p => p.AskForOneAsync(It.IsAny<NumberRequest>()))
                .Callback<NumberRequest>(d => sent = d)
                .ReturnsAsync(ANumber(1, "145-12-45400-123456", "New", DateTime.Now));
            var controller = new AccountNumbersController(ConfigurationWithBranches(), proxy.Object);

            //When
            ActionResult result = await controller.Ask(new NumberRequest
            {
                Branch = "45400",
                RequestedBy = "marie.tremblay"
            });

            //Then the application adds its own number before calling
            Assert.Equal("12", sent?.CallingSystem);
            Assert.Equal("Index", Assert.IsType<RedirectToActionResult>(result).ActionName);
        }

        [Fact]
        public async Task Ask_TheCallingSystemNumberIsReadFromTheConfiguration()
        {
            //Given an application registered under another number
            var proxy = new Mock<IAccountNumberProxy>();
            NumberRequest? sent = null;
            proxy.Setup(p => p.AskForOneAsync(It.IsAny<NumberRequest>()))
                .Callback<NumberRequest>(d => sent = d)
                .ReturnsAsync(ANumber(1, "145-99-45400-123456", "New", DateTime.Now));
            var controller = new AccountNumbersController(
                Configuration(("CallingSystem", "99")), proxy.Object);

            //When
            await controller.Ask(new NumberRequest
            {
                Branch = "45400",
                RequestedBy = "marie.tremblay"
            });

            //Then the one from the configuration is the one that goes
            Assert.Equal("99", sent?.CallingSystem);
        }

        [Fact]
        public async Task Ask_ComesBackToThePageWhenTheApiGivesOutNothing()
        {
            //Given an API that answers no number
            var proxy = new Mock<IAccountNumberProxy>();
            proxy.Setup(p => p.AskForOneAsync(It.IsAny<NumberRequest>()))
                .ReturnsAsync((AccountNumber?)null);
            var controller = new AccountNumbersController(ConfigurationWithBranches(), proxy.Object);

            //When
            ActionResult result = await controller.Ask(new NumberRequest
            {
                Branch = "45400",
                RequestedBy = "marie.tremblay"
            });

            //Then the visitor reads why, instead of a blank page
            Assert.IsType<ViewResult>(result);
            Assert.Contains("could not give out a number", controller.ModelState[string.Empty]!.Errors[0].ErrorMessage,
                StringComparison.Ordinal);
        }

        [Fact]
        public void Ask_SupportsAConfigurationWithoutBranches()
        {
            //Given a configuration file with no list of branches
            var controller = new AccountNumbersController(Configuration(("CallingSystem", "12")),
                new Mock<IAccountNumberProxy>().Object);

            //Then the page still opens, with an empty list
            Assert.IsType<ViewResult>(controller.Ask());
            Assert.Empty(Assert.IsType<List<SelectListItem>>(controller.ViewBag.Branches));
        }
    }
}
