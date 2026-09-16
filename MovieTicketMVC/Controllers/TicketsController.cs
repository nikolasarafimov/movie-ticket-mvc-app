using MovieTicketMVC.Models;
using MovieTicketMVC.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace MovieTicketMVC.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private const int PremiumSeatPrice = 500;
        private const int StandardSeatPrice = 250;

        private static readonly Regex SeatIdRegex =
            new Regex(
                @"^R([1-9]|10)C([1-9]|10)$",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase);

        private static readonly HashSet<string> AllowedProjectionTimes =
            new HashSet<string>(
                new[]
                {
                    "10:00",
                    "14:00",
                    "18:00",
                    "21:00"
                },
                StringComparer.Ordinal);

        private static readonly HashSet<string> AllowedAgeCategories =
            new HashSet<string>(
                new[]
                {
                    "18+",
                    "Under18"
                },
                StringComparer.OrdinalIgnoreCase);

        private readonly ApplicationDbContext _context;

        public TicketsController()
        {
            _context =
                new ApplicationDbContext();
        }

        // GET: Tickets/GetUnavailableSeats
        [HttpGet]
        public async Task<JsonResult> GetUnavailableSeats(
            int movieId,
            DateTime? day,
            string time)
        {
            string normalizedTime;

            if (movieId <= 0 ||
                !day.HasValue ||
                !TryNormalizeProjectionTime(
                    time,
                    out normalizedTime))
            {
                return EmptySeatResult();
            }

            var movie =
                await _context.Movies
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        m => m.Id == movieId);

            if (movie == null)
            {
                return EmptySeatResult();
            }

            var selectedDay =
                day.Value.Date;

            if (selectedDay < DateTime.Today ||
                selectedDay < movie.ReleaseDate.Date)
            {
                return EmptySeatResult();
            }

            var dayEnd =
                selectedDay.AddDays(1);

            var existingTickets =
                await _context.Tickets
                    .AsNoTracking()
                    .Where(t =>
                        t.MovieId == movieId &&
                        t.SelectedDay >= selectedDay &&
                        t.SelectedDay < dayEnd &&
                        t.SelectedTime == normalizedTime)
                    .ToListAsync();

            var purchasedSeats =
                existingTickets
                    .SelectMany(t =>
                        (t.SelectedSeats ??
                         string.Empty)
                        .Split(
                            new[] { ',' },
                            StringSplitOptions
                                .RemoveEmptyEntries))
                    .Select(seat =>
                        seat.Trim()
                            .ToUpperInvariant())
                    .Where(seat =>
                        SeatIdRegex.IsMatch(seat))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .OrderBy(
                        GetSeatSortValue)
                    .ToList();

            return Json(
                purchasedSeats,
                JsonRequestBehavior.AllowGet);
        }

        // GET: Tickets
        [HttpGet]
        public ActionResult Index()
        {
            var query =
                _context.Tickets
                    .AsNoTracking()
                    .Include(t => t.Movie)
                    .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var userEmail =
                    User.Identity.Name;

                query =
                    query.Where(
                        t => t.Email == userEmail);
            }

            var tickets =
                query
                    .OrderByDescending(
                        t => t.Id)
                    .ToList();

            return View(tickets);
        }

        // GET: Tickets/Buy
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Buy()
        {
            if (!User.Identity.IsAuthenticated)
            {
                ViewBag.ReturnUrl =
                    Url.Action(
                        "Buy",
                        "Tickets");

                return View(
                    "LoginPrompt");
            }

            PopulateMovieLists();

            var ticket =
                new Ticket
                {
                    Email =
                        User.Identity.Name
                };

            return View(ticket);
        }

        // POST: Tickets/Buy
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Buy(
            [Bind(
                Include =
                    "MovieId,SelectedDay,SelectedTime,AgeCategory")]
            Ticket ticket,
            string[] selectedSeats)
        {
            PopulateMovieLists();

            var normalizedSeats =
                NormalizeSelectedSeats(
                    selectedSeats,
                    out var invalidSeats);

            ticket.Email =
                User.Identity.Name;

            ticket.SelectedSeats =
                string.Join(
                    ",",
                    normalizedSeats);

            ticket.NumberOfSeats =
                normalizedSeats.Count;

            ticket.SelectedDay =
                ticket.SelectedDay.Date;

            ModelState.Remove(
                nameof(Ticket.Email));

            ModelState.Remove(
                nameof(Ticket.SelectedSeats));

            ModelState.Remove(
                nameof(Ticket.NumberOfSeats));

            ModelState.Remove(
                nameof(Ticket.TotalPrice));

            ModelState.Remove(
                nameof(Ticket.UnavailableSeats));

            if (string.IsNullOrWhiteSpace(
                    ticket.Email))
            {
                ModelState.AddModelError(
                    "",
                    "Не можевме да ја утврдиме e-mail адресата на најавениот корисник.");
            }

            if (invalidSeats.Any())
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Невалидни седишта: "
                    + string.Join(
                        ", ",
                        invalidSeats));
            }

            if (!normalizedSeats.Any())
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Мора да одберете барем едно седиште.");
            }

            if (normalizedSeats.Count > 100)
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Не можете да одберете повеќе од 100 седишта.");
            }

            if (ticket.MovieId <= 0)
            {
                ModelState.AddModelError(
                    "MovieId",
                    "Ве молиме одберете филм.");
            }

            Movie movie =
                null;

            if (ticket.MovieId > 0)
            {
                movie =
                    await _context.Movies
                        .SingleOrDefaultAsync(
                            m =>
                                m.Id ==
                                ticket.MovieId);
            }

            if (movie == null)
            {
                ModelState.AddModelError(
                    "MovieId",
                    "Невалиден филм.");
            }

            string normalizedTime;

            if (!TryNormalizeProjectionTime(
                    ticket.SelectedTime,
                    out normalizedTime))
            {
                ModelState.AddModelError(
                    "SelectedTime",
                    "Невалиден термин на проекција.");
            }
            else
            {
                ticket.SelectedTime =
                    normalizedTime;
            }

            var ageCategoryIsValid =
                !string.IsNullOrWhiteSpace(
                    ticket.AgeCategory) &&
                AllowedAgeCategories.Contains(
                    ticket.AgeCategory.Trim());

            if (!ageCategoryIsValid)
            {
                ModelState.AddModelError(
                    "AgeCategory",
                    "Невалидна возрастна категорија.");
            }
            else
            {
                ticket.AgeCategory =
                    string.Equals(
                        ticket.AgeCategory,
                        "18+",
                        StringComparison.OrdinalIgnoreCase)
                        ? "18+"
                        : "Under18";
            }

            var today =
                DateTime.Today;

            if (ticket.SelectedDay < today)
            {
                ModelState.AddModelError(
                    "SelectedDay",
                    "Не можете да одберете ден во минатото.");
            }

            if (movie != null)
            {
                if (ticket.SelectedDay <
                    movie.ReleaseDate.Date)
                {
                    ModelState.AddModelError(
                        "SelectedDay",
                        "Не можете да одберете ден пред почетокот на филмот.");
                }

                if (movie.IsForAdults == true &&
                    ageCategoryIsValid &&
                    !string.Equals(
                        ticket.AgeCategory,
                        "18+",
                        StringComparison.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "AgeCategory",
                        "За овој филм мора да потврдите дека имате 18 или повеќе години.");
                }
            }

            if (!string.IsNullOrWhiteSpace(
                    normalizedTime) &&
                ticket.SelectedDay == today &&
                TryGetProjectionDateTime(
                    ticket.SelectedDay,
                    normalizedTime,
                    out var projectionDateTime) &&
                projectionDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    "SelectedTime",
                    "Избраната проекција е веќе помината денес.");
            }

            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            ticket.TotalPrice =
                CalculateTotalPrice(
                    normalizedSeats);

            ticket.Movie =
                movie;

            using (var transaction =
                _context.Database.BeginTransaction(
                    IsolationLevel.Serializable))
            {
                try
                {
                    var dayStart =
                        ticket.SelectedDay.Date;

                    var dayEnd =
                        dayStart.AddDays(1);

                    var existingTickets =
                        await _context.Tickets
                            .Where(t =>
                                t.MovieId ==
                                    ticket.MovieId &&
                                t.SelectedDay >=
                                    dayStart &&
                                t.SelectedDay <
                                    dayEnd &&
                                t.SelectedTime ==
                                    ticket.SelectedTime)
                            .ToListAsync();

                    var takenSeats =
                        new HashSet<string>(
                            existingTickets
                                .SelectMany(t =>
                                    (t.SelectedSeats ??
                                     string.Empty)
                                    .Split(
                                        new[] { ',' },
                                        StringSplitOptions
                                            .RemoveEmptyEntries))
                                .Select(seat =>
                                    seat.Trim()
                                        .ToUpperInvariant()),
                            StringComparer
                                .OrdinalIgnoreCase);

                    var overlappingSeats =
                        normalizedSeats
                            .Where(seat =>
                                takenSeats.Contains(
                                    seat))
                            .OrderBy(
                                GetSeatSortValue)
                            .ToList();

                    if (overlappingSeats.Any())
                    {
                        transaction.Rollback();

                        ModelState.AddModelError(
                            "SelectedSeats",
                            "Следните седишта во меѓувреме беа резервирани: "
                            + string.Join(
                                ", ",
                                overlappingSeats));

                        return View(ticket);
                    }

                    _context.Tickets.Add(
                        ticket);

                    await _context
                        .SaveChangesAsync();

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();

                    Trace.TraceError(
                        "Ticket reservation failed for movie {0}, day {1:yyyy-MM-dd}, time {2}: {3}",
                        ticket.MovieId,
                        ticket.SelectedDay,
                        ticket.SelectedTime,
                        ex);

                    ModelState.AddModelError(
                        "",
                        "Резервацијата не можеше да се заврши. "
                        + "Можно е достапноста на седиштата да се променила. "
                        + "Обидете се повторно.");

                    return View(ticket);
                }
            }

            try
            {
                var pdfBytes =
                    new TicketPdfGenerator()
                        .GeneratePdf(ticket);

                await TicketEmailService
                    .SendTicketConfirmationAsync(
                        to: ticket.Email,
                        subject:
                            "ВАШИТЕ КИНО БИЛЕТИ",
                        body:
                            "Ви благодариме за купувањето! "
                            + "Во прилог ги испраќаме вашите билети "
                            + "и инструкциите за плаќање.",
                        attachmentBytes:
                            pdfBytes,
                        attachmentName:
                            "MovieTickets.pdf");

                TempData["SuccessMessage"] =
                    "Ви благодариме! Билетите се успешно резервирани "
                    + "и испратени на вашата e-mail адреса.";
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Ticket {0} was created, but confirmation delivery failed: {1}",
                    ticket.Id,
                    ex);

                TempData["SuccessMessage"] =
                    "Билетите се успешно резервирани, "
                    + "но e-mail потврдата не можеше да биде испратена. "
                    + "Резервацијата е достапна во вашата историја.";
            }

            return RedirectToAction(
                "Summary",
                new
                {
                    id = ticket.Id
                });
        }

        // GET: Tickets/Summary/5
        [HttpGet]
        public ActionResult Summary(int id)
        {
            var ticket =
                _context.Tickets
                    .AsNoTracking()
                    .Include(t => t.Movie)
                    .SingleOrDefault(
                        t => t.Id == id);

            if (ticket == null)
            {
                return HttpNotFound();
            }

            if (!User.IsInRole("Admin") &&
                !string.Equals(
                    ticket.Email,
                    User.Identity.Name,
                    StringComparison.OrdinalIgnoreCase))
            {
                return new HttpStatusCodeResult(
                    403);
            }

            return View(ticket);
        }

        private void PopulateMovieLists()
        {
            var today =
                DateTime.Today;

            var movies =
                _context.Movies
                    .AsNoTracking()
                    .OrderBy(
                        m => m.ReleaseDate)
                    .ThenBy(
                        m => m.Title)
                    .ToList();

            ViewBag.CurrentMovies =
                movies
                    .Where(m =>
                        m.ReleaseDate.Date <= today)
                    .ToList();

            ViewBag.ComingSoonMovies =
                movies
                    .Where(m =>
                        m.ReleaseDate.Date > today)
                    .ToList();
        }

        private JsonResult EmptySeatResult()
        {
            return Json(
                new string[0],
                JsonRequestBehavior.AllowGet);
        }

        private static List<string>
            NormalizeSelectedSeats(
                IEnumerable<string> selectedSeats,
                out List<string> invalidSeats)
        {
            invalidSeats =
                new List<string>();

            if (selectedSeats == null)
            {
                return new List<string>();
            }

            var normalizedSeats =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (var rawSeat in selectedSeats)
            {
                if (string.IsNullOrWhiteSpace(
                        rawSeat))
                {
                    continue;
                }

                var seat =
                    rawSeat.Trim()
                        .ToUpperInvariant();

                if (!SeatIdRegex.IsMatch(
                        seat))
                {
                    invalidSeats.Add(
                        rawSeat.Trim());

                    continue;
                }

                normalizedSeats.Add(
                    seat);
            }

            return normalizedSeats
                .OrderBy(
                    GetSeatSortValue)
                .ToList();
        }

        private static int CalculateTotalPrice(
            IEnumerable<string> seats)
        {
            var total =
                0;

            foreach (var seat in seats)
            {
                var match =
                    SeatIdRegex.Match(
                        seat);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "Invalid seat identifier.");
                }

                var row =
                    int.Parse(
                        match.Groups[1].Value,
                        CultureInfo.InvariantCulture);

                total +=
                    row <= 2
                        ? PremiumSeatPrice
                        : StandardSeatPrice;
            }

            return total;
        }

        private static int GetSeatSortValue(
            string seatId)
        {
            var match =
                SeatIdRegex.Match(
                    seatId ??
                    string.Empty);

            if (!match.Success)
            {
                return int.MaxValue;
            }

            var row =
                int.Parse(
                    match.Groups[1].Value,
                    CultureInfo.InvariantCulture);

            var column =
                int.Parse(
                    match.Groups[2].Value,
                    CultureInfo.InvariantCulture);

            return (row * 100)
                   + column;
        }

        private static bool
            TryNormalizeProjectionTime(
                string value,
                out string normalizedTime)
        {
            normalizedTime =
                null;

            if (string.IsNullOrWhiteSpace(
                    value))
            {
                return false;
            }

            DateTime parsedTime;

            if (!DateTime.TryParseExact(
                    value.Trim(),
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedTime))
            {
                return false;
            }

            var normalized =
                parsedTime.ToString(
                    "HH:mm",
                    CultureInfo.InvariantCulture);

            if (!AllowedProjectionTimes.Contains(
                    normalized))
            {
                return false;
            }

            normalizedTime =
                normalized;

            return true;
        }

        private static bool
            TryGetProjectionDateTime(
                DateTime day,
                string normalizedTime,
                out DateTime projectionDateTime)
        {
            projectionDateTime =
                default(DateTime);

            DateTime parsedTime;

            if (!DateTime.TryParseExact(
                    normalizedTime,
                    "HH:mm",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsedTime))
            {
                return false;
            }

            projectionDateTime =
                day.Date.Add(
                    parsedTime.TimeOfDay);

            return true;
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}