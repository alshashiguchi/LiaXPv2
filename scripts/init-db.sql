-- LiaXP Database Schema - Updated with CompanyId consistency
-- All tables now use CompanyId (GUID) for foreign keys
-- CompanyCode is maintained as a business identifier only

-- Drop existing tables if they exist (for clean setup)
IF OBJECT_ID('ChatMessage', 'U') IS NOT NULL DROP TABLE ChatMessage;
IF OBJECT_ID('MessageLog', 'U') IS NOT NULL DROP TABLE MessageLog;
IF OBJECT_ID('ReviewQueue', 'U') IS NOT NULL DROP TABLE ReviewQueue;
IF OBJECT_ID('InsightCache', 'U') IS NOT NULL DROP TABLE InsightCache;
IF OBJECT_ID('ImportStatus', 'U') IS NOT NULL DROP TABLE ImportStatus;
IF OBJECT_ID('Sale', 'U') IS NOT NULL DROP TABLE Sale;
IF OBJECT_ID('Goal', 'U') IS NOT NULL DROP TABLE Goal;
IF OBJECT_ID('Seller', 'U') IS NOT NULL DROP TABLE Seller;
IF OBJECT_ID('Store', 'U') IS NOT NULL DROP TABLE Store;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
IF OBJECT_ID('Company', 'U') IS NOT NULL DROP TABLE Company;
GO

-- ============================================================================
-- COMPANY TABLE
-- ============================================================================
-- Root aggregate in multi-tenant architecture
-- Both Id (GUID) and Code (string) are maintained
-- Id = Technical key for all FKs
-- Code = Business key for external APIs and user-friendly identification
-- ============================================================================
CREATE TABLE Company (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Code NVARCHAR(50) NOT NULL UNIQUE,  -- Business identifier (e.g., "ACME")
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    -- Indexes
    INDEX IX_Company_Code NONCLUSTERED (Code) WHERE IsDeleted = 0,
    INDEX IX_Company_IsActive NONCLUSTERED (IsActive) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- USERS TABLE - FIXED
-- ============================================================================
-- Now uses CompanyId (GUID) instead of CompanyCode (string)
-- This ensures consistency with all other tables
-- ============================================================================
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,  -- ✅ FIXED: Now uses CompanyId instead of CompanyCode
    Email NVARCHAR(200) NOT NULL,
    PasswordHash NVARCHAR(500) NOT NULL,
    FullName NVARCHAR(200) NOT NULL,
    [Role] INT NOT NULL,  -- 1=Admin, 2=Manager, 3=Seller
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    LastLoginAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    -- Constraints
    CONSTRAINT FK_Users_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Users_Email_Company UNIQUE (Email, CompanyId),
    CONSTRAINT CK_Users_Role CHECK ([Role] IN (1, 2, 3)),
    
    -- Indexes
    INDEX IX_Users_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_Users_Email NONCLUSTERED (Email) WHERE IsDeleted = 0,
    INDEX IX_Users_IsActive NONCLUSTERED (IsActive) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- STORE TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_Store_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Store UNIQUE (CompanyId, [Name]),
    
    INDEX IX_Store_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- SELLER TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_Seller_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Seller_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT UQ_Seller UNIQUE (CompanyId, SellerCode),
    
    INDEX IX_Seller_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_Seller_StoreId NONCLUSTERED (StoreId) WHERE IsDeleted = 0,
    INDEX IX_Seller_PhoneE164 NONCLUSTERED (PhoneE164) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- GOAL TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_Goal_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Goal_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT FK_Goal_Seller FOREIGN KEY (SellerId) REFERENCES Seller(Id),
    CONSTRAINT UQ_Goal UNIQUE (CompanyId, SellerId, [Month]),
    
    INDEX IX_Goal_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_Goal_SellerId NONCLUSTERED (SellerId) WHERE IsDeleted = 0,
    INDEX IX_Goal_Month NONCLUSTERED ([Month]) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- SALE TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_Sale_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Sale_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT FK_Sale_Seller FOREIGN KEY (SellerId) REFERENCES Seller(Id),
    
    INDEX IX_Sale_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_Sale_StoreId NONCLUSTERED (StoreId) WHERE IsDeleted = 0,
    INDEX IX_Sale_SellerId NONCLUSTERED (SellerId) WHERE IsDeleted = 0,
    INDEX IX_Sale_SaleDate NONCLUSTERED (SaleDate) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- IMPORT STATUS TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_ImportStatus_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    
    INDEX IX_ImportStatus_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- INSIGHT CACHE TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_InsightCache_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT FK_InsightCache_Store FOREIGN KEY (StoreId) REFERENCES Store(Id),
    CONSTRAINT FK_InsightCache_Seller FOREIGN KEY (SellerId) REFERENCES Seller(Id),
    
    INDEX IX_InsightCache_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_InsightCache_InsightDate NONCLUSTERED (InsightDate) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- REVIEW QUEUE TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_ReviewQueue_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    
    INDEX IX_ReviewQueue_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_ReviewQueue_Status NONCLUSTERED ([Status]) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- MESSAGE LOG TABLE
-- ============================================================================
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
    
    CONSTRAINT FK_MessageLog_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    
    INDEX IX_MessageLog_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_MessageLog_SentAt NONCLUSTERED (SentAt) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- CHAT MESSAGE TABLE - FIXED
-- ============================================================================
-- Now uses CompanyId (GUID) instead of CompanyCode (string)
-- This ensures consistency with all other tables
-- ============================================================================
CREATE TABLE ChatMessage (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CompanyId UNIQUEIDENTIFIER NOT NULL,  -- ✅ FIXED: Now uses CompanyId instead of CompanyCode
    UserId UNIQUEIDENTIFIER NOT NULL,
    UserMessage NVARCHAR(MAX) NOT NULL,
    AssistantResponse NVARCHAR(MAX) NOT NULL,
    Intent INT NOT NULL,  -- IntentType enum
    Metadata NVARCHAR(MAX) NULL,  -- JSON with additional context
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    
    CONSTRAINT FK_ChatMessage_Company FOREIGN KEY (CompanyId) REFERENCES Company(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ChatMessage_User FOREIGN KEY (UserId) REFERENCES Users(Id),
    
    INDEX IX_ChatMessage_CompanyId NONCLUSTERED (CompanyId) WHERE IsDeleted = 0,
    INDEX IX_ChatMessage_UserId NONCLUSTERED (UserId) WHERE IsDeleted = 0,
    INDEX IX_ChatMessage_CreatedAt NONCLUSTERED (CreatedAt) WHERE IsDeleted = 0
);
GO

-- ============================================================================
-- SEED DATA
-- ============================================================================
-- Insert sample company for testing
DECLARE @CompanyId UNIQUEIDENTIFIER = NEWID();

INSERT INTO Company (Id, Code, [Name], [Description], IsActive)
VALUES (@CompanyId, 'ACME', 'ACME Corporation', 'Sample company for testing', 1);

-- Insert sample admin user (password: Admin@123)
-- Note: Password hash must be generated by application using PBKDF2
INSERT INTO Users (CompanyId, Email, PasswordHash, FullName, [Role], IsActive)
VALUES (
    @CompanyId,
    'admin@acme.com',
    'REPLACE_WITH_HASHED_PASSWORD', -- Replace with actual hashed password from application
    'Admin User',
    1,  -- Admin role
    1
);

GO

PRINT '✅ Database schema created successfully!';
PRINT '✅ All tables now use CompanyId (GUID) for consistency';
PRINT '✅ CompanyCode maintained as business identifier only';
PRINT '';
PRINT '📊 Sample data created:';
PRINT '   - Company: ACME';
PRINT '   - User: admin@acme.com (password needs to be hashed by application)';
PRINT '';
PRINT '🔑 Key Architectural Decisions:';
PRINT '   1. CompanyId (GUID) = Technical key for all foreign keys';
PRINT '   2. CompanyCode (string) = Business key for external APIs';
PRINT '   3. All tables use CompanyId for consistency';
PRINT '   4. Both Id and Code are indexed for performance';
GO