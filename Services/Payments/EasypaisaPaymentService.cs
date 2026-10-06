using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public class EasypaisaPaymentService : IPaymentService
    {
        private readonly EasypaisaSettings _settings;
        private readonly ILogger<EasypaisaPaymentService> _logger;

        public EasypaisaPaymentService(IOptions<EasypaisaSettings> options, ILogger<EasypaisaPaymentService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public PaymentProvider Provider => PaymentProvider.Easypaisa;

        public Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing Easypaisa payment for DonationId {DonationId}, Amount {Amount} {Currency}",
                request.DonationId, request.Amount, request.Currency);

            var txnId = string.IsNullOrWhiteSpace(request.InternalTransactionId)
                ? $"EP{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}"
                : request.InternalTransactionId;

            // 1. Validate Amount
            if (request.Amount <= 0)
            {
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    TransactionId = txnId,
                    FailureReason = "Invalid donation amount. Amount must be positive.",
                    Message = "Payment rejected.",
                    RawResponse = "{\"responseCode\": \"0001\", \"desc\": \"Invalid Amount\"}"
                });
            }

            // 2. Manual Payment Screenshot Verification Flow (Account: 03272762043)
            // DO NOT automatically approve; mark as Pending Verification for admin review
            var depositRef = !string.IsNullOrWhiteSpace(request.DepositReference)
                ? request.DepositReference.Trim()
                : $"EP-TID-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(100, 999)}";

            var rawPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                channel = "Easypaisa",
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
                Message = "Your Easypaisa payment screenshot and details have been received and marked as Pending Verification.",
                RawResponse = rawPayload
            });
        }

        public Task<PaymentResultDto> VerifyCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Verifying Easypaisa callback for TransactionId {TxnId}", callback.TransactionId);

            if (_settings.SandboxMode)
            {
                var isOk = callback.ResponseCode == "0000" || string.IsNullOrEmpty(callback.ResponseCode);
                return Task.FromResult(new PaymentResultDto
                {
                    IsSuccessful = isOk,
                    Status = isOk ? DonationStatus.Paid : DonationStatus.Failed,
                    TransactionId = callback.TransactionId ?? Guid.NewGuid().ToString("N"),
                    GatewayTransactionId = callback.GatewayTransactionId ?? $"EP-{DateTime.UtcNow.Ticks}",
                    Message = isOk ? "Easypaisa verification successful." : (callback.ResponseMessage ?? "Payment failed"),
                    FailureReason = isOk ? null : callback.ResponseMessage
                });
            }

            var success = callback.ResponseCode == "0000";
            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = success,
                Status = success ? DonationStatus.Paid : DonationStatus.Failed,
                TransactionId = callback.TransactionId ?? string.Empty,
                GatewayTransactionId = callback.GatewayTransactionId,
                Message = callback.ResponseMessage ?? (success ? "Approved" : "Failed"),
                FailureReason = success ? null : callback.ResponseMessage
            });
        }

        public Task<PaymentResultDto> RefundAsync(PaymentRefundDto refundRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Refunding Easypaisa transaction {TxnId} for amount {Amount}",
                refundRequest.TransactionId, refundRequest.Amount);

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Refunded,
                TransactionId = refundRequest.TransactionId,
                GatewayTransactionId = $"EP-REF-{DateTime.UtcNow.Ticks}",
                Message = "Refund processed successfully via Easypaisa."
            });
        }

        private static string GenerateChecksum(Dictionary<string, string> parameters, string hashKey)
        {
            var raw = $"{parameters["storeId"]}&{parameters["orderId"]}&{parameters["transactionAmount"]}&{hashKey}";
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
            return Convert.ToBase64String(bytes);
        }
    }
}
