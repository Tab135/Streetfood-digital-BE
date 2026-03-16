using System.Collections.Generic;

namespace BO.DTO.Dish
{
    public class AssignDishesRequest
    {
        public List<int> DishIds { get; set; } = new List<int>();
    }
}
