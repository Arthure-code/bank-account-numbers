namespace GenNumeros.ApplicationCore.DTOs
{
    /// <summary>
    /// Un numéro attribué, tel que les systèmes appelants le lisent.
    /// </summary>
    public class NumeroDossierDto
    {
        /// <summary>Identifiant du dossier.</summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>Le numéro attribué, au format XXX-XX-XXXXX-XXXXXX.</summary>
        /// <example>145-12-45400-232054</example>
        public string NumeroCompte { get; set; } = string.Empty;

        /// <summary>Identifiant de la personne qui l'a demandé.</summary>
        /// <example>employe.limoilou</example>
        public string IdDemandeur { get; set; } = string.Empty;

        /// <summary>État du numéro. Vaut Nouveau à l'attribution.</summary>
        /// <example>Nouveau</example>
        public string Statut { get; set; } = string.Empty;

        /// <summary>Moment de l'attribution.</summary>
        public DateTime DateCreation { get; set; }
    }
}
