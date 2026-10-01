namespace GenNumeros.ApplicationCore.DTOs
{
    /// <summary>
    /// Un numero attribue, tel que les systemes appelants le lisent.
    /// </summary>
    public class NumeroDossierDto
    {
        /// <summary>Identifiant du dossier.</summary>
        public int Id { get; set; }

        /// <summary>Le numero attribue, au format XXX-XX-XXXXX-XXXXXX.</summary>
        public string NumeroCompte { get; set; } = string.Empty;

        /// <summary>Identifiant de la personne qui l'a demande.</summary>
        public string IdDemandeur { get; set; } = string.Empty;

        /// <summary>Etat du numero. Vaut Nouveau a l'attribution.</summary>
        public string Statut { get; set; } = string.Empty;

        /// <summary>Moment de l'attribution.</summary>
        public DateTime DateCreation { get; set; }
    }
}
