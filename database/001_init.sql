IF DB_ID(N'GmaoBusHvac') IS NULL
BEGIN
    CREATE DATABASE GmaoBusHvac;
END
GO

USE GmaoBusHvac;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'sec') EXEC('CREATE SCHEMA sec');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'crm') EXEC('CREATE SCHEMA crm');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'asset') EXEC('CREATE SCHEMA asset');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'ops') EXEC('CREATE SCHEMA ops');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'stock') EXEC('CREATE SCHEMA stock');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'buy') EXEC('CREATE SCHEMA buy');
IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = 'bill') EXEC('CREATE SCHEMA bill');
GO

IF OBJECT_ID('bill.PaymentAllocations', 'U') IS NOT NULL DROP TABLE bill.PaymentAllocations;
IF OBJECT_ID('bill.Payments', 'U') IS NOT NULL DROP TABLE bill.Payments;
IF OBJECT_ID('bill.InvoiceWorkOrders', 'U') IS NOT NULL DROP TABLE bill.InvoiceWorkOrders;
IF OBJECT_ID('bill.InvoiceLines', 'U') IS NOT NULL DROP TABLE bill.InvoiceLines;
IF OBJECT_ID('bill.Invoices', 'U') IS NOT NULL DROP TABLE bill.Invoices;
IF OBJECT_ID('stock.StockMovements', 'U') IS NOT NULL DROP TABLE stock.StockMovements;
IF OBJECT_ID('buy.PurchaseOrderLines', 'U') IS NOT NULL DROP TABLE buy.PurchaseOrderLines;
IF OBJECT_ID('buy.PurchaseOrders', 'U') IS NOT NULL DROP TABLE buy.PurchaseOrders;
IF OBJECT_ID('buy.Suppliers', 'U') IS NOT NULL DROP TABLE buy.Suppliers;
IF OBJECT_ID('ops.TimeEntries', 'U') IS NOT NULL DROP TABLE ops.TimeEntries;
IF OBJECT_ID('ops.WorkOrderParts', 'U') IS NOT NULL DROP TABLE ops.WorkOrderParts;
IF OBJECT_ID('ops.WorkOrders', 'U') IS NOT NULL DROP TABLE ops.WorkOrders;
IF OBJECT_ID('ops.PreventivePlans', 'U') IS NOT NULL DROP TABLE ops.PreventivePlans;
IF OBJECT_ID('asset.BusEquipments', 'U') IS NOT NULL DROP TABLE asset.BusEquipments;
IF OBJECT_ID('asset.Buses', 'U') IS NOT NULL DROP TABLE asset.Buses;
IF OBJECT_ID('stock.Parts', 'U') IS NOT NULL DROP TABLE stock.Parts;
IF OBJECT_ID('stock.Warehouses', 'U') IS NOT NULL DROP TABLE stock.Warehouses;
IF OBJECT_ID('crm.Sites', 'U') IS NOT NULL DROP TABLE crm.Sites;
IF OBJECT_ID('crm.Clients', 'U') IS NOT NULL DROP TABLE crm.Clients;
IF OBJECT_ID('sec.Users', 'U') IS NOT NULL DROP TABLE sec.Users;
GO

IF OBJECT_ID('bill.vInvoiceBalance', 'V') IS NOT NULL DROP VIEW bill.vInvoiceBalance;
IF OBJECT_ID('stock.vCurrentStock', 'V') IS NOT NULL DROP VIEW stock.vCurrentStock;
GO

IF OBJECT_ID('ops.WorkOrderNumberSeq', 'SO') IS NOT NULL DROP SEQUENCE ops.WorkOrderNumberSeq;
IF OBJECT_ID('buy.PurchaseOrderNumberSeq', 'SO') IS NOT NULL DROP SEQUENCE buy.PurchaseOrderNumberSeq;
IF OBJECT_ID('bill.InvoiceNumberSeq', 'SO') IS NOT NULL DROP SEQUENCE bill.InvoiceNumberSeq;
GO

CREATE TABLE sec.Users (
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_sec_Users PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    PasswordSalt NVARCHAR(128) NOT NULL,
    FullName NVARCHAR(120) NOT NULL,
    Email NVARCHAR(120) NULL,
    Phone NVARCHAR(40) NULL,
    RoleCode NVARCHAR(20) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_sec_Users_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_sec_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    LastLoginAtUtc DATETIME2(0) NULL,
    CONSTRAINT UQ_sec_Users_Username UNIQUE (Username),
    CONSTRAINT CK_sec_Users_Role CHECK (RoleCode IN ('ADMIN', 'TECHNICIAN'))
);
GO

CREATE UNIQUE INDEX IX_sec_Users_Email ON sec.Users(Email) WHERE Email IS NOT NULL;
GO

CREATE TABLE crm.Clients (
    ClientId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_crm_Clients PRIMARY KEY,
    Code NVARCHAR(30) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    BillingAddressLine1 NVARCHAR(150) NULL,
    BillingAddressLine2 NVARCHAR(150) NULL,
    BillingPostalCode NVARCHAR(20) NULL,
    BillingCity NVARCHAR(80) NULL,
    BillingCountry NVARCHAR(80) NULL,
    VatNumber NVARCHAR(50) NULL,
    PaymentTermDays INT NOT NULL CONSTRAINT DF_crm_Clients_PaymentTerm DEFAULT 30,
    DefaultHourlyRateExclTax DECIMAL(18,2) NOT NULL CONSTRAINT DF_crm_Clients_HourlyRate DEFAULT 75,
    IsActive BIT NOT NULL CONSTRAINT DF_crm_Clients_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_crm_Clients_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_crm_Clients_Code UNIQUE (Code)
);
GO

CREATE TABLE crm.Sites (
    SiteId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_crm_Sites PRIMARY KEY,
    ClientId INT NOT NULL,
    Code NVARCHAR(30) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    AddressLine1 NVARCHAR(150) NULL,
    AddressLine2 NVARCHAR(150) NULL,
    PostalCode NVARCHAR(20) NULL,
    City NVARCHAR(80) NULL,
    Country NVARCHAR(80) NULL,
    Latitude DECIMAL(9,6) NULL,
    Longitude DECIMAL(9,6) NULL,
    ContactName NVARCHAR(120) NULL,
    ContactPhone NVARCHAR(40) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_crm_Sites_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_crm_Sites_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_crm_Sites_Client FOREIGN KEY (ClientId) REFERENCES crm.Clients(ClientId),
    CONSTRAINT UQ_crm_Sites_Client_Code UNIQUE (ClientId, Code)
);
GO

CREATE TABLE asset.Buses (
    BusId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_asset_Buses PRIMARY KEY,
    ClientId INT NOT NULL,
    SiteId INT NULL,
    FleetNumber NVARCHAR(50) NOT NULL,
    RegistrationNumber NVARCHAR(30) NULL,
    Vin NVARCHAR(50) NULL,
    Brand NVARCHAR(80) NULL,
    Model NVARCHAR(80) NULL,
    YearOfManufacture INT NULL,
    CurrentMileageKm INT NOT NULL CONSTRAINT DF_asset_Buses_Mileage DEFAULT 0,
    StatusCode NVARCHAR(30) NOT NULL CONSTRAINT DF_asset_Buses_Status DEFAULT 'ACTIVE',
    Notes NVARCHAR(4000) NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_asset_Buses_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_asset_Buses_Client FOREIGN KEY (ClientId) REFERENCES crm.Clients(ClientId),
    CONSTRAINT FK_asset_Buses_Site FOREIGN KEY (SiteId) REFERENCES crm.Sites(SiteId),
    CONSTRAINT UQ_asset_Buses_Client_Fleet UNIQUE (ClientId, FleetNumber),
    CONSTRAINT CK_asset_Buses_Status CHECK (StatusCode IN ('ACTIVE', 'IN_SERVICE', 'OUT_OF_SERVICE', 'SCRAPPED'))
);
GO

CREATE TABLE asset.BusEquipments (
    EquipmentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_asset_BusEquipments PRIMARY KEY,
    BusId INT NOT NULL,
    EquipmentTypeCode NVARCHAR(30) NOT NULL,
    Manufacturer NVARCHAR(80) NULL,
    Model NVARCHAR(80) NULL,
    SerialNumber NVARCHAR(80) NULL,
    CommissioningDate DATE NULL,
    StatusCode NVARCHAR(20) NOT NULL CONSTRAINT DF_asset_BusEquipments_Status DEFAULT 'ACTIVE',
    LastPreventiveDate DATE NULL,
    NextPreventiveDate DATE NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_asset_BusEquipments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_asset_BusEquipments_Bus FOREIGN KEY (BusId) REFERENCES asset.Buses(BusId),
    CONSTRAINT CK_asset_BusEquipments_Type CHECK (EquipmentTypeCode IN ('AIR_CONDITIONING', 'HEATING', 'VENTILATION')),
    CONSTRAINT CK_asset_BusEquipments_Status CHECK (StatusCode IN ('ACTIVE', 'INACTIVE', 'FAULTY'))
);
GO

CREATE UNIQUE INDEX IX_asset_BusEquipments_SerialNumber ON asset.BusEquipments(SerialNumber) WHERE SerialNumber IS NOT NULL;
GO

CREATE TABLE stock.Warehouses (
    WarehouseId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_stock_Warehouses PRIMARY KEY,
    SiteId INT NULL,
    Code NVARCHAR(30) NOT NULL,
    Name NVARCHAR(100) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_stock_Warehouses_IsActive DEFAULT 1,
    CONSTRAINT FK_stock_Warehouses_Site FOREIGN KEY (SiteId) REFERENCES crm.Sites(SiteId),
    CONSTRAINT UQ_stock_Warehouses_Code UNIQUE (Code)
);
GO

CREATE TABLE stock.Parts (
    PartId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_stock_Parts PRIMARY KEY,
    PartNumber NVARCHAR(50) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(4000) NULL,
    UnitCode NVARCHAR(20) NOT NULL CONSTRAINT DF_stock_Parts_Unit DEFAULT 'EA',
    PurchasePriceExclTax DECIMAL(18,2) NOT NULL CONSTRAINT DF_stock_Parts_Purchase DEFAULT 0,
    SalePriceExclTax DECIMAL(18,2) NOT NULL CONSTRAINT DF_stock_Parts_Sale DEFAULT 0,
    MinimumStock DECIMAL(18,3) NOT NULL CONSTRAINT DF_stock_Parts_Minimum DEFAULT 0,
    IsActive BIT NOT NULL CONSTRAINT DF_stock_Parts_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_stock_Parts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_stock_Parts_PartNumber UNIQUE (PartNumber)
);
GO

CREATE TABLE buy.Suppliers (
    SupplierId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_buy_Suppliers PRIMARY KEY,
    Code NVARCHAR(30) NOT NULL,
    Name NVARCHAR(150) NOT NULL,
    Email NVARCHAR(120) NULL,
    Phone NVARCHAR(40) NULL,
    AddressLine1 NVARCHAR(150) NULL,
    PostalCode NVARCHAR(20) NULL,
    City NVARCHAR(80) NULL,
    Country NVARCHAR(80) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_buy_Suppliers_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_buy_Suppliers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_buy_Suppliers_Code UNIQUE (Code)
);
GO

CREATE SEQUENCE ops.WorkOrderNumberSeq AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE buy.PurchaseOrderNumberSeq AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE bill.InvoiceNumberSeq AS INT START WITH 1 INCREMENT BY 1;
GO

CREATE TABLE ops.PreventivePlans (
    PreventivePlanId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ops_PreventivePlans PRIMARY KEY,
    EquipmentId INT NOT NULL,
    PlanName NVARCHAR(120) NOT NULL,
    FrequencyTypeCode NVARCHAR(20) NOT NULL,
    FrequencyValue INT NOT NULL,
    EstimatedDurationMinutes INT NOT NULL CONSTRAINT DF_ops_PreventivePlans_Duration DEFAULT 60,
    ChecklistTemplate NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ops_PreventivePlans_IsActive DEFAULT 1,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ops_PreventivePlans_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ops_PreventivePlans_Equipment FOREIGN KEY (EquipmentId) REFERENCES asset.BusEquipments(EquipmentId),
    CONSTRAINT CK_ops_PreventivePlans_Frequency CHECK (FrequencyTypeCode IN ('MONTH', 'DAY', 'KM'))
);
GO

CREATE TABLE ops.WorkOrders (
    WorkOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ops_WorkOrders PRIMARY KEY,
    WorkOrderNumber NVARCHAR(30) NOT NULL,
    TypeCode NVARCHAR(20) NOT NULL,
    StatusCode NVARCHAR(20) NOT NULL CONSTRAINT DF_ops_WorkOrders_Status DEFAULT 'OPEN',
    PriorityCode NVARCHAR(20) NOT NULL CONSTRAINT DF_ops_WorkOrders_Priority DEFAULT 'NORMAL',
    ClientId INT NOT NULL,
    SiteId INT NULL,
    BusId INT NULL,
    EquipmentId INT NULL,
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    ReportedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ops_WorkOrders_ReportedAt DEFAULT SYSUTCDATETIME(),
    ScheduledStartUtc DATETIME2(0) NULL,
    ScheduledEndUtc DATETIME2(0) NULL,
    AssignedTechnicianId INT NULL,
    OpenedByUserId INT NOT NULL,
    ClosedAtUtc DATETIME2(0) NULL,
    ResolutionNotes NVARCHAR(MAX) NULL,
    LaborMinutes INT NOT NULL CONSTRAINT DF_ops_WorkOrders_Labor DEFAULT 0,
    TravelKm DECIMAL(10,2) NOT NULL CONSTRAINT DF_ops_WorkOrders_Travel DEFAULT 0,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ops_WorkOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_ops_WorkOrders_Number UNIQUE (WorkOrderNumber),
    CONSTRAINT FK_ops_WorkOrders_Client FOREIGN KEY (ClientId) REFERENCES crm.Clients(ClientId),
    CONSTRAINT FK_ops_WorkOrders_Site FOREIGN KEY (SiteId) REFERENCES crm.Sites(SiteId),
    CONSTRAINT FK_ops_WorkOrders_Bus FOREIGN KEY (BusId) REFERENCES asset.Buses(BusId),
    CONSTRAINT FK_ops_WorkOrders_Equipment FOREIGN KEY (EquipmentId) REFERENCES asset.BusEquipments(EquipmentId),
    CONSTRAINT FK_ops_WorkOrders_AssignedTechnician FOREIGN KEY (AssignedTechnicianId) REFERENCES sec.Users(UserId),
    CONSTRAINT FK_ops_WorkOrders_OpenedBy FOREIGN KEY (OpenedByUserId) REFERENCES sec.Users(UserId),
    CONSTRAINT CK_ops_WorkOrders_Type CHECK (TypeCode IN ('PREVENTIVE', 'CURATIVE')),
    CONSTRAINT CK_ops_WorkOrders_Status CHECK (StatusCode IN ('OPEN', 'PLANNED', 'IN_PROGRESS', 'DONE', 'CANCELLED')),
    CONSTRAINT CK_ops_WorkOrders_Priority CHECK (PriorityCode IN ('LOW', 'NORMAL', 'HIGH', 'CRITICAL'))
);
GO

CREATE INDEX IX_ops_WorkOrders_AssignedTechnician_Status ON ops.WorkOrders(AssignedTechnicianId, StatusCode, ScheduledStartUtc);
GO

CREATE TABLE ops.TimeEntries (
    TimeEntryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ops_TimeEntries PRIMARY KEY,
    TechnicianId INT NOT NULL,
    WorkOrderId INT NULL,
    StartAtUtc DATETIME2(0) NOT NULL,
    EndAtUtc DATETIME2(0) NULL,
    StartLatitude DECIMAL(9,6) NULL,
    StartLongitude DECIMAL(9,6) NULL,
    StartAccuracyMeters DECIMAL(10,2) NULL,
    EndLatitude DECIMAL(9,6) NULL,
    EndLongitude DECIMAL(9,6) NULL,
    EndAccuracyMeters DECIMAL(10,2) NULL,
    Notes NVARCHAR(2000) NULL,
    StatusCode NVARCHAR(20) NOT NULL CONSTRAINT DF_ops_TimeEntries_Status DEFAULT 'OPEN',
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ops_TimeEntries_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_ops_TimeEntries_Technician FOREIGN KEY (TechnicianId) REFERENCES sec.Users(UserId),
    CONSTRAINT FK_ops_TimeEntries_WorkOrder FOREIGN KEY (WorkOrderId) REFERENCES ops.WorkOrders(WorkOrderId),
    CONSTRAINT CK_ops_TimeEntries_Status CHECK (StatusCode IN ('OPEN', 'CLOSED', 'CANCELLED'))
);
GO

CREATE INDEX IX_ops_TimeEntries_Technician_StartAt ON ops.TimeEntries(TechnicianId, StartAtUtc DESC);
GO

CREATE TABLE buy.PurchaseOrders (
    PurchaseOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_buy_PurchaseOrders PRIMARY KEY,
    PurchaseOrderNumber NVARCHAR(30) NOT NULL,
    SupplierId INT NOT NULL,
    SiteId INT NULL,
    StatusCode NVARCHAR(30) NOT NULL CONSTRAINT DF_buy_PurchaseOrders_Status DEFAULT 'DRAFT',
    OrderedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_buy_PurchaseOrders_OrderedAt DEFAULT SYSUTCDATETIME(),
    ExpectedAtUtc DATETIME2(0) NULL,
    Notes NVARCHAR(2000) NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_buy_PurchaseOrders_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_buy_PurchaseOrders_Number UNIQUE (PurchaseOrderNumber),
    CONSTRAINT FK_buy_PurchaseOrders_Supplier FOREIGN KEY (SupplierId) REFERENCES buy.Suppliers(SupplierId),
    CONSTRAINT FK_buy_PurchaseOrders_Site FOREIGN KEY (SiteId) REFERENCES crm.Sites(SiteId),
    CONSTRAINT FK_buy_PurchaseOrders_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES sec.Users(UserId),
    CONSTRAINT CK_buy_PurchaseOrders_Status CHECK (StatusCode IN ('DRAFT', 'ORDERED', 'PARTIALLY_RECEIVED', 'RECEIVED', 'CANCELLED'))
);
GO

CREATE TABLE buy.PurchaseOrderLines (
    PurchaseOrderLineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_buy_PurchaseOrderLines PRIMARY KEY,
    PurchaseOrderId INT NOT NULL,
    PartId INT NOT NULL,
    QuantityOrdered DECIMAL(18,3) NOT NULL,
    QuantityReceived DECIMAL(18,3) NOT NULL CONSTRAINT DF_buy_PurchaseOrderLines_Received DEFAULT 0,
    UnitPriceExclTax DECIMAL(18,2) NOT NULL,
    TaxRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_buy_PurchaseOrderLines_TaxRate DEFAULT 20,
    CONSTRAINT FK_buy_PurchaseOrderLines_Order FOREIGN KEY (PurchaseOrderId) REFERENCES buy.PurchaseOrders(PurchaseOrderId),
    CONSTRAINT FK_buy_PurchaseOrderLines_Part FOREIGN KEY (PartId) REFERENCES stock.Parts(PartId)
);
GO

CREATE TABLE ops.WorkOrderParts (
    WorkOrderPartId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ops_WorkOrderParts PRIMARY KEY,
    WorkOrderId INT NOT NULL,
    PartId INT NOT NULL,
    WarehouseId INT NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL,
    UnitSalePrice DECIMAL(18,2) NOT NULL,
    ConsumedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_ops_WorkOrderParts_ConsumedAt DEFAULT SYSUTCDATETIME(),
    ConsumedByUserId INT NOT NULL,
    CONSTRAINT FK_ops_WorkOrderParts_WorkOrder FOREIGN KEY (WorkOrderId) REFERENCES ops.WorkOrders(WorkOrderId),
    CONSTRAINT FK_ops_WorkOrderParts_Part FOREIGN KEY (PartId) REFERENCES stock.Parts(PartId),
    CONSTRAINT FK_ops_WorkOrderParts_Warehouse FOREIGN KEY (WarehouseId) REFERENCES stock.Warehouses(WarehouseId),
    CONSTRAINT FK_ops_WorkOrderParts_User FOREIGN KEY (ConsumedByUserId) REFERENCES sec.Users(UserId)
);
GO

CREATE TABLE stock.StockMovements (
    StockMovementId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_stock_StockMovements PRIMARY KEY,
    PartId INT NOT NULL,
    WarehouseId INT NOT NULL,
    WorkOrderId INT NULL,
    PurchaseOrderLineId INT NULL,
    MovementTypeCode NVARCHAR(20) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_stock_StockMovements_UnitCost DEFAULT 0,
    Reference NVARCHAR(120) NULL,
    OccurredAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_stock_StockMovements_OccurredAt DEFAULT SYSUTCDATETIME(),
    PerformedByUserId INT NULL,
    CONSTRAINT FK_stock_StockMovements_Part FOREIGN KEY (PartId) REFERENCES stock.Parts(PartId),
    CONSTRAINT FK_stock_StockMovements_Warehouse FOREIGN KEY (WarehouseId) REFERENCES stock.Warehouses(WarehouseId),
    CONSTRAINT FK_stock_StockMovements_WorkOrder FOREIGN KEY (WorkOrderId) REFERENCES ops.WorkOrders(WorkOrderId),
    CONSTRAINT FK_stock_StockMovements_PurchaseOrderLine FOREIGN KEY (PurchaseOrderLineId) REFERENCES buy.PurchaseOrderLines(PurchaseOrderLineId),
    CONSTRAINT FK_stock_StockMovements_User FOREIGN KEY (PerformedByUserId) REFERENCES sec.Users(UserId),
    CONSTRAINT CK_stock_StockMovements_Type CHECK (MovementTypeCode IN ('IN', 'OUT', 'ADJUSTMENT'))
);
GO

CREATE INDEX IX_stock_StockMovements_Part_Warehouse ON stock.StockMovements(PartId, WarehouseId);
GO

CREATE TABLE bill.Invoices (
    InvoiceId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bill_Invoices PRIMARY KEY,
    InvoiceNumber NVARCHAR(30) NOT NULL,
    ClientId INT NOT NULL,
    SiteId INT NULL,
    StatusCode NVARCHAR(30) NOT NULL CONSTRAINT DF_bill_Invoices_Status DEFAULT 'DRAFT',
    IssueDate DATE NOT NULL,
    DueDate DATE NOT NULL,
    CurrencyCode CHAR(3) NOT NULL CONSTRAINT DF_bill_Invoices_Currency DEFAULT 'EUR',
    Notes NVARCHAR(2000) NULL,
    CreatedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_bill_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_bill_Invoices_Number UNIQUE (InvoiceNumber),
    CONSTRAINT FK_bill_Invoices_Client FOREIGN KEY (ClientId) REFERENCES crm.Clients(ClientId),
    CONSTRAINT FK_bill_Invoices_Site FOREIGN KEY (SiteId) REFERENCES crm.Sites(SiteId),
    CONSTRAINT FK_bill_Invoices_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES sec.Users(UserId),
    CONSTRAINT CK_bill_Invoices_Status CHECK (StatusCode IN ('DRAFT', 'ISSUED', 'PARTIALLY_PAID', 'PAID', 'CANCELLED'))
);
GO

CREATE TABLE bill.InvoiceLines (
    InvoiceLineId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bill_InvoiceLines PRIMARY KEY,
    InvoiceId INT NOT NULL,
    Description NVARCHAR(250) NOT NULL,
    Quantity DECIMAL(18,3) NOT NULL,
    UnitPriceExclTax DECIMAL(18,2) NOT NULL,
    TaxRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_bill_InvoiceLines_TaxRate DEFAULT 20,
    SortOrder INT NOT NULL CONSTRAINT DF_bill_InvoiceLines_Sort DEFAULT 1,
    CONSTRAINT FK_bill_InvoiceLines_Invoice FOREIGN KEY (InvoiceId) REFERENCES bill.Invoices(InvoiceId)
);
GO

CREATE TABLE bill.InvoiceWorkOrders (
    InvoiceWorkOrderId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bill_InvoiceWorkOrders PRIMARY KEY,
    InvoiceId INT NOT NULL,
    WorkOrderId INT NOT NULL,
    CONSTRAINT FK_bill_InvoiceWorkOrders_Invoice FOREIGN KEY (InvoiceId) REFERENCES bill.Invoices(InvoiceId),
    CONSTRAINT FK_bill_InvoiceWorkOrders_WorkOrder FOREIGN KEY (WorkOrderId) REFERENCES ops.WorkOrders(WorkOrderId),
    CONSTRAINT UQ_bill_InvoiceWorkOrders_WorkOrder UNIQUE (WorkOrderId)
);
GO

CREATE TABLE bill.Payments (
    PaymentId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bill_Payments PRIMARY KEY,
    ClientId INT NOT NULL,
    PaymentDate DATE NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    MethodCode NVARCHAR(20) NOT NULL,
    Reference NVARCHAR(100) NULL,
    Notes NVARCHAR(1000) NULL,
    ReceivedByUserId INT NOT NULL,
    CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_bill_Payments_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_bill_Payments_Client FOREIGN KEY (ClientId) REFERENCES crm.Clients(ClientId),
    CONSTRAINT FK_bill_Payments_User FOREIGN KEY (ReceivedByUserId) REFERENCES sec.Users(UserId),
    CONSTRAINT CK_bill_Payments_Method CHECK (MethodCode IN ('TRANSFER', 'CHECK', 'CARD', 'CASH'))
);
GO

CREATE TABLE bill.PaymentAllocations (
    PaymentAllocationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_bill_PaymentAllocations PRIMARY KEY,
    PaymentId INT NOT NULL,
    InvoiceId INT NOT NULL,
    AmountAllocated DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_bill_PaymentAllocations_Payment FOREIGN KEY (PaymentId) REFERENCES bill.Payments(PaymentId),
    CONSTRAINT FK_bill_PaymentAllocations_Invoice FOREIGN KEY (InvoiceId) REFERENCES bill.Invoices(InvoiceId)
);
GO

CREATE VIEW stock.vCurrentStock
AS
SELECT
    sm.PartId,
    sm.WarehouseId,
    SUM(CASE sm.MovementTypeCode WHEN 'IN' THEN sm.Quantity WHEN 'OUT' THEN -sm.Quantity ELSE sm.Quantity END) AS QuantityOnHand
FROM stock.StockMovements sm
GROUP BY sm.PartId, sm.WarehouseId;
GO

CREATE VIEW bill.vInvoiceBalance
AS
SELECT
    i.InvoiceId,
    COALESCE(SUM(il.Quantity * il.UnitPriceExclTax), 0) AS TotalExclTax,
    COALESCE(SUM(il.Quantity * il.UnitPriceExclTax * il.TaxRate / 100.0), 0) AS TaxAmount,
    COALESCE(SUM(il.Quantity * il.UnitPriceExclTax * (1 + il.TaxRate / 100.0)), 0) AS TotalInclTax,
    COALESCE(pa.PaidAmount, 0) AS PaidAmount,
    COALESCE(SUM(il.Quantity * il.UnitPriceExclTax * (1 + il.TaxRate / 100.0)), 0) - COALESCE(pa.PaidAmount, 0) AS Balance
FROM bill.Invoices i
LEFT JOIN bill.InvoiceLines il ON il.InvoiceId = i.InvoiceId
LEFT JOIN (
    SELECT InvoiceId, SUM(AmountAllocated) AS PaidAmount
    FROM bill.PaymentAllocations
    GROUP BY InvoiceId
) pa ON pa.InvoiceId = i.InvoiceId
GROUP BY i.InvoiceId, pa.PaidAmount;
GO
