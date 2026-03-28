USE GmaoBusHvac;
GO

SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sec.Users WHERE Username = 'admin')
BEGIN
    INSERT INTO sec.Users (Username, PasswordHash, PasswordSalt, FullName, Email, RoleCode, IsActive)
    VALUES ('admin', 'oEbW6/Zi1y/EnRPv60eezbD7sGfVMlUQ2Jq6LS3krRI=', 'gsWWymUgM1uOTv9zCKE2Pg==', 'Administrateur GMAO', 'admin@gmao.local', 'ADMIN', 1);
END

IF NOT EXISTS (SELECT 1 FROM sec.Users WHERE Username = 'tech1')
BEGIN
    INSERT INTO sec.Users (Username, PasswordHash, PasswordSalt, FullName, Email, RoleCode, IsActive)
    VALUES ('tech1', 'oNLico1Mgw/Kge/iH5TFoUQ+74Rf1+j3SscEtFkiRzU=', 'prfb9hVWPH/SBjvwBEyClQ==', 'Technicien Démonstration', 'tech1@gmao.local', 'TECHNICIAN', 1);
END

IF NOT EXISTS (SELECT 1 FROM crm.Clients WHERE Code = 'TRANSDEV')
BEGIN
    INSERT INTO crm.Clients (Code, Name, BillingAddressLine1, BillingPostalCode, BillingCity, BillingCountry, VatNumber, PaymentTermDays, DefaultHourlyRateExclTax, IsActive)
    VALUES ('TRANSDEV', 'Transdev Démo', '1 rue du Dépôt', '97400', 'Saint-Denis', 'France', 'FR00123456789', 30, 82.50, 1);
END

IF NOT EXISTS (
    SELECT 1
    FROM crm.Sites s
    INNER JOIN crm.Clients c ON c.ClientId = s.ClientId
    WHERE c.Code = 'TRANSDEV' AND s.Code = 'DEPOT-SUD')
BEGIN
    INSERT INTO crm.Sites (ClientId, Code, Name, AddressLine1, PostalCode, City, Country, Latitude, Longitude, ContactName, ContactPhone, IsActive)
    SELECT c.ClientId, 'DEPOT-SUD', 'Dépôt Sud', '12 avenue des Ateliers', '97410', 'Saint-Pierre', 'France', -21.339300, 55.478100, 'Chef Atelier', '0262 00 00 00', 1
    FROM crm.Clients c
    WHERE c.Code = 'TRANSDEV';
END

IF NOT EXISTS (SELECT 1 FROM stock.Warehouses WHERE Code = 'MAG-SUD')
BEGIN
    INSERT INTO stock.Warehouses (SiteId, Code, Name, IsActive)
    SELECT s.SiteId, 'MAG-SUD', 'Magasin Dépôt Sud', 1
    FROM crm.Sites s
    INNER JOIN crm.Clients c ON c.ClientId = s.ClientId
    WHERE c.Code = 'TRANSDEV' AND s.Code = 'DEPOT-SUD';
END

IF NOT EXISTS (SELECT 1 FROM buy.Suppliers WHERE Code = 'HVACPARTS')
BEGIN
    INSERT INTO buy.Suppliers (Code, Name, Email, Phone, AddressLine1, PostalCode, City, Country, IsActive)
    VALUES ('HVACPARTS', 'HVAC Parts Océan Indien', 'sales@hvacparts.local', '0262 11 22 33', '5 rue des Fournisseurs', '97420', 'Le Port', 'France', 1);
END

IF NOT EXISTS (SELECT 1 FROM stock.Parts WHERE PartNumber = 'FILTRE-CABINE-01')
BEGIN
    INSERT INTO stock.Parts (PartNumber, Name, Description, UnitCode, PurchasePriceExclTax, SalePriceExclTax, MinimumStock, IsActive)
    VALUES ('FILTRE-CABINE-01', 'Filtre cabine climatisation', 'Filtre standard bus urbain', 'EA', 12.50, 24.90, 5, 1);
END

IF NOT EXISTS (SELECT 1 FROM stock.Parts WHERE PartNumber = 'COURROIE-COMP-01')
BEGIN
    INSERT INTO stock.Parts (PartNumber, Name, Description, UnitCode, PurchasePriceExclTax, SalePriceExclTax, MinimumStock, IsActive)
    VALUES ('COURROIE-COMP-01', 'Courroie compresseur', 'Courroie de remplacement compresseur climatisation', 'EA', 19.80, 39.50, 3, 1);
END

IF NOT EXISTS (
    SELECT 1
    FROM asset.Buses b
    INNER JOIN crm.Clients c ON c.ClientId = b.ClientId
    WHERE c.Code = 'TRANSDEV' AND b.FleetNumber = 'BUS-001')
BEGIN
    INSERT INTO asset.Buses (ClientId, SiteId, FleetNumber, RegistrationNumber, Vin, Brand, Model, YearOfManufacture, CurrentMileageKm, StatusCode, Notes)
    SELECT c.ClientId, s.SiteId, 'BUS-001', 'AB-123-CD', 'VF1BUS001HVAC0001', 'Iveco', 'Urbanway', 2023, 84500, 'ACTIVE', 'Bus de démonstration'
    FROM crm.Clients c
    INNER JOIN crm.Sites s ON s.ClientId = c.ClientId AND s.Code = 'DEPOT-SUD'
    WHERE c.Code = 'TRANSDEV';
END

IF NOT EXISTS (
    SELECT 1
    FROM asset.BusEquipments e
    INNER JOIN asset.Buses b ON b.BusId = e.BusId
    WHERE b.FleetNumber = 'BUS-001' AND e.EquipmentTypeCode = 'AIR_CONDITIONING')
BEGIN
    INSERT INTO asset.BusEquipments (BusId, EquipmentTypeCode, Manufacturer, Model, SerialNumber, CommissioningDate, StatusCode, LastPreventiveDate, NextPreventiveDate)
    SELECT b.BusId, 'AIR_CONDITIONING', 'Thermo King', 'AC-ROOF-500', 'TK-AC-BUS001', '2023-03-15', 'ACTIVE', '2026-02-15', '2026-05-15'
    FROM asset.Buses b
    WHERE b.FleetNumber = 'BUS-001';
END

IF NOT EXISTS (
    SELECT 1
    FROM asset.BusEquipments e
    INNER JOIN asset.Buses b ON b.BusId = e.BusId
    WHERE b.FleetNumber = 'BUS-001' AND e.EquipmentTypeCode = 'HEATING')
BEGIN
    INSERT INTO asset.BusEquipments (BusId, EquipmentTypeCode, Manufacturer, Model, SerialNumber, CommissioningDate, StatusCode, LastPreventiveDate, NextPreventiveDate)
    SELECT b.BusId, 'HEATING', 'Webasto', 'HEAT-BUS-200', 'WEB-HEAT-BUS001', '2023-03-15', 'ACTIVE', '2026-02-10', '2026-08-10'
    FROM asset.Buses b
    WHERE b.FleetNumber = 'BUS-001';
END

IF NOT EXISTS (SELECT 1 FROM ops.PreventivePlans WHERE PlanName = 'Contrôle trimestriel climatisation BUS-001')
BEGIN
    INSERT INTO ops.PreventivePlans (EquipmentId, PlanName, FrequencyTypeCode, FrequencyValue, EstimatedDurationMinutes, ChecklistTemplate, IsActive)
    SELECT e.EquipmentId,
           'Contrôle trimestriel climatisation BUS-001',
           'MONTH',
           3,
           90,
           N'["Contrôler pression","Nettoyer filtres","Vérifier condensats","Tester soufflage"]',
           1
    FROM asset.BusEquipments e
    INNER JOIN asset.Buses b ON b.BusId = e.BusId
    WHERE b.FleetNumber = 'BUS-001' AND e.EquipmentTypeCode = 'AIR_CONDITIONING';
END

IF NOT EXISTS (
    SELECT 1
    FROM stock.StockMovements sm
    INNER JOIN stock.Parts p ON p.PartId = sm.PartId
    WHERE p.PartNumber = 'FILTRE-CABINE-01' AND sm.Reference = 'SEED-INITIAL-STOCK')
BEGIN
    INSERT INTO stock.StockMovements (PartId, WarehouseId, MovementTypeCode, Quantity, UnitCost, Reference, PerformedByUserId)
    SELECT p.PartId, w.WarehouseId, 'IN', 25, 12.50, 'SEED-INITIAL-STOCK', u.UserId
    FROM stock.Parts p
    CROSS JOIN stock.Warehouses w
    CROSS JOIN sec.Users u
    WHERE p.PartNumber = 'FILTRE-CABINE-01' AND w.Code = 'MAG-SUD' AND u.Username = 'admin';
END

IF NOT EXISTS (
    SELECT 1
    FROM stock.StockMovements sm
    INNER JOIN stock.Parts p ON p.PartId = sm.PartId
    WHERE p.PartNumber = 'COURROIE-COMP-01' AND sm.Reference = 'SEED-INITIAL-STOCK')
BEGIN
    INSERT INTO stock.StockMovements (PartId, WarehouseId, MovementTypeCode, Quantity, UnitCost, Reference, PerformedByUserId)
    SELECT p.PartId, w.WarehouseId, 'IN', 10, 19.80, 'SEED-INITIAL-STOCK', u.UserId
    FROM stock.Parts p
    CROSS JOIN stock.Warehouses w
    CROSS JOIN sec.Users u
    WHERE p.PartNumber = 'COURROIE-COMP-01' AND w.Code = 'MAG-SUD' AND u.Username = 'admin';
END

IF NOT EXISTS (SELECT 1 FROM ops.WorkOrders WHERE Title = 'Diagnostic climatisation insuffisante BUS-001')
BEGIN
    DECLARE @WorkOrderNumber NVARCHAR(30);
    DECLARE @Sequence INT = NEXT VALUE FOR ops.WorkOrderNumberSeq;
    SET @WorkOrderNumber = CONCAT('WO-', YEAR(SYSUTCDATETIME()), '-', RIGHT(REPLICATE('0', 6) + CAST(@Sequence AS VARCHAR(6)), 6));

    INSERT INTO ops.WorkOrders
    (
        WorkOrderNumber, TypeCode, StatusCode, PriorityCode, ClientId, SiteId, BusId, EquipmentId,
        Title, Description, ScheduledStartUtc, ScheduledEndUtc, AssignedTechnicianId, OpenedByUserId
    )
    SELECT
        @WorkOrderNumber,
        'CURATIVE',
        'PLANNED',
        'HIGH',
        c.ClientId,
        s.SiteId,
        b.BusId,
        e.EquipmentId,
        'Diagnostic climatisation insuffisante BUS-001',
        'Le soufflage froid est insuffisant en exploitation.',
        DATEADD(DAY, 1, SYSUTCDATETIME()),
        DATEADD(HOUR, 2, DATEADD(DAY, 1, SYSUTCDATETIME())),
        tech.UserId,
        admin.UserId
    FROM crm.Clients c
    INNER JOIN crm.Sites s ON s.ClientId = c.ClientId AND s.Code = 'DEPOT-SUD'
    INNER JOIN asset.Buses b ON b.ClientId = c.ClientId AND b.FleetNumber = 'BUS-001'
    INNER JOIN asset.BusEquipments e ON e.BusId = b.BusId AND e.EquipmentTypeCode = 'AIR_CONDITIONING'
    CROSS JOIN sec.Users tech
    CROSS JOIN sec.Users admin
    WHERE c.Code = 'TRANSDEV' AND tech.Username = 'tech1' AND admin.Username = 'admin';
END
GO
