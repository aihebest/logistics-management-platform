/* ============================================================================
   Grant Coordinator rights — Daniella Emechebe and James Shofunde
   ----------------------------------------------------------------------------
   Requested after the HOD / Director of Logistics meeting, 09 Sep 2026.

   Coordinator lets them:
     • edit and remove driver records
     • edit Movement Register entries
     • correct fuel logs
     • approve intrastate trips and close movements

   IMPORTANT — this is only half the job.
   The platform re-syncs a user's role from their Entra ID app role on every
   sign-in. If you change the role here but not in Entra, it will revert the
   next time they log in.

   So do BOTH:
     1. Entra ID → Enterprise applications → logistics-platform-api
        → Users and groups → assign each of them the "Coordinator" app role.
     2. Run this script, so they hold the role immediately without waiting for
        a fresh sign-in.

   Run against: sqldb-logistics on sql-deslogistics-prod-jk4s6b
   ========================================================================== */

SET NOCOUNT ON;

DECLARE @targets TABLE (Email NVARCHAR(256));

-- Adjust these if their mailboxes differ from the pattern below.
INSERT INTO @targets (Email) VALUES
    (N'daniella.emechebe@desicongroup.com'),
    (N'james.shofunde@desicongroup.com');

-- ── Show what we are about to change ────────────────────────────────────────
SELECT  u.Id, u.FullName, u.Email, u.Role AS CurrentRole, u.IsActive
FROM    Users u
JOIN    @targets t ON LOWER(u.Email) = LOWER(t.Email);

-- ── Apply ───────────────────────────────────────────────────────────────────
UPDATE  u
SET     u.Role = N'Coordinator'
FROM    Users u
JOIN    @targets t ON LOWER(u.Email) = LOWER(t.Email)
WHERE   u.Role <> N'Coordinator';

PRINT CONCAT(@@ROWCOUNT, ' user(s) updated to Coordinator.');

-- ── Confirm ─────────────────────────────────────────────────────────────────
SELECT  u.FullName, u.Email, u.Role, u.IsActive
FROM    Users u
JOIN    @targets t ON LOWER(u.Email) = LOWER(t.Email);

/* If either row is missing, they have never signed in and have no platform
   record yet. Add them from Platform Users in the app (Full Name + Email +
   role Coordinator) — that pre-registers them so notifications reach them and
   their account links up on first sign-in. */
