using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MovieTicketMVC.Services
{
    public sealed class SeatLockInfo
    {
        public int MovieId { get; set; }

        public DateTime Day { get; set; }

        public string Time { get; set; }

        public string SeatId { get; set; }

        public string ConnectionId { get; set; }

        public string OwnerEmail { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public Guid? PurchaseId { get; set; }
    }

    public static class SeatLockService
    {
        private static readonly object SyncRoot = new object();

        private static readonly TimeSpan LockLifetime =
            TimeSpan.FromMinutes(15);

        private static readonly TimeSpan PurchaseLifetime =
            TimeSpan.FromMinutes(5);

        private static readonly Dictionary<string, SeatLockInfo> Locks =
            new Dictionary<string, SeatLockInfo>(StringComparer.Ordinal);

        private static readonly Regex SeatIdRegex =
            new Regex(
                @"^R([1-9]|10)C([1-9]|10)$",
                RegexOptions.Compiled |
                RegexOptions.IgnoreCase |
                RegexOptions.CultureInvariant);

        private static readonly HashSet<string> AllowedProjectionTimes =
            new HashSet<string>(
                new[] { "10:00", "14:00", "18:00", "21:00" },
                StringComparer.Ordinal);

        private static readonly Timer ExpirationTimer;

        public static event Action<IReadOnlyCollection<SeatLockInfo>> LocksExpired;

        public static event Action<IReadOnlyCollection<SeatLockInfo>> PurchaseLocksReleased;

        static SeatLockService()
        {
            ExpirationTimer = new Timer(
                CleanupExpiredLocks,
                null,
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(10));
        }

        public static bool TryLock(
            int movieId,
            DateTime day,
            string time,
            string seatId,
            string connectionId,
            string ownerEmail)
        {
            string normalizedTime;
            string normalizedSeatId;

            if (string.IsNullOrWhiteSpace(ownerEmail) ||
                !TryNormalizeInput(
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

            var key = BuildKey(movieId, day, normalizedTime, normalizedSeatId);
            var acquired = false;
            List<SeatLockInfo> expiredLocks;

            lock (SyncRoot)
            {
                var nowUtc = DateTime.UtcNow;
                expiredLocks = RemoveExpiredLocks(nowUtc);

                SeatLockInfo existingLock;

                if (Locks.TryGetValue(key, out existingLock))
                {
                    if (!existingLock.PurchaseId.HasValue &&
                        string.Equals(
                            existingLock.ConnectionId,
                            connectionId,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            existingLock.OwnerEmail,
                            ownerEmail,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        existingLock.ExpiresAtUtc = nowUtc.Add(LockLifetime);
                        acquired = true;
                    }
                }
                else
                {
                    Locks.Add(key, new SeatLockInfo
                    {
                        MovieId = movieId,
                        Day = day.Date,
                        Time = normalizedTime,
                        SeatId = normalizedSeatId,
                        ConnectionId = connectionId,
                        OwnerEmail = ownerEmail,
                        ExpiresAtUtc = nowUtc.Add(LockLifetime)
                    });

                    acquired = true;
                }
            }

            NotifyLocks(LocksExpired, expiredLocks, "expiration");
            return acquired;
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

            var key = BuildKey(movieId, day, normalizedTime, normalizedSeatId);
            var unlocked = false;
            List<SeatLockInfo> expiredLocks;

            lock (SyncRoot)
            {
                expiredLocks = RemoveExpiredLocks(DateTime.UtcNow);
                SeatLockInfo existingLock;

                if (Locks.TryGetValue(key, out existingLock) &&
                    !existingLock.PurchaseId.HasValue &&
                    string.Equals(
                        existingLock.ConnectionId,
                        connectionId,
                        StringComparison.Ordinal))
                {
                    unlocked = Locks.Remove(key);
                }
            }

            NotifyLocks(LocksExpired, expiredLocks, "expiration");
            return unlocked;
        }

        public static IReadOnlyCollection<SeatLockInfo> ReleaseAll(
            string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                return new List<SeatLockInfo>();
            }

            List<SeatLockInfo> expiredLocks;
            var releasedLocks = new List<SeatLockInfo>();

            lock (SyncRoot)
            {
                expiredLocks = RemoveExpiredLocks(DateTime.UtcNow);
                var keysToRelease = Locks
                    .Where(pair =>
                        !pair.Value.PurchaseId.HasValue &&
                        string.Equals(
                            pair.Value.ConnectionId,
                            connectionId,
                            StringComparison.Ordinal))
                    .Select(pair => pair.Key)
                    .ToList();

                foreach (var key in keysToRelease)
                {
                    releasedLocks.Add(CopyLockInfo(Locks[key]));
                    Locks.Remove(key);
                }
            }

            NotifyLocks(LocksExpired, expiredLocks, "expiration");
            return releasedLocks;
        }

        public static IReadOnlyCollection<SeatLockInfo> GetActiveLocks(
            int movieId,
            DateTime day,
            string time)
        {
            string normalizedTime;

            if (!TryNormalizeProjection(movieId, day, time, out normalizedTime))
            {
                return new List<SeatLockInfo>();
            }

            List<SeatLockInfo> expiredLocks;
            List<SeatLockInfo> activeLocks;

            lock (SyncRoot)
            {
                expiredLocks = RemoveExpiredLocks(DateTime.UtcNow);
                activeLocks = Locks.Values
                    .Where(seatLock =>
                        seatLock.MovieId == movieId &&
                        seatLock.Day == day.Date &&
                        string.Equals(
                            seatLock.Time,
                            normalizedTime,
                            StringComparison.Ordinal))
                    .Select(CopyLockInfo)
                    .OrderBy(seatLock => GetSeatSortValue(seatLock.SeatId))
                    .ToList();
            }

            NotifyLocks(LocksExpired, expiredLocks, "expiration");
            return activeLocks;
        }

        public static Guid? TryBeginPurchase(
            int movieId,
            DateTime day,
            string time,
            IEnumerable<string> seatIds,
            string connectionId,
            string ownerEmail)
        {
            string normalizedTime;

            if (string.IsNullOrWhiteSpace(connectionId) ||
                string.IsNullOrWhiteSpace(ownerEmail) ||
                !TryNormalizeProjection(movieId, day, time, out normalizedTime) ||
                seatIds == null)
            {
                return null;
            }

            var selectedSeats = seatIds.ToList();

            if (selectedSeats.Count == 0 || selectedSeats.Count > 100 ||
                selectedSeats.Any(seat =>
                    string.IsNullOrWhiteSpace(seat) ||
                    !SeatIdRegex.IsMatch(seat)) ||
                selectedSeats.Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                    selectedSeats.Count)
            {
                return null;
            }

            var purchaseId = Guid.NewGuid();
            var acquired = false;
            List<SeatLockInfo> expiredLocks;

            lock (SyncRoot)
            {
                var nowUtc = DateTime.UtcNow;
                expiredLocks = RemoveExpiredLocks(nowUtc);

                var locksToPurchase = new List<SeatLockInfo>();
                acquired = true;

                foreach (var seatId in selectedSeats)
                {
                    SeatLockInfo seatLock;
                    var key = BuildKey(movieId, day, normalizedTime, seatId);

                    if (!Locks.TryGetValue(key, out seatLock) ||
                        seatLock.PurchaseId.HasValue ||
                        !string.Equals(
                            seatLock.ConnectionId,
                            connectionId,
                            StringComparison.Ordinal) ||
                        !string.Equals(
                            seatLock.OwnerEmail,
                            ownerEmail,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        acquired = false;
                        break;
                    }

                    locksToPurchase.Add(seatLock);
                }

                if (acquired)
                {
                    foreach (var seatLock in locksToPurchase)
                    {
                        seatLock.PurchaseId = purchaseId;
                        seatLock.ExpiresAtUtc = nowUtc.Add(PurchaseLifetime);
                    }
                }
            }

            NotifyLocks(LocksExpired, expiredLocks, "expiration");
            return acquired ? (Guid?)purchaseId : null;
        }

        public static void EndPurchase(Guid purchaseId)
        {
            if (purchaseId == Guid.Empty)
            {
                return;
            }

            List<SeatLockInfo> expiredLocks;
            var releasedLocks = new List<SeatLockInfo>();

            lock (SyncRoot)
            {
                expiredLocks = RemoveExpiredLocks(DateTime.UtcNow);
                var entries = Locks
                    .Where(pair => pair.Value.PurchaseId == purchaseId)
                    .ToList();

                foreach (var entry in entries)
                {
                    releasedLocks.Add(CopyLockInfo(entry.Value));
                    Locks.Remove(entry.Key);
                }
            }

            NotifyLocks(LocksExpired, expiredLocks, "expiration");
            NotifyLocks(PurchaseLocksReleased, releasedLocks, "purchase release");
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
                string.IsNullOrWhiteSpace(seatId) ||
                !TryNormalizeProjection(movieId, day, time, out normalizedTime))
            {
                return false;
            }

            normalizedSeatId = seatId.Trim().ToUpperInvariant();
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

        private static List<SeatLockInfo> RemoveExpiredLocks(DateTime nowUtc)
        {
            var expiredEntries = Locks
                .Where(pair => pair.Value.ExpiresAtUtc <= nowUtc)
                .ToList();
            var expiredLocks = new List<SeatLockInfo>(expiredEntries.Count);

            foreach (var entry in expiredEntries)
            {
                expiredLocks.Add(CopyLockInfo(entry.Value));
                Locks.Remove(entry.Key);
            }

            return expiredLocks;
        }

        private static void CleanupExpiredLocks(object state)
        {
            try
            {
                List<SeatLockInfo> expiredLocks;

                lock (SyncRoot)
                {
                    expiredLocks = RemoveExpiredLocks(DateTime.UtcNow);
                }

                NotifyLocks(LocksExpired, expiredLocks, "expiration");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Seat lock cleanup failed: {0}", ex);
            }
        }

        private static void NotifyLocks(
            Action<IReadOnlyCollection<SeatLockInfo>> handler,
            List<SeatLockInfo> seatLocks,
            string operation)
        {
            if (handler == null || seatLocks == null || seatLocks.Count == 0)
            {
                return;
            }

            try
            {
                handler(seatLocks.AsReadOnly());
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "Seat lock {0} notification failed: {1}",
                    operation,
                    ex);
            }
        }

        private static SeatLockInfo CopyLockInfo(SeatLockInfo source)
        {
            return new SeatLockInfo
            {
                MovieId = source.MovieId,
                Day = source.Day,
                Time = source.Time,
                SeatId = source.SeatId,
                ConnectionId = source.ConnectionId,
                OwnerEmail = source.OwnerEmail,
                ExpiresAtUtc = source.ExpiresAtUtc,
                PurchaseId = source.PurchaseId
            };
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