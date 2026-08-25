using EquipmentDechargeManager.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;

namespace EquipmentDechargeManager.Services;

public class DechargeDocumentTemplate : IDocument
{
    private readonly Decharge _decharge;
    private readonly string? _logoPath;

    public DechargeDocumentTemplate(Decharge decharge, string? logoPath = null)
    {
        _decharge = decharge;
        _logoPath = logoPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "sonatrach_logo.png");
        if (!File.Exists(_logoPath))
        {
            // Fallback to project root Assets folder if BaseDirectory path doesn't exist
            string projectLogo = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "sonatrach_logo.png");
            if (File.Exists(projectLogo))
            {
                _logoPath = projectLogo;
            }
        }
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(36);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Logo on the left
            row.ConstantItem(140).Height(50).Element(logoContainer =>
            {
                if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                {
                    logoContainer.Image(_logoPath);
                }
                else
                {
                    logoContainer.Text("SONATRACH").FontSize(14).Bold().FontColor(Colors.Orange.Darken2);
                }
            });

            // Center Organizational Hierarchy
            row.RelativeItem().Column(col =>
            {
                col.Item().AlignCenter().Text("EXPLORATION PRODUCTION").FontSize(9).Bold();
                col.Item().AlignCenter().Text("DIVISION PRODUCTION").FontSize(9).Bold();
                col.Item().AlignCenter().Text("DIRECTION REGIONALE HASSI R'MEL").FontSize(9).Bold();
                col.Item().AlignCenter().Text("DIRECTION INFORMATIQUE").FontSize(9).Bold();
            });

            // Blank spacer on right for balance
            row.ConstantItem(100);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(15).Column(col =>
        {
            col.Spacing(15);

            // Title Box
            col.Item().AlignCenter().Column(titleCol =>
            {
                titleCol.Item().Text("DÉCHARGE").FontSize(20).Bold().FontColor(Colors.Blue.Darken3).LetterSpacing(0.1f);
                titleCol.Item().PaddingTop(4).Text($"N° : {_decharge.DechargeNumber}  |  Date : {_decharge.IssueDate:dd/MM/yyyy}").FontSize(11).Medium();
            });

            // Employee Section Box
            col.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(12).Column(empCol =>
            {
                empCol.Spacing(6);
                empCol.Item().Text("INFORMATIONS DU BÉNÉFICIAIRE").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

                empCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Nom et Prénom : ").Bold();
                        t.Span(_decharge.Employee?.FullName ?? "N/A");
                    });
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Matricule : ").Bold();
                        t.Span(_decharge.Employee?.Matricule ?? "N/A");
                    });
                });

                empCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Fonction : ").Bold();
                        t.Span(_decharge.Employee?.Function ?? "N/A");
                    });
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Structure : ").Bold();
                        t.Span(_decharge.Employee?.Structure ?? "N/A");
                    });
                });

                empCol.Item().Row(row =>
                {
                    row.RelativeItem().Text(t =>
                    {
                        t.Span("Région : ").Bold();
                        t.Span(_decharge.Employee?.Region ?? "N/A");
                    });
                });
            });

            // Equipment Table Section
            col.Item().Column(eqCol =>
            {
                eqCol.Item().PaddingBottom(6).Text("ÉQUIPEMENTS ASSIGNÉS").FontSize(11).Bold().FontColor(Colors.Blue.Darken3);

                eqCol.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);   // N°
                        columns.RelativeColumn(3);    // Designation / Type
                        columns.RelativeColumn(2);    // N° Serie
                        columns.RelativeColumn(2);    // N° Inventaire
                        columns.RelativeColumn(1.5f); // Code SH
                        columns.RelativeColumn(2);    // Condition
                    });

                    // Table Header
                    table.Header(header =>
                    {
                        header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Medium).Padding(4).AlignCenter().Text("N°").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Équipement / Modèle").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("N° Série").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("N° Inventaire").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("Code SH").Bold();
                        header.Cell().Background(Colors.Grey.Lighten2).Border(1).BorderColor(Colors.Grey.Medium).Padding(4).Text("État").Bold();
                    });

                    int index = 1;
                    foreach (var item in _decharge.Items)
                    {
                        var eq = item.Equipment;
                        string title = eq != null ? $"{eq.Type} {eq.Brand} {eq.Model}".Trim() : "Équipement";
                        string serial = eq?.SerialNumber ?? "-";
                        string inv = eq?.InventoryNumber ?? "-";
                        string sh = eq?.ShCode ?? "-";
                        string cond = item.ConditionAtAssignment ?? "Bon";

                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).AlignCenter().Text($"{index:D2}");
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(title).Medium();
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(serial);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(inv);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(sh);
                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(5).Text(cond);

                        index++;
                    }
                });
            });

            // Notes / Remarks if present
            if (!string.IsNullOrWhiteSpace(_decharge.Notes))
            {
                col.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(notesCol =>
                {
                    notesCol.Item().Text("OBSERVATIONS :").Bold().FontSize(9);
                    notesCol.Item().Text(_decharge.Notes).FontSize(9).Italic();
                });
            }

            // Signatures Section
            col.Item().PaddingTop(20).Row(row =>
            {
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Height(90).Column(sig =>
                {
                    sig.Item().AlignCenter().Text("LE CÉDANT").Bold().FontSize(10);
                    sig.Item().AlignCenter().Text("(Nom, Prénom & Signature)").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                });

                row.ConstantItem(20);

                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Height(90).Column(sig =>
                {
                    sig.Item().AlignCenter().Text("LE PRENEUR").Bold().FontSize(10);
                    sig.Item().AlignCenter().Text("(Nom, Prénom & Signature)").FontSize(8).Italic().FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("Document généré par Equipment Decharge Manager").FontSize(8).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Page ");
                t.CurrentPageNumber();
                t.Span(" sur ");
                t.TotalPages();
            });
        });
    }
}
