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

        protected override void Seed(
            ApplicationDbContext context)
        {
            SeedAdmin(context);
            SeedMovies(context);

            context.SaveChanges();
        }

        private static void SeedAdmin(
            ApplicationDbContext context)
        {
            const string adminRoleName = "Admin";

            var roleManager =
                new RoleManager<IdentityRole>(
                    new RoleStore<IdentityRole>(
                        context));

            if (!roleManager.RoleExists(
                    adminRoleName))
            {
                var roleResult =
                    roleManager.Create(
                        new IdentityRole(
                            adminRoleName));

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create the Admin role: "
                        + string.Join(
                            "; ",
                            roleResult.Errors));
                }
            }

            var adminEmail =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_ADMIN_EMAIL");

            var adminPassword =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_ADMIN_PASSWORD");

            var hasAdminEmail =
                !string.IsNullOrWhiteSpace(
                    adminEmail);

            var hasAdminPassword =
                !string.IsNullOrWhiteSpace(
                    adminPassword);

            if (!hasAdminEmail &&
                !hasAdminPassword)
            {
                return;
            }

            if (!hasAdminEmail ||
                !hasAdminPassword)
            {
                throw new InvalidOperationException(
                    "Both MOVIETICKET_ADMIN_EMAIL and "
                    + "MOVIETICKET_ADMIN_PASSWORD must be configured "
                    + "when admin seeding is enabled.");
            }

            adminEmail =
                adminEmail.Trim();

            var userManager =
                new UserManager<ApplicationUser>(
                    new UserStore<ApplicationUser>(
                        context));

            var adminUser =
                userManager.FindByName(
                    adminEmail)
                ?? userManager.FindByEmail(
                    adminEmail);

            if (adminUser == null)
            {
                adminUser =
                    new ApplicationUser
                    {
                        UserName =
                            adminEmail,

                        Email =
                            adminEmail,

                        EmailConfirmed =
                            true
                    };

                var createResult =
                    userManager.Create(
                        adminUser,
                        adminPassword);

                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to create the administrator account: "
                        + string.Join(
                            "; ",
                            createResult.Errors));
                }
            }

            if (!userManager.IsInRole(
                    adminUser.Id,
                    adminRoleName))
            {
                var roleResult =
                    userManager.AddToRole(
                        adminUser.Id,
                        adminRoleName);

                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        "Failed to assign the administrator role: "
                        + string.Join(
                            "; ",
                            roleResult.Errors));
                }
            }
        }

        private static void SeedMovies(
            ApplicationDbContext context)
        {
            var movies =
                new[]
                {
                    new Movie
                    {
                        Title =
                            "Shadow of the Forgotten",

                        LengthInMinutes =
                            124,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                1,
                                12),

                        Description =
                            "A reclusive journalist stumbles upon a decades-old cold case while investigating a missing girl. As he digs deeper, he realizes the truth has been deliberately buried—along with those who sought it before him.",

                        IsForAdults =
                            true,

                        Genre =
                            "Мистерија",

                        Rating =
                            7.5m,

                        Actors =
                            "Saoirse Ronan, Rami Malek, Michael Fassbender, Lupita Nyong’o, Richard Madden",

                        LocalTrailerPath =
                            "~/Content/Videos/ShadowTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Echoes of the Void",

                        LengthInMinutes =
                            108,

                        ReleaseDate =
                            new DateTime(
                                2024,
                                12,
                                25),

                        Description =
                            "After a failed deep-space expedition, the sole survivor returns to Earth—only to find that something came back with him. As reality starts unraveling, he questions whether he ever truly escaped the void.",

                        IsForAdults =
                            false,

                        Genre =
                            "Научна фантастика",

                        Rating =
                            8.0m,

                        Actors =
                            "Riz Ahmed, Rooney Mara, Tilda Swinton, John David Washington, Jessie Buckley",

                        LocalTrailerPath =
                            "~/Content/Videos/EchoesTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "The Clockmaker’s Curse",

                        LengthInMinutes =
                            132,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                2,
                                1),

                        Description =
                            "A young apprentice discovers that his mentor’s clockwork creations can manipulate time itself. But when a mysterious figure demands the ultimate timepiece, he must race against destiny to prevent catastrophe.",

                        IsForAdults =
                            true,

                        Genre =
                            "Авантура",

                        Rating =
                            7.8m,

                        Actors =
                            "Tom Hiddleston, Eva Green, Mia Goth, Daniel Radcliffe, Helena Bonham Carter",

                        LocalTrailerPath =
                            "~/Content/Videos/ClockmasterTrailer.mp4",

                        Language =
                            "Француски"
                    },

                    new Movie
                    {
                        Title =
                            "Fractured Allegiance",

                        LengthInMinutes =
                            117,

                        ReleaseDate =
                            new DateTime(
                                2024,
                                11,
                                8),

                        Description =
                            "A rogue CIA agent, accused of treason, must uncover a global conspiracy while being hunted by both his former allies and deadly mercenaries. With time running out, he must decide who he can trust.",

                        IsForAdults =
                            true,

                        Genre =
                            "Акција",

                        Rating =
                            8.0m,

                        Actors =
                            "Jeremy Strong, Viola Davis, Cillian Murphy, Elizabeth Debicki, Jeffrey Wright",

                        LocalTrailerPath =
                            "~/Content/Videos/FracturedTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Beneath the Crimson Moon",

                        LengthInMinutes =
                            99,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                2,
                                12),

                        Description =
                            "A group of college students performing an ancient ritual as part of a dare accidentally awaken a vengeful spirit tied to the town’s bloody past. The full moon rises, and the hunt begins.",

                        IsForAdults =
                            false,

                        Genre =
                            "Хорор",

                        Rating =
                            6.5m,

                        Actors =
                            "Anya Taylor-Joy, Oscar Isaac, Florence Pugh, Mahershala Ali, Dev Patel",

                        LocalTrailerPath =
                            "~/Content/Videos/BeneathTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "The Last Symphony",

                        LengthInMinutes =
                            135,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                7,
                                20),

                        Description =
                            "A world-renowned pianist, diagnosed with a degenerative illness, embarks on a journey to compose one final masterpiece. Along the way, he reconnects with lost love and the passion he thought he had abandoned.",

                        IsForAdults =
                            false,

                        Genre =
                            "Драма",

                        Rating =
                            8.0m,

                        Actors =
                            "Cate Blanchett, Benedict Cumberbatch, Alicia Vikander, Adrien Brody, Jodie Comer",

                        LocalTrailerPath =
                            "~/Content/Videos/LastTrailer.mp4",

                        Language =
                            "Италијански"
                    },

                    new Movie
                    {
                        Title =
                            "The Iron Pact",

                        LengthInMinutes =
                            145,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                9,
                                9),

                        Description =
                            "In the midst of WWII, an elite German officer defects with top-secret intelligence. As Allied forces race to extract him, the Nazis unleash a relentless pursuit to ensure he never leaves occupied territory alive.",

                        IsForAdults =
                            true,

                        Genre =
                            "Историски",

                        Rating =
                            7.9m,

                        Actors =
                            "Adam Driver, Marion Cotillard, Mads Mikkelsen, Emily Blunt, Matthias Schoenaerts",

                        LocalTrailerPath =
                            "~/Content/Videos/IronTrailer.mp4",

                        Language =
                            "Германски"
                    },

                    new Movie
                    {
                        Title =
                            "Love in Reverse",

                        LengthInMinutes =
                            112,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                2,
                                14),

                        Description =
                            "After a tragic accident, a brilliant scientist discovers a way to relive his most cherished memories with his lost love. But the deeper he dives into the past, the more he realizes that some things are never meant to be undone.",

                        IsForAdults =
                            false,

                        Genre =
                            "Романса",

                        Rating =
                            7.0m,

                        Actors =
                            "Timothée Chalamet, Zendaya, Andrew Garfield, Florence Pugh, Dakota Johnson",

                        LocalTrailerPath =
                            "~/Content/Videos/LoveTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "The Marionette’s Game",

                        LengthInMinutes =
                            90,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                10,
                                19),

                        Description =
                            "A ruthless crime syndicate orchestrates deadly games where players unknowingly gamble with their lives. A detective going undercover must outmaneuver the puppet masters before he becomes their next pawn.",

                        IsForAdults =
                            true,

                        Genre =
                            "Трилер",

                        Rating =
                            6.8m,

                        Actors =
                            "Mia Wasikowska, Barry Keoghan, Toni Collette, Willem Dafoe, Florence Pugh",

                        LocalTrailerPath =
                            "~/Content/Videos/MarionetteTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Wild Horizons",

                        LengthInMinutes =
                            129,

                        ReleaseDate =
                            new DateTime(
                                2025,
                                8,
                                29),

                        Description =
                            "A wandering outlaw searching for redemption stumbles into a dying frontier town ruled by a ruthless railroad tycoon. As tensions mount, he must decide whether to fight for justice or disappear into the wilderness forever.",

                        IsForAdults =
                            false,

                        Genre =
                            "Вестерн",

                        Rating =
                            7.0m,

                        Actors =
                            "Pedro Pascal, Zoë Kravitz, Paul Mescal, Lupita Nyong’o, Jacob Elordi",

                        LocalTrailerPath =
                            "~/Content/Videos/WildTrailer.mp4",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "City of Glass",

                        LengthInMinutes =
                            118,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                3,
                                27),

                        Description =
                            "A young architect discovers that the futuristic city she helped design hides a surveillance system capable of predicting crimes before they happen. When the system identifies her as its next suspect, she is forced to uncover who is controlling it.",

                        IsForAdults =
                            false,

                        Genre =
                            "Трилер",

                        Rating =
                            7.6m,

                        Actors =
                            "Rebecca Ferguson, John Boyega, Vanessa Kirby, Lakeith Stanfield",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Northern Lights",

                        LengthInMinutes =
                            106,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                5,
                                15),

                        Description =
                            "Two strangers meet during a winter journey across northern Scandinavia and discover that both are running from decisions that could permanently change their lives.",

                        IsForAdults =
                            false,

                        Genre =
                            "Драма",

                        Rating =
                            7.4m,

                        Actors =
                            "Daisy Edgar-Jones, Harris Dickinson, Stellan Skarsgård, Renate Reinsve",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Zero Hour",

                        LengthInMinutes =
                            127,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                7,
                                10),

                        Description =
                            "When a coordinated cyberattack shuts down transportation and communications across Europe, a security analyst has only hours to trace the source before a second attack causes irreversible damage.",

                        IsForAdults =
                            true,

                        Genre =
                            "Акција",

                        Rating =
                            8.1m,

                        Actors =
                            "Idris Elba, Jodie Comer, Daniel Kaluuya, Léa Seydoux",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "After the Rain",

                        LengthInMinutes =
                            104,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                10,
                                9),

                        Description =
                            "After returning to her coastal hometown for the first time in fifteen years, a photographer uncovers letters that reveal why her family suddenly disappeared from the town’s social life.",

                        IsForAdults =
                            false,

                        Genre =
                            "Драма",

                        Rating =
                            7.7m,

                        Actors =
                            "Saoirse Ronan, Paul Mescal, Olivia Colman, Josh O’Connor",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Orbit Seven",

                        LengthInMinutes =
                            136,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                11,
                                6),

                        Description =
                            "The crew of an experimental orbital research station receives a signal from an abandoned satellite that should have stopped transmitting more than thirty years earlier.",

                        IsForAdults =
                            false,

                        Genre =
                            "Научна фантастика",

                        Rating =
                            8.3m,

                        Actors =
                            "Jessica Chastain, Steven Yeun, Dev Patel, Rebecca Hall",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Midnight Verdict",

                        LengthInMinutes =
                            121,

                        ReleaseDate =
                            new DateTime(
                                2026,
                                12,
                                18),

                        Description =
                            "A celebrated defense attorney receives evidence proving that her most famous acquittal may have freed the wrong person, forcing her to choose between protecting her career and exposing the truth.",

                        IsForAdults =
                            true,

                        Genre =
                            "Криминал",

                        Rating =
                            7.9m,

                        Actors =
                            "Viola Davis, Oscar Isaac, Mark Ruffalo, Rosamund Pike",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "The Atlas Protocol",

                        LengthInMinutes =
                            142,

                        ReleaseDate =
                            new DateTime(
                                2027,
                                1,
                                22),

                        Description =
                            "An international intelligence team discovers a dormant Cold War protocol capable of triggering autonomous military systems around the world and must disable it before it activates.",

                        IsForAdults =
                            true,

                        Genre =
                            "Акција",

                        Rating =
                            8.2m,

                        Actors =
                            "Michael Fassbender, Ana de Armas, Mahershala Ali, Mads Mikkelsen",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Letters from Tomorrow",

                        LengthInMinutes =
                            110,

                        ReleaseDate =
                            new DateTime(
                                2027,
                                2,
                                12),

                        Description =
                            "A university student begins receiving handwritten letters that accurately describe events one week before they happen, including a warning about someone she has never met.",

                        IsForAdults =
                            false,

                        Genre =
                            "Романса",

                        Rating =
                            7.5m,

                        Actors =
                            "Thomasin McKenzie, Louis Partridge, Florence Pugh, Andrew Scott",

                        Language =
                            "Англиски"
                    },

                    new Movie
                    {
                        Title =
                            "Kingdom of Ash",

                        LengthInMinutes =
                            151,

                        ReleaseDate =
                            new DateTime(
                                2027,
                                4,
                                2),

                        Description =
                            "After the assassination of a beloved king, three rival heirs form temporary alliances while an ancient enemy returns to reclaim the kingdom they are fighting to inherit.",

                        IsForAdults =
                            true,

                        Genre =
                            "Фантазија",

                        Rating =
                            8.4m,

                        Actors =
                            "Richard Madden, Jodie Comer, Dev Patel, Anya Taylor-Joy",

                        Language =
                            "Англиски"
                    }
                };

            var today =
                DateTime.Today;

            foreach (var movie in movies)
            {
                movie.IsCurrentlyShowing =
                    movie.ReleaseDate.Date <= today;

                var movieExists =
                    context.Movies.Any(
                        existingMovie =>
                            existingMovie.Title ==
                            movie.Title);

                if (!movieExists)
                {
                    context.Movies.Add(
                        movie);
                }
            }
        }
    }
}