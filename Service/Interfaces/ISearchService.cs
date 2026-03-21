using BO.DTO.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface ISearchService
    {
        Task<List<SearchResultDto>> SearchAsync(SearchFilterDto filter);
    }
}
