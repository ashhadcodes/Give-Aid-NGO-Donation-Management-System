using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public interface IPaymentService
    {
        PaymentProvider Provider { get; }
        Task<PaymentResultDto> ProcessPaymentAsync(PaymentRequestDto request, CancellationToken cancellationToken = default);
        Task<PaymentResultDto> VerifyCallbackAsync(PaymentCallbackDto callback, CancellationToken cancellationToken = default);
        Task<PaymentResultDto> RefundAsync(PaymentRefundDto refundRequest, CancellationToken cancellationToken = default);
    }
}
