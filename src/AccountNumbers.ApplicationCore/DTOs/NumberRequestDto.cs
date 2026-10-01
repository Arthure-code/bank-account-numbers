using System.ComponentModel.DataAnnotations;

namespace AccountNumbers.ApplicationCore.DTOs
{
    /// <summary>
    /// What a calling system must supply to obtain a number.
    /// </summary>
    public class NumberRequestDto
    {
        /// <summary>
        /// Calling system number, two digits long.
        /// </summary>
        /// <example>12</example>
        [Required(ErrorMessage = "The calling system number is required.")]
        [RegularExpression("^[0-9]{2}$", ErrorMessage = "A calling system number is two digits long.")]
        public string CallingSystem { get; set; } = string.Empty;

        /// <summary>
        /// Branch number, five digits long.
        /// </summary>
        /// <example>45400</example>
        [Required(ErrorMessage = "The branch number is required.")]
        [RegularExpression("^[0-9]{5}$", ErrorMessage = "A branch number is five digits long.")]
        public string Branch { get; set; } = string.Empty;

        /// <summary>
        /// Identifier of the person asking for the number.
        /// </summary>
        /// <example>employee.limoilou</example>
        [Required(ErrorMessage = "The requester identifier is required.")]
        [StringLength(50, ErrorMessage = "A requester identifier is at most 50 characters long.")]
        public string RequestedBy { get; set; } = string.Empty;
    }
}
