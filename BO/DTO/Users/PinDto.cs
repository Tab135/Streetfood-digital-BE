namespace BO.DTO.Users;

public class PinStatusDto
{
    public bool HasPin { get; set; }
}

public class SetPinDto
{
    public string Pin { get; set; } = string.Empty;
}

public class ChangePinDto
{
    public string CurrentPin { get; set; } = string.Empty;
    public string NewPin { get; set; } = string.Empty;
}

public class VerifyPinDto
{
    public string Pin { get; set; } = string.Empty;
}

public class VerifyPinResponseDto
{
    public bool Success { get; set; }
    public int? AttemptsRemaining { get; set; }
}

public class RemovePinDto
{
    public string Pin { get; set; } = string.Empty;
}
