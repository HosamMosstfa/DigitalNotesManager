using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DigitalNotesManager.Models
{
    public class Note
    {
        [Key]
        public int NoteID { get; set; }


        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;


        [Required]
        public string Content { get; set; } = string.Empty; // plain text for search/export txt


        public string? ContentRtf { get; set; } // RTF for formatting


        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;


        public DateTime CreationDate { get; set; } = DateTime.Now;
        public DateTime? ReminderDate { get; set; }


        public int UserID { get; set; }


        [ForeignKey("UserID")]
        public User? User { get; set; }


        public bool IsReminderShown { get; set; } = false;
    }
}