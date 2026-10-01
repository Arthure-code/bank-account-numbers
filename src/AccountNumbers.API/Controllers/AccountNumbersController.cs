using AccountNumbers.ApplicationCore.DTOs;
using AccountNumbers.ApplicationCore.Entities;
using AccountNumbers.ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AccountNumbers.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class AccountNumbersController : ControllerBase
    {
        private readonly IAccountNumberService _numeros;

        public AccountNumbersController(IAccountNumberService numbers)
        {
            _numeros = numbers;
        }

        /// <summary>
        /// Returns every account number given out.
        /// </summary>
        /// <remarks>
        /// The list runs from the most recent to the oldest, with the status
        /// of each number and the identifier of the person who asked for it.
        /// </remarks>
        /// <returns>The complete list of the numbers given out.</returns>
        /// <response code="200">List returned, empty when no number has been given out yet.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<AccountNumberDto>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<AccountNumberDto>> Get()
        {
            IEnumerable<AccountNumber> numbers = await _numeros.GetAllAsync();

            return numbers.Select(ToDto);
        }

        /// <summary>
        /// Gives out a new account number and records it.
        /// </summary>
        /// <remarks>
        /// A number follows the XXX-XX-XXXXX-XXXXXX format: the bank, the
        /// calling system, the branch, then an account number drawn at
        /// random whose last two digits form an even number. The number
        /// given out is unique and its status reads New.
        ///
        /// Sample request:
        ///
        ///     POST /api/AccountNumbers
        ///     {
        ///        "callingSystem": "12",
        ///        "branch": "45400",
        ///        "requestedBy": "employee.limoilou"
        ///     }
        ///
        /// </remarks>
        /// <param name="request">The calling system, the branch and the requester.</param>
        /// <returns>The number given out.</returns>
        /// <response code="201">Number given out and recorded.</response>
        /// <response code="400">Request incomplete or malformed.</response>
        /// <response code="409">No free number could be drawn for this branch.</response>
        [HttpPost]
        [ProducesResponseType(typeof(AccountNumberDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<AccountNumberDto>> Post([FromBody] NumberRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Request missing.",
                    Detail = "The body of the request carries no demand."
                });
            }

            AccountNumber? given = await _numeros.GenerateAsync(
                request.CallingSystem, request.Branch, request.RequestedBy);

            if (given == null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "No free number.",
                    Detail = "No free number could be given out for this request."
                });
            }

            return CreatedAtAction(nameof(Get), new { id = given.Id }, ToDto(given));
        }

        // A file as the calling systems read it: its data, and nothing of
        // the entity that carries it.
        private static AccountNumberDto ToDto(AccountNumber file)
        {
            return new AccountNumberDto
            {
                Id = file.Id,
                Number = file.Number,
                RequestedBy = file.RequestedBy,
                Status = file.Status,
                AttributedOn = file.AttributedOn
            };
        }
    }
}
