using System.Globalization;
using Client.MVC.Interfaces;
using Client.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Client.MVC.Controllers
{
    public class GestionComptesController : Controller
    {
        // Les numeros qui n'ont pas encore servi.
        private const string StatutNouveau = "Nouveau";

        // Le numero que la banque a donne a cette application. Il peut
        // changer, donc il se lit dans la configuration.
        private const string SystemeAppelantParDefaut = "12";

        private readonly IConfiguration _config;
        private readonly INumerosProxy _numeros;

        public GestionComptesController(IConfiguration config, INumerosProxy numeros)
        {
            _config = config;
            _numeros = numeros;
        }

        // GET: GestionComptes
        public async Task<ActionResult> Index()
        {
            List<NumeroDossier> numeros = await _numeros.ObtenirTousLesNumeros();

            return View(numeros
                .Where(n => string.Equals(n.Statut, StatutNouveau, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.DateCreation)
                .ThenByDescending(n => n.Id)
                .ToList());
        }

        // GET: GestionComptes/DemanderNumero
        public ActionResult DemanderNumero()
        {
            ViewBag.Succursales = ListeDesSuccursales();

            return View(new DemandeDeNumero());
        }

        // POST: GestionComptes/DemanderNumero
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DemanderNumero(DemandeDeNumero demande)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Succursales = ListeDesSuccursales();
                return View(demande);
            }

            // Le demandeur choisit sa succursale et s'identifie ; le numero
            // du systeme appelant, lui, appartient a l'application.
            demande.SystemeAppelant = _config["SystemeAppelant"] ?? SystemeAppelantParDefaut;

            NumeroDossier? attribue = await _numeros.DemanderUnNumero(demande);

            if (attribue == null)
            {
                ModelState.AddModelError(string.Empty,
                    "L'API n'a pas pu attribuer de numero. Reessayez dans un moment.");
                ViewBag.Succursales = ListeDesSuccursales();
                return View(demande);
            }

            return RedirectToAction(nameof(Index));
        }

        private List<SelectListItem> ListeDesSuccursales()
        {
            return ObtenirSuccursales()
                .Select(s => new SelectListItem
                {
                    Value = s.Numero.ToString("D5", CultureInfo.InvariantCulture),
                    Text = s.Nom + " (" + s.Numero.ToString("D5", CultureInfo.InvariantCulture) + ")"
                })
                .ToList();
        }

        // Obtient la liste des succursales depuis le fichier de configuration
        private List<Succursale> ObtenirSuccursales()
        {
            return _config.GetSection("Succursales").Get<List<Succursale>>() ?? new List<Succursale>();
        }
    }
}
