using MovieTicketMVC.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MovieTicketMVC.Controllers
{
    public class MoviesController : Controller
    {
        private const int MaxTrailerSizeBytes =
            200 * 1024 * 1024;

        private static readonly HashSet<string> AllowedVideoExtensions =
            new HashSet<string>(
                new[]
                {
                    ".mp4",
                    ".webm",
                    ".ogg"
                },
                StringComparer.OrdinalIgnoreCase);

        private readonly ApplicationDbContext _context;

        public MoviesController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: Movies/Current
        [HttpGet]
        public ActionResult Current(
            string searchString,
            string sortOrder,
            string ageFilter)
        {
            var today = DateTime.Today;

            IQueryable<Movie> query =
                _context.Movies
                    .AsNoTracking()
                    .Where(m => m.ReleaseDate <= today);

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
                var search =
                    searchString.Trim();

                query = query.Where(
                    m => m.Title.Contains(search));
            }

            if (string.IsNullOrWhiteSpace(sortOrder))
            {
                sortOrder = "title_asc";
            }

            ViewBag.TitleSortParam =
                sortOrder == "title_asc"
                    ? "title_desc"
                    : "title_asc";

            switch (sortOrder)
            {
                case "title_desc":
                    query = query.OrderByDescending(
                        m => m.Title);
                    break;

                default:
                    query = query.OrderBy(
                        m => m.Title);
                    break;
            }

            return View(query.ToList());
        }

        // GET: Movies/ComingSoon
        [HttpGet]
        public ActionResult ComingSoon(
            string searchString,
            string sortOrder,
            string ageFilter)
        {
            var today = DateTime.Today;

            IQueryable<Movie> query =
                _context.Movies
                    .AsNoTracking()
                    .Where(m => m.ReleaseDate > today);

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
                var search =
                    searchString.Trim();

                query = query.Where(
                    m => m.Title.Contains(search));
            }

            if (string.IsNullOrWhiteSpace(sortOrder))
            {
                sortOrder = "title_asc";
            }

            ViewBag.TitleSortParam =
                sortOrder == "title_asc"
                    ? "title_desc"
                    : "title_asc";

            switch (sortOrder)
            {
                case "title_desc":
                    query = query.OrderByDescending(
                        m => m.Title);
                    break;

                default:
                    query = query.OrderBy(
                        m => m.Title);
                    break;
            }

            return View(query.ToList());
        }

        // GET: Movies/Details/5
        [HttpGet]
        public ActionResult Details(int id)
        {
            var movie =
                _context.Movies
                    .AsNoTracking()
                    .SingleOrDefault(
                        m => m.Id == id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                IsCurrentlyShowing(movie)
                    ? "Current"
                    : "ComingSoon";

            return View(movie);
        }

        // GET: Movies/Create
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Create()
        {
            ViewBag.ActiveMovieSection =
                "ComingSoon";

            return View(new Movie());
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Create(
            Movie movie,
            HttpPostedFileBase videoFile)
        {
            ViewBag.ActiveMovieSection =
                "ComingSoon";

            if (videoFile == null ||
                videoFile.ContentLength <= 0)
            {
                ModelState.AddModelError(
                    "videoFile",
                    "Мора да прикачите trailer видео.");
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
                movie.ReleaseDate <= DateTime.Today;

            try
            {
                _context.Movies.Add(movie);
                _context.SaveChanges();
            }
            catch
            {
                TryDeleteTrailer(
                    trailerPath);

                throw;
            }

            return RedirectToAction(
                IsCurrentlyShowing(movie)
                    ? "Current"
                    : "ComingSoon");
        }

        // GET: Movies/Edit/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(int id)
        {
            var movie =
                _context.Movies.Find(id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                IsCurrentlyShowing(movie)
                    ? "Current"
                    : "ComingSoon";

            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult Edit(
            Movie movie,
            string ReturnSection,
            HttpPostedFileBase videoFile)
        {
            var movieInDb =
                _context.Movies.Find(movie.Id);

            if (movieInDb == null)
            {
                return HttpNotFound();
            }

            ViewBag.ReturnSection =
                ReturnSection;

            ViewBag.ActiveMovieSection =
                IsCurrentlyShowing(movieInDb)
                    ? "Current"
                    : "ComingSoon";

            if (!ModelState.IsValid)
            {
                movie.LocalTrailerPath =
                    movieInDb.LocalTrailerPath;

                return View(movie);
            }

            string newTrailerPath =
                null;

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
                movieInDb.ReleaseDate <= DateTime.Today;

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
                    TryDeleteTrailer(
                        newTrailerPath);
                }

                throw;
            }

            if (!string.IsNullOrWhiteSpace(
                    newTrailerPath) &&
                !string.IsNullOrWhiteSpace(
                    oldTrailerPath) &&
                !string.Equals(
                    newTrailerPath,
                    oldTrailerPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteTrailer(
                    oldTrailerPath);
            }

            return RedirectToAction(
                IsCurrentlyShowing(movieInDb)
                    ? "Current"
                    : "ComingSoon");
        }

        // GET: Movies/Delete/5
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int id)
        {
            var movie =
                _context.Movies
                    .AsNoTracking()
                    .SingleOrDefault(
                        m => m.Id == id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            ViewBag.ActiveMovieSection =
                IsCurrentlyShowing(movie)
                    ? "Current"
                    : "ComingSoon";

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult DeleteConfirmed(int id)
        {
            var movie =
                _context.Movies.Find(id);

            if (movie == null)
            {
                return HttpNotFound();
            }

            var hasTickets =
                _context.Tickets.Any(
                    t => t.MovieId == movie.Id);

            if (hasTickets)
            {
                ViewBag.ActiveMovieSection =
                    IsCurrentlyShowing(movie)
                        ? "Current"
                        : "ComingSoon";

                ModelState.AddModelError(
                    "",
                    "Филмот не може да се избрише бидејќи за него веќе постојат купени билети.");

                return View(
                    "Delete",
                    movie);
            }

            var trailerPath =
                movie.LocalTrailerPath;

            var wasCurrent =
                IsCurrentlyShowing(movie);

            _context.Movies.Remove(movie);
            _context.SaveChanges();

            if (!string.IsNullOrWhiteSpace(
                    trailerPath))
            {
                TryDeleteTrailer(
                    trailerPath);
            }

            return RedirectToAction(
                wasCurrent
                    ? "Current"
                    : "ComingSoon");
        }

        private bool TrySaveTrailer(
            HttpPostedFileBase videoFile,
            out string virtualPath,
            out string errorMessage)
        {
            virtualPath =
                null;

            errorMessage =
                null;

            if (videoFile == null ||
                videoFile.ContentLength <= 0)
            {
                errorMessage =
                    "Мора да прикачите trailer видео.";

                return false;
            }

            if (videoFile.ContentLength >
                MaxTrailerSizeBytes)
            {
                errorMessage =
                    "Trailer видеото не смее да биде поголемо од 200 MB.";

                return false;
            }

            var extension =
                Path.GetExtension(
                    videoFile.FileName);

            if (string.IsNullOrWhiteSpace(
                    extension) ||
                !AllowedVideoExtensions.Contains(
                    extension))
            {
                errorMessage =
                    "Дозволени се само MP4, WebM и Ogg видео датотеки.";

                return false;
            }

            extension =
                extension.ToLowerInvariant();

            if (!HasValidVideoSignature(
                    videoFile,
                    extension))
            {
                errorMessage =
                    "Прикачената датотека не е валидно видео од дозволениот формат.";

                return false;
            }

            var fileName =
                Guid.NewGuid()
                    .ToString("N")
                + extension;

            try
            {
                var videosDirectory =
                    Server.MapPath(
                        "~/Content/Videos/");

                if (string.IsNullOrWhiteSpace(
                        videosDirectory))
                {
                    errorMessage =
                        "Не можеше да се одреди директориумот за trailer видеата.";

                    return false;
                }

                if (!Directory.Exists(
                        videosDirectory))
                {
                    Directory.CreateDirectory(
                        videosDirectory);
                }

                var physicalPath =
                    Path.Combine(
                        videosDirectory,
                        fileName);

                videoFile.SaveAs(
                    physicalPath);
            }
            catch (IOException)
            {
                errorMessage =
                    "Trailer видеото не можеше да се зачува.";

                return false;
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage =
                    "Апликацијата нема дозвола да го зачува trailer видеото.";

                return false;
            }
            catch (HttpException)
            {
                errorMessage =
                    "Trailer видеото не можеше да се зачува.";

                return false;
            }

            virtualPath =
                "~/Content/Videos/"
                + fileName;

            return true;
        }

        private static bool HasValidVideoSignature(
            HttpPostedFileBase videoFile,
            string extension)
        {
            var stream =
                videoFile.InputStream;

            if (stream == null ||
                !stream.CanRead)
            {
                return false;
            }

            long originalPosition =
                0;

            if (stream.CanSeek)
            {
                originalPosition =
                    stream.Position;

                stream.Position =
                    0;
            }

            try
            {
                var header =
                    new byte[12];

                var bytesRead =
                    stream.Read(
                        header,
                        0,
                        header.Length);

                if (extension == ".mp4")
                {
                    return bytesRead >= 8 &&
                           header[4] == (byte)'f' &&
                           header[5] == (byte)'t' &&
                           header[6] == (byte)'y' &&
                           header[7] == (byte)'p';
                }

                if (extension == ".webm")
                {
                    return bytesRead >= 4 &&
                           header[0] == 0x1A &&
                           header[1] == 0x45 &&
                           header[2] == 0xDF &&
                           header[3] == 0xA3;
                }

                if (extension == ".ogg")
                {
                    return bytesRead >= 4 &&
                           header[0] == (byte)'O' &&
                           header[1] == (byte)'g' &&
                           header[2] == (byte)'g' &&
                           header[3] == (byte)'S';
                }

                return false;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position =
                        originalPosition;
                }
            }
        }

        private static bool IsCurrentlyShowing(
            Movie movie)
        {
            return movie != null &&
                   movie.ReleaseDate.Date <= DateTime.Today;
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

            if (string.IsNullOrWhiteSpace(
                    fileName))
            {
                return;
            }

            try
            {
                var videosDirectory =
                    Server.MapPath(
                        "~/Content/Videos/");

                if (string.IsNullOrWhiteSpace(
                        videosDirectory))
                {
                    return;
                }

                var physicalPath =
                    Path.Combine(
                        videosDirectory,
                        fileName);

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
            catch (HttpException)
            {
            }
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