-- =============================================================================
-- Stored Procedure: Atomic Token Creation
-- =============================================================================
-- Used by TokenRepository.CreateAsync() to atomically assign token numbers.
-- The UPDATE with UPDLOCK/ROWLOCK prevents two concurrent requests from
-- getting the same token number without requiring a table-level lock.
--
-- Called from C# via raw SQL:
--   UPDATE Sessions WITH (ROWLOCK, UPDLOCK)
--   SET LastIssuedTokenNumber = LastIssuedTokenNumber + 1
--   OUTPUT INSERTED.LastIssuedTokenNumber
--   WHERE Id = @SessionId
--
-- The stored procedure below is an equivalent alternative if you prefer
-- to call a proc instead of inline SQL.
-- =============================================================================

CREATE OR ALTER PROCEDURE sp_IssueToken
    @SessionId      INT,
    @PatientName    NVARCHAR(150)   = NULL,
    @TokenId        BIGINT          OUTPUT,
    @TokenNumber    INT             OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    -- Atomically increment the counter on the session row using a row-level lock.
    -- UPDLOCK: prevents another reader from taking an update lock simultaneously.
    -- ROWLOCK: confines locking to this single row (no page escalation).
    UPDATE Sessions WITH (UPDLOCK, ROWLOCK)
    SET    LastIssuedTokenNumber = LastIssuedTokenNumber + 1
    WHERE  Id = @SessionId
       AND Status IN (1, 2); -- Active or Paused only

    IF @@ROWCOUNT = 0
    BEGIN
        ROLLBACK;
        RAISERROR ('Session not found or not in an active state.', 16, 1);
        RETURN;
    END

    SELECT @TokenNumber = LastIssuedTokenNumber
    FROM   Sessions WITH (NOLOCK)
    WHERE  Id = @SessionId;

    INSERT INTO Tokens (SessionId, TokenNumber, PatientName, Status, QueueOrder, CreatedAt)
    VALUES (@SessionId, @TokenNumber, @PatientName, 0, @TokenNumber, SYSUTCDATETIME());

    SET @TokenId = SCOPE_IDENTITY();

    COMMIT;
END;
GO
