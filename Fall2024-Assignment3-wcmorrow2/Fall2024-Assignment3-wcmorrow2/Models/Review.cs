﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Fall2024_Assignment3_wcmorrow2.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ContentId { get; set; }
        [Required]
        public string Content { get; set; }
        [Required]
        public string SentimentScore { get; set; }
    }

}
