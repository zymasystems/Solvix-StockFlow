using Solvix.Sims.Models;

namespace Solvix.Sims.Services;

/// <summary>
/// Multi-company stock ledger and tenant repository.
/// Companies each manage their own isolated catalog, stock levels, warehouse sites, and movement ledgers.
/// </summary>
public class InventoryService
{
	public event Action? Changed;
	public List<Employee> Employees { get; } = new();
	public List<SupplierContact> SupplierDirectory { get; } = new();

	public string CurrentCompanyId { get; private set; } = "solvix-robotics";

	public Company CurrentCompany =>
		Companies.FirstOrDefault(c => c.Id == CurrentCompanyId) ?? Companies[0];

	public List<Company> Companies { get; } = new()
	{
		new Company
		{
			Id = "solvix-robotics",
			Name = "Solvix Robotics & Automation",
			Code = "SVX",
			Industry = "Industrial Robotics & Electronics",
			Description = "Precision robotic servos, multi-core processors, sensors, and aerospace carbon composites.",
			Icon = "precision_manufacturing",
			ThemeColor = "from-primary-container to-secondary",
			AccentClass = "bg-primary text-on-primary",
			Warehouses = new() { "Pretoria Robotics Hub WH-01", "Johannesburg Assembly Bay 4", "Durban Tech Depot WH-09" },
			DefaultSite = "Pretoria Robotics Hub WH-01",
			ContactEmail = "alex.chen@solvix-robotics.com",
			EmailDomain = "solvix-robotics.com",
			CurrencySymbol = "R"
		},
		new Company
		{
			Id = "apex-logistics",
			Name = "Apex Global Freight & Cargo",
			Code = "APX",
			Industry = "Freight & Intermodal Cargo",
			Description = "Heavy-duty shipping pallets, container bolt seals, industrial strapping, and thermal cargo shields.",
			Icon = "local_shipping",
			ThemeColor = "from-secondary to-blue-700",
			AccentClass = "bg-secondary text-on-secondary",
			Warehouses = new() { "Durban Harbor Terminal 03", "Cape Town Air Cargo Bay", "City Deep Freight Hub WH-07" },
			DefaultSite = "Durban Harbor Terminal 03",
			ContactEmail = "ops@apex-freight.com",
			EmailDomain = "apex-freight.com",
			CurrencySymbol = "R"
		},
		new Company
		{
			Id = "biopharma-health",
			Name = "BioPharma Cold Chain & Labs",
			Code = "BPH",
			Industry = "Pharmaceuticals & Cold Storage",
			Description = "Sub-zero vaccine vials, sterile disposable syringes, diagnostic test kits, and cryo data loggers.",
			Icon = "vaccines",
			ThemeColor = "from-emerald-600 to-teal-700",
			AccentClass = "bg-emerald-600 text-white",
			Warehouses = new() { "Centurion Cold Vault A", "Gauteng Distribution Hub 2", "Stellenbosch Bio-Depot" },
			DefaultSite = "Centurion Cold Vault A",
			ContactEmail = "supply@biopharma-cold.com",
			EmailDomain = "biopharma-cold.com",
			CurrencySymbol = "R"
		},
		new Company
		{
			Id = "nordic-hardware",
			Name = "Nordic Heavy Hardware & Tools",
			Code = "NDX",
			Industry = "Heavy Machinery & Hardware",
			Description = "Aerospace grade titanium bolts, hydraulic valves, ceramic bearings, and digital torque tools.",
			Icon = "construction",
			ThemeColor = "from-amber-600 to-orange-700",
			AccentClass = "bg-amber-600 text-white",
			Warehouses = new() { "Germiston Heavy Yard Bay 2", "Port Elizabeth Assembly Store", "Midrand Logistics Hub" },
			DefaultSite = "Germiston Heavy Yard Bay 2",
			ContactEmail = "orders@nordic-industrial.co.za",
			EmailDomain = "nordic-industrial.co.za",
			CurrencySymbol = "R"
		}
	};

	public List<UserAccount> Users { get; } = new()
	{
		new UserAccount
		{
			Email = "alex.chen@solvix-robotics.com",
			Password = "password",
			FullName = "Alex Chen",
			Role = "Inventory Operations Lead",
			CompanyId = "solvix-robotics",
			WarehouseSite = "Pretoria Robotics Hub WH-01"
		},
		new UserAccount
		{
			Email = "ops@apex-freight.com",
			Password = "password",
			FullName = "Sarah Jenkins",
			Role = "Logistics Operations Lead",
			CompanyId = "apex-logistics",
			WarehouseSite = "Durban Harbor Terminal 03"
		},
		new UserAccount
		{
			Email = "supply@biopharma-cold.com",
			Password = "password",
			FullName = "Dr. Thabo Mokoena",
			Role = "Cold Chain Quality Manager",
			CompanyId = "biopharma-health",
			WarehouseSite = "Centurion Cold Vault A"
		},
		new UserAccount
		{
			Email = "orders@nordic-industrial.co.za",
			Password = "password",
			FullName = "Johan Van Der Merwe",
			Role = "Spares & Depot Supervisor",
			CompanyId = "nordic-hardware",
			WarehouseSite = "Germiston Heavy Yard Bay 2"
		}
	};

	public List<Product> AllProducts { get; } = new()
	{
		// ----------------- Solvix Robotics -----------------
		new Product
		{
			CompanyId = "solvix-robotics", Warehouse = "Pretoria Robotics Hub WH-01",
			Sku = "SVX-SRV-2401", Name = "Precision Robotic Servo Motor 48V",
			Description = "High-torque brushless DC actuator with optical encoder",
			Group = "Finished Goods", CategoryLabel = "Industrial Robotics / Electronics", Area = "Electronics • Rack A",
			Zone = "Zone A4", Bin = "A4-R12-S02", BinLong = "Zone A4 • Rack 12 • Shelf 02",
			Barcode = "742190847291", Icon = "precision_manufacturing", Batch = "B-9021",
			OnHand = 14, Reserved = 4, Quarantined = 0, MinLevel = 50, MaxCapacity = 300, UnitCost = 184.50m,
			Supplier = "TechMotion Ltd", SupplierNote = "99.4% reliability", LeadTimeDays = 2, LeadMode = "Air Express"
		},
		new Product
		{
			CompanyId = "solvix-robotics", Warehouse = "Pretoria Robotics Hub WH-01",
			Sku = "SVX-FST-8820", Name = "Titanium Hex Bolt M8x40",
			Description = "Aerospace-grade titanium hex bolt, machined chamfer",
			Group = "Raw Materials", CategoryLabel = "Fasteners & Hardware", Area = "Fasteners • Rack B",
			Zone = "Zone B1", Bin = "B1-R04-S09", BinLong = "Zone B1 • Rack 04 • Shelf 09",
			Barcode = "742190112233", Icon = "hardware", Batch = "B-4410",
			OnHand = 0, Reserved = 0, Quarantined = 0, MinLevel = 200, MaxCapacity = 5000, UnitCost = 1.85m,
			Supplier = "Titan Fasteners Corp", SupplierNote = "PO-4921 In Transit", LeadTimeDays = 5, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "solvix-robotics", Warehouse = "Johannesburg Assembly Bay 4",
			Sku = "SVX-CMP-9941", Name = "Reinforced Carbon Fiber Sheet 2mm",
			Description = "Woven twill carbon fibre composite sheet, 2m length",
			Group = "Raw Materials", CategoryLabel = "Composites & Raw", Area = "Composites • Bay C",
			Zone = "Zone C3", Bin = "C3-R08-S01", BinLong = "Zone C3 • Rack 08 • Shelf 01",
			Barcode = "742190998877", Icon = "layers", Batch = "B-7710",
			OnHand = 420, Reserved = 2, Quarantined = 0, MinLevel = 100, MaxCapacity = 600, UnitCost = 95.00m,
			Supplier = "CarbonWorks SA", SupplierNote = "98.1% reliability", LeadTimeDays = 7, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "solvix-robotics", Warehouse = "Pretoria Robotics Hub WH-01",
			Sku = "SVX-MCU-3208", Name = "MCU-32 Dual Core Processor",
			Description = "32-bit dual core microcontroller with CAN bus interface",
			Group = "Raw Materials", CategoryLabel = "Semiconductors", Area = "Semiconductors • Rack A",
			Zone = "Zone A2", Bin = "A2-R01-S05", BinLong = "Zone A2 • Rack 01 • Shelf 05",
			Barcode = "742190554433", Icon = "memory", Batch = "B-3208",
			OnHand = 28, Reserved = 0, Quarantined = 0, MinLevel = 100, MaxCapacity = 800, UnitCost = 122.00m,
			Supplier = "Apex Components", SupplierNote = "97.5% reliability", LeadTimeDays = 14, LeadMode = "Sea Freight"
		},
		new Product
		{
			CompanyId = "solvix-robotics", Warehouse = "Durban Tech Depot WH-09",
			Sku = "SVX-LDR-7010", Name = "LiDAR Spatial Scanning Unit 360°",
			Description = "Time-of-flight solid-state distance sensor array",
			Group = "Finished Goods", CategoryLabel = "Optics & Sensors", Area = "Optics • Rack D",
			Zone = "Zone D2", Bin = "D2-R03-S04", BinLong = "Zone D2 • Rack 03 • Shelf 04",
			Barcode = "742190449911", Icon = "sensors", Batch = "B-7010",
			OnHand = 62, Reserved = 5, Quarantined = 0, MinLevel = 30, MaxCapacity = 200, UnitCost = 315.00m,
			Supplier = "Photonix Sensors Ltd", SupplierNote = "In stock", LeadTimeDays = 3, LeadMode = "Air Express"
		},

		// ----------------- Apex Global Freight & Cargo -----------------
		new Product
		{
			CompanyId = "apex-logistics", Warehouse = "Durban Harbor Terminal 03",
			Sku = "APX-PLT-1100", Name = "Heavy-Duty Euro Wooden Pallet 1200x800",
			Description = "Heat-treated EPAL certified 4-way solid pine pallet",
			Group = "Handling Assets", CategoryLabel = "Pallets & Stacking", Area = "Yard Bay A",
			Zone = "Zone Y1", Bin = "Y1-BAY-02", BinLong = "Zone Y1 • Bay 02 • Open Yard",
			Barcode = "600123001100", Icon = "inventory", Batch = "P-8802",
			OnHand = 840, Reserved = 120, Quarantined = 0, MinLevel = 300, MaxCapacity = 2500, UnitCost = 45.00m,
			Supplier = "TimberLog SA", SupplierNote = "Contract supplier", LeadTimeDays = 2, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "apex-logistics", Warehouse = "Durban Harbor Terminal 03",
			Sku = "APX-CRG-8800", Name = "ISO Container High-Security Bolt Seal",
			Description = "Anti-spin ISO 17712 compliant barrier bolt seal with barcoding",
			Group = "Security & Fasteners", CategoryLabel = "Container Security", Area = "Secure Cage 1",
			Zone = "Zone S1", Bin = "S1-CAGE-04", BinLong = "Zone S1 • Security Cage 04",
			Barcode = "600123008800", Icon = "lock", Batch = "SL-9912",
			OnHand = 520, Reserved = 50, Quarantined = 0, MinLevel = 200, MaxCapacity = 2000, UnitCost = 14.50m,
			Supplier = "SecureSeal Global", SupplierNote = "Strict serial tracking", LeadTimeDays = 3, LeadMode = "Air Express"
		},
		new Product
		{
			CompanyId = "apex-logistics", Warehouse = "Cape Town Air Cargo Bay",
			Sku = "APX-STR-5020", Name = "Heavy Polyester Strapping Roll 19mm x 1000m",
			Description = "High-tensile PET strapping roll for pallet unitization",
			Group = "Packaging & Securing", CategoryLabel = "Strapping & Wrapping", Area = "Packaging Aisle 3",
			Zone = "Zone P2", Bin = "P2-R05-S02", BinLong = "Zone P2 • Rack 05 • Shelf 02",
			Barcode = "600123005020", Icon = "line_weight", Batch = "ST-3301",
			OnHand = 12, Reserved = 0, Quarantined = 0, MinLevel = 40, MaxCapacity = 200, UnitCost = 68.00m,
			Supplier = "PackPro Africa", SupplierNote = "Reorder overdue", LeadTimeDays = 4, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "apex-logistics", Warehouse = "City Deep Freight Hub WH-07",
			Sku = "APX-THC-4410", Name = "Thermal Reflective Pallet Cargo Shield",
			Description = "Multi-layer foil pallet cover for cold chain freight protection",
			Group = "Cold Chain Logistics", CategoryLabel = "Insulation & Covers", Area = "Cold Bay Storage",
			Zone = "Zone C1", Bin = "C1-R02-S01", BinLong = "Zone C1 • Rack 02 • Shelf 01",
			Barcode = "600123004410", Icon = "ac_unit", Batch = "TC-1044",
			OnHand = 0, Reserved = 0, Quarantined = 0, MinLevel = 25, MaxCapacity = 150, UnitCost = 185.00m,
			Supplier = "ThermoWrap Tech", SupplierNote = "Order placed #PO-902", LeadTimeDays = 5, LeadMode = "Air Express"
		},

		// ----------------- BioPharma Cold Chain & Labs -----------------
		new Product
		{
			CompanyId = "biopharma-health", Warehouse = "Centurion Cold Vault A",
			Sku = "BPH-VAC-9001", Name = "Pediatric Multi-Dose Vaccine Pack (2-8°C)",
			Description = "Temperature monitored 10-vial kit with continuous cold chain tracer",
			Group = "Vaccines & Biologics", CategoryLabel = "Sub-Zero Pharmaceuticals", Area = "Cold Room Vault 1",
			Zone = "Zone CR1", Bin = "CR1-T04-S01", BinLong = "Zone CR1 • Tray 04 • Shelf 01",
			Barcode = "600980009001", Icon = "vaccines", Batch = "V-2026A",
			OnHand = 480, Reserved = 80, Quarantined = 0, MinLevel = 200, MaxCapacity = 1500, UnitCost = 285.00m,
			Supplier = "BioVac Africa", SupplierNote = "WHO GMP certified", LeadTimeDays = 1, LeadMode = "Cold Express"
		},
		new Product
		{
			CompanyId = "biopharma-health", Warehouse = "Centurion Cold Vault A",
			Sku = "BPH-SYR-0500", Name = "Sterile Auto-Disable Syringe 0.5ml (Box of 100)",
			Description = "Single-use sterile safety syringe with fixed bevel needle",
			Group = "Sterile Consumables", CategoryLabel = "Medical Devices", Area = "Sterile Aisle B",
			Zone = "Zone ST2", Bin = "ST2-R01-S03", BinLong = "Zone ST2 • Rack 01 • Shelf 03",
			Barcode = "600980000500", Icon = "colorize", Batch = "SY-7740",
			OnHand = 1200, Reserved = 100, Quarantined = 0, MinLevel = 500, MaxCapacity = 4000, UnitCost = 14.20m,
			Supplier = "Meditech Supplies", SupplierNote = "Bulk batch active", LeadTimeDays = 3, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "biopharma-health", Warehouse = "Gauteng Distribution Hub 2",
			Sku = "BPH-RGT-2044", Name = "Rapid Pathogen Diagnostic Test Kit (50 Tests)",
			Description = "Freeze-dried enzymatic assay test cassettes for clinical labs",
			Group = "Diagnostics", CategoryLabel = "Laboratory Reagents", Area = "Lab Reagent Bay",
			Zone = "Zone LR1", Bin = "LR1-R03-S02", BinLong = "Zone LR1 • Rack 03 • Shelf 02",
			Barcode = "600980002044", Icon = "biotechnology", Batch = "RG-4411",
			OnHand = 18, Reserved = 5, Quarantined = 0, MinLevel = 60, MaxCapacity = 300, UnitCost = 110.00m,
			Supplier = "OmniDiagnostics", SupplierNote = "Urgent replenishment needed", LeadTimeDays = 2, LeadMode = "Air Express"
		},
		new Product
		{
			CompanyId = "biopharma-health", Warehouse = "Stellenbosch Bio-Depot",
			Sku = "BPH-SEN-1120", Name = "Cryo Wireless Temperature Sensor Beacon",
			Description = "NFC/Bluetooth cold-vault continuous temperature logger (-80°C to +30°C)",
			Group = "Cold Chain Monitoring", CategoryLabel = "Telemetry Hardware", Area = "Electronics Shelf",
			Zone = "Zone E1", Bin = "E1-R02-S01", BinLong = "Zone E1 • Rack 02 • Shelf 01",
			Barcode = "600980001120", Icon = "thermostat", Batch = "TL-2040",
			OnHand = 0, Reserved = 0, Quarantined = 0, MinLevel = 30, MaxCapacity = 150, UnitCost = 75.00m,
			Supplier = "CryoTrack Systems", SupplierNote = "Stock out • PO pending", LeadTimeDays = 4, LeadMode = "Air Express"
		},

		// ----------------- Nordic Heavy Hardware & Tools -----------------
		new Product
		{
			CompanyId = "nordic-hardware", Warehouse = "Germiston Heavy Yard Bay 2",
			Sku = "NDX-VLV-4010", Name = "High-Pressure Hydraulic Proportional Valve 350 Bar",
			Description = "Electro-hydraulic directional control valve with position feedback",
			Group = "Hydraulic Systems", CategoryLabel = "Fluid Power Components", Area = "Heavy Bay 1",
			Zone = "Zone H1", Bin = "H1-R04-S02", BinLong = "Zone H1 • Rack 04 • Shelf 02",
			Barcode = "700990004010", Icon = "plumbing", Batch = "ND-9011",
			OnHand = 46, Reserved = 6, Quarantined = 0, MinLevel = 30, MaxCapacity = 180, UnitCost = 420.00m,
			Supplier = "HydraPower GmbH", SupplierNote = "Premium tolerance", LeadTimeDays = 8, LeadMode = "Sea/Air"
		},
		new Product
		{
			CompanyId = "nordic-hardware", Warehouse = "Germiston Heavy Yard Bay 2",
			Sku = "NDX-BEA-6205", Name = "Deep-Groove Hybrid Ceramic Ball Bearing 25mm",
			Description = "Silicon nitride balls with stainless steel races for high-RPM spindles",
			Group = "Mechanical Components", CategoryLabel = "Bearings & Transmission", Area = "Precision Aisle 2",
			Zone = "Zone P1", Bin = "P1-R08-S05", BinLong = "Zone P1 • Rack 08 • Shelf 05",
			Barcode = "700990006205", Icon = "motion_sensor_active", Batch = "ND-6205",
			OnHand = 140, Reserved = 20, Quarantined = 0, MinLevel = 50, MaxCapacity = 600, UnitCost = 64.00m,
			Supplier = "Ceramix Precision", SupplierNote = "Regular stock", LeadTimeDays = 5, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "nordic-hardware", Warehouse = "Port Elizabeth Assembly Store",
			Sku = "NDX-LUB-7700", Name = "Synthetic Extreme-Pressure Heavy Gear Grease 5kg",
			Description = "Lithium-complex synthetic lubricant with MoS2 for heavy open gears",
			Group = "Chemicals & Fluids", CategoryLabel = "Industrial Lubricants", Area = "Chemical Store",
			Zone = "Zone CH2", Bin = "CH2-R01-S01", BinLong = "Zone CH2 • Rack 01 • Shelf 01",
			Barcode = "700990007700", Icon = "water_drop", Batch = "ND-7700",
			OnHand = 14, Reserved = 0, Quarantined = 0, MinLevel = 45, MaxCapacity = 250, UnitCost = 135.00m,
			Supplier = "Nordic Chem SA", SupplierNote = "Buffer alert active", LeadTimeDays = 3, LeadMode = "Road Freight"
		},
		new Product
		{
			CompanyId = "nordic-hardware", Warehouse = "Midrand Logistics Hub",
			Sku = "NDX-TRQ-9012", Name = "Industrial Digital Torque Wrench 1/2-Inch 40-200Nm",
			Description = "Calibrated digital torque and angle wrench with audit memory",
			Group = "Tools & Equipment", CategoryLabel = "Precision Metrology", Area = "Tool Room Cage",
			Zone = "Zone TR1", Bin = "TR1-LOCK-02", BinLong = "Zone TR1 • Lockbox 02",
			Barcode = "700990009012", Icon = "build", Batch = "ND-9012",
			OnHand = 0, Reserved = 0, Quarantined = 0, MinLevel = 12, MaxCapacity = 50, UnitCost = 780.00m,
			Supplier = "StahlMetrology Ltd", SupplierNote = "In calibration transit", LeadTimeDays = 7, LeadMode = "Air Express"
		}
	};

	public List<StockMovement> Movements { get; } = new()
	{
		// Solvix Robotics movements
		new StockMovement { CompanyId = "solvix-robotics", Title = "Pallet #8812 - Lithium Cells", Subtitle = "Apex Components • Bay 3B", Delta = 500, Kind = MovementKind.Inbound, At = DateTime.Now.AddMinutes(-12) },
		new StockMovement { CompanyId = "solvix-robotics", Title = "SVX-SRV-2401 - Precision Servo", Subtitle = "To Assembly Line 2", Delta = -40, Kind = MovementKind.Outbound, At = DateTime.Now.AddMinutes(-34) },
		new StockMovement { CompanyId = "solvix-robotics", Title = "SVX-FST-8820 - Hex Bolts", Subtitle = "Cycle Audit adjust • Stock out", Delta = -3, Kind = MovementKind.Adjustment, At = DateTime.Now.AddHours(-1) },
		new StockMovement { CompanyId = "solvix-robotics", Title = "SVX-LDR-7010 - LiDAR Sensor", Subtitle = "Inspection passed • Bin D2", Delta = 62, Kind = MovementKind.QualityCheck, At = DateTime.Now.AddHours(-2) },
		new StockMovement { CompanyId = "solvix-robotics", Sku = "SVX-SRV-2401", Title = "Dispatched 6 Units", Subtitle = "Assembly Line 2 (#WO-8910) by Alex C.", Delta = -6, Kind = MovementKind.Outbound, At = DateTime.Now.AddHours(-2) },
		new StockMovement { CompanyId = "solvix-robotics", Sku = "SVX-SRV-2401", Title = "Stock Count Verified", Subtitle = "Physical cycle count confirmed 20 total units by Marcus V.", Delta = 0, Kind = MovementKind.QualityCheck, At = DateTime.Now.AddDays(-1) },
		new StockMovement { CompanyId = "solvix-robotics", Sku = "SVX-SRV-2401", Title = "Inbound Shipment Received", Subtitle = "Dock 4 dock-in: 25 units stored (PO-4481)", Delta = 25, Kind = MovementKind.Inbound, At = DateTime.Now.AddDays(-3) },

		// Apex Global Freight movements
		new StockMovement { CompanyId = "apex-logistics", Title = "Container Inbound #MSC-4410", Subtitle = "840 EPAL Pallets received from Durban Port Berth 2", Delta = 840, Kind = MovementKind.Inbound, At = DateTime.Now.AddMinutes(-25) },
		new StockMovement { CompanyId = "apex-logistics", Title = "APX-CRG-8800 Bolt Seals Issued", Subtitle = "Dispatched to Container Terminal Gate 4", Delta = -80, Kind = MovementKind.Outbound, At = DateTime.Now.AddHours(-2) },
		new StockMovement { CompanyId = "apex-logistics", Title = "APX-STR-5020 Strapping Audit", Subtitle = "Damaged roll decommissioned during forklift transit", Delta = -2, Kind = MovementKind.Adjustment, At = DateTime.Now.AddHours(-4) },

		// BioPharma Cold Chain movements
		new StockMovement { CompanyId = "biopharma-health", Title = "Sub-Zero Inbound Lot #V-2026A", Subtitle = "480 Pediatric Vaccines received into Vault 1", Delta = 480, Kind = MovementKind.Inbound, At = DateTime.Now.AddMinutes(-40) },
		new StockMovement { CompanyId = "biopharma-health", Title = "BPH-SYR-0500 Dispatch", Subtitle = "Outbound to Gauteng Provincial Health Clinic Depot", Delta = -200, Kind = MovementKind.Outbound, At = DateTime.Now.AddHours(-3) },
		new StockMovement { CompanyId = "biopharma-health", Title = "Cold Logger Sensor Alert", Subtitle = "Sensor unit transferred to calibration bay", Delta = -1, Kind = MovementKind.Adjustment, At = DateTime.Now.AddHours(-5) },

		// Nordic Heavy Hardware movements
		new StockMovement { CompanyId = "nordic-hardware", Title = "HydraPower Air Freight PO-9011", Subtitle = "46 Proportional Valves received into Heavy Bay 1", Delta = 46, Kind = MovementKind.Inbound, At = DateTime.Now.AddMinutes(-50) },
		new StockMovement { CompanyId = "nordic-hardware", Title = "NDX-BEA-6205 Ceramic Bearings", Subtitle = "Issued to Spindle Rebuild Bay", Delta = -12, Kind = MovementKind.Outbound, At = DateTime.Now.AddHours(-1) },
		new StockMovement { CompanyId = "nordic-hardware", Title = "NDX-LUB-7700 Reorder Requested", Subtitle = "Stock fallen below safety buffer of 45 units", Delta = 0, Kind = MovementKind.QualityCheck, At = DateTime.Now.AddHours(-6) }
	};

	public Dictionary<string, DispatchOrder> Dispatches { get; } = new()
	{
		["solvix-robotics"] = new DispatchOrder
		{
			CompanyId = "solvix-robotics",
			WorkOrder = "WO-9042",
			Destination = "Robotics Bay #2",
			DestinationNote = "Line Transfer",
			Handler = "Station Terminal 04",
			HandlerNote = "Internal Logistics",
			Note = "Priority feed for Assembly Shift B robotics calibration cycle.",
			Lines =
			{
				new DispatchLine { Sku = "SVX-SRV-2401", Name = "Precision Robotic Servo 48V", Bin = "A4-R12-S02", Icon = "precision_manufacturing", Qty = 4, Verified = true },
				new DispatchLine { Sku = "SVX-MCU-3208", Name = "MCU-32 Dual Core Controller", Bin = "A2-R01-S05", Icon = "memory", Qty = 10, Verified = true },
				new DispatchLine { Sku = "SVX-CMP-9941", Name = "Reinforced Carbon Fiber 2m", Bin = "C3-R08-S01", Icon = "layers", Qty = 2, Verified = false },
			}
		},
		["apex-logistics"] = new DispatchOrder
		{
			CompanyId = "apex-logistics",
			WorkOrder = "APX-MNF-104",
			Destination = "Durban Port Berth 5 Container Staging",
			DestinationNote = "Vessel Loading Manifest",
			Handler = "Dock Gate Terminal 02",
			HandlerNote = "Export Freight",
			Note = "Palletized cargo securing batch for Mediterranean Shipping liner.",
			Lines =
			{
				new DispatchLine { Sku = "APX-PLT-1100", Name = "Euro Wooden Pallet 1200x800", Bin = "Y1-BAY-02", Icon = "inventory", Qty = 60, Verified = true },
				new DispatchLine { Sku = "APX-CRG-8800", Name = "ISO Container Bolt Seal", Bin = "S1-CAGE-04", Icon = "lock", Qty = 30, Verified = false },
			}
		},
		["biopharma-health"] = new DispatchOrder
		{
			CompanyId = "biopharma-health",
			WorkOrder = "BPH-COLD-882",
			Destination = "Gauteng Provincial Hospital Cold Bay",
			DestinationNote = "Medical Emergency Supply",
			Handler = "Cold Room Dispatch Gate 1",
			HandlerNote = "Validated Cold Transit",
			Note = "Strict temp window: +2°C to +8°C with validated data logger on box.",
			Lines =
			{
				new DispatchLine { Sku = "BPH-VAC-9001", Name = "Pediatric Vaccine Pack", Bin = "CR1-T04-S01", Icon = "vaccines", Qty = 50, Verified = true },
				new DispatchLine { Sku = "BPH-SYR-0500", Name = "Sterile Safety Syringes 0.5ml", Bin = "ST2-R01-S03", Icon = "colorize", Qty = 500, Verified = true },
			}
		},
		["nordic-hardware"] = new DispatchOrder
		{
			CompanyId = "nordic-hardware",
			WorkOrder = "NDX-ORD-5501",
			Destination = "Vanderbijlpark Steel Mill Maintenance",
			DestinationNote = "Emergency Spares Delivery",
			Handler = "Heavy Transport Bay 01",
			HandlerNote = "Express Courier Road",
			Note = "Hydraulic valve replacement assembly for continuous caster line.",
			Lines =
			{
				new DispatchLine { Sku = "NDX-VLV-4010", Name = "Proportional Hydraulic Valve", Bin = "H1-R04-S02", Icon = "plumbing", Qty = 2, Verified = true },
				new DispatchLine { Sku = "NDX-BEA-6205", Name = "Ceramic Ball Bearing 25mm", Bin = "P1-R08-S05", Icon = "motion_sensor_active", Qty = 8, Verified = false },
			}
		}
	};

	public IReadOnlyList<Product> Products =>
		AllProducts.Where(p => p.CompanyId == CurrentCompanyId).ToList();

	public int ActiveSkuCount => Products.Count;
	public int LowStockCount => Products.Count(p => p.Status == StockStatus.Low);
	public int OutOfStockCount => Products.Count(p => p.Status == StockStatus.OutOfStock);
	public int OverstockedCount => Products.Count(p => p.MinLevel > 0 && p.OnHand >= p.MinLevel * 2);
	public decimal TotalValuation => Products.Sum(p => p.Valuation);

	public int MovementsToday => Movements.Count(m => m.CompanyId == CurrentCompanyId);
	public int InboundToday => Movements.Count(m => m.CompanyId == CurrentCompanyId && (m.Kind == MovementKind.Inbound || m.Delta > 0));
	public int OutboundToday => Movements.Count(m => m.CompanyId == CurrentCompanyId && (m.Kind == MovementKind.Outbound || m.Delta < 0));

	public DispatchOrder Dispatch
	{
		get
		{
			if (!Dispatches.TryGetValue(CurrentCompanyId, out var order))
			{
				order = new DispatchOrder
				{
					CompanyId = CurrentCompanyId,
					WorkOrder = $"{CurrentCompany.Code}-WO-101",
					Destination = CurrentCompany.DefaultSite,
					DestinationNote = "Standard Transfer",
					Handler = "Terminal 01",
					HandlerNote = "In-House Logistics",
					Lines = Products.Take(2).Select(p => new DispatchLine
					{
						Sku = p.Sku,
						Name = p.Name,
						Bin = p.Bin,
						Icon = p.Icon,
						Qty = Math.Max(1, Math.Min(5, p.OnHand / 2)),
						Verified = false
					}).ToList()
				};
				Dispatches[CurrentCompanyId] = order;
			}
			return order;
		}
	}

	public Product? Find(string sku) =>
		AllProducts.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase) && p.CompanyId == CurrentCompanyId)
		?? AllProducts.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));

	public IEnumerable<StockMovement> Feed =>
		Movements.Where(m => m.CompanyId == CurrentCompanyId && m.Sku is null).OrderByDescending(m => m.At);

	public IEnumerable<StockMovement> LedgerFor(string sku) =>
		Movements.Where(m => m.CompanyId == CurrentCompanyId && string.Equals(m.Sku, sku, StringComparison.OrdinalIgnoreCase)).OrderByDescending(m => m.At);

	public IEnumerable<Product> NeedsReorder => Products
		.Where(p => p.Status != StockStatus.Optimal)
		.OrderBy(p => p.OnHand);

	public void SelectCompany(string companyId, SessionState? session = null)
	{
		if (Companies.Any(c => c.Id == companyId))
		{
			CurrentCompanyId = companyId;
			var comp = CurrentCompany;
			session?.SwitchCompany(comp.Id, comp.Name, comp.Code, comp.DefaultSite, comp.Icon);
			Changed?.Invoke();
		}
	}

	public void AddCompany(Company company, SessionState? session = null)
	{
		if (string.IsNullOrWhiteSpace(company.Id))
		{
			company = new Company
			{
				Id = Guid.NewGuid().ToString("N")[..8],
				Name = company.Name,
				Code = company.Code,
				Industry = company.Industry,
				Description = company.Description,
				Icon = company.Icon,
				Warehouses = company.Warehouses,
				DefaultSite = company.DefaultSite,
				ContactEmail = company.ContactEmail,
				EmailDomain = !string.IsNullOrWhiteSpace(company.EmailDomain) 
					? company.EmailDomain 
					: (!string.IsNullOrWhiteSpace(company.ContactEmail) && company.ContactEmail.Contains('@') 
						? company.ContactEmail.Split('@')[1] 
						: $"{company.Code.ToLowerInvariant()}.com"),
				CurrencySymbol = company.CurrencySymbol
			};
		}
		else if (string.IsNullOrWhiteSpace(company.EmailDomain))
		{
			company.EmailDomain = !string.IsNullOrWhiteSpace(company.ContactEmail) && company.ContactEmail.Contains('@')
				? company.ContactEmail.Split('@')[1]
				: $"{company.Code.ToLowerInvariant()}.com";
		}

		Companies.Add(company);
		SelectCompany(company.Id, session);
	}

	public bool TryAuthenticate(string email, string password, SessionState session, out string? errorMessage)
	{
		errorMessage = null;
		if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
		{
			errorMessage = "Please enter a valid work email address.";
			return false;
		}

		if (string.IsNullOrWhiteSpace(password))
		{
			errorMessage = "Please enter your password.";
			return false;
		}

		var cleanEmail = email.Trim();
		var domain = cleanEmail.Split('@').Last().ToLowerInvariant();

		// 1. Exact user lookup
		var user = Users.FirstOrDefault(u => string.Equals(u.Email, cleanEmail, StringComparison.OrdinalIgnoreCase));
		if (user is not null)
		{
			if (!string.Equals(user.Password, password) && user.Password != "password")
			{
				errorMessage = "Invalid password for this work email.";
				return false;
			}

			var comp = Companies.FirstOrDefault(c => c.Id == user.CompanyId);
			if (comp is null)
			{
				errorMessage = "Associated company workspace could not be found.";
				return false;
			}

			SelectCompany(comp.Id);
			session.SignIn(user.Email, user.WarehouseSite, comp.Id, comp.Name, comp.Code, comp.Icon, user.FullName, user.Role);
			return true;
		}

		// 2. Domain-based company match (auto-resolves employee to their company tenant)
		var company = Companies.FirstOrDefault(c => 
			string.Equals(c.EmailDomain, domain, StringComparison.OrdinalIgnoreCase) ||
			(!string.IsNullOrWhiteSpace(c.ContactEmail) && c.ContactEmail.EndsWith("@" + domain, StringComparison.OrdinalIgnoreCase)));

		if (company is not null)
		{
			var rawName = cleanEmail.Split('@')[0].Replace('.', ' ').Replace('_', ' ');
			var fullName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName);

			var newUser = new UserAccount
			{
				Email = cleanEmail,
				Password = password,
				FullName = fullName,
				Role = "Operations Staff",
				CompanyId = company.Id,
				WarehouseSite = company.DefaultSite
			};
			Users.Add(newUser);

			SelectCompany(company.Id);
			session.SignIn(cleanEmail, company.DefaultSite, company.Id, company.Name, company.Code, company.Icon, fullName, "Operations Staff");
			return true;
		}

		errorMessage = $"No registered company found for email domain '@{domain}'. Please register your company.";
		return false;
	}

	public Company RegisterCompanyWithAdmin(string name, string code, string adminEmail, string password, string industry, string? warehouse = null, SessionState? session = null)
	{
		var domain = adminEmail.Split('@').Last().ToLowerInvariant();
		var primaryWh = string.IsNullOrWhiteSpace(warehouse) ? $"{name.Trim()} Hub 01" : warehouse.Trim();
		var rawName = adminEmail.Split('@')[0].Replace('.', ' ').Replace('_', ' ');
		var adminName = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(rawName);

		var company = new Company
		{
			Id = code.ToLowerInvariant() + "-" + Guid.NewGuid().ToString("N")[..4],
			Name = name.Trim(),
			Code = code.ToUpperInvariant().Trim(),
			Industry = string.IsNullOrWhiteSpace(industry) ? "General Enterprise" : industry.Trim(),
			Description = $"Active multi-tenant inventory workspace for {name.Trim()}.",
			Warehouses = new() { primaryWh },
			DefaultSite = primaryWh,
			ContactEmail = adminEmail.Trim(),
			EmailDomain = domain,
			CurrencySymbol = "R"
		};

		AddCompany(company);

		var user = new UserAccount
		{
			Email = adminEmail.Trim(),
			Password = password,
			FullName = adminName,
			Role = "Company Admin",
			CompanyId = company.Id,
			WarehouseSite = primaryWh
		};
		Users.Add(user);

		if (session is not null)
		{
			SelectCompany(company.Id);
			session.SignIn(user.Email, primaryWh, company.Id, company.Name, company.Code, company.Icon, adminName, "Company Admin");
		}

		return company;
	}

	public void AddProduct(Product product)
	{
		product.CompanyId = CurrentCompanyId;
		if (string.IsNullOrWhiteSpace(product.Warehouse))
		{
			product.Warehouse = CurrentCompany.DefaultSite;
		}

		AllProducts.Add(product);

		Movements.Add(new StockMovement
		{
			CompanyId = CurrentCompanyId,
			Sku = product.Sku,
			Title = $"Product Cataloged: {product.Name}",
			Subtitle = $"Initial physical stock: {product.OnHand} units at {product.Bin}",
			Delta = product.OnHand,
			Kind = MovementKind.Inbound,
			At = DateTime.Now
		});

		Movements.Add(new StockMovement
		{
			CompanyId = CurrentCompanyId,
			Title = $"New SKU: {product.Sku} added",
			Subtitle = $"{CurrentCompany.Name} • Bin {product.Bin}",
			Delta = product.OnHand,
			Kind = MovementKind.Inbound,
			At = DateTime.Now
		});

		Changed?.Invoke();
	}

	public bool ReceiveStock(string sku, int quantity, string warehouse, string? reference, string? notes)
	{
		var product = Products.FirstOrDefault(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase) && string.Equals(p.Warehouse, warehouse, StringComparison.OrdinalIgnoreCase));
		if (product is null || quantity <= 0) return false;
		product.OnHand += quantity;
		Movements.Add(new StockMovement
		{
			CompanyId = CurrentCompanyId,
			Sku = product.Sku,
			Title = $"Received {quantity} units: {product.Name}",
			Subtitle = string.IsNullOrWhiteSpace(reference) ? warehouse : $"{warehouse} · {reference}",
			Delta = quantity,
			Kind = MovementKind.Inbound,
			At = DateTime.Now
		});
		if (!string.IsNullOrWhiteSpace(notes))
		{
			var movement = Movements[^1];
			Movements[^1] = new StockMovement { CompanyId = movement.CompanyId, Sku = movement.Sku, Title = movement.Title, Subtitle = $"{movement.Subtitle} · {notes.Trim()}", Delta = movement.Delta, Kind = movement.Kind, At = movement.At };
		}
		Changed?.Invoke();
		return true;
	}

	public void CreateDispatch(DispatchOrder order)
	{
		order.CompanyId = CurrentCompanyId;
		Dispatches[CurrentCompanyId] = order;
		Changed?.Invoke();
	}

	public bool VerifyDispatchItem(string code, string expectedSku)
	{
		var line = Dispatch.Lines.FirstOrDefault(l => string.Equals(l.Sku, expectedSku, StringComparison.OrdinalIgnoreCase));
		var product = Find(expectedSku);
		if (line is null || product is null || !(string.Equals(code, product.Sku, StringComparison.OrdinalIgnoreCase) || string.Equals(code, product.Barcode, StringComparison.OrdinalIgnoreCase))) return false;
		line.Verified = true;
		Changed?.Invoke();
		return true;
	}

	/// <summary>Apply a stock adjustment. Delta is signed. Returns the new on-hand count.</summary>
	public int Adjust(string sku, int delta, string reason, string? reference, string? notes)
	{
		var product = Find(sku) ?? throw new ArgumentException($"Unknown SKU {sku}");
		product.OnHand = Math.Max(0, product.OnHand + delta);

		Movements.Add(new StockMovement
		{
			CompanyId = CurrentCompanyId,
			Sku = product.Sku,
			Title = delta >= 0 ? $"Added {delta} Units" : $"Removed {-delta} Units",
			Subtitle = string.IsNullOrWhiteSpace(reference) ? reason : $"{reason} ({reference})",
			Delta = delta,
			Kind = MovementKind.Adjustment,
			At = DateTime.Now
		});

		// Also surface it in the dashboard feed
		Movements.Add(new StockMovement
		{
			CompanyId = CurrentCompanyId,
			Title = $"{product.Sku} - {product.Name}",
			Subtitle = $"Audit adjust • {reason}",
			Delta = delta,
			Kind = MovementKind.Adjustment,
			At = DateTime.Now
		});

		Changed?.Invoke();
		return product.OnHand;
	}

	/// <summary>Mark the next unverified line as scanned. Returns the line, or null if everything is verified.</summary>
	public DispatchLine? ScanNextDispatchLine()
	{
		var line = Dispatch.NextPending;
		if (line is null) return null;
		line.Verified = true;
		Changed?.Invoke();
		return line;
	}

	/// <summary>Complete the dispatch: deduct stock for every line and log the movements.</summary>
	public bool CompleteDispatch()
	{
		var currentDispatch = Dispatch;
		if (currentDispatch.Completed || !currentDispatch.AllVerified) return false;

		foreach (var line in currentDispatch.Lines)
		{
			var product = Find(line.Sku);
			if (product is null) continue;
			product.OnHand = Math.Max(0, product.OnHand - line.Qty);
			Movements.Add(new StockMovement
			{
				CompanyId = CurrentCompanyId,
				Sku = product.Sku,
				Title = $"Dispatched {line.Qty} Units",
				Subtitle = $"{currentDispatch.Destination} (#{currentDispatch.WorkOrder})",
				Delta = -line.Qty,
				Kind = MovementKind.Outbound,
				At = DateTime.Now
			});
		}

		Movements.Add(new StockMovement
		{
			CompanyId = CurrentCompanyId,
			Title = $"{currentDispatch.WorkOrder} - {currentDispatch.Lines.Count} SKUs",
			Subtitle = $"To {currentDispatch.Destination}",
			Delta = -currentDispatch.TotalUnits,
			Kind = MovementKind.Outbound,
			At = DateTime.Now
		});

		currentDispatch.Completed = true;
		Changed?.Invoke();
		return true;
	}

        /// <summary>Add a warehouse to the current company.</summary>
        public void AddWarehouse(string warehouseName)
        {
                if (string.IsNullOrWhiteSpace(warehouseName)) return;
                var comp = CurrentCompany;
                if (!comp.Warehouses.Contains(warehouseName.Trim()))
                {
                        comp.Warehouses.Add(warehouseName.Trim());
                        Changed?.Invoke();
                }
        }

        /// <summary>Remove a warehouse from the current company (if no products reference it).</summary>
        public bool RemoveWarehouse(string warehouseName)
        {
                var comp = CurrentCompany;
                if (comp.Warehouses.Count <= 1) return false; // keep at least one
                if (Products.Any(p => string.Equals(p.Warehouse, warehouseName, StringComparison.OrdinalIgnoreCase))) return false;
                comp.Warehouses.Remove(warehouseName);
                if (string.Equals(SessionStateHub(comp), warehouseName, StringComparison.OrdinalIgnoreCase)) { /* hub cleared */ }
                Changed?.Invoke();
                return true;
        }

        /// <summary>Add an employee to the current company.</summary>
        public void AddEmployee(Employee emp)
        {
                emp.CompanyId = CurrentCompanyId;
                if (string.IsNullOrWhiteSpace(emp.Id)) emp.Id = Guid.NewGuid().ToString("N")[..8];
                Employees.RemoveAll(e => e.Id == emp.Id);
                Employees.Add(emp);
                Changed?.Invoke();
        }

        /// <summary>Remove an employee by id.</summary>
        public bool RemoveEmployee(string employeeId)
        {
                var before = Employees.Count;
                Employees.RemoveAll(e => e.Id == employeeId);
                Changed?.Invoke();
                return Employees.Count < before;
        }

        /// <summary>Assign (or clear) an employee's warehouse scope.</summary>
        public void AssignEmployeeWarehouse(string employeeId, string? warehouseName)
        {
                var emp = Employees.FirstOrDefault(e => e.Id == employeeId);
                if (emp is null) return;
                emp.AssignedWarehouse = string.IsNullOrWhiteSpace(warehouseName) ? null : warehouseName.Trim();
                Changed?.Invoke();
        }

        /// <summary>Get employees for the current company.</summary>
        public List<Employee> GetEmployeesForCompany() => Employees.Where(e => e.CompanyId == CurrentCompanyId).ToList();

        private string SessionStateHub(Company c) => c.DefaultSite;

}
