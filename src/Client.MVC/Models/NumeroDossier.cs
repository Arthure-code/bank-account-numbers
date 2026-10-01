using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.MVC.Models
{
    public class NumeroDossier 
    {
        public int Id { get; set; }
        public string NumeroCompte { get; set; } = string.Empty;
        public string IdDemandeur { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }

    }
}
