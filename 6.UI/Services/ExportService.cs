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

                    // التقرير كان بلا ترويسة شركة ولا شعار ولا فوتر — عنوان عارٍ فقط.
                    ComposeCompanyHeader(page.Header(), title, $"{DateTime.Now:yyyy-MM-dd HH:mm}");

                    page.Content().PaddingTop(10).Table(table =>
                    {
                        table.ColumnsDefinition(def =>
                        {
                            foreach (var _ in cols)
                                def.RelativeColumn();
                        });

                        // نفس شبكة الطباعة: حدّ كامل لكل خلية وعنوان موسَّط — كان التصدير يرسم فاصلاً سفلياً
                        // فقط فيُقرأ الجدول ككتلة نص، بخلاف الفاتورة المطبوعة.
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

        /// <summary>ترويسة الشركة: الشعار وبياناتها — نقطة واحدة يستهلكها تصدير الشبكات وتصدير المستندات،
        /// فلا يظهر الشعار في الفاتورة ويغيب عن التقرير.</summary>
        private void ComposeCompanyHeader(QuestPDF.Infrastructure.IContainer container, string title, string subtitle)
        {
            var logo = System.Convert.FromBase64String(
                string.IsNullOrWhiteSpace(_settings.Get(SettingKeys.Company.LogoData, "")) ? "" : _settings.Get(SettingKeys.Company.LogoData, ""));

            var details = new[] { SettingKeys.Company.TaxNumber, SettingKeys.Company.Phone, SettingKeys.Company.Address }
                .Select(k => _settings.Get(k, "")).Where(v => !string.IsNullOrWhiteSpace(v)).ToList();

            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    if (logo.Length > 0) row.ConstantItem(56).Height(56).Image(logo).FitArea();

                    row.RelativeItem().PaddingHorizontal(8).Column(info =>
                    {
                        info.Item().Text(_settings.Get(SettingKeys.Company.Name, "")).FontSize(ExportTheme.TitleFontSize).Bold();
                        if (details.Count > 0)
                            info.Item().Text(string.Join("  •  ", details)).FontSize(ExportTheme.BodyFontSize);
                    });
                });

                col.Item().PaddingTop(6).AlignCenter().Text(title ?? "").FontSize(ExportTheme.HeaderFontSize).Bold();

                if (!string.IsNullOrWhiteSpace(subtitle))
                    col.Item().AlignCenter().Text(subtitle).FontSize(ExportTheme.BodyFontSize);
            });
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
                        header.Cell()
                            .Background(ExportTheme.HeaderBackgroundHex)
                            .Border(1).BorderColor(ExportTheme.OutlineHex)
                            .Padding(5).AlignCenter()
                            .Text(c.Header).FontSize(ExportTheme.HeaderFontSize).Bold();
                });

                foreach (var row in section.Rows ?? new List<Dictionary<string, object>>())
                    foreach (var c in columns)
                    {
                        var raw = row.TryGetValue(c.Key, out var v) ? v : null;
                        var text = raw is IFormattable f && !string.IsNullOrEmpty(c.Format)
                            ? f.ToString(c.Format, CultureInfo.InvariantCulture)
                            : raw?.ToString() ?? "";
                        table.Cell().Border(1).BorderColor(ExportTheme.OutlineHex).Padding(5).Text(text);
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
