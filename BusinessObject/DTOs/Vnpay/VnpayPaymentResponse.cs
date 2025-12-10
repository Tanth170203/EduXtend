namespace BusinessObject.DTOs.Vnpay;

public class VnpayPaymentResponse
{
    public string PaymentUrl { get; set; } = string.Empty;
    public long TransactionId { get; set; }
}
