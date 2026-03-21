namespace BO.DTO.Order;

public class OrderPickupCodeResponseDto
{
    public int OrderId { get; set; }
    public string VerificationCode { get; set; } = string.Empty;
    public string QrContent { get; set; } = string.Empty;
}
