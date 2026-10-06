using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public class BankTransferPaymentService : IPaymentService
    {
        private readonly ILogger<BankTransferPaymentService> _logger;

        public BankTransferPaymentService(ILogger<BankTransferPaymentService> logger)
        {
            _logger = logger;
        }

        public PaymentProvider Provider => PaymentProvider.BankTransfer;

        public Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing manual Bank Transfer / Direct Deposit for DonationId {DonationId}, Amount {Amount} {Currency}, Ref: {Ref}",
                request.DonationId, request.Amount, request.Currency, request.DepositReference);

            var txnId = string.IsNullOrWhiteSpace(request.InternalTransactionId)
                ? $"BANK{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}"
                : request.InternalTransactionId;

            var depositRef = string.IsNullOrWhiteSpace(request.DepositReference)
                ? $"DEP-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}"
                : request.DepositReference.Trim();

            var rawPayload = JsonSerializer.Serialize(new
            {
                channel = request.DepositorBank ?? "Bank Transfer",
                depositReference = depositRef,
                depositDate = request.DepositDate?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
                proofFilePath = request.ProofFilePath,
                transferNotes = request.TransferNotes,
                donorName = request.DonorName,
                donorPhone = request.DonorPhone,
                status = "Awaiting Verification",
                submittedAt = DateTime.UtcNow
            });

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Pending,
                TransactionId = txnId,
                GatewayTransactionId = depositRef,
                Message = "Manual payment submission received successfully. Awaiting admin review.",
                RawResponse = rawPayload
            });
        }

        public Task<PaymentResultDto> VerifyCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Pending,
                TransactionId = callback.TransactionId ?? Guid.NewGuid().ToString("N"),
                GatewayTransactionId = callback.GatewayTransactionId,
                Message = "Manual payment verification recorded."
            });
        }

        public Task<PaymentResultDto> RefundAsync(PaymentRefundDto refundRequest, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Processing Manual Bank Transfer reversal / refund for Transaction {TxnId} for amount {Amount}",
                refundRequest.TransactionId, refundRequest.Amount);

            return Task.FromResult(new PaymentResultDto
            {
                IsSuccessful = true,
                Status = DonationStatus.Refunded,
                TransactionId = refundRequest.TransactionId,
                GatewayTransactionId = $"BANK-REF-{DateTime.UtcNow.Ticks}",
                Message = "Manual payment refund marked successfully."
            });
        }
    }
}
