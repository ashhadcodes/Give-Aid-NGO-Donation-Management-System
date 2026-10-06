using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System;
using System.Collections.Generic;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public class PaymentRequestDto
    {
        public int DonationId { get; set; }
        public string InternalTransactionId { get; set; } = Guid.NewGuid().ToString("N");
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "PKR";
        public string DonorName { get; set; } = string.Empty;
        public string DonorEmail { get; set; } = string.Empty;
        public string? DonorPhone { get; set; }
        public PaymentProvider PaymentMethod { get; set; }
        public string? ReturnUrl { get; set; }

        // Card payment specific (never stored in database)
        public string? CardNumber { get; set; }
        public string? CardHolderName { get; set; }
        public string? ExpiryMonth { get; set; }
        public string? ExpiryYear { get; set; }
        public string? Cvv { get; set; }

        // Mobile Wallet / Gateway Specific
        public string? MobileAccountNumber { get; set; }
        public string? CnicLast6Digits { get; set; }
        public string? Description { get; set; }

        // Manual Bank Transfer / Direct Deposit Specific
        public string? DepositReference { get; set; }
        public string? DepositorBank { get; set; }
        public System.DateTime? DepositDate { get; set; }
        public string? ProofFilePath { get; set; }
        public string? TransferNotes { get; set; }
    }

    public class PaymentResultDto
    {
        public bool IsSuccessful { get; set; }
        public bool RequiresRedirect { get; set; }
        public string? RedirectUrl { get; set; }
        public IDictionary<string, string>? PostData { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string? GatewayTransactionId { get; set; }
        public DonationStatus Status { get; set; } = DonationStatus.Pending;
        public string Message { get; set; } = string.Empty;
        public string? FailureReason { get; set; }
        public string? MaskedCardNumber { get; set; }
        public string? CardBrand { get; set; }
        public string? RawResponse { get; set; }
    }

    public class PaymentCallbackDto
    {
        public PaymentProvider Provider { get; set; }
        public string? TransactionId { get; set; }
        public string? GatewayTransactionId { get; set; }
        public string? ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? SecureHash { get; set; }
        public decimal? Amount { get; set; }
        public IDictionary<string, string> FormFields { get; set; } = new Dictionary<string, string>();
    }

    public class PaymentRefundDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }
}
