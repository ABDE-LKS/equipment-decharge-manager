# Equipment Décharge Manager - Project Structure & Code Documentation

## Table of Contents
1. [Project Overview](#project-overview)
2. [Root-Level Files](#root-level-files)
3. [Folder Structure & Contents](#folder-structure--contents)

---

## Project Overview

**Equipment Décharge Manager** is a desktop application for managing equipment loans and returns in organizations. Built with **Avalonia UI** and **Entity Framework Core** with **PostgreSQL** backend.

**Key Features:**
- Manage employees and their information
- Manage equipment inventory
- Create and track equipment decharges (loans)
- Mark returned equipment
- View history and dashboard summaries
- Print/export documents as PDF
- Import equipment records from CSV/Excel

---

## Root-Level Files

### `Program.cs`
**Purpose:** Application entry point and startup configuration.

**What it does:**
- Creates a single-instance mutex (`EquipmentDechargeManager_SingleInstance`) to ensure only one app instance runs at a time
- Calls `BuildAvaloniaApp()` to configure the Avalonia framework
- Initializes the desktop application lifetime with developer tools in DEBUG mode
- Uses Inter font for UI rendering

**Key Code Pattern:**
```csharp
[STAThread]
public static void Main(string[] args)
{
    using var mutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
    if (!createdNew)
        return; // Prevent multiple instances
    
    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
}
```

---

### `App.axaml.cs`
**Purpose:** Application lifecycle and initialization logic.

**What it does:**
- Loads XAML resources via `AvaloniaXamlLoader`
- Initializes the database during app startup via `DatabaseInitializer.InitializeAsync()`
- Creates the main window with `MainWindowViewModel`
- Displays database error dialogs if initialization fails
- Handles graceful shutdown if database setup is required

**Key Responsibilities:**
- Framework initialization
- Database validation
- Main window creation
- Error handling and UI feedback

---

### `ViewLocator.cs`
**Purpose:** Automatic view resolution from view models (convention-based routing).

**What it does:**
- Implements `IDataTemplate` to bind view models to their corresponding views
- Uses reflection to convert `EquipmentDechargeManager.ViewModels.XxxViewModel` → `EquipmentDechargeManager.Views.XxxView`
- Instantiates view controls dynamically when a view model is set as data context
- Returns "Not Found" message if view doesn't exist

**Example:**
- `DashboardViewModel` → `DashboardView`
- `EmployeesViewModel` → `EmployeesView`

---

### `EquipmentDechargeManager.csproj`
**Purpose:** Project configuration, dependencies, and build settings.

**Key Settings:**
- **Target Framework:** .NET 10.0 (net10.0)
- **Output Type:** WinExe (Windows desktop application)
- **Manifest:** app.manifest for Windows integration
- **Single Instance:** Task to kill previous instance before rebuild

**Main Dependencies:**
- **Avalonia** 12.1.1 - UI framework
- **EntityFrameworkCore** 10.0.11 - ORM with PostgreSQL
- **QuestPDF** 2026.8.0 - PDF generation
- **ClosedXML** 0.105.1 - Excel file handling
- **CsvHelper** 33.1.0 - CSV parsing
- **CommunityToolkit.Mvvm** 8.4.2 - MVVM utilities
- **Npgsql** - PostgreSQL database driver

---

### `appsettings.json`
**Purpose:** Database configuration file.

**Contains:**
```json
{
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Database": "equipment_decharge_manager",
    "Username": "postgres",
    "Password": "YOUR_PASSWORD"
  }
}
```

**Note:** This file is copied to output directory and is required for database connection.

---

### `README.md`
**Purpose:** User and developer documentation.

**Contains:**
- Project description and features
- Architecture overview
- Setup instructions
- Database schema documentation
- API/workflow descriptions

---

### `database.md`
**Purpose:** Database schema and entity relationships documentation.

**Contains:**
- Entity descriptions (Employee, Equipment, Decharge, DechargeItem)
- Field definitions and constraints
- Relationships and foreign keys
- Unique indexes

---

### `dotnet-tools.json`
**Purpose:** Local .NET tool configuration.

**Contains:** References to local development tools (e.g., Entity Framework CLI tools)

---

---

## Folder Structure & Contents

### 📁 `Views/` - UI Screens (XAML & Code-Behind)

**Purpose:** Avalonia XAML UI screens and their code-behind logic.

**Files:**

#### `MainWindow.axaml` / `MainWindow.axaml.cs`
- **Role:** Root application window
- **Contains:** Main layout with navigation bar, current view container, tab buttons
- **Displays:** Current active view based on `MainWindowViewModel.CurrentView`

#### `DashboardView.axaml` / `DashboardView.axaml.cs`
- **Role:** Home/dashboard screen
- **Displays:** Summary statistics, quick actions, recent decharges
- **Features:** Navigation shortcuts to other sections

#### `DechargesView.axaml` / `DechargesView.axaml.cs`
- **Role:** List of active decharges
- **Features:**
  - Search and filter decharges
  - Create new decharge button
  - List of employee decharges with status
  - Click to view details

#### `DechargeDetailsView.axaml` / `DechargeDetailsView.axaml.cs`
- **Role:** View and edit single decharge details
- **Features:**
  - Display all equipment items in decharge
  - Mark items as returned
  - Add/remove items
  - Print or export as PDF
  - Delete decharge

#### `EmployeesView.axaml` / `EmployeesView.axaml.cs`
- **Role:** Manage employees list
- **Features:**
  - View all employees
  - Search by name/matricule
  - Add new employee
  - Edit employee details
  - Delete employee (if no active decharges)

#### `EquipmentView.axaml` / `EquipmentView.axaml.cs`
- **Role:** Manage equipment inventory
- **Features:**
  - View all equipment
  - Filter by status (Available, Assigned, Maintenance)
  - Add new equipment
  - Edit equipment details
  - Track serial/inventory numbers

#### `HistoryView.axaml` / `HistoryView.axaml.cs`
- **Role:** View completed/closed decharges
- **Features:**
  - List of historical decharges
  - Search and filter
  - View details of past decharges

#### `DataImportWizardView.axaml` / `DataImportWizardView.axaml.cs`
- **Role:** Multi-step wizard for importing data
- **Steps:**
  1. Select file (CSV or Excel)
  2. Map columns to database fields
  3. Validate data
  4. Preview and confirm
  5. Import and display results

#### `SettingsView.axaml` / `SettingsView.axaml.cs`
- **Role:** Application settings and configuration
- **Features:**
  - Database connection settings
  - Localization/language selection
  - Application preferences
  - Logo upload for PDF exports

---

### 📁 `ViewModels/` - Presentation Logic (MVVM)

**Purpose:** Separation of UI logic from views, handles data binding and user commands.

**Base Class:**

#### `ViewModelBase.cs`
- **Base class:** Inherits from `CommunityToolkit.Mvvm.ObservableObject`
- **Purpose:** Common functionality for all view models (property change notifications)

**View Models:**

#### `MainWindowViewModel.cs`
- **Role:** Main application navigation controller
- **Properties:**
  - `CurrentView` - Current active view model
  - `ActiveTab` - Currently active tab name (Dashboard, Employees, Equipment, etc.)
  - `Is[Tab]Active` - Boolean flags for tab visibility
- **Commands:**
  - `NavigateDashboard()` - Switch to dashboard
  - `NavigateEmployees()` - Switch to employees list
  - `NavigateEquipment()` - Switch to equipment list
  - `NavigateHistory()` - Switch to history
  - `NavigateDecharges()` - Switch to decharges list
  - `NavigateDechargeDetails(id, openReturnModal)` - Open decharge details
  - `NavigateSettings()` - Switch to settings
  - `NavigateDataImportWizard()` - Open import wizard
- **Logic:** Switches views when tabs are clicked, manages which view is displayed

#### `DashboardViewModel.cs`
- **Role:** Home screen data and commands
- **Properties:**
  - `TotalEmployees` - Count of employees
  - `TotalEquipment` - Count of equipment
  - `ActiveDecharges` - Count of active decharges
  - `RecentDecharges` - List of latest decharges
  - Dashboard statistics
- **Commands:**
  - `LoadDashboardAsync()` - Fetch data from database
  - `NavigateToDetailsRequested` - Event to open decharge details

#### `DechargesViewModel.cs`
- **Role:** Manage active decharges (create, list, search, filter)
- **Properties:**
  - `ActiveDecharges` - Observable collection of decharges
  - `SelectedEmployeeFilter` - Filter by employee
  - `SearchText` - Search query
  - `IsCreateFormOpen` - Toggle create form visibility
  - `DechargeNumber` - New decharge number
  - `SelectedEmployee` - Employee for new decharge
  - `AvailableEquipment` - List of equipment available to assign
  - `NewDechargeItems` - Equipment items being added to new decharge
- **Commands:**
  - `LoadDechargesAsync()` - Fetch active decharges
  - `CreateDechargeAsync()` - Create new decharge
  - `AddEquipmentItemAsync(equipment)` - Add item to new decharge
  - `RemoveEquipmentItem(item)` - Remove item from new decharge
  - `DeleteDechargeAsync(id)` - Delete decharge
  - `SearchDecharges()` - Filter list by search text
- **Logic:** Manages create/update/delete operations, validates data, updates UI

#### `DechargeDetailsViewModel.cs`
- **Role:** Display and manage single decharge details
- **Properties:**
  - `Decharge` - Current decharge object
  - `Items` - Equipment items in decharge
  - `SelectedItem` - Currently selected item
  - `ConditionAtReturn` - Condition when item is returned
  - `Notes` - Decharge notes
- **Commands:**
  - `LoadDechargeAsync(id)` - Fetch decharge from database
  - `ReturnItemAsync(itemId)` - Mark equipment as returned
  - `SaveNotesAsync()` - Save decharge notes
  - `PrintPdfAsync()` - Generate and print PDF
  - `ExportPdfAsync()` - Save PDF to file
  - `DeleteDechargeAsync()` - Delete decharge
- **Logic:** Handles item return workflow, PDF generation

#### `EmployeesViewModel.cs`
- **Role:** Manage employees (CRUD operations)
- **Properties:**
  - `Employees` - Observable collection of all employees
  - `FilteredEmployees` - Search results
  - `SelectedEmployee` - Currently selected employee
  - `IsEditMode` - Toggle edit/view mode
  - Form fields: `FullName`, `Matricule`, `Function`, `Structure`, `Region`
- **Commands:**
  - `LoadEmployeesAsync()` - Fetch all employees
  - `CreateEmployeeAsync()` - Save new employee
  - `UpdateEmployeeAsync()` - Save employee changes
  - `DeleteEmployeeAsync(id)` - Delete employee
  - `SearchEmployees()` - Filter by search text
  - `EditEmployee(employee)` - Load employee for editing
  - `CancelEdit()` - Discard changes
- **Logic:** Full CRUD for employee management, validation

#### `EquipmentViewModel.cs`
- **Role:** Manage equipment inventory
- **Properties:**
  - `AllEquipment` - Observable collection of all equipment
  - `FilteredEquipment` - Based on status filter
  - `SelectedEquipment` - Currently selected item
  - `StatusFilter` - Filter by EquipmentStatus
  - Form fields: `Type`, `Brand`, `Model`, `SerialNumber`, `InventoryNumber`, `ShCode`
- **Commands:**
  - `LoadEquipmentAsync()` - Fetch all equipment
  - `CreateEquipmentAsync()` - Add new equipment
  - `UpdateEquipmentAsync()` - Update equipment
  - `DeleteEquipmentAsync(id)` - Delete equipment
  - `FilterByStatus(status)` - Filter equipment
- **Logic:** Full CRUD for equipment, status tracking

#### `HistoryViewModel.cs`
- **Role:** View completed decharges
- **Properties:**
  - `HistoricalDecharges` - Observable collection of closed decharges
  - `SearchText` - Search query
  - `DateRangeFilter` - Filter by date range
- **Commands:**
  - `LoadHistoryAsync()` - Fetch closed decharges
  - `SearchHistory()` - Filter by text
  - `FilterByDateRange()` - Filter by dates
  - `NavigateToDetails(id)` - Open decharge details
- **Logic:** Read-only view of historical data

#### `SettingsViewModel.cs`
- **Role:** Application settings
- **Properties:**
  - `DatabaseHost`, `DatabasePort`, `DatabaseName`, `Username` - Connection settings
  - `Language` - Language selection
  - `LogoPath` - Path to organization logo
- **Commands:**
  - `SaveSettingsAsync()` - Persist settings
  - `ResetSettingsAsync()` - Restore defaults
  - `TestDatabaseConnection()` - Validate connection
  - `BrowseLogoAsync()` - File picker for logo
- **Logic:** Manage app configuration

#### `DataImportWizardViewModel.cs`
- **Role:** Multi-step import wizard
- **Properties:**
  - `CurrentStep` - Current wizard step (1-5)
  - `SelectedFile` - Path to CSV/Excel file
  - `ColumnMapping` - Map file columns to database fields
  - `ValidationResult` - Import validation errors/warnings
  - `ImportType` - Employees or Equipment
- **Commands:**
  - `BrowseFileAsync()` - File picker
  - `LoadFileAsync()` - Load and parse file
  - `ValidateDataAsync()` - Validate before import
  - `ImportAsync()` - Execute import
  - `NextStep()` / `PreviousStep()` - Navigate wizard
- **Logic:** Guides user through import process with validation

---

### 📁 `Data/` - Database Models & Context

**Purpose:** Entity Framework Core definitions and database context.

#### `DechargeDbContext.cs`
- **Role:** Entity Framework DbContext for all database operations
- **DbSets:**
  - `DbSet<Employee>` - Employees
  - `DbSet<Equipment>` - Equipment
  - `DbSet<Decharge>` - Decharges (loans)
  - `DbSet<DechargeItem>` - Individual equipment items in a decharge
- **OnModelCreating():** Configures:
  - Unique indexes (Matricule, SerialNumber, InventoryNumber, DechargeNumber)
  - Field max lengths
  - Required fields
  - Relationships and foreign keys

#### `DechargeDbContextFactory.cs`
- **Role:** Factory for creating DbContext instances during migrations
- **Used by:** Entity Framework CLI for migrations
- **Purpose:** Provides connection configuration to EF migration tools

---

### 📁 `Data/Entities/` - Database Models

#### `Employee.cs`
- **Properties:**
  - `Id` - Primary key
  - `FullName` - Employee name (required, max 200 chars)
  - `Matricule` - Employee ID (required, unique, max 50 chars)
  - `Function` - Job title
  - `Structure` - Department/division
  - `Region` - Geographic region
  - `Decharges` - Collection of decharges assigned to employee
- **Methods:**
  - `DisplayName` - Returns formatted string for UI: "Matricule — Name — Function"
  - `ToString()` - Returns DisplayName

#### `Equipment.cs`
- **Properties:**
  - `Id` - Primary key
  - `Type` - Equipment type (required, e.g., "Laptop", "Monitor")
  - `Brand` - Manufacturer (required, e.g., "Dell", "HP")
  - `Model` - Model name (required)
  - `SerialNumber` - Unique serial number (optional, unique)
  - `InventoryNumber` - Organization inventory number (optional, unique)
  - `ShCode` - Internal code (optional)
  - `Status` - Current status (Available, Assigned, Maintenance)
  - `DechargeItems` - Collection of decharges containing this equipment
- **Display Methods:**
  - `DisplaySerialNumber` - Returns "—" if empty
  - `DisplayInventoryNumber` - Returns "—" if empty
  - `DisplayShCode` - Returns "—" if empty

#### `Decharge.cs`
- **Role:** Equipment loan/assignment record
- **Properties:**
  - `Id` - Primary key
  - `DechargeNumber` - Unique decharge number (e.g., "D-001-2026")
  - `EmployeeId` - Foreign key to Employee
  - `Employee` - Navigation to Employee
  - `IssueDate` - Date decharge was created
  - `Status` - Active/Closed (Active by default)
  - `Notes` - Optional notes
  - `Items` - Collection of equipment items
- **Purpose:** Represents a "loan" or "handover" of equipment to an employee

#### `DechargeItem.cs`
- **Role:** Individual item within a decharge
- **Properties:**
  - `Id` - Primary key
  - `DechargeId` - Foreign key to Decharge
  - `EquipmentId` - Foreign key to Equipment
  - `ConditionAtAssignment` - Condition when given (e.g., "New", "Good", "Fair")
  - `ConditionAtReturn` - Condition when returned
  - `ReturnDate` - Date item was returned (null if not yet returned)
  - `Decharge` - Navigation to Decharge
  - `Equipment` - Navigation to Equipment
- **Purpose:** Tracks individual equipment items and their condition at assignment/return

#### `EquipmentStatus.cs`
- **Role:** Enum for equipment status
- **Values:**
  - `Available` - Not assigned to anyone
  - `Assigned` - Currently assigned to employee
  - `Maintenance` - In repair or maintenance

---

### 📁 `Services/` - Business Logic & External Operations

**Purpose:** Reusable services for database, import, PDF, printing, and configuration.

#### `DatabaseConfiguration.cs`
- **Role:** Centralized database configuration management
- **Reads from:**
  - `appsettings.json` file
  - `DATABASE_URL` environment variable
  - Default hardcoded values
- **Main Methods:**
  - `GetSettings()` - Returns DatabaseSettings object
  - `GetConnectionString()` - Returns connection string for application
  - `GetAdminConnectionString()` - Returns admin connection (for database creation)
  - `FindConfigFilePath()` - Locates appsettings.json
  - `GetSetupInstructions()` - Returns setup guide for users
- **Purpose:** Single source of truth for database connection details

#### `DatabaseInitializer.cs`
- **Role:** Initialize database on application startup
- **Returns:** `DatabaseInitResult` (Success flag + error messages)
- **Main Method:** `InitializeAsync()`
  - Waits for PostgreSQL server to become reachable (10 retry attempts)
  - Creates database if it doesn't exist
  - Applies Entity Framework migrations
  - Seeds initial data if needed
- **Error Handling:** Returns detailed error messages and setup instructions for UI dialogs
- **Purpose:** Ensures database is ready before app starts

#### `ExcelCsvReader.cs`
- **Role:** Parse CSV and Excel files
- **Input:** File path to .xlsx or .csv
- **Output:** `RawFileData` object containing:
  - `Headers` - List of column names
  - `Rows` - List of row data (each row is List<string>)
- **Supports:**
  - Excel files (.xlsx) via ClosedXML
  - CSV files (.csv) via CsvHelper
- **Used by:** DataImportService for employee/equipment import

#### `DataImportService.cs`
- **Role:** Validate and import employee/equipment data
- **Main Classes:**
  - `ImportRowError` - Error details per row
  - `ImportValidationResult` - Validation results with lists of valid/invalid rows
- **Methods:**
  - `ValidateEmployeesAsync(rawData, columnMapping)` - Validate employee rows
  - `ValidateEquipmentAsync(rawData, columnMapping)` - Validate equipment rows
  - `ImportValidatedDataAsync(validationResult)` - Save to database
- **Validation Checks:**
  - Required fields present
  - Unique constraints (no duplicate matricules, serial numbers)
  - Data format validation
- **Purpose:** Safe data import with error reporting

#### `PdfService.cs`
- **Role:** PDF generation for decharges
- **Main Methods:**
  - `GeneratePdfBytes(decharge, logoPath)` - Generate PDF in memory
  - `SavePdfToFileAsync(decharge, targetFilePath, logoPath)` - Save to file
- **Template:** Uses `DechargeDocumentTemplate` for layout
- **Library:** QuestPDF Community Edition
- **Output:** Decharge document with employee/equipment details, signatures, etc.

#### `DechargeDocumentTemplate.cs`
- **Role:** PDF layout and formatting for decharges
- **Generates:** Professional decharge document with:
  - Organization logo (if provided)
  - Header with decharge number and date
  - Employee information (name, matricule, function)
  - Equipment list (type, brand, model, serial, condition)
  - Signature lines
  - Notes/remarks section
- **Library:** QuestPDF Fluent API

#### `PrintService.cs`
- **Role:** Print documents to physical printer
- **Main Method:** `PrintAsync(decharge, printerName)`
- **Purpose:** Direct printing of decharges to Windows printers
- **Note:** May use PdfService internally to generate PDF then send to print queue

#### `SettingsService.cs`
- **Role:** Manage application settings persistence
- **Stores:**
  - Database connection settings
  - UI preferences (language, theme)
  - Logo path for PDF exports
- **Persistence:** Saves to appsettings.json

---

### 📁 `Migrations/` - Database Schema History

**Purpose:** Entity Framework Core migration files that track database schema changes.

**Files:**
- `20260827085232_InitialCleanSchema.cs` - Initial database schema
- `20260827132019_RemoveDechargeUpdatedAt.cs` - Removed UpdatedAt column from Decharge
- `20260827132936_RemoveDechargeCreatedAt.cs` - Removed CreatedAt column from Decharge
- `.Designer.cs` files - Generated metadata for each migration
- `DechargeDbContextModelSnapshot.cs` - Current state of database model

**Purpose:** Allow version control of database changes, enable rollback/forward migrations

---

### 📁 `Assets/` - Images & Resources

**Purpose:** Static resources embedded in application.

**Contents:**
- Icons for UI buttons and menus
- Application images/logos
- Fonts (LatoFont/ subfolder)
- Localization strings for different languages (cs/, de/, es/, fr/, it/, ja/, ko/, pl/, etc.)

---

### 📁 `installer/` - Application Installer

#### `EquipmentDechargeManager.iss`
- **Purpose:** Inno Setup script for creating Windows installer
- **Generates:** Installer (.exe) for end users
- **Includes:** Application files, dependencies, start menu shortcuts, uninstaller
- **Output:** Stored in `Output/` subfolder after build

---

### 📁 `bin/` - Compiled Output

**Purpose:** Build artifacts.

**Structure:**
- `Debug/net10.0/` - Debug build output
- `Release/net10.0/` - Release build output
- **Contents:**
  - Compiled .exe and .dll files
  - appsettings.json
  - Dependencies
  - Runtime configuration files

---

### 📁 `obj/` - Build Temporary Files

**Purpose:** Intermediate build artifacts.

**Contents:**
- NuGet package references and metadata
- Project dependency information

---

---

## Application Workflow

### 1. **Startup Flow**
```
Program.Main()
  → Check single-instance mutex
  → BuildAvaloniaApp() + StartWithClassicDesktopLifetime()
  → App.OnFrameworkInitializationCompleted()
  → DatabaseInitializer.InitializeAsync()
    → Verify PostgreSQL server is reachable
    → Create database (if doesn't exist)
    → Run Entity Framework migrations
  → Create MainWindow with MainWindowViewModel
  → Display DashboardView
```

### 2. **Navigation Flow**
```
User clicks tab (e.g., "Employees")
  → MainWindowViewModel.NavigateEmployees()
  → CurrentView = new EmployeesViewModel()
  → ActiveTab = "Employees"
  → ViewLocator converts EmployeesViewModel → EmployeesView
  → EmployeesView displays with data from EmployeesViewModel
```

### 3. **Create Decharge Flow**
```
User clicks "Create Decharge" in DechargesView
  → DechargesViewModel.IsCreateFormOpen = true
  → User selects Employee + Equipment items
  → DechargesViewModel.CreateDechargeAsync()
  → Create new Decharge record in database
  → Add DechargeItem records for each equipment
  → Refresh DechargesViewModel.ActiveDecharges list
  → Display success message
```

### 4. **Return Equipment Flow**
```
User opens DechargeDetailsView
  → DechargeDetailsViewModel.LoadDechargeAsync(id)
  → Display all DechargeItems
  → User clicks "Return Item" on an item
  → DechargeDetailsViewModel.ReturnItemAsync(itemId)
  → Update DechargeItem.ConditionAtReturn and ReturnDate
  → Update Equipment.Status to Available
  → If all items returned → Mark Decharge as Closed
```

### 5. **Import Data Flow**
```
User opens DataImportWizardView
  → Step 1: Browse and select CSV/Excel file
    → ExcelCsvReader.ReadFile() → RawFileData
  → Step 2: Map file columns to database fields
  → Step 3: Validate data
    → DataImportService.ValidateEmployeesAsync()
    → Check duplicates, required fields, data format
  → Step 4: Review validation errors/warnings
  → Step 5: Confirm import
    → DataImportService.ImportValidatedDataAsync()
    → Save employees/equipment to database
```

### 6. **PDF Export Flow**
```
User opens DechargeDetailsView
  → User clicks "Export PDF"
  → DechargeDetailsViewModel.ExportPdfAsync()
  → PdfService.GeneratePdfBytes(decharge, logoPath)
    → DechargeDocumentTemplate renders PDF layout
  → PdfService.SavePdfToFileAsync(decharge, filePath)
  → Save file to user-selected location
  → Display success message
```

---

## Key Technologies & Libraries

| Technology | Version | Purpose |
|------------|---------|---------|
| .NET | 10.0 | Application runtime |
| Avalonia | 12.1.1 | Desktop UI framework |
| Entity Framework Core | 10.0.11 | ORM for database |
| PostgreSQL | 10+ | Database engine |
| QuestPDF | 2026.8.0 | PDF generation |
| ClosedXML | 0.105.1 | Excel file handling |
| CsvHelper | 33.1.0 | CSV parsing |
| Npgsql | Latest | PostgreSQL .NET driver |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM utilities |

---

## Database Connection

**Configuration Priority:**
1. `DATABASE_URL` environment variable (if set)
2. `appsettings.json` file (local configuration)
3. Default values (localhost:5432, postgres/empty password)

**Connection Validation:**
- Automatically tested on startup
- User informed with setup instructions if unable to connect
- Retry logic for temporary connection issues

---

## Summary

This is a well-structured **3-tier application** with:

| Layer | Folder | Purpose |
|-------|--------|---------|
| **Presentation** | `Views/`, `ViewModels/` | UI and presentation logic |
| **Business Logic** | `Services/` | Business rules, import, export |
| **Data Access** | `Data/`, `Migrations/` | Database models and EF Core |
| **Configuration** | Root files | App startup and settings |

The architecture follows **MVVM pattern** with **separation of concerns**, making it maintainable and testable.
