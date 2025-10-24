-- LiaXP Database Schema for Azure SQL Server

-- Drop existing tables if they exist (for clean setup)
IF OBJECT_ID('MessageLog', 'U') IS NOT NULL DROP TABLE MessageLog;
IF OBJECT_ID('ReviewQueue', 'U') IS NOT NULL DROP TABLE ReviewQueue;
IF OBJECT_ID('InsightCache', 'U') IS NOT NULL DROP TABLE InsightCache;
IF OBJECT_ID('ImportStatus', 'U') IS NOT NULL DROP TABLE ImportStatus;
IF OBJECT_ID('Sale', 'U') IS NOT NULL DROP TABLE Sale;
IF OBJECT_ID('Goal', 'U') IS NOT NULL DROP TABLE Goal;
IF OBJECT_ID('Seller', 'U') IS NOT NULL DROP TABLE Seller;
IF OBJECT_ID('Store', 'U') IS NOT NULL DROP TABLE Store;
IF OBJECT_ID('Company', 'U') IS NOT NULL DROP TABLE Company;
GO

-- Company Table
CREATE TABLE Company (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(50) NOT NULL UNIQUE,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0
);

-- Store Table
CREATE TABLE Store (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    [Address] NVARCHAR(300) NULL,
    Phone NVARCHAR(30) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Store_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    CONSTRAINT UQ_Store UNIQUE (CompanyId, [Name])
);

-- Seller Table
CREATE TABLE Seller (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    StoreId UNIQUEIDENTIFIER NOT NULL,
    SellerCode NVARCHAR(60) NOT NULL,
    [Name] NVARCHAR(150) NOT NULL,
    PhoneE164 NVARCHAR(30) NULL,
    Email NVARCHAR(200) NULL,
    [Status] NVARCHAR(30) NOT NULL DEFAULT 'Active',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Seller_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    CONSTRAINT FK_Seller_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT UQ_Seller UNIQUE (CompanyId, SellerCode)
);

-- Goal Table
CREATE TABLE Goal (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    StoreId UNIQUEIDENTIFIER NOT NULL,
    SellerId UNIQUEIDENTIFIER NOT NULL,
    [Month] DATE NOT NULL,
    TargetValue DECIMAL(14,2) NOT NULL,
    TargetTicket DECIMAL(10,2) NULL,
    TargetConversion DECIMAL(6,3) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Goal_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    CONSTRAINT FK_Goal_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT FK_Goal_Seller FOREIGN KEY (SellerId) REFERENCES Seller(Id),
    CONSTRAINT UQ_Goal UNIQUE (CompanyId, SellerId, [Month])
);

-- Sale Table
CREATE TABLE Sale (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    StoreId UNIQUEIDENTIFIER NOT NULL,
    SellerId UNIQUEIDENTIFIER NOT NULL,
    SaleDate DATE NOT NULL,
    TotalValue DECIMAL(14,2) NOT NULL,
    ItemsQty INT NOT NULL,
    AvgTicket DECIMAL(10,2) NOT NULL,
    Category NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_Sale_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    CONSTRAINT FK_Sale_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT FK_Sale_Seller FOREIGN KEY (SellerId) REFERENCES Seller(Id)
);

-- ImportStatus Table
CREATE TABLE ImportStatus (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    FileHash NVARCHAR(64) NOT NULL,
    ImportedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastTrainedHash NVARCHAR(64) NULL,
    LastTrainedAt DATETIME2 NULL,
    IsStale BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ImportStatus_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id)
);

-- InsightCache Table
CREATE TABLE InsightCache (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    StoreId UNIQUEIDENTIFIER NULL,
    SellerId UNIQUEIDENTIFIER NULL,
    InsightDate DATE NOT NULL,
    InsightType NVARCHAR(50) NOT NULL,
    DataJson NVARCHAR(MAX) NOT NULL,
    CachedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_InsightCache_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    CONSTRAINT FK_InsightCache_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT FK_InsightCache_Seller FOREIGN KEY (SellerId) REFERENCES Seller(Id)
);

-- ReviewQueue Table
CREATE TABLE ReviewQueue (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    Moment NVARCHAR(20) NOT NULL,
    RecipientPhone NVARCHAR(30) NOT NULL,
    RecipientName NVARCHAR(150) NOT NULL,
    DraftMessage NVARCHAR(MAX) NOT NULL,
    EditedMessage NVARCHAR(MAX) NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT 'Pending',
    ReviewedAt DATETIME2 NULL,
    ReviewedBy NVARCHAR(150) NULL,
    SentAt DATETIME2 NULL,
    ErrorMessage NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ReviewQueue_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id)
);

-- MessageLog Table
CREATE TABLE MessageLog (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,
    Direction NVARCHAR(20) NOT NULL,
    PhoneFrom NVARCHAR(30) NOT NULL,
    PhoneTo NVARCHAR(30) NOT NULL,
    [Message] NVARCHAR(MAX) NOT NULL,
    Provider NVARCHAR(20) NOT NULL,
    ExternalId NVARCHAR(100) NULL,
    [Status] NVARCHAR(20) NOT NULL,
    SentAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ErrorMessage NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_MessageLog_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id)
);

-- Create Indexes
CREATE INDEX IX_Store_CompanyId ON Store(CompanyId);
CREATE INDEX IX_Seller_CompanyId ON Seller(CompanyId);
CREATE INDEX IX_Seller_StoreId ON Seller(StoreId);
CREATE INDEX IX_Seller_PhoneE164 ON Seller(PhoneE164);
CREATE INDEX IX_Goal_CompanyId ON Goal(CompanyId);
CREATE INDEX IX_Goal_SellerId ON Goal(SellerId);
CREATE INDEX IX_Goal_Month ON Goal([Month]);
CREATE INDEX IX_Sale_CompanyId ON Sale(CompanyId);
CREATE INDEX IX_Sale_StoreId ON Sale(StoreId);
CREATE INDEX IX_Sale_SellerId ON Sale(SellerId);
CREATE INDEX IX_Sale_SaleDate ON Sale(SaleDate);
CREATE INDEX IX_InsightCache_CompanyId ON InsightCache(CompanyId);
CREATE INDEX IX_InsightCache_InsightDate ON InsightCache(InsightDate);
CREATE INDEX IX_ReviewQueue_CompanyId ON ReviewQueue(CompanyId);
CREATE INDEX IX_ReviewQueue_Status ON ReviewQueue([Status]);
CREATE INDEX IX_MessageLog_CompanyId ON MessageLog(CompanyId);
CREATE INDEX IX_MessageLog_SentAt ON MessageLog(SentAt);

GO

-- Insert sample company for testing
INSERT INTO Company (Code, [Name], [Description], IsActive)
VALUES ('ACME', 'ACME Corporation', 'Sample company for testing', 1);

GO

PRINT 'Database schema created successfully!';
