namespace AccountNumbers.ApplicationCore.DTOs
{
    /// <summary>
    /// A number that has been given out, as the calling systems read it.
    /// </summary>
    public class AccountNumberDto
    {
        /// <summary>Identifier of the file.</summary>
        /// <example>1</example>
        public int Id { get; set; }

        /// <summary>The number given out, in the XXX-XX-XXXXX-XXXXXX format.</summary>
        /// <example>145-12-45400-232054</example>
        public string Number { get; set; } = string.Empty;

        /// <summary>Identifier of the person who asked for it.</summary>
        /// <example>employee.limoilou</example>
        public string RequestedBy { get; set; } = string.Empty;

        /// <summary>Status of the number. Reads New when it is given out.</summary>
        /// <example>New</example>
        public string Status { get; set; } = string.Empty;

        /// <summary>Moment it was given out.</summary>
        public DateTime AttributedOn { get; set; }
    }
}
