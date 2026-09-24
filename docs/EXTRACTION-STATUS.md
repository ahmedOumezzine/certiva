# Standalone extraction status

## Result

The standalone copy is structurally independent from the AdminPortal solution and can be restored, built, and tested as its own .NET 9 solution. It is **not ready for public GitHub publication yet** because the source repository has a database credential exposure that requires rotation/history cleanup, and the Certiva branding/source license has not been confirmed.

## Extraction

- Web app: `src/Certiva.Web`; tests: `tests/Certiva.Domain.Tests`, `tests/Certiva.Application.Tests`, `tests/Certiva.Infrastructure.Tests`, and `tests/Certiva.Web.Tests`; solution: `Certiva.sln`.
- No project reference to the parent `Core` project or the custom repository package remains.
- Only domain entities and enums used by the exam app were copied. `BaseEntity` is local and retains the original five persistence properties.
- `ApplicationDbContext` and a new `InitialCertivaSchema` migration now belong to the standalone app. `docs/database/InitialCertivaSchema.sql` is an idempotent SQL Server script.
- Identity tables are in the initial migration. Startup creates the Admin role; an Admin user is created or promoted only when both `AdminUser:Email` and `AdminUser:Password` are configured. Demo exam seed is disabled unless `SeedData:ExamsDemo=true` in Development.
- Machine-specific publish profiles, user-specific project files, and environment appsettings overrides were excluded. `.gitignore` excludes these patterns and build/test artifacts.
- No repository-wide license was invented; see `LICENSE-REVIEW.md` and `THIRD-PARTY-NOTICES.md`.

## Validation

- NuGet restore completed from the available local package cache. The environment could not contact nuget.org for vulnerability metadata, so advisory freshness was not verified.
- Debug build: passed. Release build: passed.
- Release tests: 63 passed, 0 failed.
- EF tooling discovered the initial migration and generated the SQL script.
- Fresh-database apply test: not completed. Creating a uniquely named LocalDB instance failed inside the Windows LocalDB API. No existing database was opened or changed.
- Playwright browser tests were not run.

## Publication blockers

1. Rotate the SQL Server credential exposed by a tracked source configuration and an older repository version. The extracted copy contains no production connection string or password. Remove the sensitive value from any history that will be published after rotation; do not rewrite the parent repository history as part of this extraction.
2. Confirm ownership and redistribution rights for the Certiva name and logos, and select the source code license with its owner before adding a `LICENSE` file.
3. Validate the generated schema against a newly created, empty SQL Server database when a working SQL Server/LocalDB environment is available. Never apply this initial migration blindly to a pre-existing or shared database.
4. Run the optional Playwright suite in CI or another environment with network access to npm and the Chromium browser package.
