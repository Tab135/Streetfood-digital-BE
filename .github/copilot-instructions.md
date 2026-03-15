# Copilot Instructions — StreetFood Digital Backend

## Project Overview

ASP.NET Core 9.0 Web API for a Vietnamese street food discovery platform. Features vendor/branch management, social authentication, PayOS subscription payments, feedback/ratings, and Vietnamese text search.

## Architecture

**5-layer N-Layer architecture** with strict one-direction dependency flow:

```
StreetFood (API)
  └── Service (Business Logic)
        └── Repository (Abstraction)
              └── DAL (Data Access / EF Core)
                    └── BO (Entities, DTOs, Enums — shared core)
```

- No CQRS, no domain events. Plain N-layer with interface-based DI throughout.
- Every service has an `I{Name}Service` interface in `Service/Interfaces/`.
- Every repository has an `I{Name}Repository` interface in `Repository/Interfaces/`.
- DAOs in `DAL/` contain direct EF Core queries. Repositories wrap DAOs and exist for DI abstraction.

## Tech Stack

- **.NET 9.0 / C# 13**, nullable reference types enabled project-wide
- **PostgreSQL** via `Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4`
- **EF Core 9.0.8** — code-first migrations in `DAL/Migrations/`
- **JWT Bearer** authentication (`Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8`)
- **BCrypt.Net-Next** for password hashing
- **PayOS** for subscription payments
- **Brevo (brevo_csharp)** + **MailKit** for transactional email
- **Google.Apis.Auth** + custom Facebook HTTP calls for OAuth
- **Scalar** (`/scalar/v1`) as the OpenAPI UI (replaces Swagger UI)
- `System.Linq.Dynamic.Core` for dynamic LINQ in repository queries

## Key Conventions

### Response Envelope

All API responses are wrapped by `ResponseMiddleware` into:

```json
{ "status": 200, "message": "...", "data": { ... }, "errorCode": null }
```

Controllers return `Ok(new { message = "...", data = ... })` — the middleware handles re-wrapping. Do not manually construct `ApiResponse<T>` in new controllers unless consistent with surrounding code.

### DTOs

- `CreateXxxDto` — for POST request bodies
- `UpdateXxxDto` — for PUT request bodies
- `XxxResponseDto` — for response payloads
- Separate public vs. authenticated views where access differs (e.g., `BranchPublicDto` vs. `BranchResponseDto`)

### Controller Routes

All controllers use `[Route("api/[controller]")]`. Follow existing route naming when adding endpoints.

### Ownership Checks

Always verify the authenticated user owns the resource before mutations. Extract `userId` from claims:

```csharp
var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
```

Pattern used in `BranchService`, `FeedbackService`, `VendorService`, etc.

### Pagination

Use `PaginatedResponse<T>` from `BO/Common/` for all paginated list endpoints. Includes `CurrentPage`, `PageSize`, `TotalPages`, `TotalCount`, `HasPrevious`, `HasNext`, `Items`.

### Roles

```csharp
// Role enum values in BO/Enums/
User = 0, Admin = 1, Moderator = 2, Vendor = 3, Manager = 4
```

Apply role-based authorization with `[Authorize(Roles = "...")]` directly on controller methods.

### Async/Await

Always use `await` for async calls. **Never use `.Result` or `.Wait()`** — blocks threadpool and risks deadlocks (there is an existing `.Result` anti-pattern in `VendorService.MapToResponseDto()` — do not replicate it).

### LicenseUrl Field

`BranchRegisterRequest.LicenseUrl` stores a JSON-serialized `List<string>`. Always serialize with `JsonSerializer.Serialize(list)` and deserialize with `JsonSerializer.Deserialize<List<string>>(value)`.

### Vietnamese Text Search

Use `TextNormalizer.NormalizeForSearch()` to strip diacritics for accent-insensitive search. Minimum keyword length is 2 characters.

### Static File Uploads

Upload files to `uploads/branches/` or `uploads/licenses/` under the web root. Public URL base: `http://159.223.47.89:5298/uploads/...` (hardcoded — be aware when modifying upload logic).

## Domain Model Quick Reference

| Entity                  | Key Notes                                                                                            |
| ----------------------- | ---------------------------------------------------------------------------------------------------- |
| `User`                  | Has `Role` enum, `Point`, `EmailVerified`, dietary/info setup flags                                  |
| `Vendor`                | Belongs to `User` (owner); has `Branches`                                                            |
| `Branch`                | Has subscription (`IsSubscribed`, `SubscriptionExpiresAt`), verification (`IsVerified`), `AvgRating` |
| `BranchRegisterRequest` | `LicenseUrl` is JSON string, `Status` is `RegisterVendorStatusEnum`                                  |
| `Dish`                  | Belongs to `Branch` and `Category`; has `DishTaste` and `DishDietaryPreference` join tables          |
| `Feedback`              | Optional `DishId` (can be for a branch generally); has images, tags, auto-updates `Branch.AvgRating` |
| `Payment`               | PayOS integration; `OrderCode` is `long`, `Status` is string enum ("PENDING","PAID","CANCELLED")     |
| `OtpVerify`             | Phone number stored in `Email` field; 3-minute expiry, 2 req/min rate limit                          |

## Authentication Flows

1. **Google OAuth** — supports both `IdToken` (mobile `signInWithGoogle`) and `AccessToken` (web `useGoogleLogin`)
2. **Facebook OAuth** — validates token via `FacebookService`, creates/finds user
3. **Phone OTP** — sends 6-digit OTP via SMS, stored in `OtpVerify` table

Email/password flow is implemented in `UserService` but **commented out** in `AuthController` — do not re-enable without discussion.

JWT tokens are valid for **7 days**, signed with HMAC-SHA256.

## Background Services

- `OtpCleanupService` — deletes expired OTP records every **1 hour**
- `SubscriptionExpiryService` — sets `IsSubscribed = false` on expired branches every **6 hours**

When adding new scheduled work, register a new `BackgroundService` subclass using the same timer-loop pattern in `Service/BackgroundServices/`.

## Adding New Features — Checklist

1. **BO**: Add entity (in `BO/Entities/`) or DTOs (in `BO/DTOs/`)
2. **DAL**: Add `DbSet<T>` to `StreetFoodDbContext`, configure in `OnModelCreating`, create migration
3. **DAL**: Add DAO class inheriting from nothing (plain class injecting `StreetFoodDbContext`)
4. **Repository**: Add interface (`IXxxRepository`) and concrete implementation wrapping the DAO
5. **Service**: Add interface (`IXxxService`) and implementation containing business logic
6. **StreetFood (API)**: Add controller, register DI in `Program.cs`

## DI Registration

All services and repositories are registered in `StreetFood/Program.cs`. Follow the existing pattern:

```csharp
builder.Services.AddScoped<IXxxRepository, XxxRepository>();
builder.Services.AddScoped<IXxxService, XxxService>();
```

## Known Issues / Anti-Patterns to Avoid

- `VendorService.MapToResponseDto()` — uses `.Result` (blocking). Do not replicate.
- `PaymentController` — has wrong namespace (`Ielts_System.Controllers.Payments`). Fix when touching that file.
- Secrets committed directly to `appsettings.json` — do not add new secrets there; use environment variable overrides or user secrets.
- CORS is restricted to `http://localhost:5173` only — update if adding new client origins.
- TODO comments with profanity exist in `BranchService` — clean up when modifying nearby code.

## File Layout Reference

```
StreetFood-digital-BE/
├── .github/
│   └── copilot-instructions.md
├── BO/
│   ├── Entities/          # 23 domain entity classes
│   ├── DTOs/              # Create/Update/Response DTOs per feature
│   ├── Enums/             # Role, RegisterVendorStatusEnum, etc.
│   ├── Common/            # PaginatedResponse<T>, ApiResponse<T>
│   └── Exceptions/        # Custom exception classes
├── DAL/
│   ├── StreetFoodDbContext.cs
│   ├── Migrations/
│   └── *.cs               # DAO classes
├── Repository/
│   ├── Interfaces/        # IXxxRepository
│   └── *.cs               # Concrete repositories
├── Service/
│   ├── Interfaces/        # IXxxService
│   ├── BackgroundServices/
│   └── *.cs               # Service implementations
└── StreetFood/
    ├── Controllers/
    ├── Middleware/
    │   └── ResponseMiddleware.cs
    ├── Program.cs
    └── appsettings.json
```
