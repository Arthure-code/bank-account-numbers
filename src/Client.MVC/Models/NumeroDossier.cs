using System.ComponentModel.DataAnnotations;

namespace Client.MVC.Models
{
    public class NumeroDossier
    {
        public int Id { get; set; }

        [Display(Name = "Account number")]
        public string NumeroCompte { get; set; } = string.Empty;

        [Display(Name = "Requested by")]
        public string IdDemandeur { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Statut { get; set; } = string.Empty;

        [Display(Name = "Attributed on")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime DateCreation { get; set; }
    }
}
