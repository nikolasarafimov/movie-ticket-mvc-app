using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity.Migrations;
using System.Linq;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using MovieTicketMVC.Models;

namespace MovieTicketMVC.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<ApplicationDbContext>
    {
        private static readonly Dictionary<string, string> LegacyGenres =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "\u041C\u0438\u0441\u0442\u0435\u0440\u0438\u0458\u0430", "Mystery" },
                { "\u041D\u0430\u0443\u0447\u043D\u0430 \u0444\u0430\u043D\u0442\u0430\u0441\u0442\u0438\u043A\u0430", "Science Fiction" },
                { "\u0410\u0432\u0430\u043D\u0442\u0443\u0440\u0430", "Adventure" },
                { "\u0410\u043A\u0446\u0438\u0458\u0430", "Action" },
                { "\u0425\u043E\u0440\u043E\u0440", "Horror" },
                { "\u0414\u0440\u0430\u043C\u0430", "Drama" },
                { "\u0418\u0441\u0442\u043E\u0440\u0438\u0441\u043A\u0438", "Historical" },
                { "\u0420\u043E\u043C\u0430\u043D\u0441\u0430", "Romance" },
                { "\u0422\u0440\u0438\u043B\u0435\u0440", "Thriller" },
                { "\u0412\u0435\u0441\u0442\u0435\u0440\u043D", "Western" },
                { "\u041A\u0440\u0438\u043C\u0438\u043D\u0430\u043B", "Crime" },
                { "\u0424\u0430\u043D\u0442\u0430\u0437\u0438\u0458\u0430", "Fantasy" }
            };

        private static readonly Dictionary<string, string> LegacyLanguages =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "\u0410\u043D\u0433\u043B\u0438\u0441\u043A\u0438", "English" },
                { "\u0424\u0440\u0430\u043D\u0446\u0443\u0441\u043A\u0438", "French" },
                { "\u0418\u0442\u0430\u043B\u0438\u0458\u0430\u043D\u0441\u043A\u0438", "Italian" },
                { "\u0413\u0435\u0440\u043C\u0430\u043D\u0441\u043A\u0438", "German" }
            };

        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
            SeedAdmin(context);

            NormalizeLegacyMovieMetadata(context);

            var seedDemoMovies = Environment.GetEnvironmentVariable(
                "MOVIETICKET_SEED_DEMO_MOVIES");

            if (string.Equals(
                    seedDemoMovies == null ? null : seedDemoMovies.Trim(),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
            {
                SeedMovies(context);
            }

            context.SaveChanges();
        }

        private static void SeedAdmin(ApplicationDbContext context)
        {
            const string adminRoleName = "Admin";

            var adminEmail = Environment.GetEnvironmentVariable(
                "MOVIETICKET_ADMIN_EMAIL");

            var adminPassword = Environment.GetEnvironmentVariable(
                "MOVIETICKET_ADMIN_PASSWORD");

            var hasAdminEmail = !string.IsNullOrWhiteSpace(adminEmail);
            var hasAdminPassword = !string.IsNullOrWhiteSpace(adminPassword);

            if (hasAdminEmail != hasAdminPassword)
            {
                throw new InvalidOperationException(
                    "Both MOVIETICKET_ADMIN_EMAIL and " +
                    "MOVIETICKET_ADMIN_PASSWORD must be configured " +
                    "when administrator seeding is enabled.");
            }

            if (hasAdminEmail)
            {
                adminEmail = adminEmail.Trim();

                if (!new EmailAddressAttribute().IsValid(adminEmail))
                {
                    throw new InvalidOperationException(
                        "MOVIETICKET_ADMIN_EMAIL must contain " +
                        "a valid email address.");
                }
            }

            var roleManager = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(context));

            if (!roleManager.RoleExists(adminRoleName))
            {
                var roleResult = roleManager.Create(
                    new IdentityRole(adminRoleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create the Admin role: " +
                        string.Join("; ", roleResult.Errors));
                }
            }

            if (!hasAdminEmail)
            {
                return;
            }

            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            userManager.UserValidator = new UserValidator<ApplicationUser>(
                userManager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            userManager.PasswordValidator = new UnicodePasswordValidator
            {
                RequiredLength = 8,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true
            };

            var adminUser =
                userManager.FindByEmail(adminEmail) ??
                userManager.FindByName(adminEmail);

            var createdBySeed = false;

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult = userManager.Create(
                    adminUser,
                    adminPassword);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create the administrator account: " +
                        string.Join("; ", createResult.Errors));
                }

                createdBySeed = true;
            }
            else
            {
                if (!string.Equals(
                        adminUser.Email,
                        adminEmail,
                        StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(
                        adminUser.UserName,
                        adminEmail,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "The existing administrator username and " +
                        "email address must match MOVIETICKET_ADMIN_EMAIL.");
                }

                if (!adminUser.EmailConfirmed)
                {
                    throw new InvalidOperationException(
                        "The existing administrator account " +
                        "must have a confirmed email address.");
                }
            }

            if (!userManager.IsInRole(adminUser.Id, adminRoleName))
            {
                if (!createdBySeed)
                {
                    throw new InvalidOperationException(
                        "The configured administrator email belongs to " +
                        "an existing non-administrator account. " +
                        "Administrator access must be granted manually " +
                        "after verifying account ownership.");
                }

                var roleResult = userManager.AddToRole(
                    adminUser.Id,
                    adminRoleName);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign the administrator role: " +
                        string.Join("; ", roleResult.Errors));
                }
            }
        }

        private static void NormalizeLegacyMovieMetadata(
            ApplicationDbContext context)
        {
            var existingMovies = context.Movies.ToList();

            foreach (var movie in existingMovies)
            {
                var translatedGenre =
                    TranslateLegacyGenre(movie.Genre);

                var translatedLanguage =
                    TranslateLegacyLanguage(movie.Language);

                if (!string.Equals(
                        movie.Genre,
                        translatedGenre,
                        StringComparison.Ordinal))
                {
                    movie.Genre = translatedGenre;
                }

                if (!string.Equals(
                        movie.Language,
                        translatedLanguage,
                        StringComparison.Ordinal))
                {
                    movie.Language = translatedLanguage;
                }
            }
        }

        private static void SeedMovies(ApplicationDbContext context)
        {
            var movies = new[]
            {
                new Movie
                {
                    Title = "Shadow of the Forgotten",
                    LengthInMinutes = 124,
                    ReleaseDate = new DateTime(2025, 1, 12),
                    Description = "A reclusive journalist stumbles upon a decades-old cold case while investigating a missing girl. As he digs deeper, he realizes the truth has been deliberately buried—along with those who sought it before him.",
                    IsForAdults = true,
                    Genre = "Mystery",
                    Rating = 7.5m,
                    Actors = "Saoirse Ronan, Rami Malek, Michael Fassbender, Lupita Nyong’o, Richard Madden",
                    LocalTrailerPath = "~/Content/Videos/ShadowTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Echoes of the Void",
                    LengthInMinutes = 108,
                    ReleaseDate = new DateTime(2024, 12, 25),
                    Description = "After a failed deep-space expedition, the sole survivor returns to Earth—only to find that something came back with him. As reality starts unraveling, he questions whether he ever truly escaped the void.",
                    IsForAdults = false,
                    Genre = "Science Fiction",
                    Rating = 8.0m,
                    Actors = "Riz Ahmed, Rooney Mara, Tilda Swinton, John David Washington, Jessie Buckley",
                    LocalTrailerPath = "~/Content/Videos/EchoesTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "The Clockmaker’s Curse",
                    LengthInMinutes = 132,
                    ReleaseDate = new DateTime(2025, 2, 1),
                    Description = "A young apprentice discovers that his mentor’s clockwork creations can manipulate time itself. But when a mysterious figure demands the ultimate timepiece, he must race against destiny to prevent catastrophe.",
                    IsForAdults = true,
                    Genre = "Adventure",
                    Rating = 7.8m,
                    Actors = "Tom Hiddleston, Eva Green, Mia Goth, Daniel Radcliffe, Helena Bonham Carter",
                    LocalTrailerPath = "~/Content/Videos/ClockmasterTrailer.mp4",
                    Language = "French"
                },
                new Movie
                {
                    Title = "Fractured Allegiance",
                    LengthInMinutes = 117,
                    ReleaseDate = new DateTime(2024, 11, 8),
                    Description = "A rogue CIA agent, accused of treason, must uncover a global conspiracy while being hunted by both his former allies and deadly mercenaries. With time running out, he must decide who he can trust.",
                    IsForAdults = true,
                    Genre = "Action",
                    Rating = 8.0m,
                    Actors = "Jeremy Strong, Viola Davis, Cillian Murphy, Elizabeth Debicki, Jeffrey Wright",
                    LocalTrailerPath = "~/Content/Videos/FracturedTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Beneath the Crimson Moon",
                    LengthInMinutes = 99,
                    ReleaseDate = new DateTime(2025, 2, 12),
                    Description = "A group of college students performing an ancient ritual as part of a dare accidentally awaken a vengeful spirit tied to the town’s bloody past. The full moon rises, and the hunt begins.",
                    IsForAdults = false,
                    Genre = "Horror",
                    Rating = 6.5m,
                    Actors = "Anya Taylor-Joy, Oscar Isaac, Florence Pugh, Mahershala Ali, Dev Patel",
                    LocalTrailerPath = "~/Content/Videos/BeneathTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "The Last Symphony",
                    LengthInMinutes = 135,
                    ReleaseDate = new DateTime(2025, 7, 20),
                    Description = "A world-renowned pianist, diagnosed with a degenerative illness, embarks on a journey to compose one final masterpiece. Along the way, he reconnects with lost love and the passion he thought he had abandoned.",
                    IsForAdults = false,
                    Genre = "Drama",
                    Rating = 8.0m,
                    Actors = "Cate Blanchett, Benedict Cumberbatch, Alicia Vikander, Adrien Brody, Jodie Comer",
                    LocalTrailerPath = "~/Content/Videos/LastTrailer.mp4",
                    Language = "Italian"
                },
                new Movie
                {
                    Title = "The Iron Pact",
                    LengthInMinutes = 145,
                    ReleaseDate = new DateTime(2025, 9, 9),
                    Description = "In the midst of WWII, an elite German officer defects with top-secret intelligence. As Allied forces race to extract him, the Nazis unleash a relentless pursuit to ensure he never leaves occupied territory alive.",
                    IsForAdults = true,
                    Genre = "Historical",
                    Rating = 7.9m,
                    Actors = "Adam Driver, Marion Cotillard, Mads Mikkelsen, Emily Blunt, Matthias Schoenaerts",
                    LocalTrailerPath = "~/Content/Videos/IronTrailer.mp4",
                    Language = "German"
                },
                new Movie
                {
                    Title = "Love in Reverse",
                    LengthInMinutes = 112,
                    ReleaseDate = new DateTime(2027, 5, 14),
                    Description = "After a tragic accident, a brilliant scientist discovers a way to relive his most cherished memories with his lost love. But the deeper he dives into the past, the more he realizes that some things are never meant to be undone.",
                    IsForAdults = false,
                    Genre = "Romance",
                    Rating = 7.0m,
                    Actors = "Timothée Chalamet, Zendaya, Andrew Garfield, Florence Pugh, Dakota Johnson",
                    LocalTrailerPath = "~/Content/Videos/LoveTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "The Marionette’s Game",
                    LengthInMinutes = 90,
                    ReleaseDate = new DateTime(2025, 10, 19),
                    Description = "A ruthless crime syndicate orchestrates deadly games where players unknowingly gamble with their lives. A detective going undercover must outmaneuver the puppet masters before he becomes their next pawn.",
                    IsForAdults = true,
                    Genre = "Thriller",
                    Rating = 6.8m,
                    Actors = "Mia Wasikowska, Barry Keoghan, Toni Collette, Willem Dafoe, Florence Pugh",
                    LocalTrailerPath = "~/Content/Videos/MarionetteTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Wild Horizons",
                    LengthInMinutes = 129,
                    ReleaseDate = new DateTime(2025, 8, 29),
                    Description = "A wandering outlaw searching for redemption stumbles into a dying frontier town ruled by a ruthless railroad tycoon. As tensions mount, he must decide whether to fight for justice or disappear into the wilderness forever.",
                    IsForAdults = false,
                    Genre = "Western",
                    Rating = 7.0m,
                    Actors = "Pedro Pascal, Zoë Kravitz, Paul Mescal, Lupita Nyong’o, Jacob Elordi",
                    LocalTrailerPath = "~/Content/Videos/WildTrailer.mp4",
                    Language = "English"
                },
                new Movie
                {
                    Title = "City of Glass",
                    LengthInMinutes = 118,
                    ReleaseDate = new DateTime(2027, 6, 11),
                    Description = "A young architect discovers that the futuristic city she helped design hides a surveillance system capable of predicting crimes before they happen. When the system identifies her as its next suspect, she is forced to uncover who is controlling it.",
                    IsForAdults = false,
                    Genre = "Thriller",
                    Rating = 7.6m,
                    Actors = "Rebecca Ferguson, John Boyega, Vanessa Kirby, Lakeith Stanfield",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Northern Lights",
                    LengthInMinutes = 106,
                    ReleaseDate = new DateTime(2027, 7, 9),
                    Description = "Two strangers meet during a winter journey across northern Scandinavia and discover that both are running from decisions that could permanently change their lives.",
                    IsForAdults = false,
                    Genre = "Drama",
                    Rating = 7.4m,
                    Actors = "Daisy Edgar-Jones, Harris Dickinson, Stellan Skarsgård, Renate Reinsve",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Zero Hour",
                    LengthInMinutes = 127,
                    ReleaseDate = new DateTime(2027, 8, 13),
                    Description = "When a coordinated cyberattack shuts down transportation and communications across Europe, a security analyst has only hours to trace the source before a second attack causes irreversible damage.",
                    IsForAdults = true,
                    Genre = "Action",
                    Rating = 8.1m,
                    Actors = "Idris Elba, Jodie Comer, Daniel Kaluuya, Léa Seydoux",
                    Language = "English"
                },
                new Movie
                {
                    Title = "After the Rain",
                    LengthInMinutes = 104,
                    ReleaseDate = new DateTime(2026, 10, 9),
                    Description = "After returning to her coastal hometown for the first time in fifteen years, a photographer uncovers letters that reveal why her family suddenly disappeared from the town’s social life.",
                    IsForAdults = false,
                    Genre = "Drama",
                    Rating = 7.7m,
                    Actors = "Saoirse Ronan, Paul Mescal, Olivia Colman, Josh O’Connor",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Orbit Seven",
                    LengthInMinutes = 136,
                    ReleaseDate = new DateTime(2026, 11, 6),
                    Description = "The crew of an experimental orbital research station receives a signal from an abandoned satellite that should have stopped transmitting more than thirty years earlier.",
                    IsForAdults = false,
                    Genre = "Science Fiction",
                    Rating = 8.3m,
                    Actors = "Jessica Chastain, Steven Yeun, Dev Patel, Rebecca Hall",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Midnight Verdict",
                    LengthInMinutes = 121,
                    ReleaseDate = new DateTime(2026, 12, 18),
                    Description = "A celebrated defense attorney receives evidence proving that her most famous acquittal may have freed the wrong person, forcing her to choose between protecting her career and exposing the truth.",
                    IsForAdults = true,
                    Genre = "Crime",
                    Rating = 7.9m,
                    Actors = "Viola Davis, Oscar Isaac, Mark Ruffalo, Rosamund Pike",
                    Language = "English"
                },
                new Movie
                {
                    Title = "The Atlas Protocol",
                    LengthInMinutes = 142,
                    ReleaseDate = new DateTime(2027, 1, 22),
                    Description = "An international intelligence team discovers a dormant Cold War protocol capable of triggering autonomous military systems around the world and must disable it before it activates.",
                    IsForAdults = true,
                    Genre = "Action",
                    Rating = 8.2m,
                    Actors = "Michael Fassbender, Ana de Armas, Mahershala Ali, Mads Mikkelsen",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Letters from Tomorrow",
                    LengthInMinutes = 110,
                    ReleaseDate = new DateTime(2027, 2, 12),
                    Description = "A university student begins receiving handwritten letters that accurately describe events one week before they happen, including a warning about someone she has never met.",
                    IsForAdults = false,
                    Genre = "Romance",
                    Rating = 7.5m,
                    Actors = "Thomasin McKenzie, Louis Partridge, Florence Pugh, Andrew Scott",
                    Language = "English"
                },
                new Movie
                {
                    Title = "Kingdom of Ash",
                    LengthInMinutes = 151,
                    ReleaseDate = new DateTime(2027, 4, 2),
                    Description = "After the assassination of a beloved king, three rival heirs form temporary alliances while an ancient enemy returns to reclaim the kingdom they are fighting to inherit.",
                    IsForAdults = true,
                    Genre = "Fantasy",
                    Rating = 8.4m,
                    Actors = "Richard Madden, Jodie Comer, Dev Patel, Anya Taylor-Joy",
                    Language = "English"
                },
                new Movie
                {
                    Title = "The Silent Frequency",
                    LengthInMinutes = 113,
                    ReleaseDate = new DateTime(2027, 9, 17),
                    Description = "A late-night radio host begins receiving mysterious transmissions from an abandoned town. The voices seem to predict events before they happen, and one message warns that she is next.",
                    IsForAdults = true,
                    Genre = "Mystery",
                    Rating = 7.6m,
                    Actors = "",
                    Language = "English"
                }
            };

            var rescheduledTitles = new[]
            {
                "Love in Reverse",
                "City of Glass",
                "Northern Lights",
                "Zero Hour"
            };

            foreach (var movie in movies)
            {
                var existingMovie = context.Movies.SingleOrDefault(
                    existing => existing.Title == movie.Title);

                if (existingMovie == null)
                {
                    movie.IsCurrentlyShowing =
                        movie.ReleaseDate.Date <= DateTime.Today;

                    context.Movies.Add(movie);
                    continue;
                }

                if (rescheduledTitles.Contains(movie.Title) &&
                    existingMovie.ReleaseDate.Date != movie.ReleaseDate.Date)
                {
                    if (context.Tickets.Any(ticket =>
                            ticket.MovieId == existingMovie.Id))
                    {
                        throw new InvalidOperationException(
                            "The release date of '" + movie.Title +
                            "' cannot be changed automatically because " +
                            "tickets exist for this movie.");
                    }

                    existingMovie.ReleaseDate = movie.ReleaseDate;
                }

                if (string.Equals(
                        TranslateLegacyGenre(existingMovie.Genre),
                        movie.Genre,
                        StringComparison.Ordinal))
                {
                    existingMovie.Genre = movie.Genre;
                }

                if (string.Equals(
                        TranslateLegacyLanguage(existingMovie.Language),
                        movie.Language,
                        StringComparison.Ordinal))
                {
                    existingMovie.Language = movie.Language;
                }

                existingMovie.IsCurrentlyShowing =
                    existingMovie.ReleaseDate.Date <= DateTime.Today;
            }
        }

        private static string TranslateLegacyGenre(string genre)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                return genre;
            }

            string translated;

            return LegacyGenres.TryGetValue(
                genre.Trim(),
                out translated)
                ? translated
                : genre;
        }

        private static string TranslateLegacyLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return language;
            }

            string translated;

            return LegacyLanguages.TryGetValue(
                language.Trim(),
                out translated)
                ? translated
                : language;
        }
    }
}