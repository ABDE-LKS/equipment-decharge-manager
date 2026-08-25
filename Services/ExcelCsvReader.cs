using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EquipmentDechargeManager.Services;

public class RawFileData
{
    public List<string> Headers { get; set; } = new();
    public List<List<string>> Rows { get; set; } = new();
}

public static class ExcelCsvReader
{
    public static RawFileData ReadFile(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".xlsx")
        {
            return ReadExcel(filePath);
        }
        else if (ext == ".csv")
        {
            return ReadCsv(filePath);
        }
        else
        {
            throw new NotSupportedException("Format de fichier non pris en charge. Veuillez sélectionner un fichier .xlsx ou .csv.");
        }
    }

    private static RawFileData ReadExcel(string filePath)
    {
        var result = new RawFileData();
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null) return result;

        var firstRow = worksheet.Row(1);
        if (firstRow.IsEmpty()) return result;

        int colCount = firstRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
        for (int c = 1; c <= colCount; c++)
        {
            result.Headers.Add(firstRow.Cell(c).GetString().Trim());
        }

        var usedRows = worksheet.RowsUsed().Skip(1);
        foreach (var row in usedRows)
        {
            var rowValues = new List<string>();
            for (int c = 1; c <= colCount; c++)
            {
                rowValues.Add(row.Cell(c).GetString().Trim());
            }

            if (rowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
            {
                result.Rows.Add(rowValues);
            }
        }

        return result;
    }

    private static RawFileData ReadCsv(string filePath)
    {
        var result = new RawFileData();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null
        };

        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, config);

        if (csv.Read() && csv.ReadHeader())
        {
            result.Headers = csv.HeaderRecord?.ToList() ?? new List<string>();

            while (csv.Read())
            {
                var rowValues = new List<string>();
                for (int i = 0; i < result.Headers.Count; i++)
                {
                    rowValues.Add(csv.GetField(i)?.Trim() ?? string.Empty);
                }

                if (rowValues.Any(v => !string.IsNullOrWhiteSpace(v)))
                {
                    result.Rows.Add(rowValues);
                }
            }
        }

        return result;
    }
}
