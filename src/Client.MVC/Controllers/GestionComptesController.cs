using System.Globalization;
using Client.MVC.Interfaces;
using Client.MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Client.MVC.Controllers
{
    public class GestionComptesController : Controller
    {
        // The numbers nobody has put to use yet.
        private const string StatutNouveau = "New";

        // The number the bank gave this application. It can change, so it
        // is read from the configuration.
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

            // The employee chooses a branch and identifies themselves; the
            // calling system number belongs to the application.
            demande.SystemeAppelant = _config["SystemeAppelant"] ?? SystemeAppelantParDefaut;

            NumeroDossier? attribue = await _numeros.DemanderUnNumero(demande);

            if (attribue == null)
            {
                ModelState.AddModelError(string.Empty,
                    "The API could not give out a number. Please try again in a moment.");
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

        // The branches come from the configuration file.
        private List<Succursale> ObtenirSuccursales()
        {
            return _config.GetSection("Succursales").Get<List<Succursale>>() ?? new List<Succursale>();
        }
    }
}
