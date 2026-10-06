using Give_Aid_NGO_Donation_Management_System.Data;
using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
        {
            var totalDonationsCount = await _context.Donations.CountAsync();
            var totalDonationAmount = await _context.Donations
                .Where(d => d.Status == DonationStatus.Paid)
                .SumAsync(d => (decimal?)d.Amount) ?? 0m;

            var successfulPaymentsCount = await _context.PaymentTransactions
                .CountAsync(p => p.Status == DonationStatus.Paid);

            var pendingPaymentsCount = await _context.PaymentTransactions
                .CountAsync(p => p.Status == DonationStatus.Pending || p.Status == DonationStatus.Processing);

            var failedPaymentsCount = await _context.PaymentTransactions
                .CountAsync(p => p.Status == DonationStatus.Failed);

            var totalUsersCount = await _context.Users.CountAsync();
            var activeCausesCount = await _context.DonationCauses.CountAsync(c => c.IsActive);
            var activeNgosCount = await _context.NGOs.CountAsync(n => n.IsActive);
            var activeProgrammesCount = await _context.Programmes.CountAsync(p => p.Status == ProgrammeStatus.Upcoming || p.Status == ProgrammeStatus.Ongoing);
            var pendingQueriesCount = await _context.SupportQueries.CountAsync(q => q.Status == QueryStatus.Open || q.Status == QueryStatus.InProgress);
            var newContactMessagesCount = await _context.ContactMessages.CountAsync(m => !m.IsRead);
            var activePartnersCount = await _context.Partners.CountAsync(p => p.IsActive);

            var recentDonations = await _context.Donations
                .Include(d => d.Cause)
                .Include(d => d.PaymentTransactions)
                .OrderByDescending(d => d.CreatedAt)
                .Take(7)
                .ToListAsync();

            var recentAuditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.CreatedAt)
                .Take(10)
                .ToListAsync();

            var recentQueries = await _context.SupportQueries
                .OrderByDescending(q => q.CreatedAt)
                .Take(5)
                .ToListAsync();

            // Monthly charts for last 6 months
            var monthlyData = new List<MonthlyChartItem>();
            var now = DateTime.UtcNow;
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var startOfMonth = new DateTime(monthDate.Year, monthDate.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1);

                var monthPaidDonations = await _context.Donations
                    .Where(d => d.Status == DonationStatus.Paid && d.CreatedAt >= startOfMonth && d.CreatedAt <= endOfMonth)
                    .ToListAsync();

                monthlyData.Add(new MonthlyChartItem
                {
                    MonthName = startOfMonth.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    TotalAmount = monthPaidDonations.Sum(d => d.Amount),
                    Count = monthPaidDonations.Count
                });
            }

            // Cause distribution
            var causeData = await _context.DonationCauses
                .Select(c => new CategoryChartItem
                {
                    Label = c.Title,
                    Amount = c.RaisedAmount,
                    Count = c.Donations.Count(d => d.Status == DonationStatus.Paid)
                })
                .OrderByDescending(c => c.Amount)
                .Take(6)
                .ToListAsync();

            // Payment method distribution
            var paymentMethods = await _context.PaymentTransactions
                .Where(p => p.Status == DonationStatus.Paid)
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new CategoryChartItem
                {
                    Label = g.Key.ToString(),
                    Amount = g.Sum(x => x.Amount),
                    Count = g.Count()
                })
                .ToListAsync();

            return new DashboardSummaryDto
            {
                TotalDonationsCount = totalDonationsCount,
                TotalDonationAmount = totalDonationAmount,
                SuccessfulPaymentsCount = successfulPaymentsCount,
                PendingPaymentsCount = pendingPaymentsCount,
                FailedPaymentsCount = failedPaymentsCount,
                TotalUsersCount = totalUsersCount,
                ActiveCausesCount = activeCausesCount,
                ActiveNgosCount = activeNgosCount,
                ActiveProgrammesCount = activeProgrammesCount,
                PendingQueriesCount = pendingQueriesCount,
                NewContactMessagesCount = newContactMessagesCount,
                ActivePartnersCount = activePartnersCount,
                RecentDonations = recentDonations,
                RecentAuditLogs = recentAuditLogs,
                RecentQueries = recentQueries,
                MonthlyDonations = monthlyData,
                DonationsByCause = causeData,
                PaymentMethodDistribution = paymentMethods
            };
        }
    }
}
