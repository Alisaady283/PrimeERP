using PrimeERP.Domain.Contracts;
using PrimeERP.Application.Services;
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

namespace PrimeERP.UI.Services
{
    /// <summary>يصدّر بيانات AppDataGrid فعلياً — يحترم ColumnPermissions فلا يصدّر أي عمود محجوب عن المستخدم الحالي.</summary>
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
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily(ExportTheme.FontFamily).FontSize(ExportTheme.BodyFontSize));
                    page.ContentFromRightToLeft();

                    page.Header().Text(title ?? "").FontSize(ExportTheme.TitleFontSize).Bold();

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
                                header.Cell().Background(ExportTheme.HeaderBackgroundHex).Padding(4).Text(col.Header).FontSize(ExportTheme.HeaderFontSize).Bold();
                        });

                        foreach (var item in rows)
                            foreach (var col in cols)
                                table.Cell().Padding(4).BorderBottom(1).BorderColor(ExportTheme.OutlineHex).Text(GetValue(item, col));
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            })
            .GeneratePdf(filePath);
        }

        /// <summary>يبني PDF كامل (عناوين/أقسام متعدّدة/توقيعات) من IPrintable — لا يكرّر Document.Create/الترخيص/إعداد الصفحة، كله هنا في مكان واحد.</summary>
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
                        page.Margin(30);
                        page.DefaultTextStyle(x => x.FontFamily(ExportTheme.FontFamily).FontSize(ExportTheme.BodyFontSize));
                        page.ContentFromRightToLeft();

                        page.Header().Column(col =>
                        {
                            if (document.ShowCompanyHeader)
                                col.Item().Text(_settings.Get(SettingKeys.Company.Name, "")).FontSize(ExportTheme.TitleFontSize).Bold();

                            col.Item().AlignCenter().Text(document.DocumentTitle ?? "").FontSize(ExportTheme.HeaderFontSize).Bold();

                            if (!string.IsNullOrWhiteSpace(document.DocumentSubtitle))
                                col.Item().AlignCenter().Text(document.DocumentSubtitle).FontSize(ExportTheme.BodyFontSize);
                        });

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

                        if (document.ShowPageNumbers)
                            page.Footer().AlignCenter().Text(x =>
                            {
                                x.Span("صفحة ");
                                x.CurrentPageNumber();
                                x.Span(" من ");
                                x.TotalPages();
                            });
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
            }
        }

        private static void RenderTable(QuestPDF.Fluent.ColumnDescriptor col, PrintSection section)
        {
            var columns = section.Columns ?? new List<PrintColumn>();

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
                        header.Cell().Background(ExportTheme.HeaderBackgroundHex).Padding(4).Text(c.Header).FontSize(ExportTheme.HeaderFontSize).Bold();
                });

                foreach (var row in section.Rows ?? new List<Dictionary<string, object>>())
                    foreach (var c in columns)
                    {
                        var raw = row.TryGetValue(c.Key, out var v) ? v : null;
                        var text = raw is IFormattable f && !string.IsNullOrEmpty(c.Format)
                            ? f.ToString(c.Format, CultureInfo.InvariantCulture)
                            : raw?.ToString() ?? "";
                        table.Cell().Padding(4).BorderBottom(1).BorderColor(ExportTheme.OutlineHex).Text(text);
                    }
            });

            if (section.Totals is { Count: > 0 })
                col.Item().PaddingTop(4).Row(row =>
                {
                    foreach (var t in section.Totals)
                        row.RelativeItem().Text($"{t.Label}: {t.Value}");
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
