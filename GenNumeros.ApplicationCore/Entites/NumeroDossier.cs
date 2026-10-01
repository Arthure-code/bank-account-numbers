using System.ComponentModel.DataAnnotations;

namespace GenNumeros.ApplicationCore.Entites
{
    // Un numero attribue : la banque ne le redonne jamais. L'unicite est
    // posee par le contexte, pour que le coeur ignore la base.
    public class NumeroDossier : BaseEntity
    {
        // Seize chiffres en quatre tranches : XXX-XX-XXXXX-XXXXXX.
        [Required]
        [StringLength(19, MinimumLength = 19)]
        [Display(Name = "Numero de compte")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Display(Name = "Demandeur")]
        public string IdDemandeur { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Statut { get; set; } = string.Empty;

        [Display(Name = "Date de creation")]
        public DateTime DateCreation { get; set; }
    }
}
