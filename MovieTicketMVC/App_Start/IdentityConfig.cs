using System;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using MovieTicketMVC.Models;

namespace MovieTicketMVC
{
    public class EmailService : IIdentityMessageService
    {
        public async Task SendAsync(IdentityMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (string.IsNullOrWhiteSpace(message.Destination))
            {
                throw new ArgumentException(
                    "Email destination is required.",
                    nameof(message));
            }

            var smtpHost =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_SMTP_HOST")
                ?? "smtp.gmail.com";

            var smtpPortValue =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_SMTP_PORT");

            var smtpPort = 587;

            if (!string.IsNullOrWhiteSpace(smtpPortValue) &&
                !int.TryParse(smtpPortValue, out smtpPort))
            {
                throw new InvalidOperationException(
                    "MOVIETICKET_SMTP_PORT must contain a valid integer.");
            }

            var smtpUsername =
                GetRequiredEnvironmentVariable(
                    "MOVIETICKET_SMTP_USERNAME");

            var smtpPassword =
                GetRequiredEnvironmentVariable(
                    "MOVIETICKET_SMTP_PASSWORD");

            var fromAddress =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_SMTP_FROM");

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                fromAddress = smtpUsername;
            }

            using (var mailMessage = new MailMessage())
            {
                mailMessage.From =
                    new MailAddress(
                        fromAddress,
                        "MovieTicket App");

                mailMessage.To.Add(
                    message.Destination);

                mailMessage.Subject =
                    message.Subject ?? string.Empty;

                mailMessage.Body =
                    message.Body ?? string.Empty;

                mailMessage.IsBodyHtml =
                    true;

                using (var client =
                    new SmtpClient(
                        smtpHost,
                        smtpPort))
                {
                    client.EnableSsl =
                        true;

                    client.UseDefaultCredentials =
                        false;

                    client.Credentials =
                        new NetworkCredential(
                            smtpUsername,
                            smtpPassword);

                    await client.SendMailAsync(
                        mailMessage);
                }
            }
        }

        private static string GetRequiredEnvironmentVariable(
            string name)
        {
            var value =
                Environment.GetEnvironmentVariable(
                    name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    name
                    + " environment variable is not configured.");
            }

            return value;
        }
    }

    public class ApplicationUserManager
        : UserManager<ApplicationUser>
    {
        public ApplicationUserManager(
            IUserStore<ApplicationUser> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(
            IdentityFactoryOptions<ApplicationUserManager> options,
            IOwinContext context)
        {
            var manager =
                new ApplicationUserManager(
                    new UserStore<ApplicationUser>(
                        context.Get<ApplicationDbContext>()));

            manager.UserValidator =
                new UserValidator<ApplicationUser>(
                    manager)
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true
                };

            manager.PasswordValidator =
                new PasswordValidator
                {
                    RequiredLength = 8,
                    RequireNonLetterOrDigit = true,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true
                };

            manager.UserLockoutEnabledByDefault =
                true;

            manager.DefaultAccountLockoutTimeSpan =
                TimeSpan.FromMinutes(5);

            manager.MaxFailedAccessAttemptsBeforeLockout =
                5;

            manager.RegisterTwoFactorProvider(
                "Email Code",
                new EmailTokenProvider<ApplicationUser>
                {
                    Subject = "MovieTicket security code",
                    BodyFormat =
                        "Your MovieTicket security code is {0}."
                });

            manager.EmailService =
                new EmailService();

            var dataProtectionProvider =
                options.DataProtectionProvider;

            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<ApplicationUser>(
                        dataProtectionProvider.Create(
                            "ASP.NET Identity"));
            }

            return manager;
        }
    }

    public class ApplicationSignInManager
        : SignInManager<ApplicationUser, string>
    {
        public ApplicationSignInManager(
            ApplicationUserManager userManager,
            IAuthenticationManager authenticationManager)
            : base(
                userManager,
                authenticationManager)
        {
        }

        public override Task<ClaimsIdentity>
            CreateUserIdentityAsync(
                ApplicationUser user)
        {
            return user.GenerateUserIdentityAsync(
                (ApplicationUserManager)UserManager);
        }

        public static ApplicationSignInManager Create(
            IdentityFactoryOptions<ApplicationSignInManager> options,
            IOwinContext context)
        {
            return new ApplicationSignInManager(
                context.GetUserManager<ApplicationUserManager>(),
                context.Authentication);
        }
    }
}