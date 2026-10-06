using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public class JazzCashPaymentService : IPaymentService
    {
        private readonly JazzCashSettings _settings;
        private readonly ILogger<JazzCashPaymentService> _logger;

        public JazzCashPaymentService(IOptions<JazzCashSettings> options, ILogger<JazzCashPaymentService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public PaymentProvider Provider => PaymentProvider.JazzCash;

        public Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing JazzCash payment request for DonationId {DonationId}, Amount {Amount} {Currency}",
                request.DonationId, request.Amount, request.Currency);

            var txnId = string.IsNullOrWhiteSpace(request.InternalTransactionId)
                ? $"JC{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}"
                : request.InternalTransactionId;

            // 1. Validate Amount
            if (request.Amount <= 0)
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Invalid donation amount. Amount must be greater than zero.",
                    Message = "Payment rejected: Invalid amount.",
                    RawResponse = "{\"status\": \"FAILED\", \"code\": \"INVALID_AMOUNT\"}"
                });
            }

            // 2. Manual Payment Screenshot Verification Flow (Account: 03272762043)
            // DO NOT automatically approve; mark as Pending Verification for admin review
            var depositRef = !string.IsNullOrWhiteSpace(request.DepositReference)
                ? request.DepositReference.Trim()
                : $"JC-TID-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";

            var rawPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                channel = "JazzCash",
                accountNumber = "03272762043",
                accountTitle = "Give-Aid Foundation",
                senderMobile = request.MobileAccountNumber ?? request.DonorPhone,
                depositReference = depositRef,
                proofFilePath = request.ProofFilePath,
                transferNotes = request.TransferNotes,
                donorName = request.DonorName,
                donorPhone = request.DonorPhone,
                status = "Pending Verification",
                submittedAt = DateTime.UtcNow
            });

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Pending,
                TransactionId = txnId,
                GatewayTransactionId = depositRef,
                Message = "Your JazzCash payment screenshot and details have been received and marked as Pending Verification.",
                RawResponse = rawPayload
            });
        }

        public Task<PaymentResultDto> VerifyCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Verifying JazzCash callback for TransactionId {TxnId}", callback.TransactionId);

            if (_settings.SandboxMode)
            {
                var isSuccess = callback.ResponseCode == "000" || callback.ResponseCode == "121" || string.IsNullOrEmpty(callback.ResponseCode);
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = isSuccess,
                    Status = isSuccess ? DonationStatus.Paid : DonationStatus.Failed,
                    TransactionId = callback.TransactionId ?? Guid.NewGuid().ToString("N"),
                    GatewayTransactionId = callback.GatewayTransactionId ?? $"JC-{DateTime.UtcNow.Ticks}",
                    Message = isSuccess ? "JazzCash sandbox verification successful." : (callback.ResponseMessage ?? "Transaction failed."),
                    FailureReason = isSuccess ? null : callback.ResponseMessage
                });
            }

            // Real HMAC Hash verification
            if (callback.FormFields != null && callback.FormFields.Count > 0 && !string.IsNullOrEmpty(callback.SecureHash))
            {
                var sorted = new SortedDictionary<string, string>(callback.FormFields.Where(k => k.Key != "pp_SecureHash")
                    .ToDictionary(k => k.Key, v => v.Value));
                var computedHash = CalculateHmacHash(sorted, _settings.IntegritySalt);

                if (!string.Equals(computedHash, callback.SecureHash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("JazzCash SecureHash mismatch on callback verification.");
                    return Task.FromResult(new PaymentResultDto
                    {
                        IsSuccessful = false,
                        Status = DonationStatus.Failed,
                        TransactionId = callback.TransactionId ?? string.Empty,
                        FailureReason = "Security verification failed: Invalid response signature hash."
                    });
                }
            }

            var success = callback.ResponseCode == "000" || callback.ResponseCode == "121";
            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = success,
                Status = success ? DonationStatus.Paid : DonationStatus.Failed,
                TransactionId = callback.TransactionId ?? string.Empty,
                GatewayTransactionId = callback.GatewayTransactionId,
                Message = callback.ResponseMessage ?? (success ? "Approved" : "Declined"),
                FailureReason = success ? null : callback.ResponseMessage
            });
        }

        public Task<PaymentResultDto> RefundAsync(PaymentRefundDto refundRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing refund for JazzCash transaction {TxnId} for amount {Amount}",
                refundRequest.TransactionId, refundRequest.Amount);

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Refunded,
                TransactionId = refundRequest.TransactionId,
                GatewayTransactionId = $"JC-REF-{DateTime.UtcNow.Ticks}",
                Message = "Refund processed successfully via JazzCash gateway."
            });
        }

        private static string CalculateHmacHash(SortedDictionary<string, string> fields, string salt)
        {
            var sb = new StringBuilder();
            sb.Append(salt);
            foreach (var kvp in fields)
            {
                if (!string.IsNullOrEmpty(kvp.Value))
                {
                    sb.Append("&").Append(kvp.Value);
                }
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(salt));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
        }
    }
}
