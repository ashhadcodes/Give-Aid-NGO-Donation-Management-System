using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using Give_Aid_NGO_Donation_Management_System.Services.Payments;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Interfaces
{
    public class DonationFilterParams
    {
        public string? Search { get; set; }
        public int? CauseId { get; set; }
        public DonationStatus? Status { get; set; }
        public PaymentProvider? PaymentMethod { get; set; }
        public System.DateTime? FromDate { get; set; }
        public System.DateTime? ToDate { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)System.Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public interface IDonationService
    {
        Task<Donation> CreateDonationAsync(Donation donation, CancellationToken cancellationToken = default);
        Task<PaymentResultDto> InitiateAndProcessPaymentAsync(int donationId, PaymentRequestDto paymentRequest, CancellationToken cancellationToken = default);
        Task<PaymentResultDto> HandlePaymentCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default);
        Task<Donation?> GetDonationByIdAsync(int id);
        Task<IEnumerable<Donation>> GetDonationsByUserIdAsync(string userId);
        Task<PagedResult<Donation>> GetPagedDonationsAsync(DonationFilterParams filterParams);
        Task<bool> RefundDonationAsync(int donationId, string? reason, CancellationToken cancellationToken = default);
        Task<bool> ApproveManualDonationAsync(int donationId, string adminUserId, string adminEmail, string? notes, CancellationToken cancellationToken = default);
        Task<bool> RejectManualDonationAsync(int donationId, string adminUserId, string adminEmail, string reason, CancellationToken cancellationToken = default);
    }
}
