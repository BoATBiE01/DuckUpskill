using DuckUpskill.Models;

namespace DuckUpskill.ViewModels;

public class CheckoutViewModel
{
    public Course Course { get; set; } = null!;
    public string PaymentMethod { get; set; } = "PromptPay QR";
    public string? AccountName { get; set; }
    public string? SlipNotes { get; set; }
}
