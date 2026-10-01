using System.Globalization;
using AccountNumbers.Web.Interfaces;
using AccountNumbers.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccountNumbers.Web.Controllers
{
    public class AccountNumbersController : Controller
    {
        // The numbers nobody has put to use yet.
        private const string NewStatus = "New";

        // The number the bank gave this application. It can change, so it
        // is read from the configuration.
        private const string DefaultCallingSystem = "12";

        private readonly IConfiguration _config;
        private readonly IAccountNumberProxy _numeros;

        public AccountNumbersController(IConfiguration config, IAccountNumberProxy numbers)
        {
            _config = config;
            _numeros = numbers;
        }

        // GET: AccountNumbers
        public async Task<ActionResult> Index()
        {
            List<AccountNumber> numbers = await _numeros.GetAllAsync();

            return View(numbers
                .Where(n => string.Equals(n.Status, NewStatus, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(n => n.AttributedOn)
                .ThenByDescending(n => n.Id)
                .ToList());
        }

        // GET: AccountNumbers/Ask
        public ActionResult Ask()
        {
            ViewBag.Branches = BranchList();

            return View(new NumberRequest());
        }

        // POST: AccountNumbers/Ask
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Ask(NumberRequest request)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Branches = BranchList();
                return View(request);
            }

            // The employee chooses a branch and identifies themselves; the
            // calling system number belongs to the application.
            request.CallingSystem = _config["CallingSystem"] ?? DefaultCallingSystem;

            AccountNumber? given = await _numeros.AskForOneAsync(request);

            if (given == null)
            {
                ModelState.AddModelError(string.Empty,
                    "The API could not give out a number. Please try again in a moment.");
                ViewBag.Branches = BranchList();
                return View(request);
            }

            return RedirectToAction(nameof(Index));
        }

        private List<SelectListItem> BranchList()
        {
            return ReadBranches()
                .Select(s => new SelectListItem
                {
                    Value = s.Number.ToString("D5", CultureInfo.InvariantCulture),
                    Text = s.Name + " (" + s.Number.ToString("D5", CultureInfo.InvariantCulture) + ")"
                })
                .ToList();
        }

        // The branches come from the configuration file.
        private List<Branch> ReadBranches()
        {
            return _config.GetSection("Branches").Get<List<Branch>>() ?? new List<Branch>();
        }
    }
}
