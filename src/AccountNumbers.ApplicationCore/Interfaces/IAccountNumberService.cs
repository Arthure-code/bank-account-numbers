using AccountNumbers.ApplicationCore.Entities;

namespace AccountNumbers.ApplicationCore.Interfaces
{
    public interface IAccountNumberService
    {
        /// <summary>
        /// Every number already given out, from the most recent to the oldest.
        /// </summary>
        Task<IEnumerable<AccountNumber>> GetAllAsync();

        /// <summary>
        /// Gives out a new number to the requester and records it.
        /// Answers null when the calling system, the branch or the
        /// requester does not follow the expected format.
        /// </summary>
        Task<AccountNumber?> GenerateAsync(string callingSystem, string branch, string requestedBy);
    }
}
