namespace BO.DTO.Users
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; }
        public int Point { get; set; }
    }
}
