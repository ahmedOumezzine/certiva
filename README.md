# Certiva

Certiva is a bilingual certification-exam practice platform built with ASP.NET Core MVC. Learners can browse exams, answer timed questions, save progress, and review results. Administrators manage exams, skills, questions, and attempts through a protected dashboard.

**Live demo:** To be configured when a public deployment is available.  
**Screenshots:** Add reviewed, privacy-safe images to `docs/screenshots/` before portfolio publication.

## Features

- French and English exam catalogue, details, questions, and result views.
- Multiple question formats, including single choice, multiple choice, true/false, short and long answer, fill-in-the-blank, and matching.
- Timed exam attempts with saved answers and immutable question/choice snapshots.
- Results, corrections, attempt history, and guest exam attempts.
- Admin-only Identity sign-in and CRUD for exams, skills, questions, and attempts.
- SEO metadata, localized canonical and alternate links, `robots.txt`, and `sitemap.xml`.
- Responsive Razor UI with locally hosted Bootstrap, jQuery, and validation assets.

## Technology

- .NET 9, ASP.NET Core MVC, Razor, and ASP.NET Core Identity
- Entity Framework Core 9 with SQL Server
- JavaScript and Bootstrap
- xUnit and `WebApplicationFactory`
- Optional Playwright browser tests in `tests/Certiva.Web.Tests/Browser`

The solution is organized into four layers:

- `src/Certiva.Domain` — domain entities and rules.
- `src/Certiva.Application` — use cases, contracts, DTOs, and application rules.
- `src/Certiva.Infrastructure` — EF Core, SQL Server, Identity persistence, migrations, and database implementations.
- `src/Certiva.Web` — MVC controllers, Razor views, localization, cookies, Admin UI, and composition root.

Automated tests are split across `tests/Certiva.Domain.Tests`, `tests/Certiva.Application.Tests`, `tests/Certiva.Infrastructure.Tests`, and `tests/Certiva.Web.Tests`.

## Prerequisites

- .NET 9 SDK
- SQL Server or SQL Server LocalDB
- Node.js and npm only for the optional Playwright suite

## Local setup

Clone the repository, then restore and initialize the local EF tool:

```sh
git clone <repository-url>
cd Certiva
dotnet restore Certiva.sln
dotnet tool restore
```

The checked-in `appsettings.json` contains no database secret. For local development, use `appsettings.Local.json` (ignored by Git) or User Secrets. Apply migrations only to a new, empty Certiva database. To use User Secrets:

```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-local-sql-server-connection-string>" --project src/Certiva.Web
dotnet user-secrets set "AdminUser:Email" "<your-admin-email>" --project src/Certiva.Web
dotnet user-secrets set "AdminUser:Password" "<your-strong-admin-password>" --project src/Certiva.Web
```

In production, provide `ConnectionStrings__DefaultConnection`, `AdminUser__Email`, and `AdminUser__Password` through the hosting environment or its secret manager. Never commit these values.

Apply the standalone schema to a new, empty Certiva database, then start the app:

```sh
dotnet ef database update --project src/Certiva.Web/Certiva.Web.csproj --startup-project src/Certiva.Web/Certiva.Web.csproj
dotnet run --project src/Certiva.Web/Certiva.Web.csproj
```

The app creates the Admin role when absent. It creates or promotes an Admin user only when both Admin email and password are configured. Demo exams are off by default; they can be seeded in Development with `SeedData__ExamsDemo=true`.

## Build and test

```sh
dotnet build Certiva.sln -c Debug
dotnet build Certiva.sln -c Release
dotnet test Certiva.sln -c Release
```

Optional browser tests:

```sh
cd tests/Certiva.Web.Tests/Browser
npm ci
npx playwright install chromium
npm test
```

## Known validation gaps

- SQL Server concurrency is not yet validated in the current environment.
- Playwright browser execution is pending in the current environment.

## Deployment

Publish the web project with the production environment enabled:

```sh
dotnet publish src/Certiva.Web/Certiva.Web.csproj -c Release -o ./publish
```

On the production server, configure these values in `appsettings.Production.json` or through the hosting environment. The real production file is ignored by Git; keep the published package private:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__DefaultConnection=<production-sql-server-connection-string>
AdminUser__Email=<admin-email>
AdminUser__Password=<strong-admin-password>
```

ASP.NET Core automatically loads `appsettings.Production.json` when present, but the environment variables take precedence. `appsettings.Production.json` is ignored by Git. Demo data remains disabled in production unless `SeedData:ExamsDemo=true` and `SeedData:AllowInProduction=true` are explicitly configured.

## License status

The source code is released under the [MIT License](LICENSE). Third-party asset notices are documented in [`docs/THIRD-PARTY-NOTICES.md`](docs/THIRD-PARTY-NOTICES.md). Do not assume permission to redistribute the Certiva brand assets until their rights are confirmed.

See [`docs/EXTRACTION-STATUS.md`](docs/EXTRACTION-STATUS.md) for validation results and remaining public-release blockers.
