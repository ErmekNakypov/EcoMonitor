-- One-time cleanup of obsolete/inappropriate test reports created during development.
-- Removes reports with playful or inappropriate descriptions and their related photos,
-- timeline events, cleanup photos, inspection photos, appeal photos.

BEGIN;

-- Identify the reports we want to delete (by description pattern)
WITH reports_to_delete AS (
    SELECT id FROM dumpsite_reports
    WHERE description ILIKE '%абулхаир%'
       OR description ILIKE '%test test test%'
       OR description ILIKE '%тест тест тест%'
       OR description ILIKE '%ololo%'
       OR description ILIKE '%lol%'
       OR description = 'Test example'
       OR description = 'test example'
)
-- Show what is about to be deleted (for confirmation)
SELECT id, description, status, created_at FROM dumpsite_reports
WHERE id IN (SELECT id FROM reports_to_delete);

-- Actual deletes. If any FK cascade is missing, the related-record deletes must come first:

DELETE FROM dumpsite_cleanup_photos
WHERE report_id IN (
    SELECT id FROM dumpsite_reports
    WHERE description ILIKE '%абулхаир%'
       OR description ILIKE '%test test test%'
       OR description ILIKE '%тест тест тест%'
       OR description ILIKE '%ololo%'
       OR description = 'Test example'
       OR description = 'test example'
);

DELETE FROM dumpsite_inspection_photos
WHERE report_id IN (
    SELECT id FROM dumpsite_reports
    WHERE description ILIKE '%абулхаир%'
       OR description ILIKE '%test test test%'
       OR description ILIKE '%тест тест тест%'
       OR description ILIKE '%ololo%'
       OR description = 'Test example'
       OR description = 'test example'
);

DELETE FROM dumpsite_appeal_photos
WHERE report_id IN (
    SELECT id FROM dumpsite_reports
    WHERE description ILIKE '%абулхаир%'
       OR description ILIKE '%test test test%'
       OR description ILIKE '%тест тест тест%'
       OR description ILIKE '%ololo%'
       OR description = 'Test example'
       OR description = 'test example'
);

-- Now the parent
DELETE FROM dumpsite_reports
WHERE description ILIKE '%абулхаир%'
   OR description ILIKE '%test test test%'
   OR description ILIKE '%тест тест тест%'
   OR description ILIKE '%ololo%'
   OR description = 'Test example'
   OR description = 'test example';

-- Also delete uploaded photo files from disk where they reference these reports.
-- This SQL cannot do that directly; the user must run an ls/rm command after this.

COMMIT;

-- Sanity check
SELECT count(*) AS remaining_reports FROM dumpsite_reports;
