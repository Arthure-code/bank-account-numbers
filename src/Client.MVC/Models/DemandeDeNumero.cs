using System.ComponentModel.DataAnnotations;

namespace Client.MVC.Models
{
    // What the employee fills in on screen. The calling system number is
    // not there: the application is the one that knows it.
    public class DemandeDeNumero
    {
        [Required(ErrorMessage = "Please choose a branch.")]
        [RegularExpression("^[0-9]{5}$", ErrorMessage = "A branch number is five digits long.")]
        [Display(Name = "Branch")]
        public string Succursale { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your identifier is required.")]
        [StringLength(50, ErrorMessage = "An identifier is at most 50 characters long.")]
        [Display(Name = "Your identifier")]
        public string IdDemandeur { get; set; } = string.Empty;

        // Filled in by the server before the request goes to the API.
        public string SystemeAppelant { get; set; } = string.Empty;
    }
}
