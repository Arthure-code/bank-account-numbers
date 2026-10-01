using System.Linq.Expressions;
using GenNumeros.ApplicationCore.Entites;

namespace GenNumeros.ApplicationCore.Interfaces
{
    public interface IAsyncRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(int id);

        Task<IEnumerable<T>> ListAsync();

        Task<IEnumerable<T>> ListAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        Task DeleteAsync(T entity);

        Task EditAsync(T entity);
    }
}
