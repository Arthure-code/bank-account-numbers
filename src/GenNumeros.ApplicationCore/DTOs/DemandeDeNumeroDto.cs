using System.ComponentModel.DataAnnotations;

namespace GenNumeros.ApplicationCore.DTOs
{
    /// <summary>
    /// Ce qu'un système appelant doit fournir pour obtenir un numéro.
    /// </summary>
    public class DemandeDeNumeroDto
    {
        /// <summary>
        /// Numéro du système appelant, sur deux chiffres.
        /// </summary>
        /// <example>12</example>
        [Required(ErrorMessage = "Le numéro du système appelant est requis.")]
        [RegularExpression("^[0-9]{2}$", ErrorMessage = "Le numéro du système appelant compte deux chiffres.")]
        public string SystemeAppelant { get; set; } = string.Empty;

        /// <summary>
        /// Numéro de la succursale, sur cinq chiffres.
        /// </summary>
        /// <example>45400</example>
        [Required(ErrorMessage = "Le numéro de succursale est requis.")]
        [RegularExpression("^[0-9]{5}$", ErrorMessage = "Le numéro de succursale compte cinq chiffres.")]
        public string Succursale { get; set; } = string.Empty;

        /// <summary>
        /// Identifiant de la personne qui demande le numéro.
        /// </summary>
        /// <example>employe.limoilou</example>
        [Required(ErrorMessage = "L'identifiant du demandeur est requis.")]
        [StringLength(50, ErrorMessage = "L'identifiant du demandeur ne dépasse pas 50 caractères.")]
        public string IdDemandeur { get; set; } = string.Empty;
    }
}
