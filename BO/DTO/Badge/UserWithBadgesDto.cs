using System.Collections.Generic;

namespace BO.DTO.Badge
{
    public class UserWithBadgesDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Point { get; set; }
        public List<BadgeWithUserInfoDto> Badges { get; set; }
    }
}
