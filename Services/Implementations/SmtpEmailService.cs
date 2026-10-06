using Give_Aid_NGO_Donation_Management_System.Configuration;
using Give_Aid_NGO_Donation_Management_System.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Give_Aid_NGO_Donation_Management_System.Services.Implementations
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IOptions<EmailSettings> options, ILogger<SmtpEmailService> logger)
        {
            _settings = options.Value;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                // In Sandbox/Development mode or if credentials not filled, log email and return success
                if (_settings.IsSandboxMode || string.IsNullOrWhiteSpace(_settings.Password))
                {
                    _logger.LogInformation("================ EMAIL DISPATCH (SANDBOX/DEV) ================");
                    _logger.LogInformation("To: {ToEmail}", toEmail);
                    _logger.LogInformation("Subject: {Subject}", subject);
                    _logger.LogInformation("Body snippet: {BodySnippet}", htmlBody.Length > 200 ? htmlBody.Substring(0, 200) + "..." : htmlBody);
                    _logger.LogInformation("==============================================================");
                    return true;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(_settings.FromEmail, _settings.FromName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = _settings.EnableSsl,
                    Timeout = 15000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {ToEmail} with subject '{Subject}'", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'", toEmail, subject);
                return false;
            }
        }

        public Task<bool> SendRegistrationWelcomeAsync(string toEmail, string userName)
        {
            var subject = "Welcome to Give-AID NGO - Together We Can Make a Difference";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #0d6efd; padding: 20px; border-radius: 6px; color: white;'>
                        <h1 style='margin: 0;'>Give-AID NGO</h1>
                        <p style='margin: 5px 0 0;'>Welfare & Donation Management System</p>
                    </div>
                    <div style='padding: 20px 0;'>
                        <h2>Welcome, {userName}!</h2>
                        <p>Thank you for joining Give-AID. Your account has been created successfully.</p>
                        <p>With Give-AID, you can support verified causes, track your donation impacts, participate in upcoming welfare programmes, and invite your friends to help make an impact.</p>
                        <p style='margin-top: 25px;'>
                            <a href='#' style='background-color: #198754; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Go to Member Portal</a>
                        </p>
                    </div>
                    <hr style='border: none; border-top: 1px solid #eee;' />
                    <p style='color: #888; font-size: 12px; text-align: center;'>&copy; {DateTime.UtcNow.Year} Give-AID NGO. All rights reserved.</p>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendPasswordResetAsync(string toEmail, string resetLink)
        {
            var subject = "Give-AID - Password Reset Request";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #0d6efd; padding: 15px; border-radius: 6px; color: white;'>
                        <h2>Give-AID Password Reset</h2>
                    </div>
                    <div style='padding: 20px 0;'>
                        <p>You requested a password reset for your Give-AID account.</p>
                        <p>Please click the button below to choose a new password:</p>
                        <p style='margin: 25px 0;'>
                            <a href='{resetLink}' style='background-color: #0d6efd; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Reset Password</a>
                        </p>
                        <p style='color: #666; font-size: 13px;'>If you did not request this, you can safely ignore this email.</p>
                    </div>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendDonationReceiptEmailAsync(string toEmail, string donorName, string receiptNumber, decimal amount, string currency, string causeTitle, string transactionId)
        {
            var subject = $"Donation Receipt {receiptNumber} - Thank You for Your Generosity!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #198754; padding: 20px; border-radius: 6px; color: white;'>
                        <h1 style='margin: 0;'>Official Donation Receipt</h1>
                        <p style='margin: 5px 0 0;'>Give-AID NGO Welfare Management System</p>
                    </div>
                    <div style='padding: 20px 0;'>
                        <p>Dear <strong>{donorName}</strong>,</p>
                        <p>We gratefully acknowledge the receipt of your generous donation to support <strong>{causeTitle}</strong>.</p>
                        <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                            <tr style='background: #f8f9fa;'>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Receipt Number</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{receiptNumber}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Donation Amount</td>
                                <td style='padding: 10px; border: 1px solid #ddd; font-size: 18px; color: #198754; font-weight: bold;'>{amount:N2} {currency}</td>
                            </tr>
                            <tr style='background: #f8f9fa;'>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Cause</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{causeTitle}</td>
                            </tr>
                            <tr>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Transaction Reference</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{transactionId}</td>
                            </tr>
                            <tr style='background: #f8f9fa;'>
                                <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Date Issued</td>
                                <td style='padding: 10px; border: 1px solid #ddd;'>{DateTime.UtcNow:dd MMM yyyy, HH:mm} UTC</td>
                            </tr>
                        </table>
                        <p>Your contribution directly impacts lives and creates lasting change in our communities.</p>
                    </div>
                    <hr style='border: none; border-top: 1px solid #eee;' />
                    <p style='color: #888; font-size: 12px; text-align: center;'>This is a computer-generated tax-exempt donation receipt. &copy; {DateTime.UtcNow.Year} Give-AID.</p>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendDonationFailureNotificationAsync(string toEmail, string donorName, decimal amount, string currency, string failureReason)
        {
            var subject = "Give-AID - Donation Transaction Notification";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #dc3545; padding: 15px; border-radius: 6px; color: white;'>
                        <h2>Donation Payment Notification</h2>
                    </div>
                    <div style='padding: 20px 0;'>
                        <p>Dear {donorName},</p>
                        <p>Your donation attempt of <strong>{amount:N2} {currency}</strong> could not be completed.</p>
                        <p><strong>Reason:</strong> {failureReason}</p>
                        <p>You may try again using a different payment method (JazzCash, Easypaisa, or Card) on the Give-AID portal.</p>
                    </div>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendQueryResponseEmailAsync(string toEmail, string userName, string querySubject, string adminResponse)
        {
            var subject = $"Give-AID Help Centre - Update on '{querySubject}'";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #0d6efd; padding: 15px; border-radius: 6px; color: white;'>
                        <h2>Give-AID Support Response</h2>
                    </div>
                    <div style='padding: 20px 0;'>
                        <p>Dear <strong>{userName}</strong>,</p>
                        <p>Our administration team has responded to your support query regarding: <em>{querySubject}</em>.</p>
                        <div style='background-color: #f1f8ff; border-left: 4px solid #0d6efd; padding: 15px; margin: 15px 0; border-radius: 4px;'>
                            <strong>Response:</strong><br />
                            <p style='margin: 8px 0 0;'>{adminResponse}</p>
                        </div>
                        <p>If you have any further questions, please feel free to reach out to us again.</p>
                    </div>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendFriendInvitationEmailAsync(string toEmail, string friendName, string senderName, string? message, string siteUrl)
        {
            var subject = $"{senderName} invited you to join Give-AID NGO Welfare Community!";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #198754; padding: 20px; border-radius: 6px; color: white;'>
                        <h1>You're Invited!</h1>
                        <p>Give-AID NGO Welfare & Donation Management System</p>
                    </div>
                    <div style='padding: 20px 0;'>
                        <p>Hi <strong>{friendName}</strong>,</p>
                        <p>Your friend <strong>{senderName}</strong> has invited you to join and support humanitarian welfare initiatives at <strong>Give-AID</strong>.</p>
                        {(string.IsNullOrWhiteSpace(message) ? "" : $"<blockquote style='background:#f9f9f9; border-left: 4px solid #198754; padding: 10px 15px; font-style: italic;'>\"{message}\"</blockquote>")}
                        <p>Give-AID empowers communities through verified programmes for children welfare, education, elderly care, women empowerment, and emergency relief.</p>
                        <p style='margin: 25px 0; text-align: center;'>
                            <a href='{siteUrl}' style='background-color: #198754; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Join Give-AID Today</a>
                        </p>
                    </div>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }

        public Task<bool> SendProgrammeInterestConfirmationAsync(string toEmail, string userName, string programmeTitle, string location, string startDate)
        {
            var subject = $"Give-AID - Programme Interest Confirmed: {programmeTitle}";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <div style='text-align: center; background-color: #0d6efd; padding: 15px; border-radius: 6px; color: white;'>
                        <h2>Programme Interest Registered</h2>
                    </div>
                    <div style='padding: 20px 0;'>
                        <p>Dear <strong>{userName}</strong>,</p>
                        <p>Thank you for expressing interest in our upcoming welfare programme: <strong>{programmeTitle}</strong>.</p>
                        <p><strong>Location:</strong> {location}</p>
                        <p><strong>Start Date:</strong> {startDate}</p>
                        <p>Our coordinators will reach out with further details and schedule updates prior to the event.</p>
                    </div>
                </div>";
            return SendEmailAsync(toEmail, subject, body);
        }
    }
}
