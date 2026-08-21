using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Services;
using PrimeERP.Application;
using PrimeERP.Application.Services;

namespace PrimeERP.UI.Components.Documents
{
    public enum DocumentStatus { Draft, Posted, Cancelled }

    /// <summary>
    /// رأس مستند عام (رقم/تاريخ/نوع/حالة/بيان + حقول إضافية) — يُجمَّع مع DocumentLinesGrid وDocumentFooter
    /// لتشكيل صفحة مستند كاملة. لا يعرف قاعدة البيانات: توليد الرقم يمر عبر INumberSequenceService المُمرَّرة.
    /// </summary>
    public partial class DocumentHeader : UserControl
    {
        public static readonly DependencyProperty DocumentNoProperty =
            DependencyProperty.Register(nameof(DocumentNo), typeof(string), typeof(DocumentHeader),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DocumentDateProperty =
            DependencyProperty.Register(nameof(DocumentDate), typeof(DateTime?), typeof(DocumentHeader),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty DocumentTypeProperty =
            DependencyProperty.Register(nameof(DocumentType), typeof(object), typeof(DocumentHeader),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnDocumentTypeChanged));

        public static readonly DependencyProperty DocumentTypesProperty =
            DependencyProperty.Register(nameof(DocumentTypes), typeof(IEnumerable), typeof(DocumentHeader));

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register(nameof(Description), typeof(string), typeof(DocumentHeader),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty StatusProperty =
            DependencyProperty.Register(nameof(Status), typeof(DocumentStatus), typeof(DocumentHeader),
                new PropertyMetadata(DocumentStatus.Draft, OnStatusChanged));

        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(DocumentHeader),
                new PropertyMetadata(false, OnIsReadOnlyChanged));

        public static readonly DependencyProperty ExtraFieldsContentProperty =
            DependencyProperty.Register(nameof(ExtraFieldsContent), typeof(object), typeof(DocumentHeader));

        public string        DocumentNo         { get => (string)GetValue(DocumentNoProperty);         set => SetValue(DocumentNoProperty, value); }
        public DateTime?      DocumentDate       { get => (DateTime?)GetValue(DocumentDateProperty);     set => SetValue(DocumentDateProperty, value); }
        public object         DocumentType       { get => GetValue(DocumentTypeProperty);                set => SetValue(DocumentTypeProperty, value); }
        public IEnumerable    DocumentTypes      { get => (IEnumerable)GetValue(DocumentTypesProperty);  set => SetValue(DocumentTypesProperty, value); }
        public string         Description        { get => (string)GetValue(DescriptionProperty);         set => SetValue(DescriptionProperty, value); }
        public DocumentStatus Status             { get => (DocumentStatus)GetValue(StatusProperty);      set => SetValue(StatusProperty, value); }
        public bool           IsReadOnly         { get => (bool)GetValue(IsReadOnlyProperty);            set => SetValue(IsReadOnlyProperty, value); }
        public object         ExtraFieldsContent { get => GetValue(ExtraFieldsContentProperty);          set => SetValue(ExtraFieldsContentProperty, value); }

        /// <summary>يُمرَّر من المستهلك — DocumentHeader لا يستدعي قاعدة البيانات مباشرة إطلاقاً.</summary>
        public INumberSequenceService NumberSequenceService { get; set; }

        public event EventHandler          DocumentNoRequested;
        public event EventHandler<DateTime?> DateChanged;
        public event EventHandler          TypeChanged;

        public DocumentHeader()
        {
            InitializeComponent();
            UpdateStatusBadge();
        }

        private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DocumentHeader)d).UpdateStatusBadge();

        private void UpdateStatusBadge()
        {
            (badge.Text, badge.Variant) = Status switch
            {
                DocumentStatus.Posted    => ("مرحّل", "success"),
                DocumentStatus.Cancelled => ("ملغي", "danger"),
                _                        => ("مسودة", "neutral")
            };
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var c = (DocumentHeader)d;
            var enabled = !(bool)e.NewValue;
            c.datePicker.IsEnabled = enabled;
            c.typeCombo.IsEnabled = enabled;
            c.descriptionBox.IsReadOnly = (bool)e.NewValue;
            c.btnGenerate.IsEnabled = enabled;
        }

        private static void OnDocumentTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((DocumentHeader)d).TypeChanged?.Invoke(d, EventArgs.Empty);

        private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e) =>
            DateChanged?.Invoke(this, DocumentDate);

        private void btnGenerate_Click(object sender, RoutedEventArgs e)
        {
            DocumentNoRequested?.Invoke(this, EventArgs.Empty);
            if (NumberSequenceService != null)
                DocumentNo = NumberSequenceService.Next(DocumentType?.ToString());
        }
    }
}
