using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Services.Print
{
    /// <summary>حقل على ورق الشيك بموضع</summary>
    public class ChequeField
    {
        public required string Text { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
        public double FontSize { get; init; } = 12;
        public bool Bold { get; init; }
    }

    public class ChequeLayout
    {
        public double PaperWidth { get; init; } = 17.5;
        public double PaperHeight { get; init; } = 8.0;

        public double OffsetX { get; init; }
        public double OffsetY { get; init; }

        public List<ChequeField> Fields { get; init; } = new();
    }

    public interface IChequePrinter
    {
        Result<FixedDocument> Build(ChequeLayout layout);

        Result<FixedDocument> BuildCalibrationSheet(ChequeLayout layout);
    }

    /// <summary>الشيك يُطبَع على ورق مطبوع</summary>
    public class ChequePrinter : IChequePrinter
    {
        private const double CmToDip = 96 / 2.54;

        public Result<FixedDocument> Build(ChequeLayout layout)
        {
            if (layout == null) return Result.Fail<FixedDocument>("لا تخطيط شيك", ErrorCode.ValidationFailed);

            var canvas = NewCanvas(layout);
            foreach (var field in layout.Fields) canvas.Children.Add(Place(field, layout));

            return Result.Ok(Wrap(canvas, layout));
        }

        public Result<FixedDocument> BuildCalibrationSheet(ChequeLayout layout)
        {
            if (layout == null) return Result.Fail<FixedDocument>("لا تخطيط شيك", ErrorCode.ValidationFailed);

            var canvas = NewCanvas(layout);

            for (double x = 0; x <= layout.PaperWidth; x++) canvas.Children.Add(Ruler(x, layout, vertical: true));
            for (double y = 0; y <= layout.PaperHeight; y++) canvas.Children.Add(Ruler(y, layout, vertical: false));

            foreach (var field in layout.Fields)
                canvas.Children.Add(Place(new ChequeField { Text = "▮ " + field.Text, X = field.X, Y = field.Y, FontSize = field.FontSize }, layout));

            return Result.Ok(Wrap(canvas, layout));
        }

        private static Canvas NewCanvas(ChequeLayout layout) => new()
        {
            Width = layout.PaperWidth * CmToDip,
            Height = layout.PaperHeight * CmToDip,
            Background = Brushes.White,
            FlowDirection = FlowDirection.RightToLeft
        };

        private static UIElement Place(ChequeField field, ChequeLayout layout)
        {
            var text = new TextBlock
            {
                Text = field.Text ?? "",
                FontSize = field.FontSize,
                FontWeight = field.Bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = Brushes.Black
            };

            Canvas.SetRight(text, (field.X + layout.OffsetX) * CmToDip);
            Canvas.SetTop(text, (field.Y + layout.OffsetY) * CmToDip);
            return text;
        }

        private static UIElement Ruler(double centimetre, ChequeLayout layout, bool vertical)
        {
            var line = new System.Windows.Shapes.Line
            {
                Stroke = Brushes.LightGray,
                StrokeThickness = centimetre % 5 == 0 ? 0.8 : 0.3,
                X1 = 0, Y1 = 0,
                X2 = vertical ? 0 : layout.PaperWidth * CmToDip,
                Y2 = vertical ? layout.PaperHeight * CmToDip : 0
            };

            Canvas.SetRight(line, vertical ? centimetre * CmToDip : 0);
            Canvas.SetTop(line, vertical ? 0 : centimetre * CmToDip);
            return line;
        }

        private static FixedDocument Wrap(Canvas canvas, ChequeLayout layout)
        {
            var page = new FixedPage { Width = canvas.Width, Height = canvas.Height };
            page.Children.Add(canvas);

            var content = new PageContent();
            ((IAddChild)content).AddChild(page);

            var document = new FixedDocument();
            document.Pages.Add(content);
            return document;
        }
    }
}
