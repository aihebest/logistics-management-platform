-- ═══════════════════════════════════════════════════════════════════════════
-- Logistics & Fleet Management Platform — Demo Seed Data
-- Run this AFTER EF Core migrations have created all tables.
-- Safe to run multiple times (uses IF NOT EXISTS / MERGE patterns).
-- ═══════════════════════════════════════════════════════════════════════════
-- Usage:
--   From SQL Server Management Studio or Azure Data Studio:
--     Connect to localhost,1433  |  User: sa  |  Password: from .env file
--     Open this file and execute (F5)
--   Via sqlcmd (inside running container):
--     docker exec -it logistics-db bash
--     sqlcmd -S localhost -U sa -P "LogisticsDemo@2026!" -d logistics-db -C -i /scripts/seed-demo-data.sql
-- ═══════════════════════════════════════════════════════════════════════════

USE [logistics-db];
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ───────────────────────────────────────────────────────────────────────────
-- GUARD: Skip seeding if data already exists
-- ───────────────────────────────────────────────────────────────────────────
IF (SELECT COUNT(*) FROM Users) > 0
BEGIN
    PRINT 'Seed data already present — skipping.';
    RETURN;
END
GO

PRINT 'Seeding demo data...';

-- ───────────────────────────────────────────────────────────────────────────
-- USERS (Drivers, Coordinators, Managers, Mechanic, Admin)
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO Users (Id, EntraObjectId, FullName, Email, PhoneNumber, Role, DriverStatus, LicenceNo, LicenceExpiry, IsActive, LastStatusChange, CreatedAt)
VALUES
-- Drivers
('A1000001-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'James Mokoena',     'j.mokoena@company.com',   '+27 82 111 0001', 'Driver', 'Available',    'GP-DL-001', '2027-06-30', 1, DATEADD(HOUR,-2,GETUTCDATE()),  DATEADD(DAY,-180,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000002', 'Sipho Dlamini',     's.dlamini@company.com',   '+27 83 111 0002', 'Driver', 'OnAssignment', 'GP-DL-002', '2026-12-31', 1, DATEADD(HOUR,-1,GETUTCDATE()),  DATEADD(DAY,-210,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000003', 'Thabo Nkosi',       't.nkosi@company.com',     '+27 84 111 0003', 'Driver', 'Available',    'GP-DL-003', '2028-03-31', 1, DATEADD(HOUR,-3,GETUTCDATE()),  DATEADD(DAY,-90,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000004', 'Nomsa Zulu',        'n.zulu@company.com',      '+27 71 111 0004', 'Driver', 'OnBreak',      'GP-DL-004', '2027-09-30', 1, DATEADD(MINUTE,-45,GETUTCDATE()), DATEADD(DAY,-150,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000005', 'Bongani Khumalo',   'b.khumalo@company.com',   '+27 73 111 0005', 'Driver', 'OnAssignment', 'GP-DL-005', '2026-08-31', 1, DATEADD(HOUR,-2,GETUTCDATE()),  DATEADD(DAY,-300,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000006', '00000000-0000-0000-0000-000000000006', 'Lungelo Mthembu',   'l.mthembu@company.com',   '+27 79 111 0006', 'Driver', 'OffDuty',      'GP-DL-006', '2027-11-30', 1, DATEADD(HOUR,-8,GETUTCDATE()),  DATEADD(DAY,-60,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000007', 'Ayanda Cele',       'a.cele@company.com',      '+27 82 111 0007', 'Driver', 'Available',    'GP-DL-007', '2025-10-31', 1, DATEADD(HOUR,-1,GETUTCDATE()),  DATEADD(DAY,-45,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000008', '00000000-0000-0000-0000-000000000008', 'Zanele Sithole',    'z.sithole@company.com',   '+27 83 111 0008', 'Driver', 'OnAssignment', 'GP-DL-008', '2028-06-30', 1, DATEADD(HOUR,-3,GETUTCDATE()),  DATEADD(DAY,-120,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000009', '00000000-0000-0000-0000-000000000009', 'Mandla Shabalala',  'm.shabalala@company.com', '+27 71 111 0009', 'Driver', 'Available',    'GP-DL-009', '2027-04-30', 1, GETUTCDATE(),                   DATEADD(DAY,-30,GETUTCDATE())),
('A1000001-0000-0000-0000-000000000010', '00000000-0000-0000-0000-000000000010', 'Precious Ndlovu',   'p.ndlovu@company.com',    '+27 84 111 0010', 'Driver', 'OffDuty',      'GP-DL-010', '2026-05-31', 1, DATEADD(HOUR,-6,GETUTCDATE()),  DATEADD(DAY,-200,GETUTCDATE())),
-- Coordinator
('A1000001-0000-0000-0000-000000000020', '00000000-0000-0000-0000-000000000020', 'Lindiwe Dube',      'l.dube@company.com',      '+27 11 555 0020', 'Coordinator', NULL, NULL, NULL, 1, NULL, DATEADD(DAY,-365,GETUTCDATE())),
-- Manager
('A1000001-0000-0000-0000-000000000030', '00000000-0000-0000-0000-000000000030', 'David van der Berg','d.vanderberg@company.com','+27 11 555 0030', 'Manager', NULL, NULL, NULL, 1, NULL, DATEADD(DAY,-400,GETUTCDATE())),
-- Mechanic
('A1000001-0000-0000-0000-000000000040', '00000000-0000-0000-0000-000000000040', 'Riaan Botha',       'r.botha@company.com',     '+27 11 555 0040', 'Mechanic', NULL, NULL, NULL, 1, NULL, DATEADD(DAY,-500,GETUTCDATE())),
-- Admin
('A1000001-0000-0000-0000-000000000050', '00000000-0000-0000-0000-000000000050', 'Admin User',        'admin@company.com',       '+27 11 555 0050', 'Admin', NULL, NULL, NULL, 1, NULL, DATEADD(DAY,-600,GETUTCDATE()));
GO

-- ───────────────────────────────────────────────────────────────────────────
-- VEHICLES
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType, OdometerKm, ServiceIntervalKm, LastServiceDate, NextServiceDate, AssignedMechanicId, CreatedAt, UpdatedAt)
VALUES
('B2000001-0000-0000-0000-000000000001', 'GP 12-34 AA', 'Toyota',   'Hilux 2.8 GD-6',     2022, 'Assigned',       'Diesel', 87450,  10000, '2025-01-15', '2025-04-15', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-500,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000002', 'GP 56-78 BB', 'Ford',     'Ranger 2.0 Bi-Turbo', 2021, 'Available',      'Diesel', 112300, 10000, '2025-03-01', '2025-06-01', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-480,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000003', 'GP 99-11 CC', 'Mercedes', 'Sprinter 316 CDI',    2020, 'Assigned',       'Diesel', 198750, 15000, '2024-11-20', '2025-02-20', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-460,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000004', 'GP 45-67 DD', 'Toyota',   'Land Cruiser 79',     2023, 'Available',      'Diesel', 43200,  10000, '2025-02-10', '2025-05-10', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-300,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000005', 'GP 33-22 EE', 'Isuzu',    'D-Max 3.0 4x4',      2021, 'InMaintenance',  'Diesel', 156900, 10000, '2024-09-05', '2024-12-05', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-400,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000006', 'GP 77-88 FF', 'Volkswagen','Amarok 3.0 TDI',     2022, 'Assigned',       'Diesel', 76500,  10000, '2025-01-28', '2025-04-28', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-380,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000007', 'GP 55-44 GG', 'Nissan',   'Navara 2.5 dCi',     2019, 'Available',      'Diesel', 234100, 10000, '2024-12-15', '2025-03-15', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-700,GETUTCDATE()), GETUTCDATE()),
('B2000001-0000-0000-0000-000000000008', 'GP 11-99 HH', 'Toyota',   'Quantum 2.7',         2023, 'OutOfService',   'Petrol', 28900,  15000, '2025-03-20', '2025-06-20', 'A1000001-0000-0000-0000-000000000040', DATEADD(DAY,-200,GETUTCDATE()), GETUTCDATE());
GO

-- ───────────────────────────────────────────────────────────────────────────
-- TRIP REQUESTS
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO TripRequests (Id, RequestedById, Purpose, PickupLocation, DestinationLocation, RequestedDateTime, Status, Priority, Notes, CreatedAt)
VALUES
('C3000001-0000-0000-0000-000000000001', 'A1000001-0000-0000-0000-000000000030', 'Site inspection — Midrand warehouse',         'Head Office, Sandton',    'Warehouse, Midrand',          DATEADD(HOUR,-3,GETUTCDATE()),   'Active',    'High',   'Board member accompanying', DATEADD(HOUR,-4,GETUTCDATE())),
('C3000001-0000-0000-0000-000000000002', 'A1000001-0000-0000-0000-000000000020', 'Staff transfer — OR Tambo airport',           'Head Office, Sandton',    'OR Tambo International',      DATEADD(HOUR,-2,GETUTCDATE()),   'Active',    'High',   'Flight at 14:00',           DATEADD(HOUR,-3,GETUTCDATE())),
('C3000001-0000-0000-0000-000000000003', 'A1000001-0000-0000-0000-000000000020', 'Document courier — Pretoria office',          'Head Office, Sandton',    'Pretoria Regional Office',    DATEADD(HOUR,-1,GETUTCDATE()),   'Pending',   'Normal', 'Legal documents — urgent',  DATEADD(HOUR,-1,GETUTCDATE())),
('C3000001-0000-0000-0000-000000000004', 'A1000001-0000-0000-0000-000000000030', 'Client meeting — Rosebank',                   'Head Office, Sandton',    'Client HQ, Rosebank',         DATEADD(HOUR,2,GETUTCDATE()),    'Pending',   'Normal', NULL,                        DATEADD(MINUTE,-30,GETUTCDATE())),
('C3000001-0000-0000-0000-000000000005', 'A1000001-0000-0000-0000-000000000020', 'Equipment delivery — Soweto depot',           'Warehouse, Midrand',      'Soweto Depot',                DATEADD(HOUR,-5,GETUTCDATE()),   'Active',    'High',   'Heavy load — bakkie required', DATEADD(HOUR,-6,GETUTCDATE())),
('C3000001-0000-0000-0000-000000000006', 'A1000001-0000-0000-0000-000000000020', 'Staff collection — Centurion station',        'Centurion Gautrain',      'Head Office, Sandton',        DATEADD(HOUR,-4,GETUTCDATE()),   'Completed', 'Normal', NULL,                        DATEADD(HOUR,-5,GETUTCDATE())),
('C3000001-0000-0000-0000-000000000007', 'A1000001-0000-0000-0000-000000000030', 'VIP transport — CSIR conference',             'Protea Hotel, Midrand',   'CSIR Conference Centre',      DATEADD(DAY,-1,GETUTCDATE()),    'Completed', 'High',   'Executive guest',           DATEADD(DAY,-1,DATEADD(HOUR,-2,GETUTCDATE()))),
('C3000001-0000-0000-0000-000000000008', 'A1000001-0000-0000-0000-000000000020', 'Parts pickup — supplier Germiston',           'Head Office, Sandton',    'Supplier Warehouse, Germiston',DATEADD(HOUR,4,GETUTCDATE()),   'Pending',   'Normal', 'Collect invoice on arrival',DATEADD(MINUTE,-15,GETUTCDATE()));
GO

-- ───────────────────────────────────────────────────────────────────────────
-- ASSIGNMENTS
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO Assignments (Id, TripRequestId, DriverId, VehicleId, AssignedById, AssignmentType, Status, StartTime, EstimatedEndTime, ActualEndTime, Notes, CreatedAt)
VALUES
('D4000001-0000-0000-0000-000000000001', 'C3000001-0000-0000-0000-000000000001', 'A1000001-0000-0000-0000-000000000002', 'B2000001-0000-0000-0000-000000000001', 'A1000001-0000-0000-0000-000000000020', 'Auto',   'Active',    DATEADD(HOUR,-3,GETUTCDATE()),  DATEADD(HOUR,1,GETUTCDATE()),   NULL,                           NULL,                     DATEADD(HOUR,-3,GETUTCDATE())),
('D4000001-0000-0000-0000-000000000002', 'C3000001-0000-0000-0000-000000000002', 'A1000001-0000-0000-0000-000000000005', 'B2000001-0000-0000-0000-000000000006', 'A1000001-0000-0000-0000-000000000020', 'Auto',   'Active',    DATEADD(HOUR,-2,GETUTCDATE()),  DATEADD(HOUR,2,GETUTCDATE()),   NULL,                           'Airport run — allow extra time for traffic', DATEADD(HOUR,-2,GETUTCDATE())),
('D4000001-0000-0000-0000-000000000003', 'C3000001-0000-0000-0000-000000000005', 'A1000001-0000-0000-0000-000000000008', 'B2000001-0000-0000-0000-000000000004', 'A1000001-0000-0000-0000-000000000020', 'Manual', 'Active',    DATEADD(HOUR,-5,GETUTCDATE()),  DATEADD(MINUTE,-30,GETUTCDATE()),NULL,                          'Manager override — Zanele requested specifically', DATEADD(HOUR,-5,GETUTCDATE())),
('D4000001-0000-0000-0000-000000000004', 'C3000001-0000-0000-0000-000000000006', 'A1000001-0000-0000-0000-000000000003', 'B2000001-0000-0000-0000-000000000002', 'A1000001-0000-0000-0000-000000000020', 'Auto',   'Completed', DATEADD(HOUR,-4,GETUTCDATE()),  DATEADD(HOUR,-2,GETUTCDATE()),  DATEADD(HOUR,-2,GETUTCDATE()),  NULL,                     DATEADD(HOUR,-4,GETUTCDATE())),
('D4000001-0000-0000-0000-000000000005', 'C3000001-0000-0000-0000-000000000007', 'A1000001-0000-0000-0000-000000000001', 'B2000001-0000-0000-0000-000000000004', 'A1000001-0000-0000-0000-000000000020', 'Auto',   'Completed', DATEADD(DAY,-1,DATEADD(HOUR,-2,GETUTCDATE())), DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()), NULL, DATEADD(DAY,-1,DATEADD(HOUR,-2,GETUTCDATE())));
GO

-- ───────────────────────────────────────────────────────────────────────────
-- MAINTENANCE RECORDS
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO MaintenanceRecords (Id, VehicleId, Type, ScheduledDate, CompletedDate, Cost, VendorName, VendorContact, Notes, Status, CreatedAt)
VALUES
-- Overdue (past due, not completed)
('E5000001-0000-0000-0000-000000000001', 'B2000001-0000-0000-0000-000000000003', 'Routine Service',   '2025-02-20', NULL,         NULL,    'AutoMark Services',  '011 555 1001', '60,000 km service due — vehicle at 198,750 km',                  'Overdue',    DATEADD(DAY,-90,GETUTCDATE())),
('E5000001-0000-0000-0000-000000000002', 'B2000001-0000-0000-0000-000000000005', 'Gearbox Repair',    '2025-01-10', NULL,         NULL,    'TransMax Workshop',  '011 555 1002', 'Gearbox slipping at highway speeds — vehicle pulled off road',   'Overdue',    DATEADD(DAY,-120,GETUTCDATE())),
-- Upcoming (within next 14 days)
('E5000001-0000-0000-0000-000000000003', 'B2000001-0000-0000-0000-000000000001', 'Oil Change',        DATEADD(DAY,5,GETUTCDATE()),  NULL,  NULL,    'AutoMark Services',  '011 555 1001', '87,450 km — due at 90,000 km',                                   'Scheduled',  DATEADD(DAY,-10,GETUTCDATE())),
('E5000001-0000-0000-0000-000000000004', 'B2000001-0000-0000-0000-000000000007', 'Vehicle Inspection','2025-03-15', NULL,         NULL,    'RoadWorthy Centre',  '011 555 1003', 'Annual roadworthy certificate renewal',                           'Scheduled',  DATEADD(DAY,-5,GETUTCDATE())),
('E5000001-0000-0000-0000-000000000005', 'B2000001-0000-0000-0000-000000000006', 'Brake Service',     DATEADD(DAY,10,GETUTCDATE()), NULL,  NULL,    'AutoMark Services',  '011 555 1001', 'Front brake pads at 20% — replace before next long trip',        'Scheduled',  DATEADD(DAY,-3,GETUTCDATE())),
-- Completed
('E5000001-0000-0000-0000-000000000006', 'B2000001-0000-0000-0000-000000000002', 'Routine Service',   '2025-03-01', '2025-03-01', 4850.00, 'AutoMark Services',  '011 555 1001', '90,000 km full service — oil, filters, belts checked',           'Completed',  DATEADD(DAY,-30,GETUTCDATE())),
('E5000001-0000-0000-0000-000000000007', 'B2000001-0000-0000-0000-000000000004', 'Tyre Replacement',  '2025-02-10', '2025-02-10', 7200.00, 'National Tyres',     '011 555 1004', 'All 4 tyres replaced — 265/70R16 Bridgestone Dueler',            'Completed',  DATEADD(DAY,-50,GETUTCDATE())),
('E5000001-0000-0000-0000-000000000008', 'B2000001-0000-0000-0000-000000000001', 'Routine Service',   '2025-01-15', '2025-01-15', 3950.00, 'AutoMark Services',  '011 555 1001', '80,000 km service — all checks passed',                          'Completed',  DATEADD(DAY,-75,GETUTCDATE())),
-- In Progress
('E5000001-0000-0000-0000-000000000009', 'B2000001-0000-0000-0000-000000000008', 'Electrical Fault',  GETUTCDATE(),               NULL,  NULL,    'ElectroPro Motors',  '011 555 1005', 'Power window failure + dashboard warning light — vehicle grounded','InProgress', DATEADD(DAY,-2,GETUTCDATE())),
('E5000001-0000-0000-0000-000000000010', 'B2000001-0000-0000-0000-000000000005', 'Clutch Replacement',DATEADD(DAY,-5,GETUTCDATE()), NULL,  NULL,    'TransMax Workshop',  '011 555 1002', 'Clutch replacement in progress — ETA 3 more working days',       'InProgress', DATEADD(DAY,-10,GETUTCDATE()));
GO

-- ───────────────────────────────────────────────────────────────────────────
-- FUEL LOGS
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO FuelLogs (Id, VehicleId, LoggedById, FuelDate, LitresFilled, CostPerLitre, TotalCost, OdometerAtFill, StationName, Notes, CreatedAt)
VALUES
('F6000001-0000-0000-0000-000000000001', 'B2000001-0000-0000-0000-000000000001', 'A1000001-0000-0000-0000-000000000002', DATEADD(DAY,-1,GETUTCDATE()),  65.5,  22.95, 1503.23, 87300,  'Engen Sandton',          NULL,                              DATEADD(DAY,-1,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000002', 'B2000001-0000-0000-0000-000000000001', 'A1000001-0000-0000-0000-000000000002', DATEADD(DAY,-8,GETUTCDATE()),  70.2,  22.75, 1597.05, 86800,  'BP Rivonia',             NULL,                              DATEADD(DAY,-8,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000003', 'B2000001-0000-0000-0000-000000000002', 'A1000001-0000-0000-0000-000000000003', DATEADD(DAY,-2,GETUTCDATE()),  80.0,  22.95, 1836.00, 112100, 'Shell Midrand',          'Full tank before long trip',      DATEADD(DAY,-2,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000004', 'B2000001-0000-0000-0000-000000000003', 'A1000001-0000-0000-0000-000000000005', DATEADD(DAY,-3,GETUTCDATE()),  95.4,  22.95, 2189.43, 198600, 'Caltex Johannesburg',    NULL,                              DATEADD(DAY,-3,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000005', 'B2000001-0000-0000-0000-000000000003', 'A1000001-0000-0000-0000-000000000005', DATEADD(DAY,-10,GETUTCDATE()), 88.7,  22.50, 1995.75, 198000, 'Total Pretoria',         NULL,                              DATEADD(DAY,-10,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000006', 'B2000001-0000-0000-0000-000000000004', 'A1000001-0000-0000-0000-000000000008', DATEADD(DAY,-4,GETUTCDATE()),  72.3,  22.95, 1659.29, 43050,  'Engen Centurion',        NULL,                              DATEADD(DAY,-4,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000007', 'B2000001-0000-0000-0000-000000000006', 'A1000001-0000-0000-0000-000000000005', DATEADD(DAY,-5,GETUTCDATE()),  78.9,  22.75, 1795.28, 76300,  'BP Sandton City',        'Slipped while collecting from airport', DATEADD(DAY,-5,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000008', 'B2000001-0000-0000-0000-000000000007', 'A1000001-0000-0000-0000-000000000009', DATEADD(DAY,-6,GETUTCDATE()),  90.0,  22.50, 2025.00, 233900, 'Shell Germiston',        NULL,                              DATEADD(DAY,-6,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000009', 'B2000001-0000-0000-0000-000000000002', 'A1000001-0000-0000-0000-000000000003', DATEADD(DAY,-15,GETUTCDATE()), 82.4,  22.20, 1829.28, 111700, 'Caltex Midrand',         NULL,                              DATEADD(DAY,-15,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000010', 'B2000001-0000-0000-0000-000000000004', 'A1000001-0000-0000-0000-000000000001', DATEADD(DAY,-20,GETUTCDATE()), 68.0,  22.10, 1502.80, 42700,  'Total Centurion',        NULL,                              DATEADD(DAY,-20,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000011', 'B2000001-0000-0000-0000-000000000001', 'A1000001-0000-0000-0000-000000000002', DATEADD(DAY,-22,GETUTCDATE()), 66.8,  22.10, 1476.28, 86400,  'Engen Fourways',         NULL,                              DATEADD(DAY,-22,GETUTCDATE())),
('F6000001-0000-0000-0000-000000000012', 'B2000001-0000-0000-0000-000000000006', 'A1000001-0000-0000-0000-000000000008', DATEADD(DAY,-25,GETUTCDATE()), 74.5,  22.10, 1646.45, 75900,  'BP Rosebank',            NULL,                              DATEADD(DAY,-25,GETUTCDATE()));
GO

-- ───────────────────────────────────────────────────────────────────────────
-- AUDIT LOG (sample entries)
-- ───────────────────────────────────────────────────────────────────────────
INSERT INTO AuditLogs (Id, EntityType, EntityId, Action, UserId, UserEmail, Timestamp, IpAddress, Notes)
VALUES
('A7000001-0000-0000-0000-000000000001', 'Assignment', 'D4000001-0000-0000-0000-000000000001', 'Created',          'A1000001-0000-0000-0000-000000000020', 'l.dube@company.com',       DATEADD(HOUR,-3,GETUTCDATE()),  '192.168.1.10', 'Auto-assignment: Sipho Dlamini selected (score 87)'),
('A7000001-0000-0000-0000-000000000002', 'Assignment', 'D4000001-0000-0000-0000-000000000003', 'Overridden',       'A1000001-0000-0000-0000-000000000030', 'd.vanderberg@company.com', DATEADD(HOUR,-5,GETUTCDATE()),  '192.168.1.11', 'Manager override: Zanele Sithole assigned instead of James Mokoena'),
('A7000001-0000-0000-0000-000000000003', 'Vehicle',    'B2000001-0000-0000-0000-000000000005', 'StatusChanged',    'A1000001-0000-0000-0000-000000000040', 'r.botha@company.com',      DATEADD(DAY,-5,GETUTCDATE()),   '192.168.1.12', 'Vehicle moved to InMaintenance — gearbox repair commenced'),
('A7000001-0000-0000-0000-000000000004', 'User',       'A1000001-0000-0000-0000-000000000004', 'StatusChanged',    'A1000001-0000-0000-0000-000000000004', 'n.zulu@company.com',       DATEADD(MINUTE,-45,GETUTCDATE()),'192.168.1.13','Driver self-updated status to OnBreak'),
('A7000001-0000-0000-0000-000000000005', 'TripRequest','C3000001-0000-0000-0000-000000000003', 'Created',          'A1000001-0000-0000-0000-000000000020', 'l.dube@company.com',       DATEADD(HOUR,-1,GETUTCDATE()),  '192.168.1.10', 'Request entered pending queue — no available drivers at submission time'),
('A7000001-0000-0000-0000-000000000006', 'Maintenance','E5000001-0000-0000-0000-000000000006', 'Completed',        'A1000001-0000-0000-0000-000000000040', 'r.botha@company.com',      DATEADD(DAY,-30,GETUTCDATE()),  '192.168.1.12', 'GP 56-78 BB — 90,000 km service signed off. Cost: R4,850');
GO

PRINT 'Demo seed data inserted successfully.';
PRINT '  Users:               14 (10 drivers, 1 coordinator, 1 manager, 1 mechanic, 1 admin)';
PRINT '  Vehicles:             8 (various statuses)';
PRINT '  Trip Requests:        8 (active, pending, completed)';
PRINT '  Assignments:          5';
PRINT '  Maintenance Records: 10 (overdue, upcoming, completed, in-progress)';
PRINT '  Fuel Logs:           12';
PRINT '  Audit Log Entries:    6';
GO
