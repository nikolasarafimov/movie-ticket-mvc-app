using System;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
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

            var smtpHost = Environment.GetEnvironmentVariable(
                "MOVIETICKET_SMTP_HOST");

            smtpHost = string.IsNullOrWhiteSpace(smtpHost)
                ? "smtp.gmail.com"
                : smtpHost.Trim();

            var smtpPort = GetSmtpPort();

            var smtpUsername = GetRequiredEnvironmentVariable(
                "MOVIETICKET_SMTP_USERNAME").Trim();

            var smtpPassword = GetRequiredEnvironmentVariable(
                "MOVIETICKET_SMTP_PASSWORD");

            var fromAddress = Environment.GetEnvironmentVariable(
                "MOVIETICKET_SMTP_FROM");

            fromAddress = string.IsNullOrWhiteSpace(fromAddress)
                ? smtpUsername
                : fromAddress.Trim();

            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(
                    fromAddress,
                    "MovieTicket App",
                    Encoding.UTF8);

                mailMessage.To.Add(
                    new MailAddress(message.Destination.Trim()));

                mailMessage.Subject =
                    message.Subject ?? string.Empty;

                mailMessage.Body =
                    message.Body ?? string.Empty;

                mailMessage.IsBodyHtml = true;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.HeadersEncoding = Encoding.UTF8;

                using (var client = new SmtpClient(
                    smtpHost,
                    smtpPort))
                {
                    client.DeliveryMethod =
                        SmtpDeliveryMethod.Network;

                    client.EnableSsl = true;
                    client.UseDefaultCredentials = false;

                    client.Credentials = new NetworkCredential(
                        smtpUsername,
                        smtpPassword);

                    await client.SendMailAsync(mailMessage);
                }
            }
        }

        private static int GetSmtpPort()
        {
            var value = Environment.GetEnvironmentVariable(
                "MOVIETICKET_SMTP_PORT");

            if (string.IsNullOrWhiteSpace(value))
            {
                return 587;
            }

            int port;

            if (!int.TryParse(
                    value.Trim(),
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out port) ||
                port < 1 ||
                port > 65535)
            {
                throw new InvalidOperationException(
                    "MOVIETICKET_SMTP_PORT must contain a valid port number between 1 and 65535.");
            }

            return port;
        }

        private static string GetRequiredEnvironmentVariable(
            string name)
        {
            var value = Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    name + " environment variable is not configured.");
            }

            return value;
        }
    }

    public sealed class UnicodePasswordValidator : PasswordValidator
    {
        public override bool IsUpper(char character)
        {
            return char.IsUpper(character);
        }

        public override bool IsLower(char character)
        {
            return char.IsLower(character);
        }

        public override bool IsDigit(char character)
        {
            return char.IsDigit(character);
        }

        public override bool IsLetterOrDigit(char character)
        {
            return char.IsLetterOrDigit(character);
        }
    }

    public class ApplicationUserManager : UserManager<ApplicationUser>
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
            var manager = new ApplicationUserManager(
                new UserStore<ApplicationUser>(
                    context.Get<ApplicationDbContext>()));

            manager.UserValidator = new UserValidator<ApplicationUser>(
                manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            manager.PasswordValidator = new UnicodePasswordValidator
            {
                RequiredLength = 8,
                RequireNonLetterOrDigit = true,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true
            };

            manager.UserLockoutEnabledByDefault = true;

            manager.DefaultAccountLockoutTimeSpan =
                TimeSpan.FromMinutes(5);

            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            manager.RegisterTwoFactorProvider(
                "Email Code",
                new EmailTokenProvider<ApplicationUser>
                {
                    Subject = "MovieTicket security code",
                    BodyFormat =
                        "Your MovieTicket security code is {0}."
                });

            manager.EmailService = new EmailService();

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
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(
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