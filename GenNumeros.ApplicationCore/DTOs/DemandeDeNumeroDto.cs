using System.ComponentModel.DataAnnotations;

namespace GenNumeros.ApplicationCore.DTOs
{
    /// <summary>
    /// Ce qu'un systeme appelant doit fournir pour obtenir un numero.
    /// </summary>
    public class DemandeDeNumeroDto
    {
        /// <summary>
        /// Numero du systeme appelant, sur deux chiffres. Exemple : 12.
        /// </summary>
        [Required(ErrorMessage = "Le numero du systeme appelant est requis.")]
        [RegularExpression("^[0-9]{2}$", ErrorMessage = "Le numero du systeme appelant compte deux chiffres.")]
        public string SystemeAppelant { get; set; } = string.Empty;

        /// <summary>
        /// Numero de la succursale, sur cinq chiffres. Exemple : 45400.
        /// </summary>
        [Required(ErrorMessage = "Le numero de succursale est requis.")]
        [RegularExpression("^[0-9]{5}$", ErrorMessage = "Le numero de succursale compte cinq chiffres.")]
        public string Succursale { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de la personne qui demande le numero.
        /// </summary>
        [Required(ErrorMessage = "L'identifiant du demandeur est requis.")]
        [StringLength(50, ErrorMessage = "L'identifiant du demandeur ne depasse pas 50 caracteres.")]
        public string IdDemandeur { get; set; } = string.Empty;
    }
}
