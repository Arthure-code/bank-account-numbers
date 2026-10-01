using GenNumeros.ApplicationCore.Entites;

namespace GenNumeros.ApplicationCore.Interfaces
{
    public interface INumerosService
    {
        /// <summary>
        /// Every number already given out, from the most recent to the oldest.
        /// </summary>
        Task<IEnumerable<NumeroDossier>> ObtenirTousLesNumeros();

        /// <summary>
        /// Gives out a new number to the requester and records it.
        /// Answers null when the calling system, the branch or the
        /// requester does not follow the expected format.
        /// </summary>
        Task<NumeroDossier?> GenererUnNumero(string systemeAppelant, string succursale, string idDemandeur);
    }
}
