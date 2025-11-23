using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.Vnpay;

public class CreateVnpayPaymentRequest
{
    [Required]
    public int PaymentId { get; set; }
}
