using AccountNumbers.API.Controllers;
using AccountNumbers.ApplicationCore.DTOs;
using AccountNumbers.ApplicationCore.Entities;
using AccountNumbers.ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AccountNumbers.API.Tests.Controllers
{
    public class AccountNumbersControllerTest
    {
        private static AccountNumber AFile(int identifier, string number, string requester = "employee.limoilou")
            => new AccountNumber
            {
                Id = identifier,
                Number = number,
                RequestedBy = requester,
                Status = "New",
                AttributedOn = new DateTime(2026, 1, 5, 9, 30, 0)
            };

        private static NumberRequestDto ARequest() => new NumberRequestDto
        {
            CallingSystem = "12",
            Branch = "45400",
            RequestedBy = "employee.limoilou"
        };

        [Fact]
        public async Task Get_AnswersEveryNumberGivenOut()
        {
            //Given two numbers already given out
            var numbers = new Mock<IAccountNumberService>();
            numbers.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<AccountNumber>
            {
                AFile(1, "145-12-45400-123456"),
                AFile(2, "145-12-45401-654320")
            });
            var controller = new AccountNumbersController(numbers.Object);

            //When
            IEnumerable<AccountNumberDto> returned = await controller.Get();

            //Then
            Assert.Equal(2, returned.Count());
            numbers.Verify(s => s.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task Get_CopiesEveryFieldOfTheFileIntoItsDto()
        {
            //Given one number given out
            var numbers = new Mock<IAccountNumberService>();
            numbers.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<AccountNumber>
            {
                AFile(7, "145-12-45400-123456", "marie.tremblay")
            });
            var controller = new AccountNumbersController(numbers.Object);

            //When
            AccountNumberDto returned = (await controller.Get()).Single();

            //Then
            Assert.Equal(7, returned.Id);
            Assert.Equal("145-12-45400-123456", returned.Number);
            Assert.Equal("marie.tremblay", returned.RequestedBy);
            Assert.Equal("New", returned.Status);
            Assert.Equal(new DateTime(2026, 1, 5, 9, 30, 0), returned.AttributedOn);
        }

        [Fact]
        public async Task Get_AnswersAnEmptyListWhenNothingWasGivenOut()
        {
            //Given no number at all
            var numbers = new Mock<IAccountNumberService>();
            numbers.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<AccountNumber>());
            var controller = new AccountNumbersController(numbers.Object);

            //Then
            Assert.Empty(await controller.Get());
        }

        [Fact]
        public async Task Post_RejectsAMissingRequest()
        {
            //Given no request body
            var numbers = new Mock<IAccountNumberService>();
            var controller = new AccountNumbersController(numbers.Object);

            //When
            ActionResult<AccountNumberDto> result = await controller.Post(null!);

            //Then nothing is given out
            Assert.IsType<BadRequestObjectResult>(result.Result);
            numbers.Verify(s => s.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        [Fact]
        public async Task Post_GivesOutTheNumberAndItsAddress()
        {
            //Given a service that gives out a number
            var numbers = new Mock<IAccountNumberService>();
            numbers.Setup(s => s.GenerateAsync("12", "45400", "employee.limoilou"))
                .ReturnsAsync(AFile(9, "145-12-45400-123456"));
            var controller = new AccountNumbersController(numbers.Object);

            //When
            ActionResult<AccountNumberDto> result = await controller.Post(ARequest());

            //Then the number comes back with the address to read it again
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            AccountNumberDto returned = Assert.IsType<AccountNumberDto>(created.Value);
            Assert.Equal("145-12-45400-123456", returned.Number);
            Assert.Equal(9, created.RouteValues!["id"]);
        }

        [Fact]
        public async Task Post_PassesToTheServiceWhatTheRequestCarries()
        {
            //Given a complete request
            var numbers = new Mock<IAccountNumberService>();
            numbers.Setup(s => s.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(AFile(1, "145-12-45400-123456"));
            var controller = new AccountNumbersController(numbers.Object);

            //When
            await controller.Post(ARequest());

            //Then the three values arrive untouched
            numbers.Verify(s => s.GenerateAsync("12", "45400", "employee.limoilou"), Times.Once);
        }

        [Fact]
        public async Task Post_AnswersConflictWhenNoNumberIsFree()
        {
            //Given a service that finds nothing to give out
            var numbers = new Mock<IAccountNumberService>();
            numbers.Setup(s => s.GenerateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((AccountNumber?)null);
            var controller = new AccountNumbersController(numbers.Object);

            //When
            ActionResult<AccountNumberDto> result = await controller.Post(ARequest());

            //Then
            Assert.IsType<ConflictObjectResult>(result.Result);
        }
    }
}
