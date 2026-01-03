using System;

namespace BO.DTO;

public class UserBadgeDto
{
    public int UserBadgeId { get; set; }

    public int UserId { get; set; }

    public int BadgeId { get; set; }

    public DateTime CreatedAt { get; set; }
}