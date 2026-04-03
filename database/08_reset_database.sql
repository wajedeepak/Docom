-- =============================================================================
-- Database Reset Script - Clear All Data
-- SQL Server 2019+
-- WARNING: This will DELETE ALL DATA from the database
-- =============================================================================

USE db_docom;
GO

-- =============================================================================
-- DISABLE FOREIGN KEY CONSTRAINTS TEMPORARILY
-- =============================================================================
ALTER TABLE Tokens NOCHECK CONSTRAINT ALL;
ALTER TABLE Sessions NOCHECK CONSTRAINT ALL;
ALTER TABLE Doctors NOCHECK CONSTRAINT ALL;
ALTER TABLE OtpRequests NOCHECK CONSTRAINT ALL;
ALTER TABLE Users NOCHECK CONSTRAINT ALL;
GO

-- =============================================================================
-- DELETE ALL ROWS (DELETE works with constraints disabled)
-- =============================================================================
-- Delete in reverse order of foreign key dependencies

-- Tokens references Sessions
DELETE FROM Tokens;
GO

-- Sessions references Doctors
DELETE FROM Sessions;
GO

-- Doctors references Users
DELETE FROM Doctors;
GO

-- OtpRequests has no dependencies
DELETE FROM OtpRequests;
GO

-- Users is last
DELETE FROM Users;
GO

-- =============================================================================
-- RE-ENABLE FOREIGN KEY CONSTRAINTS
-- =============================================================================
ALTER TABLE Tokens WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE Sessions WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE Doctors WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE OtpRequests WITH CHECK CHECK CONSTRAINT ALL;
ALTER TABLE Users WITH CHECK CHECK CONSTRAINT ALL;
GO

-- =============================================================================
-- RESET IDENTITY SEEDS (so IDs start from 1 again)
-- =============================================================================
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Doctors', RESEED, 0);
DBCC CHECKIDENT ('Sessions', RESEED, 0);
DBCC CHECKIDENT ('Tokens', RESEED, 0);
DBCC CHECKIDENT ('OtpRequests', RESEED, 0);
GO

-- =============================================================================
-- INSERT SEED ADMIN USER
-- =============================================================================
-- Admin user for initial setup
INSERT INTO Users (Email, Name, Role, IsActive, CreatedAt)
VALUES (
    'wajedeepak@gmail.com',
    'Admin',
    0,  -- 0 = Admin role
    1,  -- Active
    SYSUTCDATETIME()
);
GO

PRINT 'Database reset complete. All data cleared, schema intact.';
PRINT 'Seed admin user inserted: wajedeepak@gmail.com';
PRINT 'Identity seeds reset to 0.';
PRINT '';
PRINT 'Next step: Use /api/auth/request-otp with the admin email to log in.';
