-- Run once to clear obsolete @test.local accounts.
-- Safe: leaves admin@ecomonitor.local and any other non-test users in place.
--
-- Usage (from repo root):
--   PGPASSWORD=devpassword123 psql -h localhost -p 5432 -U ecomonitor_app \
--       -d ecomonitor -f dev-data/reset-test-accounts.sql
--
-- After running, start the app once in Development. The DbInitializer
-- seeds the new `nakypoverm+*@kstu.kg` test accounts on startup.

BEGIN;

DELETE FROM user_roles
WHERE user_id IN (SELECT id FROM users WHERE email LIKE '%@test.local');

DELETE FROM users
WHERE email LIKE '%@test.local';

COMMIT;

SELECT email FROM users ORDER BY email;
