using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Implementations
{
    public class ReceiptService : IReceiptService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReceiptService> _logger;

        public ReceiptService(ApplicationDbContext context, ILogger<ReceiptService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DonationReceipt> GenerateReceiptAsync(int donationId)
        {
            var existingReceipt = await _context.DonationReceipts
                .FirstOrDefaultAsync(r => r.DonationId == donationId);

            if (existingReceipt != null)
            {
                return existingReceipt;
            }

            var donation = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .FirstOrDefaultAsync(d => d.Id == donationId);

            if (donation == null)
            {
                throw new InvalidOperationException($"Donation with ID {donationId} not found.");
            }

            var lastTxn = donation.PaymentTransactions.LastOrDefault();
            var paymentMethod = lastTxn?.PaymentMethod.ToString() ?? "Online";
            var txnRef = lastTxn?.TransactionId ?? Guid.NewGuid().ToString("N");

            var receiptNumber = $"GA-{DateTime.UtcNow:yyyyMMdd}-{donation.Id:D4}";

            var receipt = new DonationReceipt
            {
                DonationId = donation.Id,
                ReceiptNumber = receiptNumber,
                DonorName = donation.Anonymous ? "Anonymous Donor" : donation.DonorName,
                DonorEmail = donation.DonorEmail,
                CauseTitle = donation.Cause?.Title ?? "Welfare Fund",
                Amount = donation.Amount,
                Currency = donation.Currency,
                PaymentMethod = paymentMethod,
                TransactionReference = txnRef,
                DigitalSignature = $"SHA256-{Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"{receiptNumber}-{donation.Id}-{donation.Amount}-{donation.DonorEmail}")))[..24]}",
                IssuedAt = DateTime.UtcNow
            };

            _context.DonationReceipts.Add(receipt);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated Donation Receipt {ReceiptNumber} for DonationId {DonationId}", receiptNumber, donationId);
            return receipt;
        }

        public async Task<DonationReceipt?> GetReceiptByNumberAsync(string receiptNumber)
        {
            return await _context.DonationReceipts
                .Include(r => r.Donation)
                    .ThenInclude(d => d!.Cause)
                .FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber);
        }

        public async Task<DonationReceipt?> GetReceiptByDonationIdAsync(int donationId)
        {
            return await _context.DonationReceipts
                .Include(r => r.Donation)
                    .ThenInclude(d => d!.Cause)
                .FirstOrDefaultAsync(r => r.DonationId == donationId);
        }
    }
}
