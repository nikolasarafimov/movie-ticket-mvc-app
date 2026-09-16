# MovieTicketMVC
[![CI](https://github.com/nikolasarafimov/movie-ticket-mvc-app/actions/workflows/ci.yml/badge.svg)](https://github.com/nikolasarafimov/movie-ticket-mvc-app/actions/workflows/ci.yml)

A full-featured cinema ticket booking web application built with **ASP.NET MVC 5** and **.NET Framework 4.7.2**.

MovieTicketMVC provides a complete cinema booking experience with movie management, authentication and authorization, real-time seat locking, secure ticket purchasing, PDF ticket generation, email confirmations, and a responsive cinematic user interface.

---

## Features

### Movie Catalogue

Movies are automatically separated into **Currently Showing** and **Coming Soon** based on their release date.

Users can:

- Browse currently showing and upcoming movies
- Search movies by title
- Filter movies by age restriction
- Sort available movies
- View detailed movie information
- Watch locally hosted trailers
- View genre, runtime, rating, language, cast, description, and release date

Administrators can additionally:

- Create new movies
- Edit existing movies
- Delete movies that do not have associated ticket purchases
- Upload and replace trailer videos
- Manage movie metadata through validated forms

Trailer uploads are validated server-side and stored in `Content/Videos`.

---

## Ticket Booking

Ticket purchasing is available to authenticated users.

The booking process includes:

1. Selecting a movie
2. Selecting an available date
3. Selecting a projection time
4. Choosing seats from an interactive **10 × 10 cinema layout**
5. Selecting an age category
6. Reviewing the dynamically calculated total
7. Confirming the purchase

### Seat Pricing

| Seat Type | Rows | Price |
|---|---:|---:|
| Premium | 1–2 | **500 MKD** |
| Standard | 3–10 | **250 MKD** |

### Projection Times

The cinema provides four projection times:

- `10:00`
- `14:00`
- `18:00`
- `21:00`

The server validates the movie, date, projection time, selected seats, age category, and final ticket price instead of relying on client-submitted values.

---

## Real-Time Seat Locking

MovieTicketMVC uses **ASP.NET SignalR** to provide real-time seat locking during the booking process.

When a user selects a seat:

- The seat is temporarily locked for that connection
- Other connected users immediately see it as unavailable
- Releasing a seat updates other clients in real time
- Locks are released when the SignalR connection disconnects
- Seat locks are scoped by movie, date, and projection time

Seat identifiers and projection times are validated server-side before a lock is created.

---

## Booking Concurrency

Before completing a purchase, the application performs another server-side availability check inside a **Serializable database transaction**.

This provides an additional concurrency safeguard when multiple users attempt to purchase overlapping seats for the same movie, date, and projection time.

---

## Ticket Summary and Booking History

After a successful purchase, users receive a digital ticket summary containing:

- Movie
- Projection date
- Projection time
- Selected seats
- Number of seats
- Age category
- Total price
- Customer email

Authenticated users can access their personal ticket history.

Administrators can view ticket purchases across the system.

---

## PDF Ticket Generation

Every successful booking generates a PDF ticket using **iTextSharp**.

The generated PDF contains:

- Movie title
- Projection date
- Projection time
- Selected seats
- Total price

The application attempts to embed Unicode-compatible Arial fonts when available and falls back to standard PDF fonts when necessary.

---

## Email Confirmation

After a successful ticket purchase, MovieTicketMVC can send a confirmation email containing the generated PDF ticket as an attachment.

SMTP configuration is provided through environment variables rather than hard-coded credentials:

```text
MOVIETICKET_SMTP_HOST
MOVIETICKET_SMTP_PORT
MOVIETICKET_SMTP_USERNAME
MOVIETICKET_SMTP_PASSWORD
MOVIETICKET_SMTP_FROM
```

If no SMTP host is specified, the application defaults to Gmail SMTP.

---

## Authentication and Authorization

Authentication is implemented using **ASP.NET Identity** and **OWIN Cookie Authentication**.

The application supports:

- User registration
- Login and logout
- Password reset
- Email confirmation
- Account lockout
- Remember Me functionality
- Two-factor authentication infrastructure
- Remember Browser functionality
- External authentication infrastructure
- Role-based authorization

### User Role

Authenticated users can:

- Browse movies
- View movie details
- Purchase tickets
- Access their own ticket history
- Manage their account

### Admin Role

Administrators can additionally:

- Create movies
- Edit movies
- Delete eligible movies
- Access ticket purchases across the system

Administrative accounts are configured through environment variables:

```text
MOVIETICKET_ADMIN_EMAIL
MOVIETICKET_ADMIN_PASSWORD
```

No administrator credentials are stored directly in the source code.

---

## Trailer Management

Administrators can upload local movie trailers through the movie management interface.

The upload system includes:

- File extension validation
- File signature validation
- Maximum file size validation
- Randomized file names
- Safe file-name handling
- Automatic replacement of old trailer files

The maximum supported trailer upload size is approximately **200 MB**.

Uploaded trailers are stored in:

```text
Content/Videos
```

---

## User Interface

The application features a custom cinematic interface built on top of Bootstrap and Razor Views.

The design includes:

- Dark cinema-inspired visual theme
- Gold accent system
- Responsive movie cards
- Interactive ticket and seat layouts
- Glass-style panels
- Responsive navigation
- Form validation states
- Hover and transition effects
- Custom page animations
- Reduced-motion accessibility support
- Mobile-responsive layouts

---

## Architecture

The project follows the traditional ASP.NET MVC application structure with additional service and real-time communication layers.

```text
MovieTicketMVC/
│
├── App_Start/
│   ├── BundleConfig.cs
│   ├── FilterConfig.cs
│   ├── IdentityConfig.cs
│   ├── RouteConfig.cs
│   └── Startup.Auth.cs
│
├── Content/
│   ├── Videos/
│   └── site.css
│
├── Controllers/
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── ManageController.cs
│   ├── MoviesController.cs
│   └── TicketsController.cs
│
├── Hubs/
│   └── SeatHub.cs
│
├── Migrations/
│   └── Configuration.cs
│
├── Models/
│   ├── AccountViewModels.cs
│   ├── IdentityModels.cs
│   ├── ManageViewModels.cs
│   ├── Movie.cs
│   └── Ticket.cs
│
├── Services/
│   ├── SeatLockService.cs
│   ├── TicketEmailService.cs
│   └── TicketPdfGenerator.cs
│
├── Views/
│   ├── Account/
│   ├── Home/
│   ├── Manage/
│   ├── Movies/
│   ├── Shared/
│   └── Tickets/
│
├── Global.asax
├── Startup.cs
├── Web.config
├── packages.config
└── MovieTicketMVC.csproj
```

---

## Technologies

| Technology | Purpose |
|---|---|
| **ASP.NET MVC 5** | Web application framework |
| **.NET Framework 4.7.2** | Application runtime |
| **C#** | Backend development |
| **Entity Framework 6** | ORM and database access |
| **ASP.NET Identity** | Authentication and user management |
| **OWIN** | Authentication middleware |
| **SignalR** | Real-time seat locking |
| **Razor** | Server-side view rendering |
| **SQL Server LocalDB** | Local development database |
| **Bootstrap** | Responsive UI foundation |
| **jQuery** | Client-side functionality |
| **iTextSharp** | PDF ticket generation |
| **SMTP** | Email ticket delivery |
| **Git / GitHub** | Version control |
| **Visual Studio 2022** | Development environment |

---

## Getting Started

### Prerequisites

To run the project locally, install:

- Visual Studio 2022
- .NET Framework 4.7.2 development tools
- SQL Server LocalDB
- NuGet
- Git

### 1. Clone the Repository

```bash
git clone https://github.com/nikolasarafimov/movie-ticket-mvc-app.git
cd movie-ticket-mvc-app
```

### 2. Restore NuGet Packages

Open the solution in Visual Studio 2022 and restore the NuGet packages.

Alternatively, use Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" MovieTicketMVC.sln `
  /t:Restore `
  /p:RestorePackagesConfig=true
```

### 3. Configure the Database

The development configuration uses **SQL Server LocalDB**.

From the Visual Studio Package Manager Console, run:

```powershell
Update-Database -ProjectName MovieTicketMVC -StartUpProjectName MovieTicketMVC -Verbose
```

Entity Framework migrations will create and initialize the database.

### 4. Configure an Administrator

To seed an administrator account, configure these environment variables:

```text
MOVIETICKET_ADMIN_EMAIL
MOVIETICKET_ADMIN_PASSWORD
```

Restart Visual Studio after adding or modifying environment variables and run the database update again.

If administrator credentials are not configured, the application can still initialize without automatically creating an administrator user.

### 5. Configure Email Delivery

To enable email delivery, configure:

```text
MOVIETICKET_SMTP_HOST
MOVIETICKET_SMTP_PORT
MOVIETICKET_SMTP_USERNAME
MOVIETICKET_SMTP_PASSWORD
MOVIETICKET_SMTP_FROM
```

For Gmail, an application-specific password may be required depending on the account configuration.

Do not commit SMTP credentials or application passwords to source control.

### 6. Build the Application

Using Visual Studio MSBuild:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" MovieTicketMVC.sln `
  /t:Build `
  /p:Configuration=Release
```

### 7. Run

Open `MovieTicketMVC.sln` in Visual Studio 2022, select the web application as the startup project, and run it using IIS Express.

---

## Security

The project applies several security measures:

- Anti-forgery tokens on state-changing MVC forms
- ASP.NET Identity password validation
- Account lockout support
- Role-based authorization
- Server-side ticket validation
- Server-side seat validation
- Serializable transaction during ticket purchase
- SMTP credentials stored outside source code
- Administrator credentials stored outside source code
- Safe trailer file-name handling
- Trailer extension and file-signature validation
- Upload size restrictions
- Local redirect validation
- Ownership checks for ticket summaries

---

## Responsive Design

The interface is designed to work across:

- Desktop
- Laptop
- Tablet
- Mobile

Layouts, movie cards, forms, ticket summaries, navigation, and the cinema seat selector adapt to smaller screen sizes.

---

## Repository Notes

Generated and machine-specific files are excluded from version control, including:

```text
.vs/
bin/
obj/
packages/
*.user
*.mdf
*.ldf
```

NuGet package definitions remain tracked through `packages.config`.

---

## Future Improvements

Potential extensions include:

- Persistent or distributed seat locks for multi-instance deployments
- Dedicated normalized ticket-seat entities with database-level uniqueness
- Cloud storage for uploaded trailer files
- Payment integration
- Expanded automated test coverage
- Automated CI/CD pipeline
- Additional external authentication providers
- Enhanced cinema schedule management

---

## Author

**Nikola Sarafimov**

GitHub: `nikolasarafimov`

---

## License

This project is intended for educational and portfolio purposes.
