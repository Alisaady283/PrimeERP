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
using System.Windows.Media.Imaging;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>يبني مستندات الطباعة من IPrintable</summary>
    public class PrintService : IPrintService
    {
        private readonly ISettingsService _settings;
        private readonly IDocumentExporter _exporter;

        public PrintService(ISettingsService settings, IDocumentExporter exporter)
        {
            _settings = settings;
            _exporter = exporter;
        }

        public IPrintDialogHost DialogHost { get; set; }


        private static T Res<T>(string key) => PaperTheme.Value<T>(key);

        public Result<FlowDocument> BuildContent(IPrintable document)
        {
            if (document == null)
                return Result.Fail<FlowDocument>("لا مستند لبنائه", ErrorCode.ValidationFailed);

            try { return Result.Ok(BuildFlowDocument(document)); }
            catch (Exception ex) { return Result.Fail<FlowDocument>($"فشل بناء مستند الطباعة: {ex.Message}", ErrorCode.Unexpected); }
        }

        public Result<FixedDocument> Build(IPrintable document)
        {
            if (document == null)
                return Result.Fail<FixedDocument>("لا مستند لبنائه", ErrorCode.ValidationFailed);

            try
            {
                var pageSize = PageSizeFor(document.Orientation, document.HalfPage);
                var labels = document.CopyLabels is { Count: > 0 } ? document.CopyLabels : new List<string> { null };

                var combined = new FixedDocument();
                foreach (var label in labels)
                {
                    var watermark = labels.IndexOf(label) == 0 ? null : label;

                    foreach (var page in BuildPages(BuildFlowDocument(document, label), pageSize, document.ShowPageNumbers, watermark))
                        combined.Pages.Add(page);
                }

                return Result.Ok(combined);
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

        public Result ExportToPdf(IPrintable document, string path) =>
            _exporter.ExportPrintableToPdf(document, path);


        private FlowDocument BuildFlowDocument(IPrintable document, string copyLabel = null)
        {
            var flow = new FlowDocument
            {
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = Res<FontFamily>("FontFamilyPrimary"),
                FontSize = Res<double>("FontSizeBase"),
                Foreground = Res<Brush>("TextPrimary"),
                Background = Res<Brush>("SurfaceDefault"),
                PagePadding = new Thickness(Res<double>("PageMargin"))
            };

            if (document.ShowCompanyHeader)
                flow.Blocks.Add(BuildCompanyHeader());

            flow.Blocks.Add(BuildTitle(document.DocumentTitle, document.DocumentSubtitle));

            if (!string.IsNullOrWhiteSpace(copyLabel))
                flow.Blocks.Add(new Paragraph(new Run(copyLabel))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = Res<double>("FontSizeSm"),
                    Foreground = Res<Brush>("TextSecondary"),
                    Margin = new Thickness(0, 0, 0, 8)
                });

            if (document.HeaderFields is { Count: > 0 })
                flow.Blocks.Add(BuildKeyValues(document.HeaderFields));

            foreach (var section in document.BuildSections() ?? new List<PrintSection>())
                foreach (var block in BuildSectionBlocks(section, document.LinesPerPage, ContentWidthFor(document)))
                    flow.Blocks.Add(block);

            if (document.FooterFields is { Count: > 0 })
                flow.Blocks.Add(BuildKeyValues(document.FooterFields));

            if (document.ShowSignatures && document.SignatureLabels is { Count: > 0 })
                flow.Blocks.Add(BuildSignatures(document.SignatureLabels));

            return document.Framed ? Frame(flow) : flow;
        }

        private static double ContentWidthFor(IPrintable document)
        {
            var margin = Res<double>("PageMargin");
            var width = PageSizeFor(document.Orientation, document.HalfPage).Width - margin * 2;
            if (document.Framed) width -= margin + 3;

            return width - 12;
        }

        private string FillToEdge(List<string> parts, List<double> shares, double width)
        {
            const string gap = "  ";
            var available = width - Measure(gap) * (parts.Count - 1);

            var filled = new string[parts.Count];
            var spent = 0d;

            for (var index = 0; index < parts.Count; index++)
            {
                var target = index == parts.Count - 1 ? available - spent : available * shares[index];

                filled[index] = Pad(parts[index], target);
                spent += Measure(filled[index]);
            }

            return string.Join(gap, filled);
        }

        private string Pad(string text, double target)
        {
            var dots = (int)Math.Floor((target - Measure(text)) / Measure("."));

            return dots <= 0 ? text : text + new string('.', dots);
        }

        private double Measure(string text) =>
            new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.RightToLeft,
                new Typeface(Res<FontFamily>("FontFamilyPrimary"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                Res<double>("FontSizeBase"), Brushes.Black, 1.0).WidthIncludingTrailingWhitespace;

        private FlowDocument Frame(FlowDocument flow)
        {
            var inner = new Section
            {
                BorderBrush = Res<Brush>("TextPrimary"),
                BorderThickness = new Thickness(1.5),
                Padding = new Thickness(Res<double>("PageMargin") / 2),
            };

            foreach (var block in flow.Blocks.ToList())
            {
                flow.Blocks.Remove(block);
                inner.Blocks.Add(block);
            }

            flow.Blocks.Add(inner);
            return flow;
        }

        private Block BuildCompanyHeader() =>
            new BlockUIContainer(PaperNodeRenderer.ToElement(
                CompanyHeaderComponent.Build(key => _settings.Get(key, "")), PaperTheme.Raw));

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
            const int PairsPerRow = 2;

            var table = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 12) };
            for (int i = 0; i < PairsPerRow; i++)
            {
                table.Columns.Add(new TableColumn { Width = new GridLength(0.28, GridUnitType.Star) });
                table.Columns.Add(new TableColumn { Width = new GridLength(0.72, GridUnitType.Star) });
            }

            var group = new TableRowGroup();
            TableRow row = null;
            var index = 0;

            foreach (var (key, value) in fields)
            {
                if (index % PairsPerRow == 0) { row = new TableRow(); group.Rows.Add(row); }

                row.Cells.Add(NewCell(key + ":", Res<Brush>("TextSecondary"), bold: false, align: TextAlignment.Right, bare: true));
                row.Cells.Add(BoxedValue(value));
                index++;
            }

            while (row != null && index % PairsPerRow != 0)
            {
                row.Cells.Add(NewCell("", Res<Brush>("TextSecondary"), bold: false, align: TextAlignment.Right, bare: true));
                row.Cells.Add(NewCell("", Res<Brush>("TextPrimary"), bold: false, align: TextAlignment.Right, bare: true));
                index++;
            }

            table.RowGroups.Add(group);
            return table;
        }

        private TableCell BoxedValue(string value) =>
            new(new Paragraph(new Run(value ?? ""))
            {
                TextAlignment = TextAlignment.Center,
                FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                Foreground = Res<Brush>("TextPrimary"),
                Margin = new Thickness(0),
                Padding = new Thickness(8, 4, 8, 4),
                BorderBrush = Res<Brush>("OutlineDefault"),
                BorderThickness = new Thickness(1)
            })
            { Padding = new Thickness(2, 3, 8, 3) };

        private IEnumerable<Block> BuildSectionBlocks(PrintSection section, int linesPerPage, double contentWidth)
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
                    var filled = section.FillParts is { Count: > 0 };
                    yield return new Paragraph(new Run(filled
                        ? FillToEdge(section.FillParts, section.FillShares, contentWidth)
                        : section.Text))
                    {
                        Margin = new Thickness(0, 2, 0, 2),
                        TextAlignment = filled ? TextAlignment.Right : TextAlignment.Justify
                    };
                    break;

                case PrintSectionType.Spacer:
                    yield return new Paragraph { Margin = new Thickness(0, 8, 0, 0) };
                    break;

                case PrintSectionType.Callout:
                    yield return BuildCallout(section.Text, section.Variant ?? StatusVariant.Info, boxed: section.Variant == StatusVariant.Neutral);
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

                case PrintSectionType.Parties:
                    if (section.Parties is { Count: > 0 }) yield return BuildParties(section.Parties);
                    break;

                case PrintSectionType.AmountInWords:
                    yield return BuildCallout(
                        (string.IsNullOrWhiteSpace(section.Title) ? "مبلغاً وقدره" : section.Title) + ": " +
                        PrimeERP.Domain.Helpers.ArabicNumberToWords.Convert(section.Amount,
                            string.IsNullOrWhiteSpace(section.Currency) ? "جنيه" : section.Currency,
                            string.IsNullOrWhiteSpace(section.SubUnit) ? "قرش" : section.SubUnit),
                        StatusVariant.Neutral);
                    break;

                case PrintSectionType.Terms:
                    if (!string.IsNullOrWhiteSpace(section.Text))
                    {
                        yield return new Paragraph(new Run(string.IsNullOrWhiteSpace(section.Title) ? "الشروط والأحكام" : section.Title))
                        {
                            FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                            Margin = new Thickness(0, 14, 0, 4)
                        };
                        yield return new Paragraph(new Run(section.Text))
                        {
                            FontSize = Res<double>("FontSizeXs"),
                            Foreground = Res<Brush>("TextSecondary")
                        };
                    }
                    break;

                case PrintSectionType.Barcode:
                    if (BuildBarcode(section.Text) is { } barcode) yield return barcode;
                    break;

                case PrintSectionType.Table:
                    if (!string.IsNullOrEmpty(section.Title))
                        yield return new Paragraph(new Run(section.Title))
                        {
                            FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                            Margin = new Thickness(0, 10, 0, 4)
                        };
                    foreach (var table in BuildTablePages(section, linesPerPage)) yield return table;
                    break;
            }
        }

        private Block BuildCallout(string text, StatusVariant variant, bool boxed = false)
        {
            if (boxed)
            {
                var table = new Table { Margin = new Thickness(0, 8, 0, 12) };
                table.Columns.Add(new TableColumn { Width = new GridLength(2, GridUnitType.Star) });
                table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

                var row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph()));
                row.Cells.Add(new TableCell(new Paragraph(new Run(text))
                {
                    TextAlignment = TextAlignment.Center,
                    FontSize = Res<double>("FontSizeLg"),
                    FontWeight = Res<FontWeight>("FontWeightSemiBold"),
                })
                {
                    BorderBrush = Res<Brush>("TextPrimary"),
                    BorderThickness = new Thickness(1.5),
                    Padding = new Thickness(10, 6, 10, 6),
                });

                table.RowGroups.Add(new TableRowGroup());
                table.RowGroups[0].Rows.Add(row);
                return table;
            }

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

        private Table BuildParties(List<PrintParty> parties)
        {
            var table = new Table { CellSpacing = 8, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var _ in parties) table.Columns.Add(new TableColumn { Width = new GridLength(1, GridUnitType.Star) });

            var row = new TableRow();
            foreach (var party in parties)
            {
                var content = new Paragraph { Margin = new Thickness(0) };
                content.Inlines.Add(new Run(party.Title) { FontSize = Res<double>("FontSizeXs"), Foreground = Res<Brush>("TextMuted") });
                content.Inlines.Add(new LineBreak());
                content.Inlines.Add(new Run(party.Name ?? "") { FontWeight = Res<FontWeight>("FontWeightSemiBold"), FontSize = Res<double>("FontSizeMd") });

                foreach (var detail in party.Details ?? new List<string>())
                {
                    if (string.IsNullOrWhiteSpace(detail)) continue;
                    content.Inlines.Add(new LineBreak());
                    content.Inlines.Add(new Run(detail) { FontSize = Res<double>("FontSizeSm"), Foreground = Res<Brush>("TextSecondary") });
                }

                row.Cells.Add(new TableCell(content)
                {
                    Padding = new Thickness(10),
                    Background = Res<Brush>("NeutralSoft"),
                    BorderBrush = Res<Brush>("OutlineDefault"),
                    BorderThickness = new Thickness(1)
                });
            }

            var group = new TableRowGroup();
            group.Rows.Add(row);
            table.RowGroups.Add(group);
            return table;
        }

        private IEnumerable<Block> BuildTablePages(PrintSection section, int linesPerPage)
        {
            var rows = section.Rows ?? new List<Dictionary<string, object>>();
            if (linesPerPage <= 0 || rows.Count <= linesPerPage)
            {
                yield return BuildTable(section);
                yield break;
            }

            for (var offset = 0; offset < rows.Count; offset += linesPerPage)
            {
                var isLast = offset + linesPerPage >= rows.Count;
                var table = BuildTable(new PrintSection
                {
                    Type = section.Type, Columns = section.Columns, RowBold = section.RowBold,
                    Rows = rows.Skip(offset).Take(linesPerPage).ToList(),
                    TotalsRow = isLast ? section.TotalsRow : null,
                    Totals = isLast ? section.Totals : null
                });

                table.BreakPageBefore = offset > 0;
                yield return table;
            }
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
                headerRow.Cells.Add(NewCell(col.Header, Res<Brush>("TextPrimary"), bold: true, align: TextAlignment.Center, isHeader: true));
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
                    row.Cells.Add(NewCell(text, Res<Brush>("TextPrimary"), bold: isBold, align: AlignFor(col, raw)));
                }
                bodyGroup.Rows.Add(row);
            }
            table.RowGroups.Add(bodyGroup);

            if (section.TotalsRow is { Count: > 0 })
            {
                var totalsGroup = new TableRowGroup();
                var totalsRow = new TableRow { Background = Res<Brush>("HeaderBackground") };

                foreach (var col in columns)
                {
                    var raw = section.TotalsRow.TryGetValue(col.Key, out var v) ? v : null;
                    totalsRow.Cells.Add(NewCell(FormatValue(raw, col.Format ?? "N2", culture), Res<Brush>("TextPrimary"), bold: true, align: AlignFor(col, raw)));
                }

                totalsGroup.Rows.Add(totalsRow);
                table.RowGroups.Add(totalsGroup);
            }

            if (section.Totals is { Count: > 0 })
            {
                var totalsGroup = new TableRowGroup();
                var span = Math.Max(1, table.Columns.Count - 3);

                foreach (var total in section.Totals)
                {
                    var row = new TableRow { Background = Res<Brush>(total.IsBold ? "BrandSoft" : "NeutralSoft") };
                    var filler = NewCell("", Res<Brush>("TextPrimary"), false, TextAlignment.Right);
                    filler.ColumnSpan = span;

                    row.Cells.Add(filler);
                    var label = NewCell(total.Label, Res<Brush>("TextSecondary"), total.IsBold, TextAlignment.Right);
                    label.ColumnSpan = 2;
                    row.Cells.Add(label);
                    row.Cells.Add(NewCell(total.Value, Res<Brush>("TextPrimary"), total.IsBold, TextAlignment.Center));
                    totalsGroup.Rows.Add(row);
                }

                table.RowGroups.Add(totalsGroup);
            }

            return table;
        }

        private Block BuildSignatures(List<string> labels)
        {
            var table = new Table { Margin = new Thickness(0, 18, 0, 0), CellSpacing = 0 };
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
                }) { Padding = new Thickness(20, 14, 20, 0) };
                row.Cells.Add(cell);
            }
            group.Rows.Add(row);
            table.RowGroups.Add(group);
            return table;
        }

        private TableCell NewCell(string text, Brush foreground, bool bold, TextAlignment align, bool isHeader = false, bool bare = false)
        {
            var paragraph = BuildCellParagraph(text);
            paragraph.TextAlignment = align;
            paragraph.FontWeight = bold ? Res<FontWeight>("FontWeightSemiBold") : Res<FontWeight>("FontWeightNormal");
            paragraph.Foreground = foreground;
            paragraph.Margin = new Thickness(0);

            return new TableCell(paragraph)
            {
                Padding = new Thickness(8, 5, 8, 5),
                BorderBrush = Res<Brush>("OutlineSubtle"),
                BorderThickness = bare ? new Thickness(0) : isHeader ? new Thickness(0.6, 0.6, 0.6, 1) : new Thickness(0.6)
            };
        }

        private Block BuildBarcode(string text)
        {
            var widths = Code128.Encode(text);
            if (widths.Count == 0) return null;

            const double Unit = 1.1, Height = 38;
            var bars = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };

            for (var i = 0; i < widths.Count; i++)
                bars.Children.Add(new System.Windows.Shapes.Rectangle
                {
                    Width = widths[i] * Unit,
                    Height = Height,
                    Fill = i % 2 == 0 ? Res<Brush>("TextPrimary") : Brushes.Transparent
                });

            var paragraph = new Paragraph { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 6, 0, 2) };
            paragraph.Inlines.Add(new InlineUIContainer(bars));
            paragraph.Inlines.Add(new LineBreak());
            paragraph.Inlines.Add(new Run(text) { FontSize = Res<double>("FontSizeXs"), Foreground = Res<Brush>("TextSecondary") });

            return paragraph;
        }

        private static readonly char[] LineSeparators = { (char)10 };


        private static Paragraph BuildCellParagraph(string text)
        {
            var paragraph = new Paragraph();
            var lines = (text ?? "").Split(LineSeparators);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0) paragraph.Inlines.Add(new LineBreak());
                paragraph.Inlines.Add(new Run(lines[i]));
            }

            return paragraph;
        }

        private static TextAlignment AlignFor(PrintColumn column, object value) =>
            !string.IsNullOrEmpty(column.Align) ? AlignOf(column.Align)
                                                : AlignOf(value is string or null ? "Right" : "Center");

        private static TextAlignment AlignOf(string align) => align switch
        {
            "Left"   => TextAlignment.Right,
            "Center" => TextAlignment.Center,
            _        => TextAlignment.Left
        };

        private static string FormatValue(object value, string format, CultureInfo culture)
        {
            if (value == null) return "";
            if (!string.IsNullOrEmpty(format) && value is IFormattable formattable)
                return formattable.ToString(format, culture);
            return value.ToString();
        }

        private static Size PageSizeFor(PrintOrientation orientation, bool halfPage = false)
        {
            const double a4Width = 793.7, a4Height = 1122.5;

            var size = orientation == PrintOrientation.Landscape
                ? new Size(a4Height, a4Width)
                : new Size(a4Width, a4Height);

            return halfPage ? new Size(size.Width, size.Height / 2) : size;
        }

        private static FixedDocument ConvertToFixedDocument(FlowDocument flowDocument, Size pageSize, bool showPageNumbers)
        {
            var document = new FixedDocument();
            foreach (var page in BuildPages(flowDocument, pageSize, showPageNumbers)) document.Pages.Add(page);

            return document;
        }

        private static UIElement Watermark(string text, Size pageSize)
        {
            var label = new System.Windows.Controls.TextBlock
            {
                Text = text,
                FontSize = 64,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Gray) { Opacity = 0.12 },
                FlowDirection = FlowDirection.RightToLeft,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new RotateTransform(-35)
            };

            FixedPage.SetLeft(label, pageSize.Width * 0.18);
            FixedPage.SetTop(label, pageSize.Height * 0.42);
            return label;
        }

        private static List<PageContent> BuildPages(FlowDocument flowDocument, Size pageSize, bool showPageNumbers, string watermark = null)
        {
            flowDocument.PageWidth  = pageSize.Width;
            flowDocument.PageHeight = pageSize.Height;
            flowDocument.ColumnWidth = pageSize.Width;

            var paginator = ((IDocumentPaginatorSource)flowDocument).DocumentPaginator;
            paginator.PageSize = pageSize;

            if (paginator is DynamicDocumentPaginator dynamicPaginator && !dynamicPaginator.IsPageCountValid)
                dynamicPaginator.ComputePageCount();

            var pages = new List<PageContent>();
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

                if (!string.IsNullOrWhiteSpace(watermark)) fixedPage.Children.Add(Watermark(watermark, pageSize));

                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(fixedPage);
                pages.Add(pageContent);
            }

            return pages;
        }
    }
}
