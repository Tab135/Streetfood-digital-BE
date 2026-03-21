using BO.DTO.Branch;

namespace BO.DTO.Search
{
    public class SearchFilterDto : ActiveBranchFilterDto
    {
        public string? Keyword { get; set; }
    }
}