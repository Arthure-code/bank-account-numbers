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
        /// Retourne tous les numéros de compte attribués.
        /// </summary>
        /// <remarks>
        /// La liste est rendue du plus récent au plus ancien, avec l'état de
        /// chaque numéro et l'identifiant de la personne qui l'a demandé.
        /// </remarks>
        /// <returns>La liste complète des numéros attribués.</returns>
        /// <response code="200">Liste retournée, vide si aucun numéro n'a encore été attribué.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<NumeroDossierDto>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<NumeroDossierDto>> Get()
        {
            IEnumerable<NumeroDossier> numeros = await _numeros.ObtenirTousLesNumeros();

            return numeros.Select(VersDto);
        }

        /// <summary>
        /// Attribue un nouveau numéro de compte et l'enregistre.
        /// </summary>
        /// <remarks>
        /// Le numéro suit le format XXX-XX-XXXXX-XXXXXX : le numéro de la
        /// banque, celui du système appelant, celui de la succursale, puis un
        /// numéro de compte tiré au hasard dont les deux derniers chiffres
        /// forment un nombre pair. Le numéro attribué est unique et son état
        /// est Nouveau.
        ///
        /// Exemple de demande :
        ///
        ///     POST /api/Numeros
        ///     {
        ///        "systemeAppelant": "12",
        ///        "succursale": "45400",
        ///        "idDemandeur": "employe.limoilou"
        ///     }
        ///
        /// </remarks>
        /// <param name="demande">Le système appelant, la succursale et le demandeur.</param>
        /// <returns>Le numéro attribué.</returns>
        /// <response code="201">Numéro attribué et enregistré.</response>
        /// <response code="400">Demande incomplète ou mal formée.</response>
        /// <response code="409">Aucun numéro libre n'a pu être tiré pour cette succursale.</response>
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
                    Title = "Demande absente.",
                    Detail = "Le corps de la requête ne porte aucune demande."
                });
            }

            NumeroDossier? attribue = await _numeros.GenererUnNumero(
                demande.SystemeAppelant, demande.Succursale, demande.IdDemandeur);

            if (attribue == null)
            {
                return Conflict(new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Aucun numéro libre.",
                    Detail = "Aucun numéro libre n'a pu être attribué pour cette demande."
                });
            }

            return CreatedAtAction(nameof(Get), new { id = attribue.Id }, VersDto(attribue));
        }

        // Un dossier tel que les systemes appelants le lisent : ses donnees,
        // et rien de l'entite qui les porte.
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
