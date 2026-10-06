using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public enum InvitationStatus
    {
        Sent = 0,
        Accepted = 1,
        Expired = 2
    }

    public class Invitation
    {
        [Key]
        public int Id { get; set; }

        public string? SenderUserId { get; set; }

        [ForeignKey("SenderUserId")]
        public virtual ApplicationUser? SenderUser { get; set; }

        [Required]
        [MaxLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string SenderEmail { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Display(Name = "Friend's Full Name")]
        public string FriendName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Display(Name = "Friend's Email Address")]
        public string FriendEmail { get; set; } = string.Empty;

        [MaxLength(1000)]
        [Display(Name = "Personal Invitation Note")]
        public string? Message { get; set; }

        public InvitationStatus Status { get; set; } = InvitationStatus.Sent;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string InvitationToken { get; set; } = Guid.NewGuid().ToString("N");
    }
}
