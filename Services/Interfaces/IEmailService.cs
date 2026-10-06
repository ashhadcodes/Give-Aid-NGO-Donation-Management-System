using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<bool> SendRegistrationWelcomeAsync(string toEmail, string userName);
        Task<bool> SendPasswordResetAsync(string toEmail, string resetLink);
        Task<bool> SendDonationReceiptEmailAsync(string toEmail, string donorName, string receiptNumber, decimal amount, string currency, string causeTitle, string transactionId);
        Task<bool> SendDonationFailureNotificationAsync(string toEmail, string donorName, decimal amount, string currency, string failureReason);
        Task<bool> SendQueryResponseEmailAsync(string toEmail, string userName, string querySubject, string adminResponse);
        Task<bool> SendFriendInvitationEmailAsync(string toEmail, string friendName, string senderName, string? message, string siteUrl);
        Task<bool> SendProgrammeInterestConfirmationAsync(string toEmail, string userName, string programmeTitle, string location, string startDate);
    }
}
