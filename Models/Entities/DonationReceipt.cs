using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public class DonationReceipt
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DonationId { get; set; }

        [ForeignKey("DonationId")]
        public virtual Donation? Donation { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string DonorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string DonorEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string CauseTitle { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "PKR";

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string TransactionReference { get; set; } = string.Empty;

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? PdfPath { get; set; }

        [MaxLength(200)]
        public string? DigitalSignature { get; set; }
    }
}
