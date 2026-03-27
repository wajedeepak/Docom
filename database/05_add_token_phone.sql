-- ─────────────────────────────────────────────────────────────────────────────
-- Migration: Add PhoneNumber column to Tokens table
-- Run this script once on any existing Docom database.
-- New installations should use 01_schema.sql which already includes this column.
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Tokens') AND name = N'PhoneNumber'
)
BEGIN
    ALTER TABLE Tokens
        ADD PhoneNumber NVARCHAR(20) NULL;
    PRINT 'Column PhoneNumber added to Tokens.';
END
ELSE
    PRINT 'Column PhoneNumber already exists — skipped.';
GO
