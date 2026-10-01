using GenNumeros.ApplicationCore.DTOs;
using GenNumeros.ApplicationCore.Entites;
using GenNumeros.ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GenNumeros.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class NumerosController : ControllerBase
    {
        private readonly INumerosService _numeros;

        public NumerosController(INumerosService numeros)
        {
            _numeros = numeros;
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
        [ProducesResponseType(typeof(IEnumerable<NumeroDossierDto>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<NumeroDossierDto>> Get()
        {
            IEnumerable<NumeroDossier> numeros = await _numeros.ObtenirTousLesNumeros();

            return numeros.Select(VersDto);
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
        ///     POST /api/Numeros
        ///     {
        ///        "systemeAppelant": "12",
        ///        "succursale": "45400",
        ///        "idDemandeur": "employee.limoilou"
        ///     }
        ///
        /// </remarks>
        /// <param name="demande">The calling system, the branch and the requester.</param>
        /// <returns>The number given out.</returns>
        /// <response code="201">Number given out and recorded.</response>
        /// <response code="400">Request incomplete or malformed.</response>
        /// <response code="409">No free number could be drawn for this branch.</response>
        [HttpPost]
        [ProducesResponseType(typeof(NumeroDossierDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        public async Task<ActionResult<NumeroDossierDto>> Post([FromBody] DemandeDeNumeroDto demande)
        {
            if (demande == null)
            {
                return BadRequest(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Request missing.",
                    Detail = "The body of the request carries no demand."
                });
            }

            NumeroDossier? attribue = await _numeros.GenererUnNumero(
                demande.SystemeAppelant, demande.Succursale, demande.IdDemandeur);

            if (attribue == null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "No free number.",
                    Detail = "No free number could be given out for this request."
                });
            }

            return CreatedAtAction(nameof(Get), new { id = attribue.Id }, VersDto(attribue));
        }

        // A file as the calling systems read it: its data, and nothing of
        // the entity that carries it.
        private static NumeroDossierDto VersDto(NumeroDossier dossier)
        {
            return new NumeroDossierDto
            {
                Id = dossier.Id,
                NumeroCompte = dossier.NumeroCompte,
                IdDemandeur = dossier.IdDemandeur,
                Statut = dossier.Statut,
                DateCreation = dossier.DateCreation
            };
        }
    }
}
