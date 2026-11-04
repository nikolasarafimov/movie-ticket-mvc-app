
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
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
                throw new ArgumentException(
                    "Recipient email is required.",
                    nameof(to));
            }

            if (attachmentBytes == null ||
                attachmentBytes.Length == 0)
            {
                throw new ArgumentException(
                    "Ticket attachment is required.",
                    nameof(attachmentBytes));
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

            var safeAttachmentName = Path.GetFileName(
                attachmentName ?? string.Empty);

            if (string.IsNullOrWhiteSpace(safeAttachmentName))
            {
                safeAttachmentName = "MovieTickets.pdf";
            }

            using (var message = new MailMessage())
            {
                message.From = new MailAddress(
                    fromAddress,
                    "MovieTicket App",
                    Encoding.UTF8);

                message.To.Add(new MailAddress(to.Trim()));

                message.Subject = subject ?? string.Empty;
                message.Body = body ?? string.Empty;
                message.IsBodyHtml = false;
                message.SubjectEncoding = Encoding.UTF8;
                message.BodyEncoding = Encoding.UTF8;
                message.HeadersEncoding = Encoding.UTF8;

                using (var attachmentStream =
                    new MemoryStream(attachmentBytes, false))
                using (var attachment = new Attachment(
                    attachmentStream,
                    safeAttachmentName,
                    MediaTypeNames.Application.Pdf))
                {
                    attachment.NameEncoding = Encoding.UTF8;

                    message.Attachments.Add(attachment);

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

                        await client.SendMailAsync(message);
                    }
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
}