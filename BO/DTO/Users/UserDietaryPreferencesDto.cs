using System.Collections.Generic;

namespace BO.DTO.Users;

public class UserDietaryPreferencesDto
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public List<string> DietaryPreferences { get; set; }
}
