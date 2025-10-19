using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DigitalNotesManager.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        // Navigation property
        public ICollection<Note> Notes { get; set; } = new List<Note>();
    }
}
