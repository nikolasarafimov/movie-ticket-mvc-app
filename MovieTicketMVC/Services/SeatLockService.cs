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

        public DateTime ExpiresAtUtc { get; set; }
    }

    public static class SeatLockService
    {
        private static readonly object SyncRoot =
            new object();

        private static readonly TimeSpan LockLifetime =
            TimeSpan.FromMinutes(15);

        private static readonly Dictionary<string, SeatLockInfo> Locks =
            new Dictionary<string, SeatLockInfo>(
                StringComparer.Ordinal);

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

            var key = BuildKey(
                movieId,
                day,
                normalizedTime,
                normalizedSeatId);

            lock (SyncRoot)
            {
                var now = DateTime.UtcNow;

                RemoveExpiredLocks(now);

                SeatLockInfo existingLock;

                if (Locks.TryGetValue(
                        key,
                        out existingLock))
                {
                    if (!string.Equals(
                            existingLock.ConnectionId,
                            connectionId,
                            StringComparison.Ordinal))
                    {
                        return false;
                    }

                    existingLock.ExpiresAtUtc =
                        now.Add(LockLifetime);

                    return true;
                }

                Locks.Add(
                    key,
                    new SeatLockInfo
                    {
                        MovieId = movieId,
                        Day = day.Date,
                        Time = normalizedTime,
                        SeatId = normalizedSeatId,
                        ConnectionId = connectionId,
                        ExpiresAtUtc = now.Add(LockLifetime)
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

            var key = BuildKey(
                movieId,
                day,
                normalizedTime,
                normalizedSeatId);

            lock (SyncRoot)
            {
                RemoveExpiredLocks(DateTime.UtcNow);

                SeatLockInfo existingLock;

                if (!Locks.TryGetValue(
                        key,
                        out existingLock))
                {
                    return false;
                }

                if (!string.Equals(
                        existingLock.ConnectionId,
                        connectionId,
                        StringComparison.Ordinal))
                {
                    return false;
                }

                return Locks.Remove(key);
            }
        }

        public static IReadOnlyCollection<SeatLockInfo> ReleaseAll(
            string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                return new List<SeatLockInfo>();
            }

            lock (SyncRoot)
            {
                RemoveExpiredLocks(DateTime.UtcNow);

                var locksToRelease = Locks
                    .Where(pair =>
                        string.Equals(
                            pair.Value.ConnectionId,
                            connectionId,
                            StringComparison.Ordinal))
                    .ToList();

                var releasedLocks = new List<SeatLockInfo>();

                foreach (var pair in locksToRelease)
                {
                    releasedLocks.Add(
                        CopyLockInfo(pair.Value));

                    Locks.Remove(pair.Key);
                }

                return releasedLocks;
            }
        }

        public static IReadOnlyCollection<SeatLockInfo> GetActiveLocks(
            int movieId,
            DateTime day,
            string time)
        {
            string normalizedTime;

            if (!TryNormalizeProjection(
                    movieId,
                    day,
                    time,
                    out normalizedTime))
            {
                return new List<SeatLockInfo>();
            }

            lock (SyncRoot)
            {
                RemoveExpiredLocks(DateTime.UtcNow);

                return Locks.Values
                    .Where(seatLock =>
                        seatLock.MovieId == movieId &&
                        seatLock.Day == day.Date &&
                        string.Equals(
                            seatLock.Time,
                            normalizedTime,
                            StringComparison.Ordinal))
                    .Select(CopyLockInfo)
                    .OrderBy(seatLock =>
                        GetSeatSortValue(seatLock.SeatId))
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
            normalizedTime = null;
            normalizedSeatId = null;

            if (string.IsNullOrWhiteSpace(connectionId) ||
                string.IsNullOrWhiteSpace(seatId))
            {
                return false;
            }

            if (!TryNormalizeProjection(
                    movieId,
                    day,
                    time,
                    out normalizedTime))
            {
                return false;
            }

            normalizedSeatId = seatId
                .Trim()
                .ToUpperInvariant();

            return SeatIdRegex.IsMatch(normalizedSeatId);
        }

        private static bool TryNormalizeProjection(
            int movieId,
            DateTime day,
            string time,
            out string normalizedTime)
        {
            normalizedTime = null;

            if (movieId <= 0 ||
                day == default(DateTime) ||
                day.Date >= DateTime.MaxValue.Date ||
                string.IsNullOrWhiteSpace(time))
            {
                return false;
            }

            var value = time.Trim();

            if (!AllowedProjectionTimes.Contains(value))
            {
                return false;
            }

            normalizedTime = value;

            return true;
        }

        private static void RemoveExpiredLocks(DateTime nowUtc)
        {
            var expiredKeys = Locks
                .Where(pair =>
                    pair.Value.ExpiresAtUtc <= nowUtc)
                .Select(pair => pair.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                Locks.Remove(key);
            }
        }

        private static SeatLockInfo CopyLockInfo(
            SeatLockInfo source)
        {
            return new SeatLockInfo
            {
                MovieId = source.MovieId,
                Day = source.Day,
                Time = source.Time,
                SeatId = source.SeatId,
                ConnectionId = source.ConnectionId,
                ExpiresAtUtc = source.ExpiresAtUtc
            };
        }

        private static int GetSeatSortValue(string seatId)
        {
            var match = SeatIdRegex.Match(
                seatId ?? string.Empty);

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