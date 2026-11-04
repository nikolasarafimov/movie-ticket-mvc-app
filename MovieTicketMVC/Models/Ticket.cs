using System;
using System.ComponentModel.DataAnnotations;

namespace MovieTicketMVC.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Range(
            1,
            int.MaxValue,
            ErrorMessage = "Please select a movie.")]
        public int MovieId { get; set; }

        [DataType(DataType.Date)]
        public DateTime SelectedDay { get; set; }

        [Required(
            ErrorMessage = "Please select a screening time.")]
        public string SelectedTime { get; set; }

        [Range(
            1,
            100,
            ErrorMessage = "Please select between 1 and 100 seats.")]
        public int NumberOfSeats { get; set; }

        [Required(
            ErrorMessage = "Please select at least one seat.")]
        public string SelectedSeats { get; set; }

        [Required(
            ErrorMessage = "Please enter your email address.")]
        [EmailAddress(
            ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; }

        [Required(
            ErrorMessage = "Please select an age category.")]
        public string AgeCategory { get; set; }

        public string UnavailableSeats { get; set; }

        public int TotalPrice { get; set; }

        public virtual Movie Movie { get; set; }
    }
}