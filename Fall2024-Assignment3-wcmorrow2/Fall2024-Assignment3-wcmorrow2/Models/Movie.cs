using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment3_wcmorrow2.Models
{
    [PrimaryKey(nameof(Id))]
    public class Movie
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? Year { get; set; }
    }
}
