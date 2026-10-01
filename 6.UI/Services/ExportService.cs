using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Domain.Contracts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using ClosedXML.Excel;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.Design.Surfaces;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Display;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PrimeERP.Domain.Helpers;

namespace PrimeERP.UI.Services
{
    /// <summary>يصدّر بيانات AppDataGrid فعلياً</summary>
    public class ExportService : IExportService, IDocumentExporter
    {
        private readonly IPermissionService _permissions;
        private readonly ISettingsService _settings;

        static ExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public ExportService(IPermissionService permissions, ISettingsService settings)
        {
            _permissions = permissions;
            _settings = settings;
        }

        public void ExportToCsv(IEnumerable data, IEnumerable<GridColumn> columns, string filePath)
        {
            var cols = VisibleColumns(columns);
            using var writer = new StreamWriter(filePath, false, new UTF8Encoding(true));

            writer.WriteLine(string.Join(",", cols.Select(c => Quote(c.Header))));

            foreach (var item in data)
                writer.WriteLine(string.Join(",", cols.Select(c => Quote(GetValue(item, c)))));
        }

        public void ExportToExcel(IEnumerable data, IEnumerable<GridColumn> columns, string filePath)
        {
            var cols = VisibleColumns(columns);

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Data");
            sheet.RightToLeft = true;

            for (int i = 0; i < cols.Count; i++)
                sheet.Cell(1, i + 1).Value = cols[i].Header;
            sheet.Row(1).Style.Font.Bold = true;
            sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml(ExportTheme.HeaderBackgroundHex);

            int row = 2;
            foreach (var item in data)
            {
                for (int i = 0; i < cols.Count; i++)
                    sheet.Cell(row, i + 1).Value = GetValue(item, cols[i]);
                row++;
            }

            sheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        public void ExportToPdf(IEnumerable data, IEnumerable<GridColumn> columns, string filePath, string title)
        {
            var cols = VisibleColumns(columns);
            var rows = data.Cast<object>().ToList();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(PaperTheme.Points("PageMargin"));
                    page.DefaultTextStyle(x => x.FontFamily(ExportTheme.FontFamily).FontSize(ExportTheme.BodyFontSize));
                    page.ContentFromRightToLeft();

                    ComposeCompanyHeader(page.Header(), title, $"{DateTime.Now:yyyy-MM-dd HH:mm}");

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            foreach (var _ in cols)
                                def.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var col in cols)
                                header.Cell()
                                    .Background(ExportTheme.HeaderBackgroundHex)
                                    .Border(1).BorderColor(ExportTheme.OutlineHex)
                                    .Padding(5).AlignCenter()
                                    .Text(col.Header).FontSize(ExportTheme.HeaderFontSize).Bold();
                        });

                        foreach (var item in rows)
                            foreach (var col in cols)
                                table.Cell()
                                    .Border(1).BorderColor(ExportTheme.OutlineHex)
                                    .Padding(5)
                                    .Text(GetValue(item, col));
                    });

                    ComposeFooter(page.Footer());
                });
            })
            .GeneratePdf(filePath);
        }

        private void ComposeFooter(QuestPDF.Infrastructure.IContainer container) =>
            container.PaddingTop(6).BorderTop(1).BorderColor(ExportTheme.OutlineHex).PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(_settings.Get(SettingKeys.Company.Name, "")).FontSize(ExportTheme.BodyFontSize);
                row.RelativeItem().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                row.RelativeItem().AlignLeft().Text($"{DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(ExportTheme.BodyFontSize);
            });

        private void ComposeCompanyHeader(QuestPDF.Infrastructure.IContainer container, string title, string subtitle)
        {
            container.Column(col =>
            {
                RenderNode(col.Item(), CompanyHeaderComponent.Build(k => _settings.Get(k, "")));

                col.Item().AlignCenter().Text(title ?? "").FontSize(ExportTheme.HeaderFontSize).Bold();

                if (!string.IsNullOrWhiteSpace(subtitle))
                    col.Item().AlignCenter().Text(subtitle).FontSize(ExportTheme.BodyFontSize);
            });
        }

        private static void RenderNode(QuestPDF.Infrastructure.IContainer container, PaperNode node)
        {
            const float ToPoints = 0.75f;

            switch (node)
            {
                case PaperText text:
                    var styled = container.Text(text.Text);
                    if (text.Role == PaperTextRole.Name)
                        styled.FontSize(ExportTheme.TitleFontSize).Bold();
                    else
                        styled.FontSize(ExportTheme.BodyFontSize);
                    break;

                case PaperImage image when !string.IsNullOrWhiteSpace(image.Data):
                    container.MaxWidth((float)image.MaxWidth * ToPoints)
                             .Height((float)image.MaxHeight * ToPoints)
                             .AlignMiddle().Image(Convert.FromBase64String(image.Data)).FitHeight();
                    break;

                case PaperStack stack:
                    container.Column(col =>
                    {
                        foreach (var child in stack.Children) RenderNode(col.Item(), child);
                    });
                    break;

                case PaperRow row:
                    container.Row(line =>
                    {
                        var children = row.Children.ToList();
                        if (children.Count > 0) RenderNode(line.RelativeItem(), children[0]);
                        foreach (var child in children.Skip(1)) RenderNode(line.AutoItem(), child);
                    });
                    break;

                case PaperRule rule:
                    container.PaddingTop((float)rule.GapAbove * ToPoints)
                             .PaddingBottom((float)rule.GapBelow * ToPoints)
                             .BorderBottom((float)rule.Thickness * ToPoints)
                             .BorderColor(ExportTheme.BrandHex);
                    break;
            }
        }

        public Result ExportPrintableToPdf(IPrintable document, string path)
        {
            if (document == null)
                return Result.Fail("لا مستند لتصديره", ErrorCode.ValidationFailed);

            try
            {
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(document.Orientation == PrintOrientation.Landscape ? PageSizes.A4.Landscape() : PageSizes.A4);
                        page.Margin(PaperTheme.Points("PageMargin"));
                        page.DefaultTextStyle(x => x.FontFamily(ExportTheme.FontFamily).FontSize(ExportTheme.BodyFontSize));
                        page.ContentFromRightToLeft();

                        ComposeCompanyHeader(page.Header(), document.DocumentTitle, document.DocumentSubtitle);

                        page.Content().Column(col =>
                        {
                            if (document.HeaderFields is { Count: > 0 })
                                col.Item().PaddingBottom(8)
                                   .Text(string.Join("   ", document.HeaderFields.Select(kv => $"{kv.Key}: {kv.Value}")));

                            foreach (var section in document.BuildSections() ?? new List<PrintSection>())
                                RenderSection(col, section);

                            if (document.FooterFields is { Count: > 0 })
                                col.Item().PaddingTop(8)
                                   .Text(string.Join("   ", document.FooterFields.Select(kv => $"{kv.Key}: {kv.Value}")));

                            if (document.ShowSignatures && document.SignatureLabels is { Count: > 0 })
                                RenderSignatures(col, document.SignatureLabels);
                        });

                        ComposeFooter(page.Footer());
                    });
                })
                .GeneratePdf(path);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail($"فشل تصدير PDF: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        private static void RenderSection(QuestPDF.Fluent.ColumnDescriptor col, PrintSection section)
        {
            switch (section.Type)
            {
                case PrintSectionType.Title:
                    col.Item().PaddingTop(8).Text(section.Title).FontSize(ExportTheme.HeaderFontSize).Bold();
                    break;

                case PrintSectionType.Text:
                    col.Item().Text(section.Text);
                    break;

                case PrintSectionType.Spacer:
                    col.Item().Height(8);
                    break;

                case PrintSectionType.KeyValues:
                    if (!string.IsNullOrEmpty(section.Title))
                        col.Item().PaddingTop(8).Text(section.Title).Bold();
                    if (section.KeyValues != null)
                        col.Item().Text(string.Join("   ", section.KeyValues.Select(kv => $"{kv.Key}: {kv.Value}")));
                    break;

                case PrintSectionType.Table:
                    if (!string.IsNullOrEmpty(section.Title))
                        col.Item().PaddingTop(8).Text(section.Title).Bold();
                    RenderTable(col, section);
                    break;

                case PrintSectionType.Callout:
                    col.Item().PaddingVertical(8)
                       .Background(ExportTheme.SoftHex(section.Variant ?? StatusVariant.Info))
                       .Border(1).BorderColor(ExportTheme.SolidHex(section.Variant ?? StatusVariant.Info))
                       .Padding(8).Text(section.Text).Bold();
                    break;

                case PrintSectionType.Parties:
                    if (section.Parties is { Count: > 0 })
                        col.Item().PaddingBottom(10).Row(row =>
                        {
                            foreach (var party in section.Parties)
                                row.RelativeItem().PaddingHorizontal(4)
                                   .Background(ExportTheme.HeaderBackgroundHex)
                                   .Border(1).BorderColor(ExportTheme.OutlineHex).Padding(8).Column(box =>
                                   {
                                       box.Item().Text(party.Title).FontSize(ExportTheme.BodyFontSize)
                                          .FontColor(ExportTheme.TextSecondaryHex);
                                       box.Item().Text(party.Name).Bold();
                                       foreach (var line in party.Details ?? new List<string>())
                                           box.Item().Text(line).FontSize(ExportTheme.BodyFontSize);
                                   });
                        });
                    break;

                case PrintSectionType.AmountInWords:
                    col.Item().PaddingVertical(8).Background(ExportTheme.HeaderBackgroundHex)
                       .Border(1).BorderColor(ExportTheme.OutlineHex).Padding(8)
                       .Text(WordsOf(section)).Bold();
                    break;

                case PrintSectionType.Terms:
                    if (!string.IsNullOrWhiteSpace(section.Text))
                    {
                        col.Item().PaddingTop(14)
                           .Text(string.IsNullOrWhiteSpace(section.Title) ? "الشروط والأحكام" : section.Title).Bold();
                        col.Item().Text(section.Text).FontSize(ExportTheme.BodyFontSize)
                           .FontColor(ExportTheme.TextSecondaryHex);
                    }
                    break;

                case PrintSectionType.Barcode:
                    RenderBarcode(col, section.Text);
                    break;
            }
        }

        private static string WordsOf(PrintSection section) =>
            (string.IsNullOrWhiteSpace(section.Title) ? "مبلغاً وقدره" : section.Title) + ": " +
            ArabicNumberToWords.Convert(section.Amount,
                string.IsNullOrWhiteSpace(section.Currency) ? "جنيه" : section.Currency,
                string.IsNullOrWhiteSpace(section.SubUnit) ? "قرش" : section.SubUnit);

        private static void RenderBarcode(QuestPDF.Fluent.ColumnDescriptor col, string text)
        {
            var widths = Code128.Encode(text ?? "");
            if (widths.Count == 0) return;

            col.Item().PaddingTop(10).AlignCenter().Height(34).Row(row =>
            {
                for (var i = 0; i < widths.Count; i++)
                    row.ConstantItem(widths[i] * 0.85f)
                       .Background(i % 2 == 0 ? ExportTheme.TextPrimaryHex : ExportTheme.SurfaceHex);
            });

            col.Item().AlignCenter().Text(text).FontSize(ExportTheme.BodyFontSize);
        }

        private static void RenderTable(QuestPDF.Fluent.ColumnDescriptor col, PrintSection section)
        {
            var columns = section.Columns ?? new List<PrintColumn>();

            static string CellText(Dictionary<string, object> source, PrintColumn column)
            {
                var raw = source.TryGetValue(column.Key, out var v) ? v : null;
                return raw is IFormattable f && !string.IsNullOrEmpty(column.Format)
                    ? f.ToString(column.Format, CultureInfo.InvariantCulture)
                    : raw?.ToString() ?? "";
            }

            col.Item().Table(table =>
            {
                table.ColumnsDefinition(def =>
                {
                    foreach (var c in columns)
                        def.RelativeColumn((float)(c.Width <= 0 ? 1 : c.Width));
                });

                table.Header(header =>
                {
                    foreach (var c in columns)
                        header.Cell()
                            .Background(ExportTheme.HeaderBackgroundHex)
                            .Border(1).BorderColor(ExportTheme.OutlineHex)
                            .Padding(5).AlignCenter()
                            .Text(c.Header).FontSize(ExportTheme.HeaderFontSize).Bold();
                });

                foreach (var row in section.Rows ?? new List<Dictionary<string, object>>())
                    foreach (var c in columns)
                    {
                        var cell = table.Cell().Border(1).BorderColor(ExportTheme.OutlineHex).Padding(5);
                        (c.Align == "Center" ? cell.AlignCenter() : cell).Text(CellText(row, c));
                    }

                if (section.TotalsRow is { Count: > 0 })
                    foreach (var c in columns)
                    {
                        var cell = table.Cell().Background(ExportTheme.HeaderBackgroundHex)
                                        .Border(1).BorderColor(ExportTheme.OutlineHex).Padding(5);
                        (c.Align == "Center" ? cell.AlignCenter() : cell).Text(CellText(section.TotalsRow, c)).Bold();
                    }
            });

            if (section.Totals is { Count: > 0 })
                foreach (var total in section.Totals)
                    col.Item().PaddingTop(2)
                       .Background(total.IsBold ? ExportTheme.BrandSoftHex : ExportTheme.HeaderBackgroundHex)
                       .Padding(5).Row(line =>
                       {
                           line.RelativeItem();
                           line.ConstantItem(150).Text(total.Label).FontColor(ExportTheme.TextSecondaryHex);
                           line.ConstantItem(90).AlignCenter().Text(total.Value).Bold();
                       });
        }

        private static void RenderSignatures(QuestPDF.Fluent.ColumnDescriptor col, List<string> labels)
        {
            col.Item().PaddingTop(30).Row(row =>
            {
                foreach (var label in labels)
                    row.RelativeItem().PaddingRight(20).BorderTop(1).BorderColor(ExportTheme.OutlineHex).PaddingTop(4).AlignCenter().Text(label);
            });
        }

        private List<GridColumn> VisibleColumns(IEnumerable<GridColumn> columns) =>
            columns
                .Where(c => c.IsVisible && (string.IsNullOrEmpty(c.PermissionKey) || _permissions.Can(c.PermissionKey)))
                .ToList();

        private static string GetValue(object item, GridColumn column)
        {
            var prop = item.GetType().GetProperty(column.Binding, BindingFlags.Public | BindingFlags.Instance);
            var raw = prop?.GetValue(item);
            if (raw == null) return "";

            if (!string.IsNullOrEmpty(column.Format) && raw is System.IFormattable formattable)
                return formattable.ToString(column.Format, CultureInfo.InvariantCulture);

            return raw.ToString();
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Contains(',') || value.Contains('"') || value.Contains('\n')
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }
    }
}
