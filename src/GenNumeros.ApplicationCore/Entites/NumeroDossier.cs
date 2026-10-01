using System.ComponentModel.DataAnnotations;

namespace GenNumeros.ApplicationCore.Entites
{
    // A number that has been given out: the bank never gives it again.
    // Uniqueness is declared by the context, so that the core knows
    // nothing about the database.
    public class NumeroDossier : BaseEntity
    {
        // Sixteen digits in four slices: XXX-XX-XXXXX-XXXXXX.
        [Required]
        [StringLength(19, MinimumLength = 19)]
        [Display(Name = "Account number")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Requested by")]
        public string IdDemandeur { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Statut { get; set; } = string.Empty;

        [Display(Name = "Attributed on")]
        public DateTime DateCreation { get; set; }
    }
}
