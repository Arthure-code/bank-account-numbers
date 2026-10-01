using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Client.MVC.Models
{
    public class NumeroDossier 
    {
        public int Id { get; set; }
        public string NumeroCompte { get; set; }
        public string IdDemandeur { get; set; }
        public string Statut { get; set; }
        public DateTime DateCreation { get; set; }

    }
}
