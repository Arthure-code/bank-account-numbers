using System.ComponentModel.DataAnnotations;

namespace Client.MVC.Models
{
    // Ce que l'employe remplit a l'ecran. Le numero du systeme appelant
    // n'y figure pas : c'est l'application qui le connait.
    public class DemandeDeNumero
    {
        [Required(ErrorMessage = "Choisissez une succursale.")]
        [RegularExpression("^[0-9]{5}$", ErrorMessage = "Le numero de succursale compte cinq chiffres.")]
        [Display(Name = "Succursale")]
        public string Succursale { get; set; } = string.Empty;

        [Required(ErrorMessage = "Votre identifiant est requis.")]
        [StringLength(50, ErrorMessage = "L'identifiant ne depasse pas 50 caracteres.")]
        [Display(Name = "Votre identifiant")]
        public string IdDemandeur { get; set; } = string.Empty;

        // Rempli par le serveur pour l'envoi a l'API.
        public string SystemeAppelant { get; set; } = string.Empty;
    }
}
