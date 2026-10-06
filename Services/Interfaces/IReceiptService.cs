using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Interfaces
{
    public interface IReceiptService
    {
        Task<DonationReceipt> GenerateReceiptAsync(int donationId);
        Task<DonationReceipt?> GetReceiptByNumberAsync(string receiptNumber);
        Task<DonationReceipt?> GetReceiptByDonationIdAsync(int donationId);
    }
}
