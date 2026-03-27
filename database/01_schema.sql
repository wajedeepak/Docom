-- =============================================================================
-- Docom Database Schema
-- SQL Server 2019+
-- Run this script on a blank database: CREATE DATABASE DocomDb
-- =============================================================================

USE db_docom;
GO

-- =============================================================================
-- TABLES
-- =============================================================================

-- Users (doctors, receptionists, admins)
CREATE TABLE Users (
    Id          INT             NOT NULL IDENTITY(1,1),
    Email       NVARCHAR(256)   NOT NULL,
    Name        NVARCHAR(150)   NOT NULL,
    Role        INT             NOT NULL,   -- 0=Admin, 1=Doctor, 2=Receptionist
    IsActive    BIT             NOT NULL    DEFAULT 1,
    CreatedAt   DATETIME2       NOT NULL    DEFAULT SYSUTCDATETIME(),
    -- Password-based authentication
    PasswordHash NVARCHAR(255)   NULL,      -- PBKDF2 hash, nullable for backward compat
    HasPasswordSet BIT           NOT NULL    DEFAULT 0,   -- Flag: user has set a password
    LastPasswordChangedAt DATETIME2    NULL,   -- Track password change date (audit trail)

    CONSTRAINT PK_Users PRIMARY KEY (Id)
);
GO

-- Doctors
CREATE TABLE Doctors (
    Id              INT             NOT NULL IDENTITY(1,1),
    Slug            NVARCHAR(100)   NOT NULL,   -- URL slug e.g. "drsharma"
    Name            NVARCHAR(150)   NOT NULL,
    Specialization  NVARCHAR(150)   NOT NULL,
    Address         NVARCHAR(300)   NULL,           -- Clinic address (optional)
    Pincode         NVARCHAR(10)    NULL,           -- 6-digit pincode (optional)
    UserId          INT             NOT NULL,
    IsActive        BIT             NOT NULL    DEFAULT 1,
    CreatedAt       DATETIME2       NOT NULL    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Doctors       PRIMARY KEY (Id),
    CONSTRAINT FK_Doctors_Users FOREIGN KEY (UserId) REFERENCES Users(Id)
);
GO

-- Sessions (one per work period per doctor)
CREATE TABLE Sessions (
    Id                      INT             NOT NULL IDENTITY(1,1),
    DoctorId                INT             NOT NULL,
    Label                   NVARCHAR(50)    NOT NULL,   -- "Morning", "Evening", etc.
    Status                  INT             NOT NULL    DEFAULT 0,
    --  0=Created, 1=Active, 2=Paused, 3=Ended
    CurrentTokenNumber      INT             NOT NULL    DEFAULT 0,
    LastIssuedTokenNumber   INT             NOT NULL    DEFAULT 0,
    CreatedAt               DATETIME2       NOT NULL    DEFAULT SYSUTCDATETIME(),
    StartedAt               DATETIME2       NULL,
    EndedAt                 DATETIME2       NULL,

    CONSTRAINT PK_Sessions          PRIMARY KEY (Id),
    CONSTRAINT FK_Sessions_Doctors  FOREIGN KEY (DoctorId) REFERENCES Doctors(Id)
);
GO

-- Tokens (one per patient per session)
CREATE TABLE Tokens (
    Id              BIGINT          NOT NULL IDENTITY(1,1),
    SessionId       INT             NOT NULL,
    TokenNumber     INT             NOT NULL,   -- resets per session (1, 2, 3 ...)
    PatientName     NVARCHAR(150)   NULL,
    PhoneNumber     NVARCHAR(20)    NULL,
    PublicTokenId   NVARCHAR(10)    NOT NULL    DEFAULT '',
    Status          INT             NOT NULL    DEFAULT 0,
    --  0=Waiting, 1=Serving, 2=Skipped, 3=Completed
    QueueOrder      INT             NOT NULL,   -- sort key; skip moves token to MAX+1
    CreatedAt       DATETIME2       NOT NULL    DEFAULT SYSUTCDATETIME(),
    ServedAt        DATETIME2       NULL,

    CONSTRAINT PK_Tokens            PRIMARY KEY (Id),
    CONSTRAINT FK_Tokens_Sessions   FOREIGN KEY (SessionId) REFERENCES Sessions(Id)
);
GO

-- OTP Requests
CREATE TABLE OtpRequests (
    Id          INT             NOT NULL IDENTITY(1,1),
    Email       NVARCHAR(256)   NOT NULL,
    OtpHash     NCHAR(64)       NOT NULL,   -- SHA-256 hex, fixed width
    ExpiresAt   DATETIME2       NOT NULL,
    IsUsed      BIT             NOT NULL    DEFAULT 0,
    CreatedAt   DATETIME2       NOT NULL    DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_OtpRequests PRIMARY KEY (Id)
);
GO

-- =============================================================================
-- INDEXES  (all performance-critical paths covered)
-- =============================================================================

-- Users: email lookup (login hot path)
CREATE UNIQUE INDEX UX_Users_Email
    ON Users (Email);
GO

-- Users: password lookup (password-based login hot path)
CREATE INDEX IX_Users_Email_HasPasswordSet
    ON Users (Email, HasPasswordSet);
GO

-- Doctors: slug lookup (patient page hot path — most frequent read in the system)
CREATE UNIQUE INDEX UX_Doctors_Slug
    ON Doctors (Slug)
    WHERE IsActive = 1;
GO

CREATE INDEX IX_Doctors_IsActive
    ON Doctors (IsActive);
GO

-- Sessions: find the active/non-ended session for a doctor
CREATE INDEX IX_Sessions_DoctorId_Status
    ON Sessions (DoctorId, Status)
    INCLUDE (CurrentTokenNumber, LastIssuedTokenNumber, Label, CreatedAt);
GO

-- Tokens: queue reads — filter by session + status, ordered by QueueOrder
-- This is the hottest index in the system
CREATE INDEX IX_Tokens_SessionId_Status_QueueOrder
    ON Tokens (SessionId, Status, QueueOrder)
    INCLUDE (TokenNumber, PatientName, CreatedAt);
GO

-- Tokens: unique token number per session
CREATE UNIQUE INDEX UX_Tokens_SessionId_TokenNumber
    ON Tokens (SessionId, TokenNumber);
GO

-- Tokens: unique public tracking ID
CREATE UNIQUE INDEX IX_Tokens_PublicTokenId
    ON Tokens (PublicTokenId)
    WHERE PublicTokenId != '';
GO

-- OTP: find unused OTP for an email
CREATE INDEX IX_OtpRequests_Email_IsUsed
    ON OtpRequests (Email, IsUsed)
    INCLUDE (OtpHash, ExpiresAt, CreatedAt);
GO

-- =============================================================================
-- SEED DATA: Initial Admin User
-- =============================================================================
-- After inserting, use the /api/auth/request-otp endpoint with this email
-- to receive a login OTP and gain admin access.

INSERT INTO Users (Email, Name, Role, IsActive)
VALUES ('wajedeepak@gmail.com', 'Platform Admin', 0, 1);
GO

-- =============================================================================
-- OPTIONAL: Sample doctor for testing
-- =============================================================================

DECLARE @AdminId INT = SCOPE_IDENTITY();

INSERT INTO Users (Email, Name, Role, IsActive)
VALUES ('drtest@docom.in', 'Dr Test', 1, 1);

DECLARE @DoctorUserId INT = SCOPE_IDENTITY();

INSERT INTO Doctors (Slug, Name, Specialization, UserId, IsActive)
VALUES ('drtest' 'Test', 'General Physician', @DoctorUserId, 1);
GO

PRINT 'Schema created successfully.';
PRINT 'Admin login email: wajedeepak@gmail.com';
PRINT 'Test doctor URL: docom.in/drtest';
GO
