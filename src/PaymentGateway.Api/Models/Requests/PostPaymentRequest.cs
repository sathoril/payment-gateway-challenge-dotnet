using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PostPaymentRequest
{
    [Required]
    [StringLength(19, MinimumLength = 14)]
    public string CardNumber { get; set; }
    
    [Required]
    [Range(1, 12)]
    public int ExpiryMonth { get; set; }
    
    [Required]
    public int ExpiryYear { get; set; }
    
    [Required]
    [StringLength(3)]
    public string Currency { get; set; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int Amount { get; set; }
    
    [Required]
    [StringLength(4, MinimumLength = 3)]
    [RegularExpression(@"^\d+$")]
    public string Cvv { get; set; }
    
    public bool IsValid()
    {
        if (Amount < 1)
            return false;
        
        if (string.IsNullOrWhiteSpace(CardNumber))
            return false;
        
        if (!CardNumber.All(char.IsDigit))
            return false;
        
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;
        if (ExpiryYear < currentYear || (ExpiryYear == currentYear && ExpiryMonth <= currentMonth))
            return false;
        
        if (!Enum.IsDefined(typeof(Currency), Currency))
            return false;

        return true;
    }
}