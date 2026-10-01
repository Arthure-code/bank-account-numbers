using GenNumeros.ApplicationCore.Entites;

namespace GenNumeros.ApplicationCore.Interfaces
{
    public interface INumerosService
    {
        /// <summary>
        /// Tous les numeros deja attribues, du plus recent au plus ancien.
        /// </summary>
        Task<IEnumerable<NumeroDossier>> ObtenirTousLesNumeros();

        /// <summary>
        /// Attribue un numero neuf au demandeur, et l'enregistre.
        /// Rend null si le systeme appelant, la succursale ou le demandeur
        /// ne respectent pas le format attendu.
        /// </summary>
        Task<NumeroDossier?> GenererUnNumero(string systemeAppelant, string succursale, string idDemandeur);
    }
}
