using EquipmentDechargeManager.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.Services;

public enum ImportEntityType
{
    Employee,
    Equipment
}

public class ImportRowError
{
    public int RowNumber { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public class ImportValidationResult
{
    public int TotalRows { get; set; }
    public int ValidCount => ValidEmployeeRows.Count + ValidEquipmentRows.Count;
    public int DuplicateCount { get; set; }
    public int ErrorCount { get; set; }

    public List<Employee> ValidEmployeeRows { get; set; } = new();
    public List<Equipment> ValidEquipmentRows { get; set; } = new();
    public List<ImportRowError> Errors { get; set; } = new();
}

public static class DataImportService
{
    public static async Task<ImportValidationResult> ValidateEmployeesAsync(
        RawFileData rawData,
        Dictionary<string, string> columnMapping)
    {
        var result = new ImportValidationResult { TotalRows = rawData.Rows.Count };

        using var db = DatabaseInitializer.CreateDbContext();
        var existingMatricules = (await db.Employees.Select(e => e.Matricule.ToLower()).ToListAsync()).ToHashSet();
        var seenFileMatricules = new HashSet<string>();

        string GetVal(List<string> row, string dbField)
        {
            if (columnMapping.TryGetValue(dbField, out var header) && !string.IsNullOrEmpty(header))
            {
                int idx = rawData.Headers.IndexOf(header);
                if (idx >= 0 && idx < row.Count)
                {
                    return row[idx].Trim();
                }
            }
            return string.Empty;
        }

        for (int i = 0; i < rawData.Rows.Count; i++)
        {
            int rowNum = i + 2; // Row 1 is header
            var row = rawData.Rows[i];

            string fullName = GetVal(row, "full_name");
            string matricule = GetVal(row, "matricule");
            string function = GetVal(row, "function");
            string structure = GetVal(row, "structure");
            string region = GetVal(row, "region");

            if (string.IsNullOrWhiteSpace(fullName))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = matricule, Reason = "Le Nom & Prénom est requis." });
                continue;
            }

            if (string.IsNullOrWhiteSpace(matricule))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = fullName, Reason = "Le Matricule est requis." });
                continue;
            }

            string matLower = matricule.ToLower();
            if (seenFileMatricules.Contains(matLower))
            {
                result.DuplicateCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = matricule, Reason = $"Le matricule '{matricule}' est en double dans le fichier." });
                continue;
            }

            if (existingMatricules.Contains(matLower))
            {
                result.DuplicateCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = matricule, Reason = $"Le matricule '{matricule}' existe déjà dans la base de données." });
                continue;
            }

            seenFileMatricules.Add(matLower);
            result.ValidEmployeeRows.Add(new Employee
            {
                FullName = fullName,
                Matricule = matricule,
                Function = function,
                Structure = structure,
                Region = region
            });
        }

        return result;
    }

    public static async Task<ImportValidationResult> ValidateEquipmentAsync(
        RawFileData rawData,
        Dictionary<string, string> columnMapping)
    {
        var result = new ImportValidationResult { TotalRows = rawData.Rows.Count };

        using var db = DatabaseInitializer.CreateDbContext();
        var existingInventoryNums = (await db.Equipments.Where(e => !string.IsNullOrEmpty(e.InventoryNumber)).Select(e => e.InventoryNumber!.ToLower()).ToListAsync()).ToHashSet();
        var existingSerialNums = (await db.Equipments.Where(e => !string.IsNullOrEmpty(e.SerialNumber)).Select(e => e.SerialNumber!.ToLower()).ToListAsync()).ToHashSet();

        var seenInventoryNums = new HashSet<string>();
        var seenSerialNums = new HashSet<string>();

        string GetVal(List<string> row, string dbField)
        {
            if (columnMapping.TryGetValue(dbField, out var header) && !string.IsNullOrEmpty(header))
            {
                int idx = rawData.Headers.IndexOf(header);
                if (idx >= 0 && idx < row.Count)
                {
                    return row[idx].Trim();
                }
            }
            return string.Empty;
        }

        for (int i = 0; i < rawData.Rows.Count; i++)
        {
            int rowNum = i + 2;
            var row = rawData.Rows[i];

            string type = GetVal(row, "type");
            string brand = GetVal(row, "brand");
            string model = GetVal(row, "model");
            string serialNumber = GetVal(row, "serial_number");
            string inventoryNumber = GetVal(row, "inventory_number");
            string shCode = GetVal(row, "sh_code");

            if (string.IsNullOrWhiteSpace(type))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = inventoryNumber, Reason = "Le Type d'équipement est requis." });
                continue;
            }

            if (string.IsNullOrWhiteSpace(inventoryNumber))
            {
                result.ErrorCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = type, Reason = "Le N° Inventaire est requis." });
                continue;
            }

            string invLower = inventoryNumber.ToLower();
            if (seenInventoryNums.Contains(invLower))
            {
                result.DuplicateCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = inventoryNumber, Reason = $"Le N° inventaire '{inventoryNumber}' est en double dans le fichier." });
                continue;
            }

            if (existingInventoryNums.Contains(invLower))
            {
                result.DuplicateCount++;
                result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = inventoryNumber, Reason = $"Le N° inventaire '{inventoryNumber}' existe déjà dans la base de données." });
                continue;
            }

            if (!string.IsNullOrWhiteSpace(serialNumber))
            {
                string snLower = serialNumber.ToLower();
                if (seenSerialNums.Contains(snLower))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = serialNumber, Reason = $"Le N° de série '{serialNumber}' est en double dans le fichier." });
                    continue;
                }

                if (existingSerialNums.Contains(snLower))
                {
                    result.DuplicateCount++;
                    result.Errors.Add(new ImportRowError { RowNumber = rowNum, Key = serialNumber, Reason = $"Le N° de série '{serialNumber}' existe déjà dans la base de données." });
                    continue;
                }

                seenSerialNums.Add(snLower);
            }

            seenInventoryNums.Add(invLower);
            result.ValidEquipmentRows.Add(new Equipment
            {
                Type = type,
                Brand = brand,
                Model = model,
                SerialNumber = serialNumber,
                InventoryNumber = inventoryNumber,
                ShCode = string.IsNullOrWhiteSpace(shCode) ? null : shCode,
                Status = EquipmentStatus.Available
            });
        }

        return result;
    }

    public static async Task<int> ExecuteImportEmployeesAsync(List<Employee> employees)
    {
        if (employees == null || employees.Count == 0) return 0;

        using var db = DatabaseInitializer.CreateDbContext();
        await db.Employees.AddRangeAsync(employees);
        return await db.SaveChangesAsync();
    }

    public static async Task<int> ExecuteImportEquipmentAsync(List<Equipment> equipment)
    {
        if (equipment == null || equipment.Count == 0) return 0;

        using var db = DatabaseInitializer.CreateDbContext();
        await db.Equipments.AddRangeAsync(equipment);
        return await db.SaveChangesAsync();
    }
}
