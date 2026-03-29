using System.Collections.Generic;

namespace BO.Common
{
    public class PaginatedResponse<T>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
        public List<T> Items { get; set; }

        // Parameterless constructor for JSON deserialization (STJ uses property setters).
        public PaginatedResponse() { Items = new List<T>(); }

        public PaginatedResponse(List<T> items, int count, int pageNumber, int pageSize)
        {
            TotalCount = count;
            // Guard against invalid paging input
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 10;
            }

            PageSize = pageSize;
            CurrentPage = pageNumber;
            TotalPages = (int)System.Math.Ceiling(count / (double)pageSize);
            Items = items;
        }
    }
}
