using Client.MVC.Models;

namespace Client.MVC.Interfaces
{
    // Ce que l'application demande a l'API, et rien de plus.
    public interface INumerosProxy
    {
        Task<List<NumeroDossier>> ObtenirTousLesNumeros();

        Task<NumeroDossier?> DemanderUnNumero(DemandeDeNumero demande);
    }
}
