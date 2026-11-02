using System;
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

            var smtpHost =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_SMTP_HOST");

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                smtpHost = "smtp.gmail.com";
            }
            else
            {
                smtpHost = smtpHost.Trim();
            }

            var smtpPortValue =
                Environment.GetEnvironmentVariable(
                    "MOVIETICKET_SMTP_PORT");

            var smtpPort = 587;

            if (!string.IsNullOrWhiteSpace(smtpPortValue))
            {
                if (!int.TryParse(
                        smtpPortValue.Trim(),
                        out smtpPort) ||
                    smtpPort < 1 ||
                    smtpPort > 65535)
                {
                    throw new InvalidOperationException(
                        "MOVIETICKET_SMTP_PORT must contain a valid port number between 1 and 65535.");
                }
            }

            var smtpUsername =
                GetRequiredEnvironmentVariable(
                    "MOVIETICKET_SMTP_USERNAME")
                .Trim();

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
            else
            {
                fromAddress = fromAddress.Trim();
            }

            var safeAttachmentName =
                Path.GetFileName(attachmentName);

            if (string.IsNullOrWhiteSpace(safeAttachmentName))
            {
                safeAttachmentName = "MovieTickets.pdf";
            }

            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(
                    fromAddress,
                    "MovieTicket App",
                    Encoding.UTF8);

                mailMessage.To.Add(
                    new MailAddress(to.Trim()));

                mailMessage.Subject =
                    subject ?? string.Empty;

                mailMessage.Body =
                    body ?? string.Empty;

                mailMessage.IsBodyHtml = false;

                mailMessage.SubjectEncoding =
                    Encoding.UTF8;

                mailMessage.BodyEncoding =
                    Encoding.UTF8;

                mailMessage.HeadersEncoding =
                    Encoding.UTF8;

                using (var attachmentStream =
                    new MemoryStream(attachmentBytes))
                using (var attachment =
                    new Attachment(
                        attachmentStream,
                        safeAttachmentName,
                        MediaTypeNames.Application.Pdf))
                {
                    attachment.NameEncoding =
                        Encoding.UTF8;

                    mailMessage.Attachments.Add(
                        attachment);

                    using (var client =
                        new SmtpClient(
                            smtpHost,
                            smtpPort))
                    {
                        client.DeliveryMethod =
                            SmtpDeliveryMethod.Network;

                        client.EnableSsl = true;

                        client.UseDefaultCredentials = false;

                        client.Credentials =
                            new NetworkCredential(
                                smtpUsername,
                                smtpPassword);

                        await client.SendMailAsync(
                            mailMessage);
                    }
                }
            }
        }

        private static string GetRequiredEnvironmentVariable(
            string name)
        {
            var value =
                Environment.GetEnvironmentVariable(name);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    name +
                    " environment variable is not configured.");
            }

            return value;
        }
    }
}