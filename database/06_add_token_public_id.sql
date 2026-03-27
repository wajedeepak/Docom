-- Migration: Add PublicTokenId column to Tokens table
-- Run on existing databases. New installs use 01_schema.sql.

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'Tokens') AND name = N'PublicTokenId'
)
BEGIN
    ALTER TABLE Tokens
        ADD PublicTokenId NVARCHAR(10) NOT NULL DEFAULT '';
    PRINT 'Column PublicTokenId added to Tokens.';
END
ELSE
    PRINT 'Column PublicTokenId already exists — skipped.';
GO

-- Create unique index if not exists
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'Tokens') AND name = N'IX_Tokens_PublicTokenId'
)
BEGIN
    CREATE UNIQUE INDEX IX_Tokens_PublicTokenId ON Tokens(PublicTokenId)
        WHERE PublicTokenId != '';
    PRINT 'Unique index on PublicTokenId created.';
END
ELSE
    PRINT 'Index IX_Tokens_PublicTokenId already exists — skipped.';
GO
