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
EXEC sp_MSForEachTable 'ALTER TABLE ? DISABLE TRIGGER ALL';
GO

-- =============================================================================
-- TRUNCATE ALL TABLES (fastest way to clear data while keeping schema)
-- =============================================================================
-- Tables with foreign key dependencies must be truncated in correct order

-- Tokens must be deleted first (references Sessions)
TRUNCATE TABLE Tokens;
GO

-- Sessions must be deleted next (references Doctors)
TRUNCATE TABLE Sessions;
GO

-- Doctors must be deleted next (references Users)
TRUNCATE TABLE Doctors;
GO

-- OtpRequests has no foreign keys
TRUNCATE TABLE OtpRequests;
GO

-- Users is last (no dependencies)
TRUNCATE TABLE Users;
GO

-- =============================================================================
-- RE-ENABLE FOREIGN KEY CONSTRAINTS
-- =============================================================================
EXEC sp_MSForEachTable 'ALTER TABLE ? ENABLE TRIGGER ALL';
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
