<p align="center">
  <img src="MovieTicketMVC/Content/Images/movieticket-logo.png" alt="MovieTicket logo" width="330">
</p>

<h1 align="center">MovieTicketMVC</h1>

<p align="center">
  A cinema booking web application built with ASP.NET MVC 5, featuring real-time seat selection, booking concurrency control, role-based administration, and PDF ticket confirmations.
</p>

<p align="center">
  <a href="https://github.com/nikolasarafimov/movie-ticket-mvc-app/actions/workflows/ci.yml"><img src="https://github.com/nikolasarafimov/movie-ticket-mvc-app/actions/workflows/ci.yml/badge.svg?branch=main" alt="CI build status"></a>
  <img src="https://img.shields.io/badge/.NET%20Framework-4.7.2-512BD4" alt=".NET Framework 4.7.2">
  <img src="https://img.shields.io/badge/ASP.NET-MVC%205-6C3483" alt="ASP.NET MVC 5">
  <img src="https://img.shields.io/badge/C%23-Backend-239120" alt="C#">
</p>

<p align="center">
  <a href="#features">Features</a> ·
  <a href="#booking-flow">Booking Flow</a> ·
  <a href="#architecture">Architecture</a> ·
  <a href="#getting-started">Getting Started</a> ·
  <a href="#limitations-and-future-work">Limitations</a>
</p>

## Overview

**MovieTicketMVC** is a full-stack cinema reservation application built using **C#**, **ASP.NET MVC 5**, **Entity Framework 6**, **ASP.NET Identity**, and **SignalR**, targeting **.NET Framework 4.7.2**.

The application combines a movie catalogue with a 100-seat booking interface. It manages temporary seat holds in real time, validates bookings on the server, and coordinates concurrent reservations using SQL Server transactions and application locks. Users can review their bookings and receive PDF confirmations by email when SMTP is configured.

The project demonstrates MVC application architecture, relational data access, authentication and authorization, real-time communication, concurrency control, file uploads, and a custom responsive user interface.

> **Important:** This application records **cinema reservations**. It does **not** integrate an online payment gateway. A generated PDF confirms a booking; it is not proof of payment.

## Features

| Area | Capabilities |
| --- | --- |
| **Movie catalogue** | Now Showing and Coming Soon, movie search and sorting, age filtering, detailed movie information, and local trailers |
| **Accounts** | Registration, login and logout, account management, password recovery, and email-related identity flows |
| **Authorization** | User and administrator roles, protected movie management, restricted ticket history |
| **Booking** | Movie, screening date, showtime, age category, and individual seat selection |
| **Live availability** | SignalR updates when temporary seat holds are acquired or released |
| **Concurrency control** | Server-side checks, serializable transactions, and SQL Server application locks |
| **Tickets** | Booking summaries, personal booking history, admin booking overview, and PDF generation |
| **Notifications** | Optional SMTP confirmation email with a PDF attachment |
| **UI** | Dark cinematic theme, responsive layouts, accessibility labels, and reduced-motion support |

### Movie management

Administrators can create and edit movies, manage their metadata, and upload local trailers. Movies with associated tickets cannot be deleted through the movie-management flow. Trailer uploads are validated server-side with a **200 MiB** maximum file size.

## Booking Flow

1. **Browse movies** and open a movie's details.
2. **Log in or register** to access the booking interface.
3. **Choose a movie, date, and showtime**.
4. **Select seats** from the interactive **10 × 10** layout. SignalR communicates temporary seat holds to other connected clients.
5. **Confirm the reservation**. The server revalidates the request, the price, the selected seat holds, and existing bookings.
6. **View your ticket** in the booking summary or **My Tickets**. The application also attempts to send an email with a PDF attachment.

### Seating and pricing

| Seating zone | Rows | Price per seat |
| --- | --- | ---: |
| Premium | 1–2 | **500 MKD** |
| Standard | 3–10 | **250 MKD** |

**Screening times:** `10:00`, `14:00`, `18:00`, `21:00`.

A seat identifier such as `R3C5` means row 3, column 5. Prices are computed and validated by the server rather than trusted from browser input.

### Real-time holds and concurrency

The application uses **ASP.NET SignalR 2** and an in-memory `SeatLockService` to manage provisional seat holds. Ordinary holds last **15 minutes**; expired and released holds can be broadcast to other connected clients.

A temporary hold is not the final source of truth. At confirmation, the server starts a **serializable SQL Server transaction**, obtains a transaction-scoped exclusive lock with `sp_getapplock`, checks persisted tickets for seat conflicts, and commits the new booking only when its checks succeed.

This provides separate safeguards for the interactive selection experience and final database persistence.

## Architecture

```mermaid
flowchart TD
    UI["Browser: Razor views, Bootstrap, JavaScript"]
    MVC["ASP.NET MVC 5 controllers"]
    Identity["ASP.NET Identity / OWIN"]
    SignalR["SignalR SeatHub"]
    Holds["In-memory SeatLockService"]
    SQL["Entity Framework 6 / SQL Server"]
    PDF["iTextSharp PDF generator"]
    SMTP["SMTP email service"]

    UI --> MVC
    UI <-->|Live seat updates| SignalR
    MVC --> Identity
    SignalR --> Holds
    MVC --> Holds
    MVC --> SQL
    MVC --> PDF
    MVC --> SMTP
    PDF -->|PDF attachment| SMTP
```

### Project structure

```text
MovieTicketMVC/
├── App_Start/       MVC, Identity, OWIN, routing, and bundles
├── Controllers/     Accounts, profiles, movies, and tickets
├── Models/          Entity and view models
├── Views/           Razor pages and shared layouts
├── Hubs/            SignalR seat communication
├── Services/        Seat locks, PDF generation, and email
├── Migrations/      EF6 migrations and seed logic
├── Content/         CSS, images, and movie trailers
├── Scripts/         Client-side libraries
├── Startup.cs       Application startup
└── Web.config       Application configuration
```

### Technology stack

| Layer | Technology |
| --- | --- |
| Runtime and language | .NET Framework 4.7.2, C# |
| Web framework | ASP.NET MVC 5, Razor |
| Data access | Entity Framework 6 |
| Local database | SQL Server LocalDB |
| Authentication | ASP.NET Identity, OWIN cookies |
| Real-time communication | ASP.NET SignalR 2 |
| Front end | Bootstrap 5, jQuery, custom CSS and JavaScript |
| PDF generation | iTextSharp |
| Notifications | SMTP |
| CI | GitHub Actions on Windows |

## Getting Started

### Requirements

This is a **classic ASP.NET / .NET Framework application** intended for a Windows development environment.

- Visual Studio 2022 with ASP.NET/web development support
- .NET Framework 4.7.2 targeting pack
- SQL Server LocalDB
- NuGet package restore support
- Git

### 1. Clone and open the solution

```bash
git clone https://github.com/nikolasarafimov/movie-ticket-mvc-app.git
cd movie-ticket-mvc-app
```

Open `MovieTicketMVC.sln` in Visual Studio and restore NuGet packages. This project uses `packages.config`.

Alternatively, use a Visual Studio Developer Command Prompt:

```powershell
nuget restore MovieTicketMVC.sln
```

### 2. Configure the database

The development connection string in `MovieTicketMVC/Web.config` targets **SQL Server LocalDB**. Adjust it if your local SQL Server environment differs.

In **Visual Studio → Tools → NuGet Package Manager → Package Manager Console**, run:

```powershell
Update-Database -ProjectName MovieTicketMVC -StartUpProjectName MovieTicketMVC -Verbose
```

Entity Framework migrations create or update the database.

### 3. Optional: populate demo movies

The predefined movie catalogue is **opt-in**. To populate it, configure this environment variable before running the database update:

```text
MOVIETICKET_SEED_DEMO_MOVIES=true
```

Restart Visual Studio after changing environment variables, then rerun `Update-Database`. Existing movies are matched by title; the seed routine does not replace the entire database.

### 4. Optional: initialize an administrator

Configure both of these environment variables:

```text
MOVIETICKET_ADMIN_EMAIL
MOVIETICKET_ADMIN_PASSWORD
```

Use a valid email address and a password that meets the application's password rules. Restart Visual Studio and run the database update.

For security, the seed logic **will not automatically promote an existing non-administrator account**. Such role assignments require separate administrator action after account ownership has been verified.

**Never commit credentials or secrets.**

### 5. Optional: enable email confirmations

| Variable | Purpose |
| --- | --- |
| `MOVIETICKET_SMTP_HOST` | SMTP server; default `smtp.gmail.com` |
| `MOVIETICKET_SMTP_PORT` | SMTP port; default `587` |
| `MOVIETICKET_SMTP_USERNAME` | SMTP username |
| `MOVIETICKET_SMTP_PASSWORD` | SMTP password or app password |
| `MOVIETICKET_SMTP_FROM` | Optional sender address; defaults to username |

The service uses TLS-enabled SMTP. Keep credentials outside source control.

**Email delivery is optional.** If PDF generation or SMTP delivery fails after the booking transaction commits, the booking remains saved and available under **My Tickets**; the application displays a delivery warning.

### 6. Build and run

In Visual Studio, build the solution, or use a Visual Studio Developer Command Prompt:

```powershell
msbuild MovieTicketMVC.sln /p:Configuration=Release /m
```

Set `MovieTicketMVC` as the startup project and run with **IIS Express over HTTPS**. The application's authentication cookies are configured as secure-only.

## Security and Validation

The implementation includes role-based authorization; anti-forgery validation on relevant MVC POST actions; server-side validation of movies, screening times, dates, age categories, seats, and prices; transaction-scoped concurrency coordination; checks that restrict access to other users' ticket summaries; and validation of trailer uploads.

Authentication uses ASP.NET Identity and OWIN cookies, with account lockout functionality. Administrator and SMTP credentials are supplied using environment variables.

These implementation safeguards are not a replacement for a security assessment prior to a public production deployment.

## Continuous Integration

The [GitHub Actions CI workflow](.github/workflows/ci.yml) runs on pushes and pull requests targeting `main`. On a Windows runner, it restores NuGet packages and builds the application in Release mode.

**CI currently checks restore and compilation; it does not run automated end-to-end booking tests or deploy the application.**

## Limitations and Future Work

- **Single-instance temporary holds:** The in-memory seat-lock service needs shared storage or coordination for multi-instance hosting.
- **No online payments:** Bookings and PDF documents are reservation confirmations, not payment receipts.
- **Optional notifications:** PDF email delivery depends on the SMTP configuration.
- **Local media storage:** Uploaded trailers use the application filesystem; persistent object storage is preferable for scalable cloud hosting.
- **Seat persistence model:** A normalized ticket-seat table with a database-level uniqueness constraint would strengthen the design.
- **Testing:** Automated integration, concurrency, and end-to-end tests remain future work.

Potential extensions include distributed holds, payment integration, automated test coverage, persistent cloud storage, and a deployment pipeline.

## Author

**Nikola Sarafimov**  
GitHub: [@nikolasarafimov](https://github.com/nikolasarafimov)

## License and Usage

This repository is presented for **educational and portfolio purposes**. It currently has **no root-level `LICENSE` file**; therefore, no open-source license is asserted for the project's own code. Third-party dependencies and assets retain their respective rights and license terms.