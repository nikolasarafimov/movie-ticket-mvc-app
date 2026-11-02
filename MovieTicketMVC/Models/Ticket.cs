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
            ErrorMessage = "Мора да одберете филм.")]
        public int MovieId { get; set; }

        [DataType(DataType.Date)]
        public DateTime SelectedDay { get; set; }

        [Required(
            ErrorMessage = "Мора да одберете термин на проекција.")]
        public string SelectedTime { get; set; }

        [Range(
            1,
            100,
            ErrorMessage = "Мора да одберете помеѓу 1 и 100 седишта.")]
        public int NumberOfSeats { get; set; }

        [Required(
            ErrorMessage = "Мора да одберете барем едно седиште.")]
        public string SelectedSeats { get; set; }

        [Required(
            ErrorMessage = "Мора да внесете е-пошта.")]
        [EmailAddress(
            ErrorMessage = "Внесете валидна е-пошта.")]
        public string Email { get; set; }

        [Required(
            ErrorMessage = "Мора да одберете возрасна категорија.")]
        public string AgeCategory { get; set; }

        public string UnavailableSeats { get; set; }

        public int TotalPrice { get; set; }

        public virtual Movie Movie { get; set; }
    }
}