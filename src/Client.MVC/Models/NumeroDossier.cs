using System.ComponentModel.DataAnnotations;

namespace Client.MVC.Models
{
    public class NumeroDossier
    {
        public int Id { get; set; }

        [Display(Name = "Numéro de compte")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Display(Name = "Demandeur")]
        public string IdDemandeur { get; set; } = string.Empty;

        [Display(Name = "Statut")]
        public string Statut { get; set; } = string.Empty;

        [Display(Name = "Attribué le")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateCreation { get; set; }
    }
}
