-- Backfill DistrictId for existing reports.
--
-- Point-in-polygon resolution is non-trivial in plain SQL with the simplified
-- 4-quadrant polygons we seed; the EF runtime path uses ray-casting in C#.
-- The supported way to backfill is the admin diagnostics button:
--
--   /Admin/Diagnostics → "Backfill districts"
--
-- (POST /Admin/Diagnostics/BackfillDistricts)
--
-- This file exists as a placeholder so the data-ops directory tells the same
-- story as the appeal / report-events backfills. To verify before running:

-- Inspect how many reports are still missing a district.
SELECT count(*) AS unassigned_reports
FROM dumpsite_reports
WHERE district_id IS NULL;

-- After running the admin action, the same query should return 0 for any
-- report whose coordinates fall inside one of the 4 Bishkek polygons.
