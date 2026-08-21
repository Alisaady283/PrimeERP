using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>
    /// تذييل مستند عام (إجماليات + رسالة حالة + إجراءات) — يُحدَّث حيّاً من DocumentLinesGrid.LineChanged/TotalsChanged.
    /// </summary>
    public partial class DocumentFooter : UserControl
    {
        public static readonly DependencyProperty TotalsSourceProperty =
            DependencyProperty.Register(nameof(TotalsSource), typeof(List<FooterTotal>), typeof(DocumentFooter));

        public static readonly DependencyProperty StatusMessageProperty =
            DependencyProperty.Register(nameof(StatusMessage), typeof(string), typeof(DocumentFooter),
                new PropertyMetadata(null, OnStatusChanged));

        public static readonly DependencyProperty StatusVariantProperty =
            DependencyProperty.Register(nameof(StatusVariant), typeof(string), typeof(DocumentFooter),
                new PropertyMetadata("neutral", OnStatusChanged));

        public static readonly DependencyProperty ActionsContentProperty =
            DependencyProperty.Register(nameof(ActionsContent), typeof(object), typeof(DocumentFooter));

        public List<FooterTotal> TotalsSource   { get => (List<FooterTotal>)GetValue(TotalsSourceProperty);  set => SetValue(TotalsSourceProperty, value); }
        public string            StatusMessage  { get => (string)GetValue(StatusMessageProperty);            set => SetValue(StatusMessageProperty, value); }
        public string            StatusVariant  { get => (string)GetValue(StatusVariantProperty);            set => SetValue(StatusVariantProperty, value); }
        public object            ActionsContent { get => GetValue(ActionsContentProperty);                   set => SetValue(ActionsContentProperty, value); }

        public DocumentFooter()
        {
            InitializeComponent();
        }

        /// <summary>تحديث حيّ للإجماليات — غلاف صريح فوق TotalsSource كما هو مطلوب، بلا فرق وظيفي عن الخاصية.</summary>
        public void UpdateTotals(List<FooterTotal> totals) => TotalsSource = totals;

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DocumentFooter)d).RefreshStatus();

        private void RefreshStatus()
        {
            var hasMessage = !string.IsNullOrEmpty(StatusMessage);
            statusWrap.Visibility = hasMessage ? Visibility.Visible : Visibility.Collapsed;
            if (!hasMessage) return;

            statusText.Text = StatusMessage;

            var (bg, fg) = StatusVariant switch
            {
                "success" => ((Brush)FindResource("SuccessSoft"),       (Brush)FindResource("Success")),
                "danger"  => ((Brush)FindResource("DangerSoft"), (Brush)FindResource("Danger")),
                "warning" => ((Brush)FindResource("WarningSoft"),  (Brush)FindResource("Warning")),
                "info"    => ((Brush)FindResource("InfoSoft"),     (Brush)FindResource("Info")),
                "brand"   => ((Brush)FindResource("BrandSoft"),    (Brush)FindResource("BrandDefault")),
                _         => ((Brush)FindResource("SurfaceSunken"),      (Brush)FindResource("TextSecondary"))
            };

            statusWrap.Background = bg;
            statusText.Foreground = fg;
        }
    }
}
