using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace MovieTicketMVC.Services
{
    public static class TicketEmailService
    {
        public static async Task SendTicketConfirmationAsync(
            string to,
            string subject,
            string body,
            byte[] attachmentBytes,
            string attachmentName)
        {
            if (string.IsNullOrWhiteSpace(to))
            {
                throw new ArgumentException("Recipient email is required.", nameof(to));
            }

            if (attachmentBytes == null || attachmentBytes.Length == 0)
            {
                throw new ArgumentException("Ticket attachment is required.", nameof(attachmentBytes));
            }

            var smtpHost =
                Environment.GetEnvironmentVariable("MOVIETICKET_SMTP_HOST")
                ?? "smtp.gmail.com";

            var smtpPortValue =
                Environment.GetEnvironmentVariable("MOVIETICKET_SMTP_PORT");

            var smtpPort = 587;

            if (!string.IsNullOrWhiteSpace(smtpPortValue)
                && !int.TryParse(smtpPortValue, out smtpPort))
            {
                throw new InvalidOperationException(
                    "MOVIETICKET_SMTP_PORT must contain a valid integer.");
            }

            var smtpUsername =
                GetRequiredEnvironmentVariable("MOVIETICKET_SMTP_USERNAME");

            var smtpPassword =
                GetRequiredEnvironmentVariable("MOVIETICKET_SMTP_PASSWORD");

            var fromAddress =
                Environment.GetEnvironmentVariable("MOVIETICKET_SMTP_FROM");

            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                fromAddress = smtpUsername;
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(fromAddress, "MovieTicket App");
                message.To.Add(to);
                message.Subject = subject ?? string.Empty;
                message.Body = body ?? string.Empty;
                message.IsBodyHtml = false;

                using (var attachmentStream = new MemoryStream(attachmentBytes))
                using (var attachment =
                    new Attachment(attachmentStream, attachmentName ?? "ticket.pdf"))
                {
                    message.Attachments.Add(attachment);

                    using (var client = new SmtpClient(smtpHost, smtpPort))
                    {
                        client.EnableSsl = true;
                        client.UseDefaultCredentials = false;
                        client.Credentials =
                            new NetworkCredential(smtpUsername, smtpPassword);

                        await client.SendMailAsync(message);
                    }
                }
            }
        }

        private static string GetRequiredEnvironmentVariable(string name)
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
}