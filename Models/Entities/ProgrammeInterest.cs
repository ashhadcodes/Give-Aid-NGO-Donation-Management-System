using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public enum InterestStatus
    {
        Pending = 0,
        Confirmed = 1,
        Cancelled = 2
    }

    public class ProgrammeInterest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProgrammeId { get; set; }

        [ForeignKey("ProgrammeId")]
        public virtual Programme? Programme { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        public InterestStatus Status { get; set; } = InterestStatus.Pending;

        public DateTime ExpressedAt { get; set; } = DateTime.UtcNow;
    }
}
