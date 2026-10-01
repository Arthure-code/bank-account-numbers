using Client.MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.MVC.Controllers
{
    public class GestionComptesController : Controller
    {

        private readonly IConfiguration _config;
        
        public GestionComptesController(IConfiguration config)
        {
      
            _config = config;
        }
        // GET: GestionComptesController
        public async Task<ActionResult> Index()
        {

            return View(new List<NumeroDossier>());
        }


        // GET: GestionComptesController/Create
        public ActionResult DemanderNumero()
        {
            return View();
        }

        // POST: GestionComptesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DemanderNumero(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        
        //Obtient la liste des succursales depuis le fichier de configuration
        private List<Succursale> ObtenirSuccursales()
        {
           return _config.GetSection("Succursales").Get<List<Succursale>>();
        }
        
    }
}
