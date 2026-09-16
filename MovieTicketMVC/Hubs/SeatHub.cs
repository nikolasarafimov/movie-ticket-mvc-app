using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using MovieTicketMVC.Services;

namespace MovieTicketMVC.Hubs
{
    [Authorize]
    public class SeatHub : Hub
    {
        public bool LockSeat(
            int movieId,
            string day,
            string time,
            string seatId)
        {
            DateTime selectedDay;

            if (!TryParseDay(day, out selectedDay))
            {
                return false;
            }

            var locked = SeatLockService.TryLock(
                movieId,
                selectedDay,
                time,
                seatId,
                Context.ConnectionId);

            if (locked)
            {
                Clients.Others.SeatLocked(
                    movieId,
                    selectedDay.ToString("yyyy-MM-dd"),
                    time,
                    seatId);
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

            if (!TryParseDay(day, out selectedDay))
            {
                return false;
            }

            var unlocked = SeatLockService.Unlock(
                movieId,
                selectedDay,
                time,
                seatId,
                Context.ConnectionId);

            if (unlocked)
            {
                Clients.Others.SeatUnlocked(
                    movieId,
                    selectedDay.ToString("yyyy-MM-dd"),
                    time,
                    seatId);
            }

            return unlocked;
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            var releasedLocks =
                SeatLockService.ReleaseAll(Context.ConnectionId);

            foreach (var releasedLock in releasedLocks)
            {
                Clients.Others.SeatUnlocked(
                    releasedLock.MovieId,
                    releasedLock.Day.ToString("yyyy-MM-dd"),
                    releasedLock.Time,
                    releasedLock.SeatId);
            }

            return base.OnDisconnected(stopCalled);
        }

        private static bool TryParseDay(
            string value,
            out DateTime day)
        {
            return DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out day);
        }
    }
}