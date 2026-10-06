using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Give_Aid_NGO_Donation_Management_System.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Implementations
{
    public class DonationService : IDonationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentServiceResolver _paymentResolver;
        private readonly IReceiptService _receiptService;
        private readonly IEmailService _emailService;
        private readonly IAuditService _auditService;
        private readonly ILogger<DonationService> _logger;

        public DonationService(
            ApplicationDbContext context,
            IPaymentServiceResolver paymentResolver,
            IReceiptService receiptService,
            IEmailService emailService,
            IAuditService auditService,
            ILogger<DonationService> logger)
        {
            _context = context;
            _paymentResolver = paymentResolver;
            _receiptService = receiptService;
            _emailService = emailService;
            _auditService = auditService;
            _logger = logger;
        }

        public async Task<Donation> CreateDonationAsync(Donation donation, CancellationToken cancellationToken = default)
        {
            if (donation.Amount <= 0)
            {
                throw new ArgumentException("Donation amount must be greater than zero.", nameof(donation.Amount));
            }

            var cause = await _context.DonationCauses.FindAsync(new object[] { donation.CauseId }, cancellationToken);
            if (cause == null || !cause.IsActive)
            {
                throw new InvalidOperationException("The selected donation cause is invalid or inactive.");
            }

            donation.Status = DonationStatus.Pending;
            donation.CreatedAt = DateTime.UtcNow;

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created new pending donation ID {DonationId} for {Amount} {Currency}",
                donation.Id, donation.Amount, donation.Currency);

            return donation;
        }

        public async Task<PaymentResultDto> InitiateAndProcessPaymentAsync(int donationId, PaymentRequestDto paymentRequest, CancellationToken cancellationToken = default)
        {
            var donation = await _context.Donations
                .Include(d => d.Cause)
                .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

            if (donation == null)
            {
                return new PaymentResultDto
                {
                    IsSuccessful = false,
                    Status = DonationStatus.Failed,
                    Message = "Donation record not found."
                };
            }

            paymentRequest.DonationId = donation.Id;
            paymentRequest.Amount = donation.Amount;
            paymentRequest.Currency = donation.Currency;
            paymentRequest.DonorEmail = donation.DonorEmail;
            paymentRequest.DonorName = donation.DonorName;
            paymentRequest.DonorPhone = donation.DonorPhone;

            var paymentService = _paymentResolver.GetPaymentService(paymentRequest.PaymentMethod);

            // Record transaction attempt
            var txn = new PaymentTransaction
            {
                DonationId = donation.Id,
                TransactionId = paymentRequest.InternalTransactionId,
                PaymentMethod = paymentRequest.PaymentMethod,
                GatewayName = paymentService.Provider.ToString(),
                Amount = donation.Amount,
                Currency = donation.Currency,
                Status = DonationStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(txn);
            await _context.SaveChangesAsync(cancellationToken);

            // Invoke payment provider
            var result = await paymentService.ProcessPaymentAsync(paymentRequest, cancellationToken);

            // Update transaction and donation
            txn.GatewayTransactionId = result.GatewayTransactionId;
            txn.Status = result.Status;
            txn.FailureReason = result.FailureReason;
            txn.MaskedCardNumber = result.MaskedCardNumber;
            txn.CardBrand = result.CardBrand;
            txn.GatewayRawResponse = result.RawResponse;
            txn.UpdatedAt = DateTime.UtcNow;

            donation.Status = result.Status;
            donation.UpdatedAt = DateTime.UtcNow;

            if (result.IsSuccessful && result.Status == DonationStatus.Paid)
            {
                // Update cause raised amount
                if (donation.Cause != null)
                {
                    donation.Cause.RaisedAmount += donation.Amount;
                    donation.Cause.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync(cancellationToken);

                // Generate Receipt
                var receipt = await _receiptService.GenerateReceiptAsync(donation.Id);

                // Send email receipt
                _ = _emailService.SendDonationReceiptEmailAsync(
                    donation.DonorEmail,
                    donation.DonorName,
                    receipt.ReceiptNumber,
                    donation.Amount,
                    donation.Currency,
                    donation.Cause?.Title ?? "Welfare Cause",
                    txn.TransactionId
                );

                await _auditService.LogAsync(
                    "DonationPaid",
                    nameof(Donation),
                    donation.Id.ToString(),
                    $"Successful donation of {donation.Amount:N2} {donation.Currency} for cause '{donation.Cause?.Title}' via {paymentRequest.PaymentMethod}",
                    donation.UserId,
                    donation.DonorName
                );
            }
            else if (!result.IsSuccessful)
            {
                await _context.SaveChangesAsync(cancellationToken);

                _ = _emailService.SendDonationFailureNotificationAsync(
                    donation.DonorEmail,
                    donation.DonorName,
                    donation.Amount,
                    donation.Currency,
                    result.FailureReason ?? "Transaction could not be completed."
                );
            }
            else
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return result;
        }

        public async Task<PaymentResultDto> HandlePaymentCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default)
        {
            var paymentService = _paymentResolver.GetPaymentService(callback.Provider);
            var result = await paymentService.VerifyCallbackAsync(callback, cancellationToken);

            var txn = await _context.PaymentTransactions
                .Include(p => p.Donation)
                    .ThenInclude(d => d!.Cause)
                .FirstOrDefaultAsync(p => p.TransactionId == callback.TransactionId, cancellationToken);

            if (txn != null && txn.Donation != null)
            {
                txn.GatewayTransactionId = result.GatewayTransactionId ?? txn.GatewayTransactionId;
                txn.Status = result.Status;
                txn.FailureReason = result.FailureReason;
                txn.UpdatedAt = DateTime.UtcNow;

                txn.Donation.Status = result.Status;
                txn.Donation.UpdatedAt = DateTime.UtcNow;

                if (result.IsSuccessful && result.Status == DonationStatus.Paid)
                {
                    if (txn.Donation.Cause != null)
                    {
                        txn.Donation.Cause.RaisedAmount += txn.Donation.Amount;
                    }

                    await _context.SaveChangesAsync(cancellationToken);

                    var receipt = await _receiptService.GenerateReceiptAsync(txn.Donation.Id);
                    _ = _emailService.SendDonationReceiptEmailAsync(
                        txn.Donation.DonorEmail,
                        txn.Donation.DonorName,
                        receipt.ReceiptNumber,
                        txn.Donation.Amount,
                        txn.Donation.Currency,
                        txn.Donation.Cause?.Title ?? "Welfare Cause",
                        txn.TransactionId
                    );
                }
                else
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }

            return result;
        }

        public async Task<Donation?> GetDonationByIdAsync(int id)
        {
            return await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.User)
                .Include(d => d.PaymentTransactions)
                .Include(d => d.Receipt)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Donation>> GetDonationsByUserIdAsync(string userId)
        {
            return await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.Receipt)
                .Include(d => d.PaymentTransactions)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<PagedResult<Donation>> GetPagedDonationsAsync(DonationFilterParams filter)
        {
            var query = _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.User)
                .Include(d => d.PaymentTransactions)
                .Include(d => d.Receipt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search.Trim().ToLower();
                query = query.Where(d => d.DonorName.ToLower().Contains(term) ||
                                         d.DonorEmail.ToLower().Contains(term) ||
                                         (d.Receipt != null && d.Receipt.ReceiptNumber.ToLower().Contains(term)) ||
                                         d.PaymentTransactions.Any(p => p.TransactionId.ToLower().Contains(term)));
            }

            if (filter.CauseId.HasValue && filter.CauseId.Value > 0)
            {
                query = query.Where(d => d.CauseId == filter.CauseId.Value);
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(d => d.Status == filter.Status.Value);
            }

            if (filter.PaymentMethod.HasValue)
            {
                query = query.Where(d => d.PaymentTransactions.Any(p => p.PaymentMethod == filter.PaymentMethod.Value));
            }

            if (filter.FromDate.HasValue)
            {
                query = query.Where(d => d.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var endOfDay = filter.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(d => d.CreatedAt <= endOfDay);
            }

            var totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(d => d.CreatedAt)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<Donation>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        public async Task<bool> RefundDonationAsync(int donationId, string? reason, CancellationToken cancellationToken = default)
        {
            var donation = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

            if (donation == null || donation.Status != DonationStatus.Paid)
            {
                return false;
            }

            var lastTxn = donation.PaymentTransactions.LastOrDefault(p => p.Status == DonationStatus.Paid);
            if (lastTxn != null)
            {
                var paymentService = _paymentResolver.GetPaymentService(lastTxn.PaymentMethod);
                await paymentService.RefundAsync(new PaymentRefundDto
                {
                    TransactionId = lastTxn.TransactionId,
                    Amount = donation.Amount,
                    Reason = reason
                }, cancellationToken);

                lastTxn.Status = DonationStatus.Refunded;
                lastTxn.UpdatedAt = DateTime.UtcNow;
            }

            donation.Status = DonationStatus.Refunded;
            donation.UpdatedAt = DateTime.UtcNow;

            if (donation.Cause != null && donation.Cause.RaisedAmount >= donation.Amount)
            {
                donation.Cause.RaisedAmount -= donation.Amount;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                "DonationRefunded",
                nameof(Donation),
                donation.Id.ToString(),
                $"Refunded donation #{donation.Id} of {donation.Amount:N2} {donation.Currency}. Reason: {reason ?? "Admin initiated refund"}"
            );

            return true;
        }

        public async Task<bool> ApproveManualDonationAsync(int donationId, string adminUserId, string adminEmail, string? notes, CancellationToken cancellationToken = default)
        {
            var donation = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .Include(d => d.Receipt)
                .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

            if (donation == null || donation.Status == DonationStatus.Paid)
            {
                return false;
            }

            donation.Status = DonationStatus.Paid;
            donation.UpdatedAt = DateTime.UtcNow;

            if (donation.Cause != null)
            {
                donation.Cause.RaisedAmount += donation.Amount;
                donation.Cause.UpdatedAt = DateTime.UtcNow;
            }

            var lastTxn = donation.PaymentTransactions.LastOrDefault();
            if (lastTxn != null)
            {
                lastTxn.Status = DonationStatus.Paid;
                lastTxn.UpdatedAt = DateTime.UtcNow;
                lastTxn.FailureReason = null;
            }
            else
            {
                var newTxn = new PaymentTransaction
                {
                    DonationId = donation.Id,
                    TransactionId = Guid.NewGuid().ToString("N"),
                    PaymentMethod = PaymentProvider.BankTransfer,
                    GatewayName = "Bank Transfer / Manual",
                    Amount = donation.Amount,
                    Currency = donation.Currency,
                    Status = DonationStatus.Paid,
                    CreatedAt = DateTime.UtcNow
                };
                donation.PaymentTransactions.Add(newTxn);
                lastTxn = newTxn;
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Generate verified receipt
            var receipt = await _receiptService.GenerateReceiptAsync(donation.Id);

            // Send official email receipt
            _ = _emailService.SendDonationReceiptEmailAsync(
                donation.DonorEmail,
                donation.DonorName,
                receipt.ReceiptNumber,
                donation.Amount,
                donation.Currency,
                donation.Cause?.Title ?? "Welfare Cause",
                lastTxn.TransactionId
            );

            await _auditService.LogAsync(
                "ManualDonationApproved",
                nameof(Donation),
                donation.Id.ToString(),
                $"Manual donation #{donation.Id} of {donation.Amount:N2} {donation.Currency} for '{donation.Cause?.Title}' verified and approved by {adminEmail}. Remarks: {notes ?? "Deposit slip verified"}",
                adminUserId,
                adminEmail
            );

            return true;
        }

        public async Task<bool> RejectManualDonationAsync(int donationId, string adminUserId, string adminEmail, string reason, CancellationToken cancellationToken = default)
        {
            var donation = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .FirstOrDefaultAsync(d => d.Id == donationId, cancellationToken);

            if (donation == null || donation.Status == DonationStatus.Paid)
            {
                return false;
            }

            donation.Status = DonationStatus.Failed;
            donation.UpdatedAt = DateTime.UtcNow;

            var lastTxn = donation.PaymentTransactions.LastOrDefault();
            if (lastTxn != null)
            {
                lastTxn.Status = DonationStatus.Failed;
                lastTxn.FailureReason = reason;
                lastTxn.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _ = _emailService.SendDonationFailureNotificationAsync(
                donation.DonorEmail,
                donation.DonorName,
                donation.Amount,
                donation.Currency,
                $"Manual payment could not be verified: {reason}"
            );

            await _auditService.LogAsync(
                "ManualDonationRejected",
                nameof(Donation),
                donation.Id.ToString(),
                $"Manual donation #{donation.Id} was rejected by {adminEmail}. Reason: {reason}",
                adminUserId,
                adminEmail
            );

            return true;
        }
    }
}
