using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public enum DonationStatus
    {
        Pending = 0,
        Processing = 1,
        Paid = 2,
        Failed = 3,
        Cancelled = 4,
        Refunded = 5
    }

    public class Donation
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public int CauseId { get; set; }

        [ForeignKey("CauseId")]
        public virtual DonationCause? Cause { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(1, 100000000, ErrorMessage = "Donation amount must be greater than zero.")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "PKR";

        [Required]
        [MaxLength(100)]
        [Display(Name = "Donor Name")]
        public string DonorName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Display(Name = "Donor Email")]
        public string DonorEmail { get; set; } = string.Empty;

        [MaxLength(30)]
        [Display(Name = "Donor Phone")]
        public string? DonorPhone { get; set; }

        public bool Anonymous { get; set; } = false;

        [MaxLength(100)]
        [Display(Name = "Donor City")]
        public string? DonorCity { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Optional Message / Dedication")]
        public string? Message { get; set; }

        [NotMapped]
        public bool IsAnonymous
        {
            get => Anonymous;
            set => Anonymous = value;
        }

        [NotMapped]
        public string? DonorNotes
        {
            get => Message;
            set => Message = value;
        }

        [NotMapped]
        public string PaymentMethod => System.Linq.Enumerable.LastOrDefault(PaymentTransactions)?.PaymentMethod.ToString() ?? Receipt?.PaymentMethod ?? "Online";

        [NotMapped]
        public string? TransactionReference => System.Linq.Enumerable.LastOrDefault(PaymentTransactions)?.TransactionId ?? Receipt?.TransactionReference ?? (Receipt != null ? Receipt.ReceiptNumber : null);

        [Required]
        public DonationStatus Status { get; set; } = DonationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        public virtual DonationReceipt? Receipt { get; set; }
    }
}
