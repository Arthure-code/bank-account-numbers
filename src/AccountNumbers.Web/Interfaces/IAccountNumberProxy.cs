using AccountNumbers.Web.Models;

namespace AccountNumbers.Web.Interfaces
{
    // What the application asks the API for, and nothing more.
    public interface IAccountNumberProxy
    {
        Task<List<AccountNumber>> GetAllAsync();

        Task<AccountNumber?> AskForOneAsync(NumberRequest request);
    }
}
