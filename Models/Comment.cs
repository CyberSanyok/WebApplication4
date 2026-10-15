using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication4.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Комментарий не может быть пустым")]
        [StringLength(500)]
        public string Text { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int MapMarkerId { get; set; }
        public MapMarker MapMarker { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
    }
}