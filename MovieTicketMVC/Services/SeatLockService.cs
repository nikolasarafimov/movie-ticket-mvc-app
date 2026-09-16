using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MovieTicketMVC.Services
{
    public sealed class SeatLockInfo
    {
        public int MovieId { get; set; }

        public DateTime Day { get; set; }

        public string Time { get; set; }

        public string SeatId { get; set; }

        public string ConnectionId { get; set; }
    }

    public static class SeatLockService
    {
        private static readonly object SyncRoot =
            new object();

        private static readonly Dictionary<string, SeatLockInfo> Locks =
            new Dictionary<string, SeatLockInfo>(
                StringComparer.Ordinal);

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

        public static bool TryLock(
            int movieId,
            DateTime day,
            string time,
            string seatId,
            string connectionId)
        {
            string normalizedTime;
            string normalizedSeatId;

            if (!TryNormalizeInput(
                    movieId,
                    day,
                    time,
                    seatId,
                    connectionId,
                    out normalizedTime,
                    out normalizedSeatId))
            {
                return false;
            }

            var key =
                BuildKey(
                    movieId,
                    day,
                    normalizedTime,
                    normalizedSeatId);

            lock (SyncRoot)
            {
                SeatLockInfo existingLock;

                if (Locks.TryGetValue(
                        key,
                        out existingLock))
                {
                    return string.Equals(
                        existingLock.ConnectionId,
                        connectionId,
                        StringComparison.Ordinal);
                }

                Locks.Add(
                    key,
                    new SeatLockInfo
                    {
                        MovieId = movieId,
                        Day = day.Date,
                        Time = normalizedTime,
                        SeatId = normalizedSeatId,
                        ConnectionId = connectionId
                    });

                return true;
            }
        }

        public static bool Unlock(
            int movieId,
            DateTime day,
            string time,
            string seatId,
            string connectionId)
        {
            string normalizedTime;
            string normalizedSeatId;

            if (!TryNormalizeInput(
                    movieId,
                    day,
                    time,
                    seatId,
                    connectionId,
                    out normalizedTime,
                    out normalizedSeatId))
            {
                return false;
            }

            var key =
                BuildKey(
                    movieId,
                    day,
                    normalizedTime,
                    normalizedSeatId);

            lock (SyncRoot)
            {
                SeatLockInfo existingLock;

                if (!Locks.TryGetValue(
                        key,
                        out existingLock) ||
                    !string.Equals(
                        existingLock.ConnectionId,
                        connectionId,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                Locks.Remove(key);

                return true;
            }
        }

        public static IReadOnlyCollection<SeatLockInfo> ReleaseAll(
            string connectionId)
        {
            if (string.IsNullOrWhiteSpace(
                    connectionId))
            {
                return new List<SeatLockInfo>();
            }

            lock (SyncRoot)
            {
                var locksToRelease =
                    Locks
                        .Where(pair =>
                            string.Equals(
                                pair.Value.ConnectionId,
                                connectionId,
                                StringComparison.Ordinal))
                        .Select(pair =>
                            new
                            {
                                Key = pair.Key,
                                Lock = pair.Value
                            })
                        .ToList();

                foreach (var item in locksToRelease)
                {
                    Locks.Remove(
                        item.Key);
                }

                return locksToRelease
                    .Select(item => item.Lock)
                    .ToList();
            }
        }

        private static bool TryNormalizeInput(
            int movieId,
            DateTime day,
            string time,
            string seatId,
            string connectionId,
            out string normalizedTime,
            out string normalizedSeatId)
        {
            normalizedTime =
                null;

            normalizedSeatId =
                null;

            if (movieId <= 0 ||
                day == default(DateTime) ||
                string.IsNullOrWhiteSpace(time) ||
                string.IsNullOrWhiteSpace(seatId) ||
                string.IsNullOrWhiteSpace(connectionId))
            {
                return false;
            }

            normalizedTime =
                time.Trim();

            if (!AllowedProjectionTimes.Contains(
                    normalizedTime))
            {
                return false;
            }

            normalizedSeatId =
                seatId.Trim()
                    .ToUpperInvariant();

            if (!SeatIdRegex.IsMatch(
                    normalizedSeatId))
            {
                return false;
            }

            return true;
        }

        private static string BuildKey(
            int movieId,
            DateTime day,
            string time,
            string seatId)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1:yyyyMMdd}|{2}|{3}",
                movieId,
                day.Date,
                time,
                seatId);
        }
    }
}