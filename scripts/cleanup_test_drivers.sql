-- ============================================================================
-- UAT cleanup — clear implementation-era users out of the Drivers roster
--
-- Why these appeared: every first-time login was auto-provisioned with the role
-- "Driver", so office staff (and admins) ended up on the Drivers page. The
-- application now defaults such users to "Staff" instead, but existing rows
-- still need correcting once.
--
-- Safe by design — this does NOT delete anyone:
--   * Deleting users would fail anyway (trips, movements and notifications
--     reference them by foreign key) and would destroy audit history.
--   * Instead their role is corrected, which removes them from the Drivers
--     list while leaving their account and history intact.
--
-- Anyone with a real licence number, or with trip assignments already against
-- them, is left untouched so genuine drivers are never demoted.
--
-- Idempotent: safe to run more than once.
-- ============================================================================

SET NOCOUNT ON;
BEGIN TRANSACTION;

-- ── Before ──────────────────────────────────────────────────────────────────
SELECT 'BEFORE' AS Stage, Role, COUNT(*) AS Users
FROM Users GROUP BY Role;

-- ── Move placeholder "drivers" to Staff ─────────────────────────────────────
-- Criteria: currently Driver, no licence recorded, and never assigned a trip.
UPDATE Users
SET    Role         = 'Staff',
       DriverStatus = NULL
WHERE  Role = 'Driver'
  AND (LicenceNo IS NULL OR LTRIM(RTRIM(LicenceNo)) = '')
  AND  NOT EXISTS (SELECT 1 FROM Assignments a WHERE a.DriverId = Users.Id);

PRINT CONCAT('Users moved from Driver to Staff: ', @@ROWCOUNT);

-- ── After ───────────────────────────────────────────────────────────────────
SELECT 'AFTER' AS Stage, Role, COUNT(*) AS Users
FROM Users GROUP BY Role;

SELECT FullName, Email, Role, DriverStatus, LicenceNo
FROM   Users
ORDER  BY Role, FullName;

COMMIT TRANSACTION;

-- ============================================================================
-- After running this:
--   1. The Drivers page will be empty — register the real drivers with
--      "+ Register Driver" (name, email, phone, licence no, expiry).
--   2. Staff keep full access to raise trip and material requests.
--   3. Assign each real driver the "Driver" app role in Entra ID so their
--      role stays correct automatically on every login.
-- ============================================================================
