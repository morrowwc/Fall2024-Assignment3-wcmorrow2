using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace Fall2024_Assignment3_wcmorrow2.Models
{
    public class Actor
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        [DisplayName("Date of Birth")]
        public DateTime DoB { get; set; }
        [DisplayName("Date of Death")]
        public DateTime? DoD { get; set; }
        [Required]
        [DisplayName("IMDB Link")]
        public string IMDBlink { get; set; }
        [DisplayName("Photo / Media")]
        public byte[]? Media { get; set; }
        [NotMapped]
        public IFormFile? MediaFile { get; set; }

    }
}
