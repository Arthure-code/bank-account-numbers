using Client.MVC.Models;

namespace Client.MVC.Interfaces
{
    // What the application asks the API for, and nothing more.
    public interface INumerosProxy
    {
        Task<List<NumeroDossier>> ObtenirTousLesNumeros();

        Task<NumeroDossier?> DemanderUnNumero(DemandeDeNumero demande);
    }
}
