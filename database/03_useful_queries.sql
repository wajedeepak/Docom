-- =============================================================================
-- Docom — Useful Operational Queries
-- =============================================================================

-- ─── Current queue for a doctor (by slug) ────────────────────────────────────
SELECT  t.TokenNumber,
        t.PatientName,
        t.Status,
        t.QueueOrder,
        t.CreatedAt
FROM    Tokens t
JOIN    Sessions s  ON s.Id = t.SessionId
JOIN    Doctors d   ON d.Id = s.DoctorId
WHERE   d.Slug      = 'drsharma'
  AND   s.Status    IN (1, 2)          -- Active or Paused session
  AND   t.Status    IN (0, 1)          -- Waiting or Serving
ORDER BY t.QueueOrder;
GO

-- ─── All active sessions right now ───────────────────────────────────────────
SELECT  d.Slug,
        d.Name,
        s.Label,
        s.Status,
        s.CurrentTokenNumber,
        s.LastIssuedTokenNumber,
        s.StartedAt
FROM    Sessions s
JOIN    Doctors  d ON d.Id = s.DoctorId
WHERE   s.Status IN (1, 2)
ORDER BY s.StartedAt DESC;
GO

-- ─── Daily token volume per doctor ───────────────────────────────────────────
SELECT  d.Slug,
        d.Name,
        CAST(t.CreatedAt AS DATE)   AS [Date],
        COUNT(*)                    AS TokensIssued
FROM    Tokens t
JOIN    Sessions s ON s.Id = t.SessionId
JOIN    Doctors  d ON d.Id = s.DoctorId
GROUP BY d.Slug, d.Name, CAST(t.CreatedAt AS DATE)
ORDER BY [Date] DESC, TokensIssued DESC;
GO

-- ─── Clean up expired (unused) OTPs older than 1 hour ────────────────────────
DELETE FROM OtpRequests
WHERE  ExpiresAt < DATEADD(HOUR, -1, SYSUTCDATETIME());
GO

-- ─── Reset a slug (admin use) ─────────────────────────────────────────────────
-- UPDATE Doctors SET Slug = 'newslug' WHERE Id = <DoctorId>;

-- ─── Disable a doctor account ────────────────────────────────────────────────
-- UPDATE Doctors SET IsActive = 0 WHERE Slug = 'drsharma';
-- UPDATE Users   SET IsActive = 0 WHERE Id   = (SELECT UserId FROM Doctors WHERE Slug = 'drsharma');
