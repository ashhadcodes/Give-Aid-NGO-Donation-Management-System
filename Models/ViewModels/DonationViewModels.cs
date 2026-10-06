using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.ViewModels
{
    public class DonateFormViewModel
    {
        [Required(ErrorMessage = "Please choose a donation cause.")]
        [Display(Name = "Donation Cause")]
        public int CauseId { get; set; }

        [Required(ErrorMessage = "Please enter an amount.")]
        [Range(10, 10000000, ErrorMessage = "Donation amount must be at least 10 PKR.")]
        [Display(Name = "Donation Amount (PKR)")]
        public decimal Amount { get; set; } = 1000m;

        [Required(ErrorMessage = "Please enter your full name.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string DonorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [MaxLength(150)]
        [Display(Name = "Email Address")]
        public string DonorEmail { get; set; } = string.Empty;

        [Phone]
        [MaxLength(30)]
        [Display(Name = "Phone Number")]
        public string? DonorPhone { get; set; }

        [Display(Name = "Keep this donation anonymous")]
        public bool Anonymous { get; set; } = false;

        [MaxLength(500)]
        [Display(Name = "Message or Dedication (Optional)")]
        public string? Message { get; set; }

        [Required(ErrorMessage = "Please select a payment method.")]
        [Display(Name = "Payment Method")]
        public PaymentProvider PaymentMethod { get; set; } = PaymentProvider.JazzCash;

        // Card Details (Only populated in memory during POST)
        [Display(Name = "Card Number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Name on Card")]
        public string? CardHolderName { get; set; }

        [Display(Name = "Expiry Month")]
        public string? ExpiryMonth { get; set; }

        [Display(Name = "Expiry Year")]
        public string? ExpiryYear { get; set; }

        [Display(Name = "CVV / Security Code")]
        public string? Cvv { get; set; }

        // Mobile Wallets (JazzCash / Easypaisa)
        [Display(Name = "Mobile Account Number")]
        public string? MobileAccountNumber { get; set; }

        [Display(Name = "CNIC Last 6 Digits (Optional for JazzCash)")]
        public string? CnicLast6Digits { get; set; }

        // Manual Payment / Bank Transfer / Direct Deposit
        [Display(Name = "Deposit / Transaction Reference / Slip ID")]
        public string? DepositReference { get; set; }

        [Display(Name = "Depositor Bank / Transfer Channel")]
        public string? DepositorBank { get; set; }

        [Display(Name = "Deposit Date")]
        [DataType(DataType.Date)]
        public System.DateTime? DepositDate { get; set; }

        [Display(Name = "Deposit Slip / Transfer Proof File")]
        public Microsoft.AspNetCore.Http.IFormFile? ProofFile { get; set; }

        [Display(Name = "Additional Transfer Details / Notes")]
        public string? TransferNotes { get; set; }

        // Available Causes for Dropdown
        public IEnumerable<DonationCause>? AvailableCauses { get; set; }
        public DonationCause? SelectedCause { get; set; }
    }

    public class DonationReceiptViewModel
    {
        public int DonationId { get; set; }
        public string ReceiptNumber { get; set; } = string.Empty;
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public string? DonorPhone { get; set; }
        public string CauseTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public string PaymentMethod { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string? GatewayTransactionId { get; set; }
        public System.DateTime IssuedAt { get; set; }
        public string Status { get; set; } = "Paid";
        public string? Message { get; set; }
    }
}
