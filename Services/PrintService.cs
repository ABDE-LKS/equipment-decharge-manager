using System;
using System.Diagnostics;
using System.IO;

namespace EquipmentDechargeManager.Services;

public static class PrintService
{
    public static void PrintPdf(string pdfFilePath)
    {
        if (string.IsNullOrEmpty(pdfFilePath) || !File.Exists(pdfFilePath))
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pdfFilePath,
                UseShellExecute = true,
                Verb = "print"
            };
            Process.Start(psi);
        }
        catch
        {
            OpenPdf(pdfFilePath);
        }
    }

    public static void OpenPdf(string pdfFilePath)
    {
        if (string.IsNullOrEmpty(pdfFilePath) || !File.Exists(pdfFilePath))
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pdfFilePath,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open PDF: {ex.Message}");
        }
    }
}
