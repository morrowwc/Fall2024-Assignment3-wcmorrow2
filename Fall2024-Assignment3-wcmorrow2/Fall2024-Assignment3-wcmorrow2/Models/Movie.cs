using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace Fall2024_Assignment3_wcmorrow2.Models
{
    public class Movie
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Title { get; set; }
        [Required]
        public int Year { get; set; }
        [Required]
        [DisplayName("IMDB Link")]
        public string IMDBlink { get; set; }
        [DisplayName("Poster / Media")]
        public byte[]? Media { get; set; }
        [NotMapped]
        public IFormFile? MediaFile { get; set; }

    }
}
