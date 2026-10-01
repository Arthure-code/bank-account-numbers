using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenNumeros.ApplicationCore.Entites
{
    public class NumeroDossier : BaseEntity
    {
        public string NumeroCompte { get; set; }
        public string IdDemandeur { get; set; }
        public string Statut { get; set; }
        public DateTime DateCreation { get; set; }
          
    }
}
