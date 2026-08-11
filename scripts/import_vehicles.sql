-- ============================================================================

-- Desicon Logistics — Vehicle import from 2026 Repairs & Maintenance Register

-- Source sheet: 'List of Vehicles' (84 vehicles)

--

-- Idempotent: re-running will NOT create duplicates (matched on RegistrationNo).

-- Year is set to 0 = 'not recorded'. Fill it in later via the Vehicles page.

-- FuelType is inferred from make/model; correct any in the UI as needed.

-- ============================================================================



SET NOCOUNT ON;

BEGIN TRANSACTION;



IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PF 4079 SPY')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PF 4079 SPY', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'5550000190', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 185 AM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 185 AM', N'NISSAN', N'PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550000223', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 178 AM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 178 AM', N'NISSAN', N'NAVARA', 0, N'Available', N'Diesel',
      109315, 10000, N'5550000224', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KRD 341 CE')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KRD 341 CE', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'5550000280', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KRD 339 CE')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KRD 339 CE', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'5550000281', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KRD 338 CE')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KRD 338 CE', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      161588, 10000, N'5550000346', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 177 AM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 177 AM', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'5550000384', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'GGU 693 TX')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'GGU 693 TX', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'5550000398', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'GGU 692 TX')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'GGU 692 TX', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'5550000399', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 179 AM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 179 AM', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'5550000618', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'GGU 695 TX')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'GGU 695 TX', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      176577, 10000, N'5550000619', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'GGU 696 TX')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'GGU 696 TX', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'5550000620', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'CX 211 RBC')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'CX 211 RBC', N'TOYOTA', N'CAMRY', 0, N'Available', N'Petrol',
      0, 10000, N'5550000641', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'GGE 70 AY')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'GGE 70 AY', N'TOYOTA', N'LANDCRUISER', 0, N'Available', N'Diesel',
      0, 10000, N'5550000735', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'LSD 65 BP')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'LSD 65 BP', N'HYUNDAI', N'CAR', 0, N'Available', N'Diesel',
      0, 10000, N'5550000906', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AGL 105 BU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AGL 105 BU', N'TOYOTA', N'HIACE BUS', 0, N'Available', N'Diesel',
      0, 10000, N'5550000912', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'SMK 126 CE')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'SMK 126 CE', N'TOYOTA', N'LAND CRUISER', 0, N'Available', N'Diesel',
      0, 10000, N'5550000950', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KRD 407 CG')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KRD 407 CG', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      270593, 10000, N'5550000956', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KRD 406 CG')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KRD 406 CG', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      223346, 10000, N'5550000957', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KTU 905 CK')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KTU 905 CK', N'TOYOTA', N'HIACE BUS', 0, N'Available', N'Diesel',
      164560, 10000, N'5550001005', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'JJJ 599 CM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'JJJ 599 CM', N'LEXUS', N'LX 570 SUV', 0, N'Available', N'Petrol',
      116289, 10000, N'5550001008', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'JJJ 597 CM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'JJJ 597 CM', N'LEXUS', N'LX 570 SUV', 0, N'Available', N'Petrol',
      0, 10000, N'5550001010', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AKD 90 CP')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AKD 90 CP', N'TOYOTA', N'PRADO VX', 0, N'Available', N'Diesel',
      0, 10000, N'5550001231', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'SMK 179 CW')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'SMK 179 CW', N'TOYOTA', N'TOYOYA CAMRY XLE', 0, N'Available', N'Petrol',
      44666, 10000, N'5550001258', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 785 CU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 785 CU', N'LEXUS', N'LX570, B6 ARMOURED', 0, N'Available', N'Petrol',
      0, 10000, N'5550001280', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'FKJ 877 DB')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'FKJ 877 DB', N'TOYOTA', N'LANDCRUISER', 0, N'Available', N'Diesel',
      0, 10000, N'5550001331', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'FKJ 876 DB')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'FKJ 876 DB', N'TOYOTA', N'LANDCRUISER', 0, N'Available', N'Diesel',
      0, 10000, N'5550001332', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'LSR 261 CX')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'LSR 261 CX', N'TOYOTA', N'PRADO VX', 0, N'Available', N'Diesel',
      0, 10000, N'5550001368', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'RUM 513 CB')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'RUM 513 CB', N'TOYOTA', N'LAND CRUISER PRADO TXL', 0, N'Available', N'Diesel',
      131048, 10000, N'5550001528', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'SMK 51 DM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'SMK 51 DM', N'HYUNDAI', N'IX 35 ELEGANCE', 0, N'Available', N'Diesel',
      117248, 10000, N'5550001616', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'SMK 48 DM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'SMK 48 DM', N'HYUNDAI', N'SANTA FE IX 45', 0, N'Available', N'Diesel',
      120426, 10000, N'5550001617', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'FKJ 862 DJ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'FKJ 862 DJ', N'HYUNDAI', N'IX 35 ELEGANCE', 0, N'Available', N'Diesel',
      144760, 10000, N'5550001620', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'FKJ 988 DJ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'FKJ 988 DJ', N'HYUNDAI', N'IX 35 ELEGANCE', 0, N'Available', N'Diesel',
      119234, 10000, N'5550001621', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'GGE 223 DK')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'GGE 223 DK', N'HYUNDAI', N'IX 35 ELEGANCE', 0, N'Available', N'Diesel',
      0, 10000, N'5550001639', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'SMK 50 DM')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'SMK 50 DM', N'HYUNDAI', N'SANTA FE IX 45 ELEGANCE', 0, N'Available', N'Diesel',
      111857, 10000, N'5550001817', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 40 DQ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 40 DQ', N'KIA', N'SPORTAGE BLACK PEARL', 0, N'Available', N'Diesel',
      0, 10000, N'5550001910', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 42 DQ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 42 DQ', N'KIA', N'CERATO', 0, N'Available', N'Diesel',
      126894, 10000, N'5550001911', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 41 DQ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 41 DQ', N'LEXUS', N'GX', 0, N'Available', N'Petrol',
      0, 10000, N'5550001912', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 43 DQ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 43 DQ', N'LEXUS', N'GX 470 JEEP', 0, N'Available', N'Petrol',
      0, 10000, N'5550001913', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 896 FW')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 896 FW', N'NISSAN', N'NAVARA PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002023', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 895 FW')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 895 FW', N'NISSAN', N'NAVARA PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002024', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 118 NT')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 118 NT', N'NISSAN', N'PICKUP NP300', 0, N'Available', N'Diesel',
      105740, 10000, N'5550002132', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'LND 583 ET')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'LND 583 ET', N'TOYOTA', N'FORTUNER', 0, N'Available', N'Diesel',
      135820, 10000, N'5550002641', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 388 ET')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 388 ET', N'TOYOTA', N'FORTUNER', 0, N'Available', N'Diesel',
      102889, 10000, N'5550002654', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'NCH 53 ST')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'NCH 53 ST', N'MITSUBISHI', N'OUTLANDER', 0, N'Available', N'Diesel',
      0, 10000, N'5550002662', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'NCH 52 ST')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'NCH 52 ST', N'MITSUBISHI', N'OUTLANDER', 0, N'Available', N'Diesel',
      98135, 10000, N'5550002663', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AGL 272 EU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AGL 272 EU', N'TOYOTA', N'HILUX PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002664', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AGL 273 EU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AGL 273 EU', N'TOYOTA', N'HILUX PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002665', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AGL 274 EU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AGL 274 EU', N'TOYOTA', N'HILUX PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002666', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AGL 275 EU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AGL 275 EU', N'TOYOTA', N'HILUX PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002667', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'PHC 82 AJ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'PHC 82 AJ', N'TOYOTA', N'HILUX PICKUP', 0, N'Available', N'Diesel',
      0, 10000, N'5550002686', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'EPE-776-EW')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'EPE-776-EW', N'NISSAN', N'URVAN 15 SEATER BUS', 0, N'Available', N'Diesel',
      0, 10000, N'5550002888', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KTU 570 EU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KTU 570 EU', N'HYUNDAI', N'TUCSON EVOLUTION', 0, N'Available', N'Diesel',
      0, 10000, N'5550002902', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KSF 375 EY')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KSF 375 EY', N'NISSAN', N'PICKUP NP300', 0, N'Available', N'Diesel',
      0, 10000, N'5550002911', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KSF 376 EY')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KSF 376 EY', N'NISSAN', N'PICKUP NP300', 0, N'Available', N'Diesel',
      127262, 10000, N'5550002912', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'NCH 54 ST')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'NCH 54 ST', N'FOTON', N'VIEW C1 MHR 15-SEATER BUS', 0, N'Available', N'Diesel',
      0, 10000, N'5550002930', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'BER 500 MU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'BER 500 MU', N'HYUNDAI', N'MINI TRUCK', 0, N'Available', N'Diesel',
      46732, 10000, N'5550002965', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'BDG 285 FA')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'BDG 285 FA', N'TOYOTA', N'RAV4', 0, N'Available', N'Petrol',
      0, 10000, N'5550002995', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'BDG 286 FA')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'BDG 286 FA', N'TOYOTA', N'RAV4', 0, N'Available', N'Petrol',
      0, 10000, N'5550002996', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'LSR 74 FC')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'LSR 74 FC', N'TOYOTA', N'LAND CRUISER', 0, N'Available', N'Diesel',
      0, 10000, N'5550003025', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'YAB 47 NQ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'YAB 47 NQ', N'TOYOTA', N'LANDCRUISER', 0, N'Available', N'Diesel',
      60335, 10000, N'5550003146', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'APP 231 FQ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'APP 231 FQ', N'LEXUS', N'GX470 JEEP', 0, N'Available', N'Petrol',
      0, 10000, N'5550003161', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'SKN 317 AU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'SKN 317 AU', N'TOYOTA', N'HIACE STANDARD ROOF AMBULANCE', 0, N'Available', N'Diesel',
      0, 10000, N'5550003637', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'BDG 159 CL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'BDG 159 CL', N'TOYOTA', N'CAMRY', 0, N'Available', N'Petrol',
      0, 10000, N'PRIVATE (GML1)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AKD 407 BD')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AKD 407 BD', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'PRIVATE (AK1)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AJ 464 AJ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AJ 464 AJ', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'PRIVATE (AK2)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AAA 07 UB')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AAA 07 UB', N'RANGE', N'ROVER', 0, N'Available', N'Petrol',
      0, 10000, N'PRIVATE (DGS)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'FST 06 AZ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'FST 06 AZ', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      141579, 10000, N'PRIVATE (GML2)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AKD 490 SU')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AKD 490 SU', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'PRIVATE (CHAIRMAN)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'KSF 761 DV')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'KSF 761 DV', N'LEXUS', N'LEXUS', 0, N'Available', N'Petrol',
      0, 10000, N'PRIVATE (AK3)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'DE 51 CON')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'DE 51 CON', N'MERCEDES', N'G-WAGON', 0, N'Available', N'Petrol',
      0, 10000, N'PRIVATE (DGS2)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'RBC 899 HN')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'RBC 899 HN', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'PRIVATE (AK4)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'EPE 520 CH')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'EPE 520 CH', N'TOYOTA', N'PRADO', 0, N'Available', N'Diesel',
      0, 10000, N'PRIVATE (AK5)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AKD 380 EJ')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AKD 380 EJ', N'WRANGLER', N'JEEP', 0, N'Available', N'Petrol',
      0, 10000, N'PRIVATE (ANIEK U)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'47')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'47', N'BMW', N'6 SERIES', 0, N'Available', N'Petrol',
      0, 10000, N'PRIVATE (AK6)', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'DBU 519 AA')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'DBU 519 AA', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'27440', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'DBU 710 AA')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'DBU 710 AA', N'TOYOTA', N'HIACE BUS (DIESEL ENGINE)', 0, N'Available', N'Diesel',
      0, 10000, N'27438', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 673 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 673 GL', N'TOYOTA', N'FORTUNER', 0, N'Available', N'Diesel',
      0, 10000, N'FA000054', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 301 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 301 GL', N'TOYOTA', N'URBAN CRUISER', 0, N'Available', N'Petrol',
      0, 10000, N'FA000059', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 674 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 674 GL', N'TOYOTA', N'URBAN CRUISER', 0, N'Available', N'Petrol',
      0, 10000, N'FA000060', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 302 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 302 GL', N'TOYOTA', N'HIACE BUS (DIESEL ENGINE)', 0, N'Available', N'Diesel',
      0, 10000, N'FA000055', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 304 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 304 GL', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'FA000056', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 303 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 303 GL', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      5465, 10000, N'FA000057', NULL, GETUTCDATE(), GETUTCDATE());

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE RegistrationNo = N'AFM 675 GL')
  INSERT INTO Vehicles (Id, RegistrationNo, Make, Model, Year, Status, FuelType,
      OdometerKm, ServiceIntervalKm, AssetTagNo, ChassisNo, CreatedAt, UpdatedAt)
  VALUES (NEWID(), N'AFM 675 GL', N'TOYOTA', N'HILUX', 0, N'Available', N'Diesel',
      0, 10000, N'FA000058', NULL, GETUTCDATE(), GETUTCDATE());



COMMIT TRANSACTION;



-- Verify

SELECT COUNT(*) AS TotalVehicles FROM Vehicles;

SELECT RegistrationNo, Make, Model, Year, FuelType, OdometerKm, AssetTagNo

FROM Vehicles ORDER BY Make, Model;