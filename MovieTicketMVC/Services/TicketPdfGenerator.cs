using System;
using System.Globalization;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using MovieTicketMVC.Models;

namespace MovieTicketMVC.Services
{
    public class TicketPdfGenerator
    {
        public byte[] GeneratePdf(Ticket ticket)
        {
            if (ticket == null)
            {
                throw new ArgumentNullException(
                    nameof(ticket));
            }

            if (ticket.Movie == null)
            {
                throw new InvalidOperationException(
                    "Ticket movie information is required to generate the PDF.");
            }

            if (string.IsNullOrWhiteSpace(
                    ticket.SelectedTime))
            {
                throw new InvalidOperationException(
                    "Ticket projection time is required to generate the PDF.");
            }

            if (string.IsNullOrWhiteSpace(
                    ticket.SelectedSeats))
            {
                throw new InvalidOperationException(
                    "Ticket seat information is required to generate the PDF.");
            }

            using (var memoryStream =
                new MemoryStream())
            {
                var document =
                    new Document(
                        PageSize.A4,
                        50,
                        50,
                        50,
                        50);

                try
                {
                    var writer =
                        PdfWriter.GetInstance(
                            document,
                            memoryStream);

                    writer.CloseStream =
                        false;

                    document.AddTitle(
                        "MovieTicket - "
                        + (ticket.Movie.Title
                           ?? "Movie Ticket"));

                    document.AddSubject(
                        "Movie ticket reservation");

                    document.AddCreator(
                        "MovieTicketMVC");

                    document.Open();

                    var titleFont =
                        CreateFont(
                            20,
                            true);

                    var labelFont =
                        CreateFont(
                            12,
                            true);

                    var valueFont =
                        CreateFont(
                            12,
                            false);

                    var title =
                        new Paragraph(
                            ticket.Movie.Title
                            ?? "Movie Ticket",
                            titleFont)
                        {
                            Alignment =
                                Element.ALIGN_CENTER,

                            SpacingAfter =
                                20f
                        };

                    document.Add(
                        title);

                    var table =
                        new PdfPTable(2)
                        {
                            WidthPercentage =
                                60f,

                            HorizontalAlignment =
                                Element.ALIGN_CENTER,

                            SpacingBefore =
                                10f
                        };

                    table.SetWidths(
                        new[]
                        {
                            1.2f,
                            2.8f
                        });

                    table.DefaultCell.BorderWidth =
                        1;

                    table.DefaultCell.Padding =
                        8;

                    AddRow(
                        table,
                        "Date:",
                        ticket.SelectedDay.ToString(
                            "dd.MM.yyyy",
                            CultureInfo.InvariantCulture),
                        labelFont,
                        valueFont,
                        BaseColor.LIGHT_GRAY);

                    AddRow(
                        table,
                        "Time:",
                        ticket.SelectedTime,
                        labelFont,
                        valueFont,
                        BaseColor.WHITE);

                    AddRow(
                        table,
                        "Seats:",
                        FormatSeats(
                            ticket.SelectedSeats),
                        labelFont,
                        valueFont,
                        BaseColor.LIGHT_GRAY);

                    AddRow(
                        table,
                        "Price:",
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} MKD",
                            ticket.TotalPrice),
                        labelFont,
                        valueFont,
                        BaseColor.WHITE);

                    document.Add(
                        table);

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

        private static Font CreateFont(
            float size,
            bool bold)
        {
            var fontsDirectory =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.Fonts);

            var fontFileName =
                bold
                    ? "arialbd.ttf"
                    : "arial.ttf";

            var fontPath =
                Path.Combine(
                    fontsDirectory,
                    fontFileName);

            if (File.Exists(fontPath))
            {
                try
                {
                    var baseFont =
                        BaseFont.CreateFont(
                            fontPath,
                            BaseFont.IDENTITY_H,
                            BaseFont.EMBEDDED);

                    return new Font(
                        baseFont,
                        size,
                        Font.NORMAL);
                }
                catch (DocumentException)
                {
                }
                catch (IOException)
                {
                }
            }

            return FontFactory.GetFont(
                bold
                    ? FontFactory.HELVETICA_BOLD
                    : FontFactory.HELVETICA,
                size);
        }

        private static string FormatSeats(
            string selectedSeats)
        {
            return string.Join(
                ", ",
                selectedSeats
                    .Split(
                        new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(seat =>
                        seat.Trim())
                    .Where(seat =>
                        !string.IsNullOrWhiteSpace(
                            seat)));
        }

        private static void AddRow(
            PdfPTable table,
            string label,
            string value,
            Font labelFont,
            Font valueFont,
            BaseColor background)
        {
            var labelCell =
                new PdfPCell(
                    new Phrase(
                        label,
                        labelFont))
                {
                    BackgroundColor =
                        background,

                    BorderColor =
                        BaseColor.DARK_GRAY,

                    Padding =
                        6,

                    HorizontalAlignment =
                        Element.ALIGN_LEFT
                };

            table.AddCell(
                labelCell);

            var valueCell =
                new PdfPCell(
                    new Phrase(
                        value ?? string.Empty,
                        valueFont))
                {
                    BackgroundColor =
                        background,

                    BorderColor =
                        BaseColor.DARK_GRAY,

                    Padding =
                        6,

                    HorizontalAlignment =
                        Element.ALIGN_LEFT
                };

            table.AddCell(
                valueCell);
        }
    }
}