# 🖥️ Equipment Decharge Manager

A Windows desktop application for managing **equipment discharge forms (Décharges)** used at **Sonatrach — Direction Informatique, Hassi R'Mel**.

Built with **C# / .NET 10**, **Avalonia UI**, **MVVM architecture**, and **PostgreSQL**.

---

## 📋 Features

- **Dashboard** — Statistics overview of employees, equipment, and décharges
- **Employee Management** — Full CRUD with search and filtering
- **Equipment Management** — Inventory tracking with status management
- **Décharge Management** — Create, view, and manage equipment discharge forms
- **Décharge PDF Generation & Printing** — Generate official A4 PDF documents with the Sonatrach header, then preview, print, or save
- **Data Import** — Bulk import employees and equipment from Excel (.xlsx) or CSV files via a guided wizard
- **Bilingual UI** — French (default) and English

---

## ⚙️ Prerequisites

Before you begin, install the following on your machine:

| Tool | Version | Download |
|------|---------|----------|
| **.NET SDK** | 10.0 or later | https://dotnet.microsoft.com/download/dotnet/10.0 |
| **Docker Desktop** | Latest | https://www.docker.com/products/docker-desktop/ |
| **Git** | Latest | https://git-scm.com/downloads |

> **Note:** Docker is required to run the PostgreSQL database. If you prefer to install PostgreSQL directly (without Docker), see the [Manual PostgreSQL Setup](#-manual-postgresql-setup-without-docker) section below.

---

## 🚀 Setup Guide (Step-by-Step)

### 1. Clone the Repository

```bash
git clone https://github.com/YOUR_USERNAME/equipment-decharge-manager.git
cd equipment-decharge-manager
```

### 2. Start the PostgreSQL Database

Make sure **Docker Desktop is running**, then execute:

```bash
docker-compose up -d
```

This creates a PostgreSQL 16 container with:

| Parameter | Value |
|-----------|-------|
| Host | `127.0.0.1` |
| Port | `5435` |
| Database | `dechargedb` |
| Username | `decharge_user` |
| Password | `decharge_password` |

To verify the container is running:

```bash
docker ps
```

You should see `equipment_decharge_postgres` in the list.

### 3. Restore NuGet Packages

```bash
dotnet restore
```

### 4. Apply Database Migrations

The project uses **Entity Framework Core** migrations to create the database schema automatically.

Install the EF Core CLI tool (one-time):

```bash
dotnet tool install --global dotnet-ef
```

Then apply all migrations:

```bash
dotnet ef database update
```

This will create all the tables: `employees`, `equipments`, `decharges`, `decharge_items`, `equipment_returns`.

### 5. Build the Application

```bash
dotnet build
```

### 6. Run the Application

```bash
dotnet run
```

The application window will open. You're ready to go! 🎉

---

## 📁 Project Structure

```
Equipment Decharge Manager/
├── Assets/                        → Images & logos (sonatrach_logo.png)
├── Data/
│   ├── Entities/                  → EF Core entity classes
│   ├── DechargeDbContext.cs       → Database context & model configuration
│   └── DechargeDbContextFactory.cs → Design-time factory for EF migrations
├── Migrations/                    → EF Core database migrations
├── MarkupExtensions/              → Avalonia XAML helpers
├── Models/                        → Display / DTO models
├── Resources/                     → Localization files (Strings.resx, .fr, .en)
├── Services/
│   ├── DatabaseInitializer.cs     → DB connection & auto-migration at startup
│   ├── DechargeDocumentTemplate.cs → QuestPDF A4 layout template
│   ├── PdfService.cs              → PDF compilation service
│   ├── PrintService.cs            → Windows printing integration
│   ├── ExcelCsvReader.cs          → Excel/CSV file parser
│   └── DataImportService.cs       → Bulk import validation & DB insertion
├── ViewModels/                    → MVVM ViewModels (application logic)
├── Views/                         → Avalonia XAML views (UI)
├── docker-compose.yml             → PostgreSQL container definition
├── schema.dbml                    → Database schema documentation
└── EquipmentDechargeManager.csproj → Project file & NuGet dependencies
```

---

## 🗄️ Manual PostgreSQL Setup (Without Docker)

If you don't want to use Docker, install PostgreSQL directly:

1. Download and install **PostgreSQL 16+** from https://www.postgresql.org/download/
2. During installation, note the port (default `5432`)
3. Open **pgAdmin** or **psql** and create the database and user:

```sql
CREATE USER decharge_user WITH PASSWORD 'decharge_password';
CREATE DATABASE dechargedb OWNER decharge_user;
GRANT ALL PRIVILEGES ON DATABASE dechargedb TO decharge_user;
```

4. Update the connection string in these two files to match your setup (change port from `5435` to `5432` if needed):

   - [`Data/DechargeDbContextFactory.cs`](Data/DechargeDbContextFactory.cs) — Line 13
   - [`Services/DatabaseInitializer.cs`](Services/DatabaseInitializer.cs) — Line 12

```
Host=127.0.0.1;Port=5432;Database=dechargedb;Username=decharge_user;Password=decharge_password
```

5. Then continue with [Step 4: Apply Database Migrations](#4-apply-database-migrations).

---

## 🛠️ Useful Commands

| Action | Command |
|--------|---------|
| Start database | `docker-compose up -d` |
| Stop database | `docker-compose down` |
| Stop database & delete data | `docker-compose down -v` |
| Build project | `dotnet build` |
| Run project | `dotnet run` |
| Add a new EF migration | `dotnet ef migrations add MigrationName` |
| Apply migrations | `dotnet ef database update` |
| Check .NET version | `dotnet --version` |

---

## 📦 NuGet Dependencies

| Package | Purpose |
|---------|---------|
| **Avalonia** (12.1.1) | Cross-platform UI framework |
| **Avalonia.Desktop** | Windows desktop host |
| **Avalonia.Themes.Fluent** | Modern Fluent design theme |
| **CommunityToolkit.Mvvm** | MVVM source generators & commands |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | PostgreSQL EF Core provider |
| **EFCore.NamingConventions** | Snake_case column naming |
| **QuestPDF** | A4 PDF document generation |
| **ClosedXML** | Excel .xlsx file reading |
| **CsvHelper** | CSV file parsing |

---

## ⚠️ Troubleshooting

### Build fails with "file is locked"
Kill any running instance of the app first:
```powershell
taskkill /F /IM EquipmentDechargeManager.exe
dotnet build
```

### Database connection refused
Make sure Docker is running and the container is up:
```bash
docker-compose up -d
docker ps
```

### EF tool not found
Install it globally:
```bash
dotnet tool install --global dotnet-ef
```

### Port 5435 already in use
Either stop the conflicting service or change the port in `docker-compose.yml`:
```yaml
ports:
  - "5436:5432"   # Change 5435 to another port
```
Then update the connection strings in the code to match.

---

## 📄 License

Internal use — Sonatrach Direction Informatique.
