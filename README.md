# HCM Digitalized Street Food - Backend API

A robust .NET 9 Web API providing the core business logic and data management for the digitalized street food services in Ho Chi Minh City.

## Tech Stack

### Core Framework
- **.NET 9.0** - The latest cross-platform framework for high-performance APIs.
- **ASP.NET Core Web API** - RESTful service architecture.
- **C# 13** - Modern language features for clean and efficient code.

### Data Access & Persistence
- **Entity Framework Core 9** - Modern ORM for database interactions.
- **PostgreSQL** - Reliable open-source relational database.
- **Repository Pattern** - Abstraction layer for data access logic.

### Security & Authentication
- **JWT (JSON Web Tokens)** - Secure stateless authentication.
- **BCrypt.Net-Next** - Industry-standard password hashing.
- **Google Auth** - External identity provider integration for social login.
- **OTP (One-Time Password)** - Email-based verification for registration and password recovery.

### Communication & Services
- **MailKit / SMTP** - Integration for automated transactional emails.
- **Dependency Injection** - Built-in .NET container for loose coupling.

## Project Structure

The project follows a classic **N-Layer Architecture** to ensure separation of concerns and maintainability.

```
StreetFood/
├── StreetFood/          # API Layer (Controllers, Program.cs, Configuration)
├── Service/             # Business Logic Layer (Services, JWT, Email logic)
├── Repository/          # Repository Layer (Data access abstractions)
├── DAL/                 # Data Access Layer (DbContext, Migrations, DAOs)
└── BO/                  # Business Objects (Entities, DTOs, Enums)
    ├── Entities/        # Database Models
    └── DTOs/            # Data Transfer Objects (grouped by feature)
        ├── Auth/        # Login, Register, Google Auth
        ├── Users/       # Profile management
        ├── Otp/         # OTP verification
        └── Password/    # Reset/Forget password
```

## Key Features

### Authentication Flow
- **Multi-factor Verification**: Registration requires email OTP verification before account creation.
- **Social Integration**: Seamless Google Login support.
- **Secure Recovery**: Robust "Forgot Password" flow with time-limited OTPs.
- **Role-Based Access**: Built-in support for different user roles (Admin, User).

### API Architecture
- **N-Layer Separation**: Clear boundaries between API, Business Logic, and Data Access.
- **Centralized Error Handling**: Consistent exception management across the service layer.
- **Automated Cleanup**: Background services for cleaning up expired OTP records.
- **DTO Mapping**: Strict use of DTOs to prevent leaking database entities to the client.

### Security Best Practices
- **Password Hashing**: Passwords are never stored in plain text (BCrypt).
- **Token Security**: JWTs with configurable expiration and secure signing keys.
- **Rate Limiting Logic**: Basic protection against OTP spamming in the service layer.

## Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/)

### Installation & Setup

```bash
# Clone the repository
git clone <repository-url>

# Navigate to the solution directory
cd StreetFood

# Restore dependencies
dotnet restore

# Update database with migrations
cd DAL
dotnet ef database update --startup-project ../StreetFood

# Run the application
cd ../StreetFood
dotnet run
```

## Environment Configuration

The application uses appsettings.json for configuration. Ensure the following sections are configured:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=StreetFoodDb;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "Key": "your_super_secret_key_at_least_32_chars",
    "Issuer": "StreetFood",
    "Audience": "StreetFoodUsers"
  },
  "GoogleAuth": {
    "ClientId": "your_google_client_id"
  },
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": "587",
    "Username": "your_email",
    "Password": "your_app_password"
  }
}
```