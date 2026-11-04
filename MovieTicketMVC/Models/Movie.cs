using System;
using System.ComponentModel.DataAnnotations;

namespace MovieTicketMVC.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required(
            ErrorMessage = "Please enter a movie title.")]
        public string Title { get; set; }

        [Range(
            1,
            600,
            ErrorMessage = "The movie duration must be between 1 and 600 minutes.")]
        public int LengthInMinutes { get; set; }

        [DataType(DataType.Date)]
        public DateTime ReleaseDate { get; set; }

        [Required(
            ErrorMessage = "Please enter a movie description.")]
        public string Description { get; set; }

        [Required(
            ErrorMessage = "Please specify whether the movie is restricted to adults.")]
        public bool? IsForAdults { get; set; }

        public bool IsCurrentlyShowing { get; set; }

        [Required(
            ErrorMessage = "Please enter a movie genre.")]
        public string Genre { get; set; }

        [DisplayFormat(
            DataFormatString = "{0:0.##}",
            ApplyFormatInEditMode = true)]
        [Range(
            typeof(decimal),
            "0",
            "10",
            ErrorMessage = "The movie rating must be between 0 and 10.")]
        public decimal? Rating { get; set; }

        public string Actors { get; set; }

        public string Language { get; set; }

        public string LocalTrailerPath { get; set; }
    }
}