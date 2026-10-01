using GenNumeros.ApplicationCore.DTOs;
using GenNumeros.ApplicationCore.Entites;
using GenNumeros.ApplicationCore.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GenNumeros.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NumerosController : ControllerBase
    {
        private readonly INumerosService _numeros;

        public NumerosController(INumerosService numeros)
        {
            _numeros = numeros;
        }

        /// <summary>
        /// Retourne tous les numeros de compte attribues.
        /// </summary>
        /// <remarks>
        /// La liste est rendue du plus recent au plus ancien, avec l'etat de
        /// chaque numero et l'identifiant de la personne qui l'a demande.
        /// </remarks>
        /// <returns>La liste complete des numeros attribues.</returns>
        /// <response code="200">Liste retournee, vide si aucun numero n'a encore ete attribue.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<NumeroDossierDto>), StatusCodes.Status200OK)]
        public async Task<IEnumerable<NumeroDossierDto>> Get()
        {
            IEnumerable<NumeroDossier> numeros = await _numeros.ObtenirTousLesNumeros();

            return numeros.Select(VersDto);
        }

        /// <summary>
        /// Attribue un nouveau numero de compte et l'enregistre.
        /// </summary>
        /// <remarks>
        /// Le numero suit le format XXX-XX-XXXXX-XXXXXX : le numero de la
        /// banque, celui du systeme appelant, celui de la succursale, puis un
        /// numero de compte tire au hasard dont les deux derniers chiffres
        /// forment un nombre pair. Le numero attribue est unique et son etat
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
        /// <param name="demande">Le systeme appelant, la succursale et le demandeur.</param>
        /// <returns>Le numero attribue.</returns>
        /// <response code="201">Numero attribue et enregistre.</response>
        /// <response code="400">Demande incomplete ou mal formee.</response>
        /// <response code="409">Aucun numero libre n'a pu etre tire pour cette succursale.</response>
        [HttpPost]
        [ProducesResponseType(typeof(NumeroDossierDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<NumeroDossierDto>> Post([FromBody] DemandeDeNumeroDto demande)
        {
            if (demande == null)
            {
                return BadRequest("La demande est absente.");
            }

            NumeroDossier? attribue = await _numeros.GenererUnNumero(
                demande.SystemeAppelant, demande.Succursale, demande.IdDemandeur);

            if (attribue == null)
            {
                return Conflict("Aucun numero libre n'a pu etre attribue pour cette demande.");
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
