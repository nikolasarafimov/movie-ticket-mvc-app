using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using MovieTicketMVC.Models;
using MovieTicketMVC.Services;

namespace MovieTicketMVC.Hubs
{
    [Authorize]
    public class SeatHub : Hub
    {
        private static readonly Regex SeatIdRegex =
            new Regex(
                @"^R([1-9]|10)C([1-9]|10)$",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

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

        public async Task<bool> LockSeat(
            int movieId,
            string day,
            string time,
            string seatId)
        {
            DateTime selectedDay;
            string normalizedTime;
            string normalizedSeatId;

            if (!TryNormalizeInput(
                    movieId,
                    day,
                    time,
                    seatId,
                    out selectedDay,
                    out normalizedTime,
                    out normalizedSeatId))
            {
                return false;
            }

            if (!IsFutureProjection(
                    selectedDay,
                    normalizedTime))
            {
                return false;
            }

            try
            {
                using (var context = new ApplicationDbContext())
                {
                    var releaseDate = await context.Movies
                        .AsNoTracking()
                        .Where(movie => movie.Id == movieId)
                        .Select(movie =>
                            (DateTime?)movie.ReleaseDate)
                        .SingleOrDefaultAsync();

                    if (!releaseDate.HasValue ||
                        selectedDay < releaseDate.Value.Date)
                    {
                        return false;
                    }

                    var nextDay = selectedDay.AddDays(1);

                    var purchasedSeatValues = await context.Tickets
                        .AsNoTracking()
                        .Where(ticket =>
                            ticket.MovieId == movieId &&
                            ticket.SelectedDay >= selectedDay &&
                            ticket.SelectedDay < nextDay &&
                            ticket.SelectedTime == normalizedTime)
                        .Select(ticket => ticket.SelectedSeats)
                        .ToListAsync();

                    if (purchasedSeatValues.Any(
                            value => ContainsSeat(
                                value,
                                normalizedSeatId)))
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Seat validation failed. Movie: {0}, Day: {1:yyyy-MM-dd}, Time: {2}, Error: {3}",
                    movieId,
                    selectedDay,
                    normalizedTime,
                    ex);

                return false;
            }

            if (!IsFutureProjection(
                    selectedDay,
                    normalizedTime))
            {
                return false;
            }

            var locked = SeatLockService.TryLock(
                movieId,
                selectedDay,
                normalizedTime,
                normalizedSeatId,
                Context.ConnectionId);

            if (locked)
            {
                Clients.Others.SeatLocked(
                    movieId,
                    selectedDay.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),
                    normalizedTime,
                    normalizedSeatId);
            }

            return locked;
        }

        public bool UnlockSeat(
            int movieId,
            string day,
            string time,
            string seatId)
        {
            DateTime selectedDay;
            string normalizedTime;
            string normalizedSeatId;

            if (!TryNormalizeInput(
                    movieId,
                    day,
                    time,
                    seatId,
                    out selectedDay,
                    out normalizedTime,
                    out normalizedSeatId))
            {
                return false;
            }

            var unlocked = SeatLockService.Unlock(
                movieId,
                selectedDay,
                normalizedTime,
                normalizedSeatId,
                Context.ConnectionId);

            if (unlocked)
            {
                Clients.Others.SeatUnlocked(
                    movieId,
                    selectedDay.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),
                    normalizedTime,
                    normalizedSeatId);
            }

            return unlocked;
        }

        public string[] GetLockedSeats(
            int movieId,
            string day,
            string time)
        {
            DateTime selectedDay;
            string normalizedTime;

            if (!TryNormalizeProjection(
                    movieId,
                    day,
                    time,
                    out selectedDay,
                    out normalizedTime))
            {
                return new string[0];
            }

            if (!IsFutureProjection(
                    selectedDay,
                    normalizedTime))
            {
                return new string[0];
            }

            return SeatLockService.GetActiveLocks(
                    movieId,
                    selectedDay,
                    normalizedTime)
                .Where(seatLock =>
                    !string.Equals(
                        seatLock.ConnectionId,
                        Context.ConnectionId,
                        StringComparison.Ordinal))
                .Select(seatLock =>
                    seatLock.SeatId)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .OrderBy(
                    seatId => GetSeatSortValue(seatId))
                .ToArray();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var releasedLocks = SeatLockService.ReleaseAll(
                Context.ConnectionId);

            foreach (var releasedLock in releasedLocks)
            {
                Clients.Others.SeatUnlocked(
                    releasedLock.MovieId,
                    releasedLock.Day.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),
                    releasedLock.Time,
                    releasedLock.SeatId);
            }

            return base.OnDisconnected(stopCalled);
        }

        private static bool TryNormalizeInput(
            int movieId,
            string day,
            string time,
            string seatId,
            out DateTime selectedDay,
            out string normalizedTime,
            out string normalizedSeatId)
        {
            normalizedSeatId = null;

            if (!TryNormalizeProjection(
                    movieId,
                    day,
                    time,
                    out selectedDay,
                    out normalizedTime))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(seatId))
            {
                return false;
            }

            normalizedSeatId =
                seatId.Trim().ToUpperInvariant();

            return SeatIdRegex.IsMatch(normalizedSeatId);
        }

        private static bool TryNormalizeProjection(
            int movieId,
            string day,
            string time,
            out DateTime selectedDay,
            out string normalizedTime)
        {
            selectedDay = default(DateTime);
            normalizedTime = null;

            if (movieId <= 0 ||
                string.IsNullOrWhiteSpace(day) ||
                string.IsNullOrWhiteSpace(time))
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                    day.Trim(),
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out selectedDay))
            {
                return false;
            }

            normalizedTime = time.Trim();

            return AllowedProjectionTimes.Contains(
                normalizedTime);
        }

        private static bool IsFutureProjection(
            DateTime selectedDay,
            string normalizedTime)
        {
            if (selectedDay < DateTime.Today ||
                selectedDay >= DateTime.MaxValue.Date)
            {
                return false;
            }

            TimeSpan projectionTime;

            if (!TimeSpan.TryParseExact(
                    normalizedTime,
                    @"hh\:mm",
                    CultureInfo.InvariantCulture,
                    out projectionTime))
            {
                return false;
            }

            return selectedDay.Add(projectionTime) >
                DateTime.Now;
        }

        private static bool ContainsSeat(
            string selectedSeats,
            string seatId)
        {
            if (string.IsNullOrWhiteSpace(selectedSeats))
            {
                return false;
            }

            return selectedSeats
                .Split(
                    new[] { ',' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(value =>
                    string.Equals(
                        value.Trim(),
                        seatId,
                        StringComparison.OrdinalIgnoreCase));
        }

        private static int GetSeatSortValue(
            string seatId)
        {
            var match = SeatIdRegex.Match(seatId);

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
    }
}