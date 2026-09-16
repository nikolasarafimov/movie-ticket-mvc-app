using System;
using System.ComponentModel.DataAnnotations;

namespace MovieTicketMVC.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Range(1, 600, ErrorMessage = "Времетраењето мора да биде помеѓу 1 и 600 минути.")]
        public int LengthInMinutes { get; set; }

        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public bool? IsForAdults { get; set; }

        public bool IsCurrentlyShowing { get; set; }

        [Required]
        public string Genre { get; set; }

        [DisplayFormat(DataFormatString = "{0:0.##}", ApplyFormatInEditMode = true)]
        [Range(0, 10, ErrorMessage = "Рејтингот мора да биде помеѓу 0 и 10.")]
        public decimal? Rating { get; set; }

        public string Actors { get; set; }

        public string Language { get; set; }

        public string LocalTrailerPath { get; set; }
    }
}