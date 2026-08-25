using PrimeERP.Domain.Contracts;
using PrimeERP.Application.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>
    /// يبني مستندات الطباعة من IPrintable فقط — لا يعرف Account/JournalEntry ولا أي Model، ولا يفتح أي نافذة
    /// بنفسه (DialogHost يتولّى العرض، نفس نمط IDialogService). كل قيمة بصرية من Resources/Print/PrintTheme.xaml
    /// حصراً — لا Resources/Themes إطلاقاً (الورق لا يتبدّل فاتح/داكن). راجع DESIGN_SYSTEM.md § أين تعيش القيم البصرية.
    /// </summary>
    public class PrintService : IPrintService
    {
        private readonly ISettingsService _settings;
        private readonly IDocumentExporter _exporter;
        private static ResourceDictionary _theme;

        public PrintService(ISettingsService settings, IDocumentExporter exporter)
        {
            _settings = settings;
            _exporter = exporter;
        }

        public IPrintDialogHost DialogHost { get; set; }

        private static ResourceDictionary Theme
        {
            get
            {
                if (_theme == null)
                {
                    // pack URI صريحة باسم التجميعة — تعمل بلا System.Windows.Application.Current قائم فعلياً (اختبارات xUnit مثلاً)،
                    // بخلاف Uri نسبية بسيطة التي تحتاج سياق تطبيق مُشغَّل لحلّها.
                    var asmName = typeof(PrintService).Assembly.GetName().Name;
                    var dict = new ResourceDictionary
                    {
                        Source = new Uri($"pack://application:,,,/{asmName};component/5.Design/Surfaces/PrintTheme.xaml", UriKind.Absolute)
                    };

                    // _theme مفرد ثابت واحد يُشارَك بين كل الخيوط التي تبني مستند طباعة (STA منفصلة متعددة عبر
                    // StaThreadHelper في الاختبارات، أو نافذة طباعة لاحقة في التطبيق) — Freezable (SolidColorBrush)
                    // يرتبط ضمنياً بخيط إنشائه ما لم يُجمَّد. بلا Freeze هنا، أول استخدام على خيط غير خيط أول تحميل
                    // للثيم يرمي "Cannot use a DependencyObject that belongs to a different thread" — اكتُشف فعلياً
                    // عند إضافة اختبار طباعة ثانٍ (F.2.4) يعمل على خيط STA مختلف عن أول اختبار طباعة (F.1.3).
                    foreach (var value in dict.Values)
                        if (value is Freezable freezable && freezable.CanFreeze)
                            freezable.Freeze();

                    _theme = dict;
                }
                return _theme;
            }
        }

        private static T Res<T>(string key) => (T)Theme[key];

        public Result<FixedDocument> Build(IPrintable document)
        {
            if (document == null)
                return Result.Fail<FixedDocument>("لا مستند لبنائه", ErrorCode.ValidationFailed);

            try
            {
                var flowDocument = BuildFlowDocument(document);
                var pageSize = PageSizeFor(document.Orientation);
                var fixedDocument = ConvertToFixedDocument(flowDocument, pageSize, document.ShowPageNumbers);
                return Result.Ok(fixedDocument);
            }
            catch (Exception ex)
            {
                return Result.Fail<FixedDocument>($"فشل بناء مستند الطباعة: {ex.Message}", ErrorCode.Unexpected);
            }
        }

        public Result Print(IPrintable document, bool showDialog = true)
        {
            var built = Build(document);
            if (built.IsFailure)
                return Result.Fail(built.ErrorMessage, built.ErrorCode);

            if (DialogHost == null)
                return Result.Fail("لا واجهة معروضة للطباعة (IPrintDialogHost غير مسجَّلة)", ErrorCode.Unexpected);

            if (showDialog)
                DialogHost.ShowPrintDialog(built.Value);
            else
                DialogHost.ShowPreview(built.Value, document.DocumentTitle);

            return Result.Ok();
        }

        public Result PrintPreview(IPrintable document)
        {
            var built = Build(document);
            if (built.IsFailure)
                return Result.Fail(built.ErrorMessage, built.ErrorCode);

            if (DialogHost == null)
                return Result.Fail("لا واجهة معروضة للمعاينة (IPrintDialogHost غير مسجَّلة)", ErrorCode.Unexpected);

            DialogHost.ShowPreview(built.Value, document.DocumentTitle);
            return Result.Ok();
        }

        /// <summary>
        /// يبني ملف PDF عبر IDocumentExporter (تنفيذه الفعلي ExportService في 6.UI، يُسجَّل في App.xaml.cs) —
        /// لا يعتمد PrintService على UI مباشرة (كان اعتماداً معكوساً Application→UI، أُصلح في R2 عبر هذا العقد
        /// في 3.Domain/Contracts، والحقن الحقيقي عبر DI في R3 — ExportService المُسجَّلة كـ IDocumentExporter).
        /// </summary>
        public Result ExportToPdf(IPrintable document, string path) =>
            _exporter.ExportPrintableToPdf(document, path);

        // ===== بناء المحتوى =====

        private FlowDocument BuildFlowDocument(IPrintable document)
        {
            var flow = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = Res<FontFamily>("FontFamilyPrimary"),
                FontSize = Res<double>("FontSizeBase"),
                Foreground = Res<Brush>("TextPrimary"),
                Background = Res<Brush>("SurfaceDefault"),
                PagePadding = new Thickness(40)
            };

            if (document.ShowCompanyHeader)
                flow.Blocks.Add(BuildCompanyHeader());

            flow.Blocks.Add(BuildTitle(document.DocumentTitle, document.DocumentSubtitle));

            if (document.HeaderFields is { Count: > 0 })
                flow.Blocks.Add(BuildKeyValues(document.HeaderFields));

            foreach (var section in document.BuildSections() ?? new List<PrintSection>())
                foreach (var block in BuildSectionBlocks(section))
                    flow.Blocks.Add(block);

            if (document.FooterFields is { Count: > 0 })
                flow.Blocks.Add(BuildKeyValues(document.FooterFields));

            if (document.ShowSignatures && document.SignatureLabels is { Count: > 0 })
                flow.Blocks.Add(BuildSignatures(document.SignatureLabels));

            return flow;
        }

        private Block BuildCompanyHeader()
        {
            var name = _settings.Get(SettingKeys.Company.Name, "");
            var tax  = _settings.Get(SettingKeys.Company.TaxNumber, "");
            var addr = _settings.Get(SettingKeys.Company.Address, "");

            var panel = new Paragraph
            {
                Margin = new Thickness(0, 0, 0, 12),
                Padding = new Thickness(0, 0, 0, 10),
                BorderBrush = Res<Brush>("OutlineDefault"),
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            panel.Inlines.Add(new Run(name) { FontSize = Res<double>("FontSizeXl"), FontWeight = Res<FontWeight>("FontWeightBold") });

            var sub = new List<string>();
            if (!string.IsNullOrWhiteSpace(tax))  sub.Add($"الرقم الضريبي: {tax}");
            if (!string.IsNullOrWhiteSpace(addr)) sub.Add(addr);

            if (sub.Count > 0)
            {
                panel.Inlines.Add(new LineBreak());
                panel.Inlines.Add(new Run(string.Join("  •  ", sub))
                {
                    FontSize = Res<double>("FontSizeSm"),
                    Foreground = Res<Brush>("TextSecondary")
                });
            }

            return panel;
        }

        private Block BuildTitle(string title, string subtitle)
        {
            var p = new Paragraph
            {
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 16, 0, 16)
            };
            p.Inlines.Add(new Run(title ?? "") { FontSize = Res<double>("FontSizeLg"), FontWeight = Res<FontWeight>("FontWeightSemiBold") });

            if (!string.IsNullOrWhiteSpace(subtitle))
            {
                p.Inlines.Add(new LineBreak());
                p.Inlines.Add(new Run(subtitle) { FontSize = Res<double>("FontSizeSm"), Foreground = Res<Brush>("TextSecondary") });
            }

            return p;
        }

        private Block BuildKeyValues(Dictionary<string, string> fields)
        {
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            for (int i = 0; i < fields.Count * 2; i++)
                table.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            var row = new TableRow();

            foreach (var (key, value) in fields)
            {
                row.Cells.Add(NewCell(key + ":", Res<Brush>("TextSecondary"), bold: false, align: TextAlignment.Right));
                row.Cells.Add(NewCell(value, Res<Brush>("TextPrimary"), bold: true, align: TextAlignment.Right));
            }

            group.Rows.Add(row);
            table.RowGroups.Add(group);
            return table;
        }

        private IEnumerable<Block> BuildSectionBlocks(PrintSection section)
        {
            switch (section.Type)
            {
                case PrintSectionType.Title:
                    yield return new Paragraph(new Run(section.Title))
                    {
                        FontSize = Res<double>("FontSizeMd"),
                        FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                        Margin = new Thickness(0, 12, 0, 6)
                    };
                    break;

                case PrintSectionType.Text:
                    yield return new Paragraph(new Run(section.Text)) { Margin = new Thickness(0, 4, 0, 4) };
                    break;

                case PrintSectionType.Spacer:
                    yield return new Paragraph { Margin = new Thickness(0, 8, 0, 0) };
                    break;

                case PrintSectionType.Callout:
                    yield return BuildCallout(section.Text, section.Variant ?? StatusVariant.Info);
                    break;

                case PrintSectionType.KeyValues:
                    if (!string.IsNullOrEmpty(section.Title))
                        yield return new Paragraph(new Run(section.Title))
                        {
                            FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                            Margin = new Thickness(0, 10, 0, 4)
                        };
                    if (section.KeyValues != null)
                        yield return BuildKeyValues(section.KeyValues);
                    break;

                case PrintSectionType.Table:
                    if (!string.IsNullOrEmpty(section.Title))
                        yield return new Paragraph(new Run(section.Title))
                        {
                            FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                            Margin = new Thickness(0, 10, 0, 4)
                        };
                    yield return BuildTable(section);
                    break;
            }
        }

        /// <summary>صندوق تحذير/معلومة بارز — الألوان الستة من PrintTheme فقط (Soft خلفية، SoftText نص، Solid حدّ)، نفس المجموعات الدلالية المستخدمة في الشاشة (Colors.xaml) وMلفات التصدير (ExportTheme)، بقيم ورق ثابتة.</summary>
        private Block BuildCallout(string text, StatusVariant variant)
        {
            return new Paragraph(new Run(text))
            {
                Background = Res<Brush>($"{variant}Soft"),
                Foreground = Res<Brush>($"{variant}SoftText"),
                BorderBrush = Res<Brush>($"{variant}Solid"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 12, 0, 12),
                FontWeight = Res<FontWeight>("FontWeightSemiBold")
            };
        }

        private Table BuildTable(PrintSection section)
        {
            var useArabicNumerals = _settings.Get(SettingKeys.UI.UseArabicNumerals, false);
            var culture = useArabicNumerals ? CultureInfo.GetCultureInfo("ar-SA") : CultureInfo.InvariantCulture;

            var columns = section.Columns ?? new List<PrintColumn>();
            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };

            foreach (var col in columns)
                table.Columns.Add(new TableColumn { Width = new GridLength(col.Width, GridUnitType.Star) });

            var headerGroup = new TableRowGroup();
            var headerRow = new TableRow { Background = Res<Brush>("HeaderBackground") };
            foreach (var col in columns)
                headerRow.Cells.Add(NewCell(col.Header, Res<Brush>("TextPrimary"), bold: true, align: AlignOf(col.Align)));
            headerGroup.Rows.Add(headerRow);
            table.RowGroups.Add(headerGroup);

            var bodyGroup = new TableRowGroup();
            foreach (var rowData in section.Rows ?? new List<Dictionary<string, object>>())
            {
                var isBold = section.RowBold?.Invoke(rowData) ?? false;
                var row = new TableRow();
                foreach (var col in columns)
                {
                    var raw = rowData.TryGetValue(col.Key, out var v) ? v : null;
                    var text = FormatValue(raw, col.Format, culture);
                    row.Cells.Add(NewCell(text, Res<Brush>("TextPrimary"), bold: isBold, align: AlignOf(col.Align)));
                }
                bodyGroup.Rows.Add(row);
            }
            table.RowGroups.Add(bodyGroup);

            if (section.Totals is { Count: > 0 })
            {
                var totalsGroup = new TableRowGroup();
                var totalsRow = new TableRow { Background = Res<Brush>("NeutralSoft") };
                foreach (var total in section.Totals)
                {
                    totalsRow.Cells.Add(NewCell(total.Label, Res<Brush>("TextSecondary"), total.IsBold, TextAlignment.Right));
                    totalsRow.Cells.Add(NewCell(total.Value, Res<Brush>("TextPrimary"), total.IsBold, TextAlignment.Right));
                }
                totalsGroup.Rows.Add(totalsRow);
                table.RowGroups.Add(totalsGroup);
            }

            return table;
        }

        private Block BuildSignatures(List<string> labels)
        {
            var table = new Table { Margin = new Thickness(0, 40, 0, 0), CellSpacing = 0 };
            foreach (var _ in labels)
                table.Columns.Add(new TableColumn());

            var group = new TableRowGroup();
            var row = new TableRow();
            foreach (var label in labels)
            {
                var cell = new TableCell(new Paragraph(new Run(label))
                {
                    TextAlignment = TextAlignment.Center,
                    Padding = new Thickness(0, 6, 0, 0),
                    BorderBrush = Res<Brush>("OutlineDefault"),
                    BorderThickness = new Thickness(0, 1, 0, 0)
                }) { Padding = new Thickness(20, 30, 20, 0) };
                row.Cells.Add(cell);
            }
            group.Rows.Add(row);
            table.RowGroups.Add(group);
            return table;
        }

        private TableCell NewCell(string text, Brush foreground, bool bold, TextAlignment align) =>
            new(new Paragraph(new Run(text ?? ""))
            {
                TextAlignment = align,
                FontWeight = bold ? Res<FontWeight>("FontWeightSemiBold") : Res<FontWeight>("FontWeightNormal"),
                Foreground = foreground,
                Margin = new Thickness(0)
            })
            { Padding = new Thickness(6, 4, 6, 4) };

        private static TextAlignment AlignOf(string align) => align switch
        {
            "Left"   => TextAlignment.Left,
            "Center" => TextAlignment.Center,
            _        => TextAlignment.Right
        };

        private static string FormatValue(object value, string format, CultureInfo culture)
        {
            if (value == null) return "";
            if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
                return formattable.ToString(format, culture);
            return value.ToString();
        }

        private static Size PageSizeFor(PrintOrientation orientation)
        {
            const double a4Width = 793.7, a4Height = 1122.5; // A4 عند 96 DPI
            return orientation == PrintOrientation.Landscape
                ? new Size(a4Height, a4Width)
                : new Size(a4Width, a4Height);
        }

        /// <summary>يحوّل FlowDocument المُرقَّم إلى FixedDocument (تقنية VisualBrush القياسية في WPF)، مع تذييل "صفحة X من Y" حقيقي لكل صفحة فعلية.</summary>
        private static FixedDocument ConvertToFixedDocument(FlowDocument flowDocument, Size pageSize, bool showPageNumbers)
        {
            flowDocument.PageWidth  = pageSize.Width;
            flowDocument.PageHeight = pageSize.Height;
            flowDocument.ColumnWidth = pageSize.Width;

            var paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
            paginator.PageSize = pageSize;

            // FlowDocument يستخدم DynamicDocumentPaginator — PageCount يبقى 0 حتى تُفرَض حسبة كاملة متزامنة.
            if (paginator is DynamicDocumentPaginator dynamicPaginator && !dynamicPaginator.IsPageCountValid)
                dynamicPaginator.ComputePageCount();

            var fixedDocument = new FixedDocument();
            int totalPages = paginator.PageCount;

            for (int i = 0; i < totalPages; i++)
            {
                var page = paginator.GetPage(i);

                var fixedPage = new FixedPage { Width = pageSize.Width, Height = pageSize.Height };
                var canvas = new Canvas
                {
                    Width = pageSize.Width,
                    Height = pageSize.Height,
                    Background = new VisualBrush(page.Visual) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }
                };
                fixedPage.Children.Add(canvas);

                if (showPageNumbers)
                {
                    var pageText = new System.Windows.Controls.TextBlock
                    {
                        Text = $"صفحة {i + 1} من {totalPages}",
                        FontFamily = Res<FontFamily>("FontFamilyPrimary"),
                        FontSize = Res<double>("FontSizeXs"),
                        Foreground = Res<Brush>("TextMuted"),
                        FlowDirection = FlowDirection.RightToLeft
                    };
                    FixedPage.SetLeft(pageText, pageSize.Width - 60);
                    FixedPage.SetTop(pageText, pageSize.Height - 24);
                    fixedPage.Children.Add(pageText);
                }

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                fixedDocument.Pages.Add(pageContent);
            }

            return fixedDocument;
        }
    }
}
