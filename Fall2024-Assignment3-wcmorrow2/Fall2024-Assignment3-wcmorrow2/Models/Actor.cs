using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment3_wcmorrow2.Models
{
    public class Actor
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public DateTime DoB { get; set; }
        public DateTime? DoD { get; set; }
    }
}
