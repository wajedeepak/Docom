-- =============================================================================
-- Migration 7: Add Password-Based Authentication
-- Date: 2026-03-26
-- Description: Extends Users table to support password-based login alongside OTP
-- =============================================================================

USE db_docom;
GO

-- Add password-related columns to Users table
ALTER TABLE Users
ADD 
    PasswordHash NVARCHAR(255) NULL,           -- Bcrypt hash, nullable for backward compat
    HasPasswordSet BIT NOT NULL DEFAULT 0,     -- Flag: user has set a password
    LastPasswordChangedAt DATETIME2 NULL;      -- Track password change date (audit trail)
GO

-- Optional: Add index on email + HasPasswordSet for password lookup queries
CREATE INDEX IX_Users_Email_HasPasswordSet 
    ON Users (Email, HasPasswordSet);
GO

PRINT 'Migration 7 completed: Password authentication columns added to Users table.';
