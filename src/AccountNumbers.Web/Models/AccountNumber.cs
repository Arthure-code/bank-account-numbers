using System.ComponentModel.DataAnnotations;

namespace AccountNumbers.Web.Models
{
    public class AccountNumber
    {
        public int Id { get; set; }

        [Display(Name = "Account number")]
        public string Number { get; set; } = string.Empty;

        [Display(Name = "Requested by")]
        public string RequestedBy { get; set; } = string.Empty;

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Attributed on")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}")]
        public DateTime AttributedOn { get; set; }
    }
}
