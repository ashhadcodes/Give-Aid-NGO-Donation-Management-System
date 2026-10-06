using System;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public enum MessageStatus
    {
        New = 0,
        InReview = 1,
        Replied = 2,
        Archived = 3
    }

    public class ContactMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Your Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        [Display(Name = "Phone Number")]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(200)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [MaxLength(3000)]
        [Display(Name = "Message")]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public MessageStatus Status { get; set; } = MessageStatus.New;

        [MaxLength(3000)]
        public string? AdminNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RepliedAt { get; set; }
    }
}
