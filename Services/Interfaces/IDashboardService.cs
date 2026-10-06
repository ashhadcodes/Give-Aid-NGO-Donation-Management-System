using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Interfaces
{
    public class DashboardSummaryDto
    {
        public int TotalDonationsCount { get; set; }
        public decimal TotalDonationAmount { get; set; }
        public int SuccessfulPaymentsCount { get; set; }
        public int PendingPaymentsCount { get; set; }
        public int FailedPaymentsCount { get; set; }
        public int TotalUsersCount { get; set; }
        public int ActiveCausesCount { get; set; }
        public int ActiveNgosCount { get; set; }
        public int ActiveProgrammesCount { get; set; }
        public int PendingQueriesCount { get; set; }
        public int NewContactMessagesCount { get; set; }
        public int ActivePartnersCount { get; set; }

        public IEnumerable<Donation> RecentDonations { get; set; } = new List<Donation>();
        public IEnumerable<AuditLog> RecentAuditLogs { get; set; } = new List<AuditLog>();
        public IEnumerable<SupportQuery> RecentQueries { get; set; } = new List<SupportQuery>();

        // Chart structures
        public List<MonthlyChartItem> MonthlyDonations { get; set; } = new();
        public List<CategoryChartItem> DonationsByCause { get; set; } = new();
        public List<CategoryChartItem> PaymentMethodDistribution { get; set; } = new();
    }

    public class MonthlyChartItem
    {
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Count { get; set; }
    }

    public class CategoryChartItem
    {
        public string Label { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public interface IDashboardService
    {
        Task<DashboardSummaryDto> GetDashboardSummaryAsync();
    }
}
