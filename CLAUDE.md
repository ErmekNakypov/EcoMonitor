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

## Dumpsite lifecycle (multi-stage cleanup workflow)
A dumpsite report passes through three roles and the following states:
`New → InReview → Confirmed → CleanupInProgress → AwaitingVerification → Resolved → (Appealed | Closed)`
(plus `Rejected` as a dead-end from `InReview`).

- **Citizen** submits via web or Telegram.
- **Inspector** takes the report (`InReview`), then either rejects it or
  confirms with at least one inspection photo + optional observations. After
  confirmation the report appears on the public map AND in the cleanup queue.
- **CleanupCrew** (own role) takes from the queue (no state change), starts
  cleanup with before-photos (`CleanupInProgress`), then marks done with
  after-photos + notes (`AwaitingVerification`).
- **Inspector** (any inspector, not necessarily the original assignee)
  verifies the cleanup (`Resolved`) or sends it back to the crew for rework
  (back to `CleanupInProgress`, feedback appended to `cleanup_notes`).

Status numeric values are frozen — never reorder the `DumpsiteStatus` enum.
New states append at the end.

## Appeal mechanism
After a report reaches `Resolved`, the citizen reporter has 7 days to appeal
if they disagree with the cleanup outcome. Otherwise the report auto-closes.

- Citizen Details view shows a red **Disagree with resolution** button while
  `Status == Resolved` and `UtcNow - ResolvedAt < 7d`. The modal collects a
  10–500 char reason and up to 5 optional photos saved under
  `wwwroot/uploads/appeals/` via `DumpsiteAppealPhoto` rows.
- `AppealReportCommand` flips the report to `Appealed`, sets `AppealedAt`,
  notifies the citizen (confirmation) and all inspectors.
- Inspector navbar has an **Appeals** link with a live count badge powered by
  `PendingAppealsBadgeViewComponent`. The Appeals queue view lists pending
  appeals oldest-first.
- On the Inspector Details view, when `Status == Appealed` the action panel
  offers **Uphold appeal** (`UpholdAppealCommand`) or **Dismiss appeal**
  (`DismissAppealCommand`). Both require a 10–1000 char `ResolutionNotes`.
  - Uphold transitions back to `CleanupInProgress`, clears
    `CleanupCompletedAt`, appends an `[Appeal upheld …]` line to
    `CleanupNotes`, and emails the citizen.
  - Dismiss returns to `Resolved` preserving the original `ResolvedAt` so
    the 7-day auto-close timer continues unchanged; emails the citizen.
- `AutoCloseExpiredReportsService` (`BackgroundService`) runs every hour and
  flips `Resolved` reports older than 7 days to `Closed`, setting `ClosedAt`.
- Numeric enum values are frozen: `Appealed = 7`, `Closed = 8`,
  `AppealOutcome { Upheld = 0, Dismissed = 1 }`.

## Cleanup flag mechanism
When a cleanup crew arrives on site and finds the report is invalid (no
dumpsite, wrong location, outside jurisdiction, photo mismatch), they flag
it with at least one mandatory evidence photo. Status moves to
`FlaggedByCleanupCrew = 9` and the assigned crew waits. Inspector reviews
in the Flagged queue and chooses one of three:
- **Reject** — crew was correct, citizen notified, `Status → Rejected`.
- **Confirm back** — dumpsite does exist, same crew re-checks
  (`Status → Confirmed`, `CleanupCrewId` preserved, inspector notes
  appended to `InspectorObservations`).
- **Reassign** — return to the queue for a different crew
  (`Status → Confirmed`, `CleanupCrewId = null`, `ReassignCount++`,
  notification skips the original flagger).

This closes the false-report gap not caught by auto-triage rules and adds
a second human-validation layer. Flag reasons are a frozen string set
(`NoDumpsiteFound`, `AlreadyClean`, `OutsideJurisdiction`, `PhotoMismatch`,
`Other`) defined in `FlagCleanupReasons`. The admin dashboard exposes a
"Cleanup-crew flag rate" stat card next to auto-triage and appeal rates —
a high flag rate signals the auto-triage thresholds are too permissive.

## Activity timeline
Every state-changing action on a dumpsite report is logged to
`dumpsite_report_events` for full audit transparency. Each row captures the
event type, who did it (user or system, with an actor-name snapshot frozen at
event time so the log stays readable after renames or deletions), when, and
any associated notes. The append-only log is rendered as a vertical
timeline on every Details page across roles via the
`Views/Shared/_ReportTimeline.cshtml` partial.

Visibility: staff and admin see the full timeline. Citizens see a filtered
view that hides internal handoffs (`InspectorTook`, `CleanupTaken`) — the
remaining events are still informative ("Auto-confirmed by triage system",
"Confirmed by inspector", "Cleanup completed", "Marked as resolved",
"Appealed by citizen", "Appeal upheld", etc.) without leaking which inspector
or crew member acted internally.

Event types are frozen (`DumpsiteEventType` 0..14). Logging is centralized in
`IReportEventLogger` (`ReportEventLogger` writes one row per call, scoped to
the request lifetime). All eleven dumpsite command handlers and the
`AutoCloseExpiredReportsService` background job log their outcomes after the
main action succeeds.

## Auto-triage system
New citizen reports are routed automatically before reaching humans.
Rules (`BishkekAutoTriageService`): at least one photo, coordinates within
Bishkek bounds, description ≥ 15 chars, no active duplicate within 50 m
(Haversine). Reports that pass every rule go straight to `Confirmed` and
notify CleanupCrew. Reports that fail any rule go to `InReview` with
`DumpsiteReport.AutoTriageReason` set to a human-readable explanation, and
the inspector is notified. The Inspector Details view shows an amber
callout with the reason. Mirrors the civic-tech pattern of Moscow "Nash
Gorod" and SeeClickFix: inspectors handle edge cases, not every report.

## Telegram bot
Citizens can submit dumpsite reports through a Telegram bot in addition to the web UI.
The bot runs as a hosted background service using long polling, no webhook required.
- Bot token in `Telegram:BotToken` (user-secrets in development).
- Bot public username in `Telegram:BotUsername` (appsettings, not a secret).
- Telegram-submitted reports land in the same `dumpsite_reports` table; `Source = Telegram`,
  `ReporterId` is null, and `TelegramUserId`/`TelegramUserName` identify the submitter.
- Conversation state is persisted in `telegram_user_sessions` so the dialog survives restarts.

## Email notifications
Web-submitted reports trigger transactional emails to the citizen reporter on
status changes (created, confirmed, rejected, resolved, appeal filed/upheld/
dismissed). Inspectors also receive an email when a citizen files an appeal.
- Bodies are Razor templates under `Views/EmailTemplates/` rendered to HTML.
- Outbound mail is queued in the `email_messages` table; an `EmailSenderHostedService`
  background worker drains the queue with linear-backoff retry.
- SMTP is delivered via `System.Net.Mail.SmtpClient` (suppressed `SYSLIB0014`); credentials
  in `Email:*` configuration (user-secrets/env vars in non-dev).
- Telegram-submitted reports skip email (no address). Web reports without a reporter
  email are also skipped.

## IoT devices (own sensors)
Admins can register IoT sensor devices and issue long-lived JWT tokens; devices
push readings to `POST /api/v1/sensors/readings`.
- JWT bearer is added as a *second* auth scheme (`DeviceJwt`); cookies remain
  the default for the web UI. Both run in parallel.
- Tokens are issued once at device creation (or via "regenerate"); only a
  SHA-256 hash is stored in `iot_devices.token_hash`.
- Authorization for device endpoints uses the `DeviceOnly` policy (requires
  the `DeviceJwt` scheme + `type=device` claim).
- Each ingested reading lives in `air_quality_readings` linked to an
  `air_quality_stations` row with `Source = OwnSensor`. The same map view
  shows pulled-from-providers and pushed-from-devices stations side by side.
- A Python mock script under `tools/sensor-mock/` simulates an ESP32 client
  for demos.

## Out of scope for MVP
- Component and deployment diagrams (later thesis review)

## Working language
All code, comments, identifiers, commit messages, and UI text are in English.
Localization resource files contain Russian, English, and Kyrgyz translations of UI labels.
