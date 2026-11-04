using MovieTicketMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MovieTicketMVC.Controllers
{
    public class MoviesController : Controller
    {
        private const int MaxTrailerSizeBytes = 200 * 1024 * 1024;

        private const string VideosVirtualDirectory =
            "~/Content/Videos/";

        private const string EditableMovieProperties =
            "Title,LengthInMinutes,ReleaseDate,Description,Genre,IsForAdults,Rating,Actors,Language";

        private const string EditableMoviePropertiesWithId =
            "Id," + EditableMovieProperties;

        private static readonly HashSet<string> AllowedVideoExtensions =
            new HashSet<string>(
                new[] { ".mp4", ".webm", ".ogg" },
                StringComparer.OrdinalIgnoreCase);

        private readonly ApplicationDbContext _context;

        public MoviesController()
        {
            _context = new ApplicationDbContext();
        }

        [HttpGet]
        public ActionResult Current(
            string searchString,
            string sortOrder,
            string ageFilter)
        {
            return ListMovies(
                true,
                searchString,
                sortOrder,
                ageFilter);
        }

        [HttpGet]
        public ActionResult ComingSoon(
            string searchString,
            string sortOrder,
            string ageFilter)
        {
            return ListMovies(
                false,
                searchString,
                sortOrder,
                ageFilter);
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            var movie = _context.Movies
                .AsNoTracking()
                .SingleOrDefault(m => m.Id == id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                GetMovieSection(movie);

            return View(movie);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.ActiveMovieSection = "ComingSoon";

            return View(new Movie());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(
            [Bind(Include = EditableMovieProperties)] Movie movie,
            HttpPostedFileBase videoFile)
        {
            ViewBag.ActiveMovieSection = "ComingSoon";

            if (movie == null)
            {
                return new HttpStatusCodeResult(400);
            }

            if (movie.ReleaseDate == default(DateTime))
            {
                ModelState.AddModelError(
                    "ReleaseDate",
                    "Please enter a valid release date.");
            }

            if (videoFile == null ||
                videoFile.ContentLength <= 0)
            {
                ModelState.AddModelError(
                    "videoFile",
                    "Please upload a movie trailer.");
            }

            if (!ModelState.IsValid)
            {
                return View(movie);
            }

            string trailerPath;
            string uploadError;

            if (!TrySaveTrailer(
                    videoFile,
                    out trailerPath,
                    out uploadError))
            {
                ModelState.AddModelError(
                    "videoFile",
                    uploadError);

                return View(movie);
            }

            movie.ReleaseDate =
                movie.ReleaseDate.Date;

            movie.LocalTrailerPath =
                trailerPath;

            movie.IsCurrentlyShowing =
                IsCurrentlyShowing(movie);

            try
            {
                _context.Movies.Add(movie);

                _context.SaveChanges();
            }
            catch
            {
                TryDeleteTrailer(trailerPath);
                throw;
            }

            return RedirectToAction(
                GetMovieSection(movie));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var movie = _context.Movies.Find(id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                GetMovieSection(movie);

            return View(movie);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(
            [Bind(Include = EditableMoviePropertiesWithId)] Movie movie,
            string ReturnSection,
            HttpPostedFileBase videoFile)
        {
            if (movie == null)
            {
                return new HttpStatusCodeResult(400);
            }

            var movieInDb =
                _context.Movies.Find(movie.Id);

            if (movieInDb == null)
            {
                return HttpNotFound();
            }

            ViewBag.ReturnSection =
                ReturnSection;

            ViewBag.ActiveMovieSection =
                GetMovieSection(movieInDb);

            if (movie.ReleaseDate == default(DateTime))
            {
                ModelState.AddModelError(
                    "ReleaseDate",
                    "Please enter a valid release date.");
            }

            if (!ModelState.IsValid)
            {
                movie.LocalTrailerPath =
                    movieInDb.LocalTrailerPath;

                return View(movie);
            }

            string newTrailerPath = null;

            if (videoFile != null &&
                videoFile.ContentLength > 0)
            {
                string uploadError;

                if (!TrySaveTrailer(
                        videoFile,
                        out newTrailerPath,
                        out uploadError))
                {
                    ModelState.AddModelError(
                        "videoFile",
                        uploadError);

                    movie.LocalTrailerPath =
                        movieInDb.LocalTrailerPath;

                    return View(movie);
                }
            }

            var oldTrailerPath =
                movieInDb.LocalTrailerPath;

            movieInDb.Title =
                movie.Title;

            movieInDb.LengthInMinutes =
                movie.LengthInMinutes;

            movieInDb.ReleaseDate =
                movie.ReleaseDate.Date;

            movieInDb.Description =
                movie.Description;

            movieInDb.Genre =
                movie.Genre;

            movieInDb.IsForAdults =
                movie.IsForAdults;

            movieInDb.Rating =
                movie.Rating;

            movieInDb.Actors =
                movie.Actors;

            movieInDb.Language =
                movie.Language;

            movieInDb.IsCurrentlyShowing =
                IsCurrentlyShowing(movieInDb);

            if (!string.IsNullOrWhiteSpace(
                    newTrailerPath))
            {
                movieInDb.LocalTrailerPath =
                    newTrailerPath;
            }

            try
            {
                _context.SaveChanges();
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(
                        newTrailerPath))
                {
                    TryDeleteTrailer(newTrailerPath);
                }

                throw;
            }

            if (!string.IsNullOrWhiteSpace(newTrailerPath) &&
                !string.IsNullOrWhiteSpace(oldTrailerPath) &&
                !string.Equals(
                    newTrailerPath,
                    oldTrailerPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                DeleteTrailerIfUnused(oldTrailerPath);
            }

            return RedirectToAction(
                GetMovieSection(movieInDb));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var movie = _context.Movies
                .AsNoTracking()
                .SingleOrDefault(m => m.Id == id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                GetMovieSection(movie);

            return View(movie);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            var movie = _context.Movies.Find(id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                GetMovieSection(movie);

            var hasTickets = _context.Tickets
                .Any(t => t.MovieId == movie.Id);

            if (hasTickets)
            {
                ModelState.AddModelError(
                    "",
                    "This movie cannot be deleted because " +
                    "it has existing ticket bookings.");

                return View("Delete", movie);
            }

            var trailerPath =
                movie.LocalTrailerPath;

            var section =
                GetMovieSection(movie);

            _context.Movies.Remove(movie);

            try
            {
                _context.SaveChanges();
            }
            catch (DbUpdateException)
            {
                _context.Entry(movie).State =
                    EntityState.Unchanged;

                ModelState.AddModelError(
                    "",
                    "The movie could not be deleted because " +
                    "related bookings may have been created " +
                    "in the meantime.");

                return View("Delete", movie);
            }

            if (!string.IsNullOrWhiteSpace(trailerPath))
            {
                DeleteTrailerIfUnused(trailerPath);
            }

            return RedirectToAction(section);
        }

        private ActionResult ListMovies(
            bool current,
            string searchString,
            string sortOrder,
            string ageFilter)
        {
            var tomorrow =
                DateTime.Today.AddDays(1);

            IQueryable<Movie> query =
                _context.Movies.AsNoTracking();

            query = current
                ? query.Where(m => m.ReleaseDate < tomorrow)
                : query.Where(m => m.ReleaseDate >= tomorrow);

            if (string.Equals(
                    ageFilter,
                    "Over18",
                    StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(
                    m => m.IsForAdults == true);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var search = searchString.Trim();

                query = query.Where(
                    m => m.Title.Contains(search));
            }

            var descending = string.Equals(
                sortOrder,
                "title_desc",
                StringComparison.OrdinalIgnoreCase);

            ViewBag.TitleSortParam = descending
                ? "title_asc"
                : "title_desc";

            query = descending
                ? query
                    .OrderByDescending(m => m.Title)
                    .ThenBy(m => m.Id)
                : query
                    .OrderBy(m => m.Title)
                    .ThenBy(m => m.Id);

            return View(
                current ? "Current" : "ComingSoon",
                query.ToList());
        }

        private bool TrySaveTrailer(
            HttpPostedFileBase videoFile,
            out string virtualPath,
            out string errorMessage)
        {
            virtualPath = null;
            errorMessage = null;

            if (videoFile == null ||
                videoFile.ContentLength <= 0)
            {
                errorMessage =
                    "Please upload a movie trailer.";

                return false;
            }

            if (videoFile.ContentLength > MaxTrailerSizeBytes)
            {
                errorMessage =
                    "The trailer must not exceed 200 MB.";

                return false;
            }

            string extension;

            try
            {
                extension = Path.GetExtension(
                    videoFile.FileName ?? string.Empty);
            }
            catch (ArgumentException)
            {
                errorMessage =
                    "The uploaded file name is invalid.";

                return false;
            }

            if (string.IsNullOrWhiteSpace(extension) ||
                !AllowedVideoExtensions.Contains(extension))
            {
                errorMessage =
                    "Only MP4, WebM, and Ogg video files are allowed.";

                return false;
            }

            extension = extension.ToLowerInvariant();

            bool hasValidSignature;

            try
            {
                hasValidSignature =
                    HasValidVideoSignature(
                        videoFile,
                        extension);
            }
            catch (IOException)
            {
                errorMessage =
                    "The uploaded video could not be read.";

                return false;
            }
            catch (NotSupportedException)
            {
                errorMessage =
                    "The uploaded video could not be read.";

                return false;
            }

            if (!hasValidSignature)
            {
                errorMessage =
                    "The uploaded file does not have " +
                    "a valid video file signature.";

                return false;
            }

            var fileName =
                Guid.NewGuid().ToString("N") +
                extension;

            string physicalPath = null;

            try
            {
                var videosDirectory =
                    Server.MapPath(
                        VideosVirtualDirectory);

                if (string.IsNullOrWhiteSpace(
                        videosDirectory))
                {
                    errorMessage =
                        "The trailer upload directory " +
                        "could not be determined.";

                    return false;
                }

                Directory.CreateDirectory(
                    videosDirectory);

                physicalPath = Path.Combine(
                    videosDirectory,
                    fileName);

                videoFile.SaveAs(physicalPath);
            }
            catch (IOException)
            {
                TryDeletePhysicalFile(physicalPath);

                errorMessage =
                    "The trailer could not be saved.";

                return false;
            }
            catch (UnauthorizedAccessException)
            {
                TryDeletePhysicalFile(physicalPath);

                errorMessage =
                    "The server does not have permission " +
                    "to save the trailer.";

                return false;
            }
            catch (HttpException)
            {
                TryDeletePhysicalFile(physicalPath);

                errorMessage =
                    "The trailer could not be saved.";

                return false;
            }

            virtualPath =
                VideosVirtualDirectory + fileName;

            return true;
        }

        private static bool HasValidVideoSignature(
            HttpPostedFileBase videoFile,
            string extension)
        {
            var stream = videoFile.InputStream;

            if (stream == null ||
                !stream.CanRead ||
                !stream.CanSeek)
            {
                return false;
            }

            var originalPosition =
                stream.Position;

            try
            {
                stream.Position = 0;

                var header = new byte[12];
                var totalBytesRead = 0;

                while (totalBytesRead < header.Length)
                {
                    var bytesRead = stream.Read(
                        header,
                        totalBytesRead,
                        header.Length - totalBytesRead);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    totalBytesRead += bytesRead;
                }

                if (extension == ".mp4")
                {
                    return totalBytesRead >= 12 &&
                           header[4] == (byte)'f' &&
                           header[5] == (byte)'t' &&
                           header[6] == (byte)'y' &&
                           header[7] == (byte)'p';
                }

                if (extension == ".webm")
                {
                    return totalBytesRead >= 4 &&
                           header[0] == 0x1A &&
                           header[1] == 0x45 &&
                           header[2] == 0xDF &&
                           header[3] == 0xA3;
                }

                if (extension == ".ogg")
                {
                    return totalBytesRead >= 4 &&
                           header[0] == (byte)'O' &&
                           header[1] == (byte)'g' &&
                           header[2] == (byte)'g' &&
                           header[3] == (byte)'S';
                }

                return false;
            }
            finally
            {
                stream.Position =
                    originalPosition;
            }
        }

        private static bool IsCurrentlyShowing(
            Movie movie)
        {
            return movie != null &&
                   movie.ReleaseDate.Date <=
                   DateTime.Today;
        }

        private static string GetMovieSection(
            Movie movie)
        {
            return IsCurrentlyShowing(movie)
                ? "Current"
                : "ComingSoon";
        }

        private void DeleteTrailerIfUnused(
            string trailerPath)
        {
            if (string.IsNullOrWhiteSpace(trailerPath))
            {
                return;
            }

            var stillInUse = _context.Movies
                .AsNoTracking()
                .Any(m =>
                    m.LocalTrailerPath == trailerPath);

            if (!stillInUse)
            {
                TryDeleteTrailer(trailerPath);
            }
        }

        private void TryDeleteTrailer(
            string localTrailerPath)
        {
            if (string.IsNullOrWhiteSpace(
                    localTrailerPath))
            {
                return;
            }

            var fileName =
                Path.GetFileName(
                    localTrailerPath);

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return;
            }

            if (!string.Equals(
                    localTrailerPath,
                    VideosVirtualDirectory + fileName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var extension =
                Path.GetExtension(fileName);

            if (!AllowedVideoExtensions.Contains(extension))
            {
                return;
            }

            var nameWithoutExtension =
                Path.GetFileNameWithoutExtension(fileName);

            Guid parsedGuid;

            if (!Guid.TryParseExact(
                    nameWithoutExtension,
                    "N",
                    out parsedGuid))
            {
                return;
            }

            try
            {
                var videosDirectory =
                    Server.MapPath(
                        VideosVirtualDirectory);

                if (string.IsNullOrWhiteSpace(
                        videosDirectory))
                {
                    return;
                }

                var physicalPath =
                    Path.Combine(
                        videosDirectory,
                        fileName);

                TryDeletePhysicalFile(physicalPath);
            }
            catch (HttpException)
            {
            }
        }

        private static void TryDeletePhysicalFile(
            string physicalPath)
        {
            if (string.IsNullOrWhiteSpace(physicalPath))
            {
                return;
            }

            try
            {
                if (System.IO.File.Exists(
                        physicalPath))
                {
                    System.IO.File.Delete(
                        physicalPath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
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