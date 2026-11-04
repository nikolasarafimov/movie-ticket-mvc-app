using MovieTicketMVC.Models;
using MovieTicketMVC.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
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
        private const int MaximumSeats = 100;

        private static readonly Regex SeatIdRegex = new Regex(
            @"^R([1-9]|10)C([1-9]|10)$",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant);

        private static readonly HashSet<string> AllowedProjectionTimes =
            new HashSet<string>(
                new[] { "10:00", "14:00", "18:00", "21:00" },
                StringComparer.Ordinal);

        private static readonly HashSet<string> AllowedAgeCategories =
            new HashSet<string>(
                new[] { "18+", "Under18" },
                StringComparer.OrdinalIgnoreCase);

        private readonly ApplicationDbContext _context;

        public TicketsController()
        {
            _context = new ApplicationDbContext();
        }

        [HttpGet]
        public async Task<JsonResult> GetUnavailableSeats(
            int movieId,
            DateTime? day,
            string time)
        {
            string normalizedTime;

            if (movieId <= 0 ||
                !day.HasValue ||
                !TryNormalizeProjectionTime(time, out normalizedTime))
            {
                return EmptySeatResult();
            }

            var selectedDay = day.Value.Date;

            if (selectedDay < DateTime.Today ||
                selectedDay >= DateTime.MaxValue.Date)
            {
                return EmptySeatResult();
            }

            var movie = await _context.Movies
                .AsNoTracking()
                .SingleOrDefaultAsync(m => m.Id == movieId);

            if (movie == null || selectedDay < movie.ReleaseDate.Date)
            {
                return EmptySeatResult();
            }

            var dayEnd = selectedDay.AddDays(1);

            var existingTickets = await _context.Tickets
                .AsNoTracking()
                .Where(t =>
                    t.MovieId == movieId &&
                    t.SelectedDay >= selectedDay &&
                    t.SelectedDay < dayEnd &&
                    t.SelectedTime == normalizedTime)
                .Select(t => t.SelectedSeats)
                .ToListAsync();

            var purchasedSeats = existingTickets
                .SelectMany(value =>
                    (value ?? string.Empty).Split(
                        new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries))
                .Select(value => value.Trim().ToUpperInvariant())
                .Where(value => SeatIdRegex.IsMatch(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetSeatSortValue)
                .ToList();

            return Json(purchasedSeats, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Index()
        {
            var query = _context.Tickets
                .AsNoTracking()
                .Include(t => t.Movie)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var email = User.Identity.Name;
                query = query.Where(t => t.Email == email);
            }

            var tickets = query
                .OrderByDescending(t => t.Id)
                .ToList();

            return View(tickets);
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Buy()
        {
            if (!User.Identity.IsAuthenticated)
            {
                ViewBag.ReturnUrl = Url.Action("Buy", "Tickets");
                return View("LoginPrompt");
            }

            PopulateMovieLists();

            return View(new Ticket
            {
                Email = User.Identity.Name
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Buy(
            [Bind(Include = "MovieId,SelectedDay,SelectedTime,AgeCategory")]
            Ticket ticket,
            string[] selectedSeats,
            string signalRConnectionId)
        {
            if (ticket == null)
            {
                return View("Error");
            }

            PopulateMovieLists();

            var tooManySubmittedSeats =
                selectedSeats != null &&
                selectedSeats.Length > MaximumSeats;

            List<string> invalidSeats = new List<string>();

            var normalizedSeats = tooManySubmittedSeats
                ? new List<string>()
                : NormalizeSelectedSeats(selectedSeats, out invalidSeats);

            ticket.Email = User.Identity.Name;
            ticket.SelectedSeats = string.Join(",", normalizedSeats);
            ticket.NumberOfSeats = normalizedSeats.Count;
            ticket.SelectedDay = ticket.SelectedDay.Date;

            ModelState.Remove(nameof(Ticket.Email));
            ModelState.Remove(nameof(Ticket.SelectedSeats));
            ModelState.Remove(nameof(Ticket.NumberOfSeats));
            ModelState.Remove(nameof(Ticket.TotalPrice));
            ModelState.Remove(nameof(Ticket.UnavailableSeats));

            if (string.IsNullOrWhiteSpace(ticket.Email))
            {
                ModelState.AddModelError(
                    "",
                    "We couldn't identify the email address of the signed-in user.");
            }

            if (tooManySubmittedSeats ||
                normalizedSeats.Count > MaximumSeats)
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "You cannot select more than 100 seats.");
            }

            if (invalidSeats.Count > 0)
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Invalid seat selections: " +
                    string.Join(", ", invalidSeats));
            }

            if (normalizedSeats.Count == 0 && !tooManySubmittedSeats)
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Please select at least one seat.");
            }

            if (ticket.MovieId <= 0)
            {
                ModelState.AddModelError(
                    "MovieId",
                    "Please select a movie.");
            }

            Movie movie = null;

            if (ticket.MovieId > 0)
            {
                movie = await _context.Movies
                    .SingleOrDefaultAsync(m => m.Id == ticket.MovieId);
            }

            if (movie == null)
            {
                ModelState.AddModelError(
                    "MovieId",
                    "The selected movie is invalid.");
            }

            string normalizedTime;

            if (!TryNormalizeProjectionTime(
                    ticket.SelectedTime,
                    out normalizedTime))
            {
                ModelState.AddModelError(
                    "SelectedTime",
                    "Please select a valid screening time.");
            }
            else
            {
                ticket.SelectedTime = normalizedTime;
            }

            var ageCategoryIsValid =
                !string.IsNullOrWhiteSpace(ticket.AgeCategory) &&
                AllowedAgeCategories.Contains(ticket.AgeCategory.Trim());

            if (!ageCategoryIsValid)
            {
                ModelState.AddModelError(
                    "AgeCategory",
                    "Please select a valid age category.");
            }
            else
            {
                ticket.AgeCategory = string.Equals(
                    ticket.AgeCategory.Trim(),
                    "18+",
                    StringComparison.OrdinalIgnoreCase)
                    ? "18+"
                    : "Under18";
            }

            if (ticket.SelectedDay < DateTime.Today)
            {
                ModelState.AddModelError(
                    "SelectedDay",
                    "You cannot book a screening in the past.");
            }

            if (ticket.SelectedDay >= DateTime.MaxValue.Date)
            {
                ModelState.AddModelError(
                    "SelectedDay",
                    "Please select a valid screening date.");
            }

            if (movie != null)
            {
                if (ticket.SelectedDay < movie.ReleaseDate.Date)
                {
                    ModelState.AddModelError(
                        "SelectedDay",
                        "You cannot book a screening before the movie's release date.");
                }

                if (movie.IsForAdults == true &&
                    ageCategoryIsValid &&
                    ticket.AgeCategory != "18+")
                {
                    ModelState.AddModelError(
                        "AgeCategory",
                        "You must confirm that you are at least 18 years old to book this movie.");
                }
            }

            DateTime projectionDateTime;
            var hasValidProjectionDateTime = TryGetProjectionDateTime(
                ticket.SelectedDay,
                normalizedTime,
                out projectionDateTime);

            if (!hasValidProjectionDateTime)
            {
                ModelState.AddModelError(
                    "SelectedTime",
                    "Please select a valid screening date and time.");
            }
            else if (projectionDateTime <= DateTime.Now)
            {
                ModelState.AddModelError(
                    "SelectedTime",
                    "The selected screening has already started.");
            }

            if (string.IsNullOrWhiteSpace(signalRConnectionId) ||
                signalRConnectionId.Length > 256)
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Your seat selection session is invalid. Please refresh the page.");
            }

            if (!ModelState.IsValid)
            {
                return View(ticket);
            }

            ticket.TotalPrice = CalculateTotalPrice(normalizedSeats);
            ticket.Movie = movie;

            var purchaseId = SeatLockService.TryBeginPurchase(
                ticket.MovieId,
                ticket.SelectedDay,
                ticket.SelectedTime,
                normalizedSeats,
                signalRConnectionId,
                ticket.Email);

            if (!purchaseId.HasValue)
            {
                ModelState.AddModelError(
                    "SelectedSeats",
                    "Your seat holds could not be verified. Please select the seats again.");
                return View(ticket);
            }

            try
            {
                using (var transaction = _context.Database
                    .BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var resource = string.Format(
                            CultureInfo.InvariantCulture,
                            "MovieTicket:{0}:{1:yyyyMMdd}:{2}",
                            ticket.MovieId,
                            ticket.SelectedDay,
                            ticket.SelectedTime);

                        var lockResult = await _context.Database
                            .SqlQuery<int>(
                                @"DECLARE @result int;
                                  EXEC @result = sys.sp_getapplock
                                      @Resource = @resourceName,
                                      @LockMode = 'Exclusive',
                                      @LockOwner = 'Transaction',
                                      @LockTimeout = 10000;
                                  SELECT @result;",
                                new SqlParameter(
                                    "@resourceName",
                                    SqlDbType.NVarChar,
                                    255)
                                {
                                    Value = resource
                                })
                            .SingleAsync();

                        if (lockResult < 0)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError(
                                "",
                                "This screening is currently processing another booking. Please try again.");
                            return View(ticket);
                        }

                        if (projectionDateTime <= DateTime.Now)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError(
                                "SelectedTime",
                                "The selected screening has already started.");
                            return View(ticket);
                        }

                        var dayStart = ticket.SelectedDay.Date;
                        var dayEnd = dayStart.AddDays(1);

                        var existingSeats = await _context.Tickets
                            .AsNoTracking()
                            .Where(t =>
                                t.MovieId == ticket.MovieId &&
                                t.SelectedDay >= dayStart &&
                                t.SelectedDay < dayEnd &&
                                t.SelectedTime == ticket.SelectedTime)
                            .Select(t => t.SelectedSeats)
                            .ToListAsync();

                        var takenSeats = new HashSet<string>(
                            existingSeats
                                .SelectMany(value =>
                                    (value ?? string.Empty).Split(
                                        new[] { ',' },
                                        StringSplitOptions.RemoveEmptyEntries))
                                .Select(value => value.Trim().ToUpperInvariant())
                                .Where(value => SeatIdRegex.IsMatch(value)),
                            StringComparer.OrdinalIgnoreCase);

                        var overlappingSeats = normalizedSeats
                            .Where(seat => takenSeats.Contains(seat))
                            .OrderBy(GetSeatSortValue)
                            .ToList();

                        if (overlappingSeats.Count > 0)
                        {
                            transaction.Rollback();
                            ModelState.AddModelError(
                                "SelectedSeats",
                                "The following seats were booked by someone else: " +
                                string.Join(", ", overlappingSeats));
                            return View(ticket);
                        }

                        _context.Tickets.Add(ticket);
                        await _context.SaveChangesAsync();
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
                            "Your booking could not be completed. Seat availability may have changed. Please try again.");
                        return View(ticket);
                    }
                }
            }
            finally
            {
                SeatLockService.EndPurchase(purchaseId.Value);
            }

            try
            {
                var pdfBytes = new TicketPdfGenerator()
                    .GeneratePdf(ticket);

                await TicketEmailService.SendTicketConfirmationAsync(
                    to: ticket.Email,
                    subject: "MovieTicket Booking Confirmation",
                    body:
                        "Thank you for booking with MovieTicket! " +
                        "Your booking confirmation is attached as a PDF.",
                    attachmentBytes: pdfBytes,
                    attachmentName: "MovieTickets.pdf");

                TempData["EmailDeliveryFailed"] = false;
                TempData["SuccessMessage"] =
                    "Your booking is confirmed. A confirmation email has been sent to your email address.";
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Ticket {0} was created, but confirmation delivery failed: {1}",
                    ticket.Id,
                    ex);

                TempData["EmailDeliveryFailed"] = true;
                TempData["SuccessMessage"] =
                    "Your booking is confirmed, but we couldn't send the confirmation email. " +
                    "You can still find your booking under My Tickets.";
            }

            return RedirectToAction("Summary", new { id = ticket.Id });
        }

        [HttpGet]
        public ActionResult Summary(int id)
        {
            var ticket = _context.Tickets
                .AsNoTracking()
                .Include(t => t.Movie)
                .SingleOrDefault(t => t.Id == id);

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
                return new HttpStatusCodeResult(403);
            }

            return View(ticket);
        }

        private void PopulateMovieLists()
        {
            var today = DateTime.Today;

            var movies = _context.Movies
                .AsNoTracking()
                .OrderBy(m => m.ReleaseDate)
                .ThenBy(m => m.Title)
                .ToList();

            ViewBag.CurrentMovies = movies
                .Where(m => m.ReleaseDate.Date <= today)
                .ToList();

            ViewBag.ComingSoonMovies = movies
                .Where(m => m.ReleaseDate.Date > today)
                .ToList();
        }

        private JsonResult EmptySeatResult()
        {
            return Json(new string[0], JsonRequestBehavior.AllowGet);
        }

        private static List<string> NormalizeSelectedSeats(
            IEnumerable<string> selectedSeats,
            out List<string> invalidSeats)
        {
            invalidSeats = new List<string>();

            if (selectedSeats == null)
            {
                return new List<string>();
            }

            var normalizedSeats = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var rawSeat in selectedSeats)
            {
                if (string.IsNullOrWhiteSpace(rawSeat))
                {
                    continue;
                }

                var seat = rawSeat.Trim().ToUpperInvariant();

                if (!SeatIdRegex.IsMatch(seat))
                {
                    invalidSeats.Add(rawSeat.Trim());
                    continue;
                }

                normalizedSeats.Add(seat);
            }

            return normalizedSeats
                .OrderBy(GetSeatSortValue)
                .ToList();
        }

        private static int CalculateTotalPrice(
            IEnumerable<string> seats)
        {
            var total = 0;

            foreach (var seat in seats)
            {
                var match = SeatIdRegex.Match(seat);

                if (!match.Success)
                {
                    throw new InvalidOperationException(
                        "Invalid seat identifier.");
                }

                var row = int.Parse(
                    match.Groups[1].Value,
                    CultureInfo.InvariantCulture);

                total += row <= 2
                    ? PremiumSeatPrice
                    : StandardSeatPrice;
            }

            return total;
        }

        private static int GetSeatSortValue(string seatId)
        {
            var match = SeatIdRegex.Match(seatId ?? string.Empty);

            if (!match.Success)
            {
                return int.MaxValue;
            }

            var row = int.Parse(
                match.Groups[1].Value,
                CultureInfo.InvariantCulture);

            var column = int.Parse(
                match.Groups[2].Value,
                CultureInfo.InvariantCulture);

            return row * 100 + column;
        }

        private static bool TryNormalizeProjectionTime(
            string value,
            out string normalizedTime)
        {
            normalizedTime = null;

            if (string.IsNullOrWhiteSpace(value))
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

            var normalized = parsedTime.ToString(
                "HH:mm",
                CultureInfo.InvariantCulture);

            if (!AllowedProjectionTimes.Contains(normalized))
            {
                return false;
            }

            normalizedTime = normalized;
            return true;
        }

        private static bool TryGetProjectionDateTime(
            DateTime day,
            string normalizedTime,
            out DateTime projectionDateTime)
        {
            projectionDateTime = default(DateTime);

            if (day == default(DateTime) ||
                day >= DateTime.MaxValue.Date ||
                string.IsNullOrWhiteSpace(normalizedTime))
            {
                return false;
            }

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

            projectionDateTime = day.Date.Add(parsedTime.TimeOfDay);
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}