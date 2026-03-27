-- ─────────────────────────────────────────────────────────────────────────────
-- Migration: Add Address and Pincode columns to Doctors table
-- Run this script once on any existing Docom database.
-- New installations should use 01_schema.sql which already includes these columns.
-- ─────────────────────────────────────────────────────────────────────────────

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Doctors') AND name = N'Address'
)
BEGIN
    ALTER TABLE Doctors
        ADD Address NVARCHAR(300) NULL;
    PRINT 'Column Address added to Doctors.';
END
ELSE
    PRINT 'Column Address already exists — skipped.';
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Doctors') AND name = N'Pincode'
)
BEGIN
    ALTER TABLE Doctors
        ADD Pincode NVARCHAR(10) NULL;
    PRINT 'Column Pincode added to Doctors.';
END
ELSE
    PRINT 'Column Pincode already exists — skipped.';
GO
