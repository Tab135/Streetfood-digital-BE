using BO.DTO.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ISearchService
    {
        /// <summary>
        /// Search for branches and dishes by keyword
        /// </summary>
        /// <param name="keyword">Search keyword (case-insensitive)</param>
        /// <returns>List of branches with matching dishes</returns>
        Task<List<SearchResultDto>> SearchAsync(string keyword);
    }
}
