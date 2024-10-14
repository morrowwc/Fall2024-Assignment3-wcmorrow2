﻿using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Fall2024_Assignment3_wcmorrow2.Models
{
    public class MovieActor
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("Actor")]
        public int? ActorId { get; set; }

        public Actor? Actor { get; set; }

        [ForeignKey("Movie")]
        public int? MovieId { get; set; }

        public Movie? Movie { get; set; }
    }

}
