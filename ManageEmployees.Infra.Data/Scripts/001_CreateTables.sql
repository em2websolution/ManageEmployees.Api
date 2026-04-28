IF OBJECT_ID('Users', 'U') IS NULL
BEGIN
    CREATE TABLE Users (
        Id              NVARCHAR(450)     NOT NULL PRIMARY KEY,
        UserName        NVARCHAR(256)     NULL,
        NormalizedUserName NVARCHAR(256)  NULL,
        Email           NVARCHAR(256)     NULL,
        NormalizedEmail NVARCHAR(256)     NULL,
        EmailConfirmed  BIT               NOT NULL DEFAULT 0,
        PasswordHash    NVARCHAR(MAX)     NULL,
        SecurityStamp   NVARCHAR(MAX)     NULL,
        ConcurrencyStamp NVARCHAR(MAX)    NULL,
        PhoneNumber     NVARCHAR(MAX)     NULL,
        PhoneNumberConfirmed BIT          NOT NULL DEFAULT 0,
        TwoFactorEnabled BIT             NOT NULL DEFAULT 0,
        LockoutEnd      DATETIMEOFFSET    NULL,
        LockoutEnabled  BIT               NOT NULL DEFAULT 0,
        AccessFailedCount INT             NOT NULL DEFAULT 0,
        FirstName       NVARCHAR(256)     NOT NULL DEFAULT '',
        LastName        NVARCHAR(256)     NOT NULL DEFAULT '',
        DocNumber       NVARCHAR(256)     NOT NULL DEFAULT ''
    );
END

IF OBJECT_ID('Roles', 'U') IS NULL
BEGIN
    CREATE TABLE Roles (
        Id              NVARCHAR(450)     NOT NULL PRIMARY KEY,
        Name            NVARCHAR(256)     NULL,
        NormalizedName  NVARCHAR(256)     NULL,
        ConcurrencyStamp NVARCHAR(MAX)    NULL
    );
END

IF OBJECT_ID('UserRoles', 'U') IS NULL
BEGIN
    CREATE TABLE UserRoles (
        UserId  NVARCHAR(450) NOT NULL,
        RoleId  NVARCHAR(450) NOT NULL,
        PRIMARY KEY (UserId, RoleId),
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
        FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
    );
END

IF OBJECT_ID('RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE RefreshTokens (
        Id        UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        UserId    NVARCHAR(450)    NOT NULL,
        Token     NVARCHAR(MAX)    NOT NULL,
        ExpireDate DATETIME2       NOT NULL,
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END

IF OBJECT_ID('Tasks', 'U') IS NULL
BEGIN
    CREATE TABLE Tasks (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        Title       NVARCHAR(200)    NOT NULL,
        Description NVARCHAR(1000)   NULL,
        Status      NVARCHAR(50)     NOT NULL DEFAULT 'Pending',
        DueDate     DATETIME2        NOT NULL,
        UserId      NVARCHAR(450)    NOT NULL,
        CreatedAt   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
        FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );
END
