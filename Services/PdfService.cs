using EquipmentDechargeManager.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.IO;
using System.Threading.Tasks;

namespace EquipmentDechargeManager.Services;

public static class PdfService
{
    static PdfService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GeneratePdfBytes(Decharge decharge, string? logoPath = null)
    {
        var document = new DechargeDocumentTemplate(decharge, logoPath);
        return document.GeneratePdf();
    }

    public static async Task SavePdfToFileAsync(Decharge decharge, string targetFilePath, string? logoPath = null)
    {
        byte[] pdfBytes = GeneratePdfBytes(decharge, logoPath);
        await File.WriteAllBytesAsync(targetFilePath, pdfBytes);
    }
}
