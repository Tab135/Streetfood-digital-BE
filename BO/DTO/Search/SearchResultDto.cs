using System.Collections.Generic;

namespace BO.DTO.Search
{
    public class SearchResultDto
    {
        public int BranchId { get; set; }
        public string Name { get; set; }
        public string AddressDetail { get; set; }
        public string City { get; set; }
        public string? Ward { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public double AvgRating { get; set; }
        public bool IsVerified { get; set; }
        public bool IsActive { get; set; }
        public List<DishSearchDto> Dishes { get; set; } = new List<DishSearchDto>();
    }
    
    public class DishSearchDto
    {
        public int DishId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSoldOut { get; set; }
        public string CategoryName { get; set; }
    }
}
