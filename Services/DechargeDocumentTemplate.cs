using EquipmentDechargeManager.Data.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Text;

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
            page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Times New Roman"));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.PaddingBottom(6).Row(row =>
        {
            // Logo on the left - LARGER SIZE
            row.ConstantItem(120).MaxHeight(80).Element(logoContainer =>
            {
                if (!string.IsNullOrEmpty(_logoPath) && File.Exists(_logoPath))
                {
                    logoContainer.MaxHeight(80).MaxWidth(120).Image(_logoPath).FitArea();
                }
                else
                {
                    logoContainer.Text("SONATRACH").FontSize(16).Bold().FontColor(Colors.Black);
                }
            });

            // Organizational Hierarchy - aligned to the left
            row.RelativeItem().Column(col =>
            {
                col.Spacing(1);
                col.Item().AlignLeft().Text("EXPLORATION PRODUCTION").FontSize(9.5f).Bold();
                col.Item().AlignLeft().Text("DIVISION PRODUCTION").FontSize(9.5f).Bold();
                col.Item().AlignLeft().Text("DIRECTION REGIONALE").FontSize(9.5f).Bold();
                col.Item().AlignLeft().Text("HASSI R'MEL").FontSize(9.5f).Bold();
                col.Item().AlignLeft().Text("DIRECTION INFORMATIQUE").FontSize(9.5f).Bold();
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Spacing(12);

            // Title
            col.Item().PaddingTop(10).AlignCenter().Text("DECHARGE").FontSize(22).Bold().FontColor(Colors.Black);

            // Declaration opener
            col.Item().PaddingTop(6).Text("Je soussigné,").FontSize(11.5f);

            // Employee fields
            col.Item().Column(empCol =>
            {
                empCol.Spacing(7);

                empCol.Item().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(11.5f));
                    t.Span("NOM & PRENOM : ").Bold();
                    t.Span(_decharge.Employee?.FullName ?? "N/A");
                });

                empCol.Item().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(11.5f));
                    t.Span("MATRICULE : ").Bold();
                    t.Span(_decharge.Employee?.Matricule ?? "N/A");
                });

                empCol.Item().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(11.5f));
                    t.Span("FONCTION : ").Bold();
                    t.Span(_decharge.Employee?.Function ?? "N/A");
                });

                empCol.Item().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(11.5f));
                    t.Span("STRUCTURE : ").Bold();
                    t.Span(_decharge.Employee?.Structure ?? "N/A");
                });

                empCol.Item().Text(t =>
                {
                    t.DefaultTextStyle(x => x.FontSize(11.5f));
                    t.Span("REGION : ").Bold();
                    t.Span(_decharge.Employee?.Region ?? "N/A");
                });
            });

            // Loan Declaration Heading
            col.Item().PaddingTop(4).Column(declCol =>
            {
                declCol.Spacing(2);
                declCol.Item().Text("Avoir reçu, à titre de prêt, ce jour le matériel suivant :").FontSize(11.5f);
                declCol.Item().Text("(Indiquer marque, type, N° de série, N° Inventaire)").FontSize(9.5f).Italic();
            });

            // Equipment list — plain bulleted list
            col.Item().PaddingTop(2).Column(eqCol =>
            {
                eqCol.Spacing(6);

                foreach (var item in _decharge.Items)
                {
                    var eq = item.Equipment;
                    string designation = eq != null ? $"{eq.Type} {eq.Brand} {eq.Model}".Trim() : "Équipement";
                    while (designation.Contains("  ")) designation = designation.Replace("  ", " ");

                    bool hasDetails = eq != null && (
                        !string.IsNullOrWhiteSpace(eq.ShCode) ||
                        !string.IsNullOrWhiteSpace(eq.SerialNumber) ||
                        !string.IsNullOrWhiteSpace(eq.InventoryNumber));

                    if (hasDetails)
                    {
                        eqCol.Item().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(11.5f));
                            t.Span("- 01 ");
                            t.Span(designation + ",").Bold();
                        });

                        var details = new StringBuilder();
                        var parts = new System.Collections.Generic.List<string>();
                        if (!string.IsNullOrWhiteSpace(eq!.ShCode))
                            parts.Add($"Code SH : {eq.ShCode}");
                        if (!string.IsNullOrWhiteSpace(eq.SerialNumber))
                            parts.Add($"N° de série : {eq.SerialNumber}");
                        if (!string.IsNullOrWhiteSpace(eq.InventoryNumber))
                            parts.Add($"N° inv. : {eq.InventoryNumber}");
                        details.Append(string.Join(", ", parts));
                        details.Append('.');

                        eqCol.Item().PaddingLeft(14).Text(details.ToString()).FontSize(11.5f).Bold();
                    }
                    else
                    {
                        eqCol.Item().Text($"- 01 {designation}.").FontSize(11.5f);
                    }
                }
            });

            // Signatures block
            col.Item().Element(ComposeSignatures);
        });
    }

    private void ComposeSignatures(IContainer container)
    {
        container.PaddingTop(200).Row(row =>
        {
            row.RelativeItem().Column(sig =>
            {
                sig.Item().Text("Le CEDANT (Nom et signature)").FontSize(11);
                sig.Item().MinHeight(30);
            });

            row.RelativeItem().Column(sig =>
            {
                sig.Item().AlignCenter().Text("Le PRENEUR").FontSize(11);
                sig.Item().MinHeight(30);
            });

            row.RelativeItem().Column(sig =>
            {
                sig.Item().AlignRight().Text($"Le {_decharge.IssueDate:dd/MM/yyyy}").FontSize(11);
                sig.Item().MinHeight(30);
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        // Footer is intentionally left empty
    }
}