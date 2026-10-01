using System.ComponentModel.DataAnnotations;

namespace AccountNumbers.ApplicationCore.Entities
{
    // A number that has been given out: the bank never gives it again.
    // Uniqueness is declared by the context, so that the core knows
    // nothing about the database.
    public class AccountNumber : BaseEntity
    {
        // Sixteen digits in four slices: XXX-XX-XXXXX-XXXXXX.
        [Required]
        [StringLength(19, MinimumLength = 19)]
        [Display(Name = "Account number")]
        public string Number { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Requested by")]
        public string RequestedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Attributed on")]
        public DateTime AttributedOn { get; set; }
    }
}
