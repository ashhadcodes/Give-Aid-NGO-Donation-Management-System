using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public class CardPaymentService : IPaymentService
    {
        private readonly CardPaymentSettings _settings;
        private readonly ILogger<CardPaymentService> _logger;

        public CardPaymentService(IOptions<CardPaymentSettings> options, ILogger<CardPaymentService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public PaymentProvider Provider => PaymentProvider.Card;

        public Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing Credit/Debit Card payment for DonationId {DonationId}, Amount {Amount} {Currency}",
                request.DonationId, request.Amount, request.Currency);

            var txnId = string.IsNullOrWhiteSpace(request.InternalTransactionId)
                ? $"CARD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}"
                : request.InternalTransactionId;

            // Sanitize raw card input
            var rawCardNumber = (request.CardNumber ?? "").Replace(" ", "").Replace("-", "");

            // 1. Basic Amount Validation
            if (request.Amount <= 0)
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Payment amount must be greater than zero.",
                    Message = "Transaction failed."
                });
            }

            // 2. Validate Card Number using Luhn Algorithm
            if (string.IsNullOrWhiteSpace(rawCardNumber) || rawCardNumber.Length < 13 || rawCardNumber.Length > 19 || !rawCardNumber.All(char.IsDigit))
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Invalid card number format.",
                    Message = "Payment declined: Card number is invalid."
                });
            }

            if (!ValidateLuhn(rawCardNumber))
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Card number failed Luhn checksum validation.",
                    Message = "Payment declined: Invalid card number."
                });
            }

            // 3. Expiry date validation
            if (!int.TryParse(request.ExpiryMonth, out int expMonth) || expMonth < 1 || expMonth > 12)
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Invalid expiry month.",
                    Message = "Payment declined: Invalid expiry date."
                });
            }

            if (!int.TryParse(request.ExpiryYear, out int expYear) || expYear < 2024 || expYear > 2050)
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Invalid expiry year.",
                    Message = "Payment declined: Invalid expiry date."
                });
            }

            var cardExpiry = new DateTime(expYear, expMonth, DateTime.DaysInMonth(expYear, expMonth), 23, 59, 59);
            if (cardExpiry < DateTime.UtcNow)
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Card has expired.",
                    Message = "Payment declined: Card is expired."
                });
            }

            // 4. CVV Validation (3 or 4 digits)
            var cvv = (request.Cvv ?? "").Trim();
            if (cvv.Length < 3 || cvv.Length > 4 || !cvv.All(char.IsDigit))
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Invalid CVV / Security code.",
                    Message = "Payment declined: Invalid CVV."
                });
            }

            // Detect card brand and mask card
            var brand = DetectCardBrand(rawCardNumber);
            var last4 = rawCardNumber.Substring(rawCardNumber.Length - 4);
            var maskedCard = $"**** **** **** {last4}";

            // Simulated Gateway Decline scenarios for testing:
            // If CVV is 000 -> Simulate 3D Secure / Bank Decline
            if (cvv == "000")
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    GatewayTransactionId = $"GW-DEC-{DateTime.UtcNow.Ticks}",
                    MaskedCardNumber = maskedCard,
                    CardBrand = brand,
                    FailureReason = "Card issuer declined authorization (Simulated decline scenario CVV 000).",
                    Message = "Payment declined by issuing bank.",
                    RawResponse = "{\"gatewayCode\": \"DO_NOT_HONOR\", \"avsCode\": \"N\", \"cvvMatch\": false}"
                });
            }

            var gatewayTxnId = $"CARD-AUTH-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(10000, 99999)}";

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Paid,
                TransactionId = txnId,
                GatewayTransactionId = gatewayTxnId,
                MaskedCardNumber = maskedCard,
                CardBrand = brand,
                Message = $"Card payment of {request.Amount:N2} {request.Currency} approved successfully via {brand}.",
                RawResponse = $"{{\"status\": \"APPROVED\", \"authCode\": \"{new Random().Next(100000, 999999)}\", \"gatewayReference\": \"{gatewayTxnId}\"}}"
            });
        }

        public Task<PaymentResultDto> VerifyCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Paid,
                TransactionId = callback.TransactionId ?? Guid.NewGuid().ToString("N"),
                GatewayTransactionId = callback.GatewayTransactionId,
                Message = "Card callback verified."
            });
        }

        public Task<PaymentResultDto> RefundAsync(PaymentRefundDto refundRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refunding Card transaction {TxnId} for amount {Amount}",
                refundRequest.TransactionId, refundRequest.Amount);

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Refunded,
                TransactionId = refundRequest.TransactionId,
                GatewayTransactionId = $"CARD-REF-{DateTime.UtcNow.Ticks}",
                Message = "Card refund processed successfully."
            });
        }

        public static bool ValidateLuhn(string cardNumber)
        {
            if (string.IsNullOrWhiteSpace(cardNumber)) return false;

            int sum = 0;
            bool alternate = false;
            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                if (!char.IsDigit(cardNumber[i])) return false;
                int n = cardNumber[i] - '0';
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }

            return (sum % 10 == 0);
        }

        public static string DetectCardBrand(string cardNumber)
        {
            if (cardNumber.StartsWith("4")) return "Visa";
            if (Regex.IsMatch(cardNumber, @"^(5[1-5]|222[1-9]|22[3-9]\d|2[3-6]\d{2}|27[01]\d|2720)")) return "Mastercard";
            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37")) return "American Express";
            if (cardNumber.StartsWith("6011") || cardNumber.StartsWith("65")) return "Discover";
            if (cardNumber.StartsWith("35")) return "JCB";
            if (cardNumber.StartsWith("62")) return "UnionPay";
            if (cardNumber.StartsWith("50") || cardNumber.StartsWith("58") || cardNumber.StartsWith("67")) return "PayPak / Maestro";
            return "Credit/Debit Card";
        }
    }
}
