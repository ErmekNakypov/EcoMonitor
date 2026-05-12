-- Backfill audit timeline events for reports that existed before Stage 3.
-- Synthesizes a minimum-viable timeline from current row state:
--   * ReportSubmitted for every report at created_at
--   * For new (post-triage) rows, AutoTriaged or SentToReview at created_at+1s
-- Later transitions (Confirmed/CleanupStarted/MarkedResolved/etc.) are skipped
-- so this stays cheap and idempotent — future actions log themselves correctly
-- via IReportEventLogger.
--
-- Run once. Re-running is safe IF dumpsite_report_events is empty for the
-- backfilled reports; this script does NOT dedup. Comment-in the truncate at
-- the top of BEGIN if you need to re-run.

BEGIN;

-- TRUNCATE TABLE dumpsite_report_events;  -- uncomment to wipe and re-backfill

-- 1. ReportSubmitted for every existing report.
INSERT INTO dumpsite_report_events
    (id, report_id, event_type, occurred_at, actor_user_id, actor_role, actor_display_name, notes, payload_json, created_at, updated_at)
SELECT
    gen_random_uuid(),
    r.id,
    0,                      -- DumpsiteEventType.ReportSubmitted
    r.created_at,
    r.reporter_id,
    'Citizen',
    COALESCE(
        u.full_name,
        '@' || NULLIF(r.telegram_user_name, ''),
        'Anonymous'
    ),
    NULL,
    NULL,
    r.created_at,
    r.created_at
FROM dumpsite_reports r
LEFT JOIN users u ON u.id = r.reporter_id
WHERE NOT EXISTS (
    SELECT 1 FROM dumpsite_report_events e
    WHERE e.report_id = r.id AND e.event_type = 0
);

-- 2. Auto-triage outcome.
-- The auto-triage system went live 2026-05-12 UTC. Reports created earlier
-- predate the system — skip the auto-triage event for those.
-- Reports with auto_triage_reason IS NULL passed all rules (AutoTriaged).
-- Reports with auto_triage_reason IS NOT NULL were sent to inspector review
-- (SentToReview), with the reason carried in notes.

INSERT INTO dumpsite_report_events
    (id, report_id, event_type, occurred_at, actor_user_id, actor_role, actor_display_name, notes, payload_json, created_at, updated_at)
SELECT
    gen_random_uuid(),
    r.id,
    1,                      -- DumpsiteEventType.AutoTriaged
    r.created_at + interval '1 second',
    NULL,
    'System',
    'Auto-triage system',
    'Passed all triage rules',
    NULL,
    r.created_at,
    r.created_at
FROM dumpsite_reports r
WHERE r.created_at >= TIMESTAMPTZ '2026-05-12 00:00:00+00'
  AND r.auto_triage_reason IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM dumpsite_report_events e
      WHERE e.report_id = r.id AND e.event_type IN (1, 2)
  );

INSERT INTO dumpsite_report_events
    (id, report_id, event_type, occurred_at, actor_user_id, actor_role, actor_display_name, notes, payload_json, created_at, updated_at)
SELECT
    gen_random_uuid(),
    r.id,
    2,                      -- DumpsiteEventType.SentToReview
    r.created_at + interval '1 second',
    NULL,
    'System',
    'Auto-triage system',
    r.auto_triage_reason,
    NULL,
    r.created_at,
    r.created_at
FROM dumpsite_reports r
WHERE r.created_at >= TIMESTAMPTZ '2026-05-12 00:00:00+00'
  AND r.auto_triage_reason IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM dumpsite_report_events e
      WHERE e.report_id = r.id AND e.event_type IN (1, 2)
  );

COMMIT;

-- Sanity checks.
SELECT count(*) AS total_events FROM dumpsite_report_events;
SELECT event_type, count(*) FROM dumpsite_report_events GROUP BY event_type ORDER BY event_type;
