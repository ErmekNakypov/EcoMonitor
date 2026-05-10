# EcoMonitor

Information system for environmental monitoring and waste management in Bishkek, Kyrgyzstan.
Bachelor thesis project at KSTU named after I. Razzakov, Software Engineering program.

## Stack
- ASP.NET Core 9 MVC with Razor Views
- C# 13, .NET 9
- PostgreSQL 15 with Entity Framework Core 9
- ASP.NET Core Identity for authentication (Administrator, Inspector, Citizen roles)
- MediatR for CQRS pattern
- FluentValidation for input validation
- Serilog for logging (console and file)
- Leaflet via CDN for maps
- Bootstrap 5 via CDN for UI
- Localization via .resx resource files: ru-RU (default), en-US, ky-KG
- Telegram.Bot for the citizen-facing reporting bot (long polling)

## Architecture
Clean Architecture in a Modular Monolith.
- EcoMonitor.Domain: pure entities, value objects, enums. No external dependencies.
- EcoMonitor.Application: CQRS commands and queries, handlers, DTOs, validators, application interfaces.
- EcoMonitor.Infrastructure: EF Core DbContext, repositories, file storage, external API clients, identity setup.
- EcoMonitor.Web: MVC controllers, Razor views, wwwroot static assets, DI composition root.

## Conventions
- All entity IDs are Guid.
- All entities have CreatedAt and UpdatedAt timestamps.
- Commands end with "Command", queries end with "Query".
- One handler per file, named after the command or query.
- Validators live next to the command or query they validate.
- File uploads go to wwwroot/uploads/{feature}/ with random Guid filenames.
- Use IFormFile in commands, save files inside handlers.
- DbContext uses snake_case naming convention for tables and columns (use EFCore.NamingConventions package).

## Connection
PostgreSQL connection string for development:
Host=localhost;Port=5432;Database=ecomonitor;Username=ecomonitor_app;Password=devpassword123

## Telegram bot
Citizens can submit dumpsite reports through a Telegram bot in addition to the web UI.
The bot runs as a hosted background service using long polling, no webhook required.
- Bot token in `Telegram:BotToken` (user-secrets in development).
- Bot public username in `Telegram:BotUsername` (appsettings, not a secret).
- Telegram-submitted reports land in the same `dumpsite_reports` table; `Source = Telegram`,
  `ReporterId` is null, and `TelegramUserId`/`TelegramUserName` identify the submitter.
- Conversation state is persisted in `telegram_user_sessions` so the dialog survives restarts.

## Out of scope for MVP
- Email notifications (deferred to later phase)
- Real sensor integration (later phase, hardware not yet procured)
- Component and deployment diagrams (later thesis review)

## Working language
All code, comments, identifiers, commit messages, and UI text are in English.
Localization resource files contain Russian, English, and Kyrgyz translations of UI labels.
