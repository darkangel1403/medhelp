using System.Collections.Generic;
using System.Threading.Tasks;

namespace MedHelp.Services
{
    public interface IDataService<T>
    {
        Task<List<T>> GetAllAsync();
        Task<bool> AddAsync(T item);
        Task<bool> UpdateAsync(T item);
        Task<bool> DeleteAsync(int id);
    }
}
