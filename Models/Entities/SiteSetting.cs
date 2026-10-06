using System;
using System.ComponentModel.DataAnnotations;

namespace Give_Aid_NGO_Donation_Management_System.Models.Entities
{
    public class SiteSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [MaxLength(250)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string Group { get; set; } = "General"; // General, Contact, Social, System

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
