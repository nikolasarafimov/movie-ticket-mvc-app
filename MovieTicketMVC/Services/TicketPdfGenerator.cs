using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MovieTicketMVC.Models;

namespace MovieTicketMVC.Services
{
    public class TicketPdfGenerator
    {
        private static readonly BaseColor PrimaryColor =
            new BaseColor(23, 30, 43);

        private static readonly BaseColor AccentColor =
            new BaseColor(180, 133, 53);

        private static readonly BaseColor MutedColor =
            new BaseColor(103, 112, 125);

        private static readonly BaseColor LightBackground =
            new BaseColor(245, 247, 250);

        private static readonly CultureInfo EnglishCulture =
            CultureInfo.GetCultureInfo("en-US");

        public byte[] GeneratePdf(Ticket ticket)
        {
            ValidateTicket(ticket);

            var formattedSeats = FormatSeats(
                ticket.SelectedSeats);

            var regularBaseFont = LoadBaseFont(false);
            var boldBaseFont = LoadBaseFont(true);

            var brandFont = new Font(
                boldBaseFont,
                23f,
                Font.NORMAL,
                AccentColor);

            var headingFont = new Font(
                boldBaseFont,
                16f,
                Font.NORMAL,
                PrimaryColor);

            var subtitleFont = new Font(
                regularBaseFont,
                10f,
                Font.NORMAL,
                MutedColor);

            var labelFont = new Font(
                boldBaseFont,
                10f,
                Font.NORMAL,
                PrimaryColor);

            var valueFont = new Font(
                regularBaseFont,
                10f,
                Font.NORMAL,
                PrimaryColor);

            var noteFont = new Font(
                regularBaseFont,
                9f,
                Font.NORMAL,
                MutedColor);

            using (var memoryStream = new MemoryStream())
            {
                var document = new Document(
                    PageSize.A4,
                    45f,
                    45f,
                    50f,
                    50f);

                var writer = PdfWriter.GetInstance(
                    document,
                    memoryStream);

                writer.CloseStream = false;

                try
                {
                    document.AddTitle(
                        "MovieTicket - Booking #" +
                        ticket.Id.ToString(
                            CultureInfo.InvariantCulture));

                    document.AddSubject(
                        "Cinema booking confirmation");

                    document.AddAuthor("MovieTicket");

                    document.AddCreator("MovieTicketMVC");

                    document.Open();

                    var brand = new Paragraph(
                        "MOVIETICKET",
                        brandFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 7f
                    };

                    document.Add(brand);

                    var subtitle = new Paragraph(
                        "BOOKING CONFIRMATION",
                        subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 25f
                    };

                    document.Add(subtitle);

                    var movieTitle = new Paragraph(
                        ticket.Movie.Title.Trim(),
                        headingFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 12f
                    };

                    document.Add(movieTitle);

                    var separator = new PdfPTable(1)
                    {
                        WidthPercentage = 100f,
                        SpacingAfter = 20f
                    };

                    var separatorCell = new PdfPCell(
                        new Phrase(string.Empty))
                    {
                        Border = Rectangle.NO_BORDER,
                        BackgroundColor = AccentColor,
                        FixedHeight = 2f
                    };

                    separator.AddCell(separatorCell);

                    document.Add(separator);

                    var detailsTable = new PdfPTable(2)
                    {
                        WidthPercentage = 100f,
                        SpacingBefore = 5f,
                        SpacingAfter = 25f
                    };

                    detailsTable.SetWidths(
                        new[] { 1.5f, 3.5f });

                    AddRow(
                        detailsTable,
                        "Booking reference",
                        "#" + ticket.Id.ToString(
                            CultureInfo.InvariantCulture),
                        labelFont,
                        valueFont,
                        LightBackground);

                    AddRow(
                        detailsTable,
                        "Movie",
                        ticket.Movie.Title.Trim(),
                        labelFont,
                        valueFont,
                        BaseColor.WHITE);

                    AddRow(
                        detailsTable,
                        "Screening date",
                        ticket.SelectedDay.ToString(
                            "dd MMM yyyy",
                            EnglishCulture),
                        labelFont,
                        valueFont,
                        LightBackground);

                    AddRow(
                        detailsTable,
                        "Screening time",
                        ticket.SelectedTime.Trim(),
                        labelFont,
                        valueFont,
                        BaseColor.WHITE);

                    AddRow(
                        detailsTable,
                        "Seats",
                        formattedSeats,
                        labelFont,
                        valueFont,
                        LightBackground);

                    AddRow(
                        detailsTable,
                        "Number of seats",
                        ticket.NumberOfSeats.ToString(
                            CultureInfo.InvariantCulture),
                        labelFont,
                        valueFont,
                        BaseColor.WHITE);

                    AddRow(
                        detailsTable,
                        "Total price",
                        string.Format(
                            EnglishCulture,
                            "{0:N0} MKD",
                            ticket.TotalPrice),
                        labelFont,
                        valueFont,
                        LightBackground);

                    if (!string.IsNullOrWhiteSpace(ticket.Email))
                    {
                        AddRow(
                            detailsTable,
                            "Email address",
                            ticket.Email.Trim(),
                            labelFont,
                            valueFont,
                            BaseColor.WHITE);
                    }

                    document.Add(detailsTable);

                    var confirmationTitle = new Paragraph(
                        "Your booking has been confirmed",
                        labelFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        SpacingAfter = 12f
                    };

                    document.Add(confirmationTitle);

                    var confirmationText = new Paragraph(
                        "Keep this document as confirmation " +
                        "of your cinema booking.",
                        noteFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        Leading = 15f,
                        SpacingAfter = 15f
                    };

                    document.Add(confirmationText);

                    var paymentNotice = new Paragraph(
                        "This document confirms your booking " +
                        "but does not constitute proof of payment.",
                        noteFont)
                    {
                        Alignment = Element.ALIGN_CENTER,
                        Leading = 15f,
                        SpacingAfter = 20f
                    };

                    document.Add(paymentNotice);

                    var footer = new Paragraph(
                        "MovieTicket - Cinema Booking Experience",
                        subtitleFont)
                    {
                        Alignment = Element.ALIGN_CENTER
                    };

                    document.Add(footer);

                    document.Close();

                    return memoryStream.ToArray();
                }
                finally
                {
                    if (document.IsOpen())
                    {
                        document.Close();
                    }
                }
            }
        }

        private static void ValidateTicket(Ticket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(
                    nameof(ticket));
            }

            if (ticket.Movie == null ||
                string.IsNullOrWhiteSpace(ticket.Movie.Title))
            {
                throw new InvalidOperationException(
                    "Ticket movie information is required to generate the PDF.");
            }

            if (ticket.SelectedDay == default(DateTime))
            {
                throw new InvalidOperationException(
                    "Ticket screening date is required to generate the PDF.");
            }

            if (string.IsNullOrWhiteSpace(ticket.SelectedTime))
            {
                throw new InvalidOperationException(
                    "Ticket screening time is required to generate the PDF.");
            }

            if (string.IsNullOrWhiteSpace(ticket.SelectedSeats))
            {
                throw new InvalidOperationException(
                    "Ticket seat information is required to generate the PDF.");
            }

            if (ticket.NumberOfSeats <= 0)
            {
                throw new InvalidOperationException(
                    "Ticket seat count must be greater than zero.");
            }

            if (ticket.TotalPrice < 0)
            {
                throw new InvalidOperationException(
                    "Ticket total price cannot be negative.");
            }
        }

        private static BaseFont LoadBaseFont(bool bold)
        {
            var fileName = bold
                ? "DejaVuSans-Bold.ttf"
                : "DejaVuSans.ttf";

            var applicationFontPath =
                HostingEnvironment.MapPath(
                    "~/App_Data/Fonts/" + fileName);

            if (!string.IsNullOrWhiteSpace(applicationFontPath) &&
                File.Exists(applicationFontPath))
            {
                return BaseFont.CreateFont(
                    applicationFontPath,
                    BaseFont.IDENTITY_H,
                    BaseFont.EMBEDDED);
            }

            var windowsFontsDirectory =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Fonts);

            if (!string.IsNullOrWhiteSpace(windowsFontsDirectory))
            {
                var windowsFontPath = Path.Combine(
                    windowsFontsDirectory,
                    bold ? "arialbd.ttf" : "arial.ttf");

                if (File.Exists(windowsFontPath))
                {
                    return BaseFont.CreateFont(
                        windowsFontPath,
                        BaseFont.IDENTITY_H,
                        BaseFont.EMBEDDED);
                }
            }

            throw new FileNotFoundException(
                "A Unicode TrueType font is required to generate " +
                "PDF tickets. Add DejaVuSans.ttf and " +
                "DejaVuSans-Bold.ttf to App_Data/Fonts.",
                fileName);
        }

        private static string FormatSeats(string selectedSeats)
        {
            if (string.IsNullOrWhiteSpace(selectedSeats))
            {
                return string.Empty;
            }

            return string.Join(
                ", ",
                selectedSeats
                    .Split(
                        new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(seat => seat.Trim())
                    .Where(seat =>
                        !string.IsNullOrWhiteSpace(seat))
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase));
        }

        private static void AddRow(
            PdfPTable table,
            string label,
            string value,
            Font labelFont,
            Font valueFont,
            BaseColor backgroundColor)
        {
            var labelCell = new PdfPCell(
                new Phrase(
                    label ?? string.Empty,
                    labelFont))
            {
                BackgroundColor = backgroundColor,
                Border = Rectangle.NO_BORDER,
                Padding = 11f,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };

            var valueCell = new PdfPCell(
                new Phrase(
                    value ?? string.Empty,
                    valueFont))
            {
                BackgroundColor = backgroundColor,
                Border = Rectangle.NO_BORDER,
                Padding = 11f,
                VerticalAlignment = Element.ALIGN_MIDDLE
            };

            table.AddCell(labelCell);
            table.AddCell(valueCell);
        }
    }
}