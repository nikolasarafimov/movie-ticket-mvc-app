namespace MovieTicketMVC.Migrations
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using MovieTicketMVC.Models;
    using System;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration
        : DbMigrationsConfiguration<ApplicationDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        protected override void Seed(ApplicationDbContext context)
        {
            SeedAdmin(context);
            SeedMovies(context);

            context.SaveChanges();
        }

        private static void SeedAdmin(ApplicationDbContext context)
        {
            const string adminRoleName = "Admin";

            var roleManager = new RoleManager<IdentityRole>(
                new RoleStore<IdentityRole>(context));

            if (!roleManager.RoleExists(adminRoleName))
            {
                var roleResult =
                    roleManager.Create(new IdentityRole(adminRoleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create the Admin role: "
                        + string.Join("; ", roleResult.Errors));
                }
            }

            var adminEmail =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_ADMIN_EMAIL");

            var adminPassword =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_ADMIN_PASSWORD");

            var hasAdminEmail =
                !string.IsNullOrWhiteSpace(adminEmail);

            var hasAdminPassword =
                !string.IsNullOrWhiteSpace(adminPassword);

            if (!hasAdminEmail && !hasAdminPassword)
            {
                return;
            }

            if (!hasAdminEmail || !hasAdminPassword)
            {
                throw new InvalidOperationException(
                    "Both MOVIETICKET_ADMIN_EMAIL and "
                    + "MOVIETICKET_ADMIN_PASSWORD must be configured "
                    + "when admin seeding is enabled.");
            }

            adminEmail = adminEmail.Trim();

            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(context));

            var adminUser =
                userManager.FindByName(adminEmail)
                ?? userManager.FindByEmail(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createResult =
                    userManager.Create(adminUser, adminPassword);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create the administrator account: "
                        + string.Join("; ", createResult.Errors));
                }
            }

            if (!userManager.IsInRole(adminUser.Id, adminRoleName))
            {
                var roleResult =
                    userManager.AddToRole(
                        adminUser.Id,
                        adminRoleName);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign the administrator role: "
                        + string.Join("; ", roleResult.Errors));
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
                    IsCurrentlyShowing = true,
                    Genre = "Мистерија",
                    Rating = 7.5m,
                    Actors = "Saoirse Ronan, Rami Malek, Michael Fassbender, Lupita Nyong’o, Richard Madden",
                    LocalTrailerPath = "~/Content/Videos/ShadowTrailer.mp4",
                    Language = "Англиски"
                },
                new Movie
                {
                    Title = "Echoes of the Void",
                    LengthInMinutes = 108,
                    ReleaseDate = new DateTime(2024, 12, 25),
                    Description = "After a failed deep-space expedition, the sole survivor returns to Earth—only to find that something came back with him. As reality starts unraveling, he questions whether he ever truly escaped the void.",
                    IsForAdults = false,
                    IsCurrentlyShowing = true,
                    Genre = "Научна фантастика",
                    Rating = 8m,
                    Actors = "Riz Ahmed, Rooney Mara, Tilda Swinton, John David Washington, Jessie Buckley",
                    LocalTrailerPath = "~/Content/Videos/EchoesTrailer.mp4",
                    Language = "Англиски"
                },
                new Movie
                {
                    Title = "The Clockmaker’s Curse",
                    LengthInMinutes = 132,
                    ReleaseDate = new DateTime(2025, 2, 1),
                    Description = "A young apprentice discovers that his mentor’s clockwork creations can manipulate time itself. But when a mysterious figure demands the ultimate timepiece, he must race against destiny to prevent catastrophe.",
                    IsForAdults = true,
                    IsCurrentlyShowing = true,
                    Genre = "Авантура",
                    Rating = 7.8m,
                    Actors = "Tom Hiddleston, Eva Green, Mia Goth, Daniel Radcliffe, Helena Bonham Carter",
                    LocalTrailerPath = "~/Content/Videos/ClockmasterTrailer.mp4",
                    Language = "Француски"
                },
                new Movie
                {
                    Title = "Fractured Allegiance",
                    LengthInMinutes = 117,
                    ReleaseDate = new DateTime(2024, 11, 8),
                    Description = "A rogue CIA agent, accused of treason, must uncover a global conspiracy while being hunted by both his former allies and deadly mercenaries. With time running out, he must decide who he can trust.",
                    IsForAdults = true,
                    IsCurrentlyShowing = true,
                    Genre = "Акција",
                    Rating = 8.0m,
                    Actors = "Jeremy Strong, Viola Davis, Cillian Murphy, Elizabeth Debicki, Jeffrey Wright",
                    LocalTrailerPath = "~/Content/Videos/FracturedTrailer.mp4",
                    Language = "Англиски"
                },
                new Movie
                {
                    Title = "Beneath the Crimson Moon",
                    LengthInMinutes = 99,
                    ReleaseDate = new DateTime(2025, 2, 12),
                    Description = "A group of college students performing an ancient ritual as part of a dare accidentally awaken a vengeful spirit tied to the town’s bloody past. The full moon rises, and the hunt begins.",
                    IsForAdults = false,
                    IsCurrentlyShowing = true,
                    Genre = "Хорор",
                    Rating = 6.5m,
                    Actors = "Anya Taylor-Joy, Oscar Isaac, Florence Pugh, Mahershala Ali, Dev Patel",
                    LocalTrailerPath = "~/Content/Videos/BeneathTrailer.mp4",
                    Language = "Англиски"
                },
                new Movie
                {
                    Title = "The Last Symphony",
                    LengthInMinutes = 135,
                    ReleaseDate = new DateTime(2025, 7, 20),
                    Description = "A world-renowned pianist, diagnosed with a degenerative illness, embarks on a journey to compose one final masterpiece. Along the way, he reconnects with lost love and the passion he thought he had abandoned.",
                    IsForAdults = false,
                    IsCurrentlyShowing = false,
                    Genre = "Драма",
                    Rating = 8m,
                    Actors = "Cate Blanchett, Benedict Cumberbatch, Alicia Vikander, Adrien Brody, Jodie Comer",
                    LocalTrailerPath = "~/Content/Videos/LastTrailer.mp4",
                    Language = "Италијански"
                },
                new Movie
                {
                    Title = "The Iron Pact",
                    LengthInMinutes = 145,
                    ReleaseDate = new DateTime(2025, 9, 9),
                    Description = "In the midst of WWII, an elite German officer defects with top-secret intelligence. As Allied forces race to extract him, the Nazis unleash a relentless pursuit to ensure he never leaves occupied territory alive.",
                    IsForAdults = true,
                    IsCurrentlyShowing = false,
                    Genre = "Историски",
                    Rating = 7.9m,
                    Actors = "Adam Driver, Marion Cotillard, Mads Mikkelsen, Emily Blunt, Matthias Schoenaerts",
                    LocalTrailerPath = "~/Content/Videos/IronTrailer.mp4",
                    Language = "Германски"
                },
                new Movie
                {
                    Title = "Love in Reverse",
                    LengthInMinutes = 112,
                    ReleaseDate = new DateTime(2026, 2, 14),
                    Description = "After a tragic accident, a brilliant scientist discovers a way to relive his most cherished memories with his lost love. But the deeper he dives into the past, the more he realizes that some things are never meant to be undone.",
                    IsForAdults = false,
                    IsCurrentlyShowing = false,
                    Genre = "Романса",
                    Rating = 7m,
                    Actors = "Timothée Chalamet, Zendaya, Andrew Garfield, Florence Pugh, Dakota Johnson",
                    LocalTrailerPath = "~/Content/Videos/LoveTrailer.mp4",
                    Language = "Англиски"
                },
                new Movie
                {
                    Title = "The Marionette’s Game",
                    LengthInMinutes = 90,
                    ReleaseDate = new DateTime(2025, 10, 19),
                    Description = "A ruthless crime syndicate orchestrates deadly games where players unknowingly gamble with their lives. A detective going undercover must outmaneuver the puppet masters before he becomes their next pawn.",
                    IsForAdults = true,
                    IsCurrentlyShowing = false,
                    Genre = "Трилер",
                    Rating = 6.8m,
                    Actors = "Mia Wasikowska, Barry Keoghan, Toni Collette, Willem Dafoe, Florence Pugh",
                    LocalTrailerPath = "~/Content/Videos/MarionetteTrailer.mp4",
                    Language = "Англиски"
                },
                new Movie
                {
                    Title = "Wild Horizons",
                    LengthInMinutes = 129,
                    ReleaseDate = new DateTime(2025, 8, 29),
                    Description = "A wandering outlaw searching for redemption stumbles into a dying frontier town ruled by a ruthless railroad tycoon. As tensions mount, he must decide whether to fight for justice or disappear into the wilderness forever.",
                    IsForAdults = false,
                    IsCurrentlyShowing = false,
                    Genre = "Вестерн",
                    Rating = 7m,
                    Actors = "Pedro Pascal, Zoë Kravitz, Paul Mescal, Lupita Nyong’o, Jacob Elordi",
                    LocalTrailerPath = "~/Content/Videos/WildTrailer.mp4",
                    Language = "Англиски"
                }
            };

            foreach (var movie in movies)
            {
                var movieExists =
                    context.Movies.Any(
                        existingMovie =>
                            existingMovie.Title == movie.Title);

                if (!movieExists)
                {
                    context.Movies.Add(movie);
                }
            }
        }
    }
}