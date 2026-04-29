-- ============================================================================
-- ManageEmployees Database Schema
-- Idempotent script: safe to re-run on existing databases.
-- ============================================================================

-- --------------------------------------------------------------------------
-- USERS (ASP.NET Identity)
-- Clustered PK on Id. Non-clustered indexes on NormalizedUserName (login)
-- and NormalizedEmail (lookup by email) — both are unique and filtered
-- to skip NULLs (Identity allows NULL before confirmation).
-- --------------------------------------------------------------------------
IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    CREATE TABLE Users (
        Id                   NVARCHAR(450)   NOT NULL,
        UserName             NVARCHAR(256)   NULL,
        NormalizedUserName   NVARCHAR(256)   NULL,
        Email                NVARCHAR(256)   NULL,
        NormalizedEmail      NVARCHAR(256)   NULL,
        EmailConfirmed       BIT             NOT NULL DEFAULT 0,
        PasswordHash         NVARCHAR(MAX)   NULL,
        SecurityStamp        NVARCHAR(MAX)   NULL,
        ConcurrencyStamp     NVARCHAR(MAX)   NULL,
        PhoneNumber          NVARCHAR(MAX)   NULL,
        PhoneNumberConfirmed BIT             NOT NULL DEFAULT 0,
        TwoFactorEnabled     BIT             NOT NULL DEFAULT 0,
        LockoutEnd           DATETIMEOFFSET  NULL,
        LockoutEnabled       BIT             NOT NULL DEFAULT 0,
        AccessFailedCount    INT             NOT NULL DEFAULT 0,
        FirstName            NVARCHAR(256)   NOT NULL DEFAULT '',
        LastName             NVARCHAR(256)   NOT NULL DEFAULT '',
        DocNumber            NVARCHAR(256)   NOT NULL DEFAULT '',

        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- FindByNameAsync — every login hits this path
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_NormalizedUserName' AND object_id = OBJECT_ID('Users'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_NormalizedUserName
        ON Users (NormalizedUserName)
        WHERE NormalizedUserName IS NOT NULL;
GO

-- FindByEmailAsync — user lookup by email
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_NormalizedEmail' AND object_id = OBJECT_ID('Users'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_NormalizedEmail
        ON Users (NormalizedEmail)
        WHERE NormalizedEmail IS NOT NULL;
GO

-- GetAllWithRolesAsync — ORDER BY FirstName (covering: LastName, Email, DocNumber, PhoneNumber)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_FirstName' AND object_id = OBJECT_ID('Users'))
    CREATE NONCLUSTERED INDEX IX_Users_FirstName
        ON Users (FirstName)
        INCLUDE (LastName, Email, DocNumber, PhoneNumber);
GO

-- --------------------------------------------------------------------------
-- ROLES (ASP.NET Identity)
-- NormalizedName is queried by FindByNameAsync, AddToRoleAsync,
-- RemoveFromRoleAsync, and GetUsersInRoleAsync (4 different code paths).
-- --------------------------------------------------------------------------
IF OBJECT_ID('Roles', 'U') IS NULL
BEGIN
    CREATE TABLE Roles (
        Id               NVARCHAR(450)   NOT NULL,
        Name             NVARCHAR(256)   NULL,
        NormalizedName   NVARCHAR(256)   NULL,
        ConcurrencyStamp NVARCHAR(MAX)   NULL,

        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Roles_NormalizedName' AND object_id = OBJECT_ID('Roles'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_Roles_NormalizedName
        ON Roles (NormalizedName)
        WHERE NormalizedName IS NOT NULL;
GO

-- --------------------------------------------------------------------------
-- USER-ROLES (junction table)
-- Composite PK (UserId, RoleId) covers lookups by UserId (GetRolesAsync).
-- Separate index on RoleId for reverse lookups (GetUsersInRoleAsync joins
-- from Roles -> UserRoles -> Users).
-- --------------------------------------------------------------------------
IF OBJECT_ID('UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE UserRoles (
        UserId  NVARCHAR(450) NOT NULL,
        RoleId  NVARCHAR(450) NOT NULL,

        CONSTRAINT PK_UserRoles     PRIMARY KEY CLUSTERED (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
    );
END
GO

-- Reverse FK lookup: GetUsersInRoleAsync, RemoveFromRoleAsync
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserRoles_RoleId' AND object_id = OBJECT_ID('UserRoles'))
    CREATE NONCLUSTERED INDEX IX_UserRoles_RoleId
        ON UserRoles (RoleId);
GO

-- --------------------------------------------------------------------------
-- REFRESH TOKENS
-- One active token per user. UserId index supports GetByUserIdAsync
-- and enforces single-token-per-user at the database level.
-- --------------------------------------------------------------------------
IF OBJECT_ID('RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE RefreshTokens (
        Id         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        UserId     NVARCHAR(450)    NOT NULL,
        Token      NVARCHAR(MAX)    NOT NULL,
        ExpireDate DATETIME2(7)     NOT NULL,

        CONSTRAINT PK_RefreshTokens      PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
GO

-- GetByUserIdAsync — also enforces one active token per user
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_RefreshTokens_UserId' AND object_id = OBJECT_ID('RefreshTokens'))
    CREATE UNIQUE NONCLUSTERED INDEX IX_RefreshTokens_UserId
        ON RefreshTokens (UserId);
GO

-- --------------------------------------------------------------------------
-- TASKS
-- UserId + CreatedAt DESC composite index covers GetByUserIdAsync with
-- its ORDER BY CreatedAt DESC. CreatedAt DESC standalone covers GetAllAsync.
-- CHECK constraint enforces valid status values at the database level.
-- NEWSEQUENTIALID() avoids clustered index fragmentation from random GUIDs.
-- --------------------------------------------------------------------------
IF OBJECT_ID('Tasks', 'U') IS NULL
BEGIN
    CREATE TABLE Tasks (
        Id          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
        Title       NVARCHAR(200)    NOT NULL,
        Description NVARCHAR(1000)   NULL,
        Status      NVARCHAR(50)     NOT NULL DEFAULT 'Pending',
        DueDate     DATETIME2(7)     NOT NULL,
        UserId      NVARCHAR(450)    NOT NULL,
        CreatedAt   DATETIME2(7)     NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT PK_Tasks      PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Tasks_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        CONSTRAINT CK_Tasks_Status CHECK (Status IN ('Pending', 'InProgress', 'Completed'))
    );
END
GO

-- GetByUserIdAsync — WHERE UserId = @UserId ORDER BY CreatedAt DESC
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_UserId_CreatedAt' AND object_id = OBJECT_ID('Tasks'))
    CREATE NONCLUSTERED INDEX IX_Tasks_UserId_CreatedAt
        ON Tasks (UserId, CreatedAt DESC)
        INCLUDE (Title, Description, Status, DueDate);
GO

-- GetAllAsync — ORDER BY CreatedAt DESC (full table scan with sort elimination)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tasks_CreatedAt' AND object_id = OBJECT_ID('Tasks'))
    CREATE NONCLUSTERED INDEX IX_Tasks_CreatedAt
        ON Tasks (CreatedAt DESC)
        INCLUDE (Title, Description, Status, DueDate, UserId);
GO
