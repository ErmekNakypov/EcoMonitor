-- One-time backfill for reports created after the auto-triage cutoff
-- (2026-05-12) that landed in InReview without a recorded
-- AutoTriageReason. We can confidently re-derive two rule categories
-- from the row itself:
--
--   * "Description too short" → length(trim(description)) < 15
--   * "No photos attached"    → photo_paths jsonb array is empty
--
-- The duplicate-detection rule cannot be re-derived without re-running
-- triage with the original DB snapshot, so it is left alone.
--
-- Status 1 corresponds to DumpsiteStatus.InReview (verify before running).
--
-- Usage from repo root:
--   PGPASSWORD=devpassword123 psql -h localhost -p 5432 -U ecomonitor_app \
--       -d ecomonitor -f dev-data/backfill-triage-reasons.sql

BEGIN;

UPDATE dumpsite_reports
SET auto_triage_reason =
    'Description too short (less than 15 characters). Inspector review needed.'
WHERE status = 1
  AND auto_triage_reason IS NULL
  AND length(trim(description)) < 15
  AND created_at >= '2026-05-12 00:00:00+00';

-- photo_paths is jsonb; an empty array is '[]' and NULL is also possible.
UPDATE dumpsite_reports
SET auto_triage_reason =
    'No photos attached. Inspector review needed to assess validity.'
WHERE status = 1
  AND auto_triage_reason IS NULL
  AND (photo_paths IS NULL OR photo_paths = '[]'::jsonb)
  AND created_at >= '2026-05-12 00:00:00+00';

COMMIT;

-- Sanity check
SELECT count(*) FILTER (WHERE auto_triage_reason IS NULL)        AS still_null,
       count(*) FILTER (WHERE auto_triage_reason IS NOT NULL)    AS now_filled
FROM dumpsite_reports
WHERE status = 1
  AND created_at >= '2026-05-12 00:00:00+00';
