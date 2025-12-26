namespace FintechPaymentPOC.Application.DTOs;

public class CreateAccountRequestDto
{
    public string Name { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; } = 0;
    public string Currency { get; set; } = "USD";
}

public class AccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

