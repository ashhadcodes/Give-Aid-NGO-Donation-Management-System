using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public enum PaymentProvider
    {
        JazzCash = 1,
        Easypaisa = 2,
        Card = 3,
        BankTransfer = 4
    }

    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DonationId { get; set; }

        [ForeignKey("DonationId")]
        public virtual Donation? Donation { get; set; }

        [Required]
        [MaxLength(100)]
        public string TransactionId { get; set; } = Guid.NewGuid().ToString("N");

        [MaxLength(150)]
        public string? GatewayTransactionId { get; set; }

        [Required]
        public PaymentProvider PaymentMethod { get; set; }

        [MaxLength(100)]
        public string GatewayName { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "PKR";

        [Required]
        public DonationStatus Status { get; set; } = DonationStatus.Pending;

        [MaxLength(500)]
        public string? FailureReason { get; set; }

        [MaxLength(30)]
        public string? MaskedCardNumber { get; set; }

        [MaxLength(50)]
        public string? CardBrand { get; set; }

        public string? GatewayRawResponse { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}
