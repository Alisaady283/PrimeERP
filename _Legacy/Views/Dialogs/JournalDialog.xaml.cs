using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PrimeERP.Core;
using PrimeERP.Database;
using PrimeERP.Models;
using PrimeERP.Validators;

namespace PrimeERP.Views.Dialogs
{
    public partial class JournalDialog : Window
    {
        private int  _entryId  = -1;
        private bool _viewOnly = false;

        private ObservableCollection<JournalLine> _lines = new ObservableCollection<JournalLine>();

        public JournalDialog()
        {
            InitializeComponent();
            SetupNew();
        }

        public JournalDialog(int entryId, bool viewOnly = false)
        {
            InitializeComponent();
            _entryId  = entryId;
            _viewOnly = viewOnly;
            LoadEntry(entryId);
        }

        private void SetupNew()
        {
            txtTitle.Text    = "قيد جديد";
            txtSubtitle.Text = "إدخال قيد محاسبي جديد";
            txtEntryNo.Text  = JournalDb.GenerateEntryNo();
            txtDate.Text     = DateTime.Today.ToString("yyyy-MM-dd");
            txtFooterInfo.Text = $"سيتم إنشاء القيد برقم: {txtEntryNo.Text}";

            SetupSources();
            SetupDefaultLines(8);
            gridLines.ItemsSource = _lines;
        }

        private void LoadEntry(int entryId)
        {
            var entry = JournalDb.GetById(entryId);
            if (entry != null)
            {
                txtTitle.Text    = _viewOnly ? "عرض قيد" : "تعديل قيد";
                txtSubtitle.Text = $"القيد رقم: {entry.EntryNo}";
                txtEntryNo.Text  = entry.EntryNo;
                txtDate.Text     = entry.EntryDate;
                txtDescription.Text = entry.Description;
                txtFooterInfo.Text = $"تاريخ الإنشاء: {entry.CreatedAt}";
            }

            SetupSources();

            foreach (var line in JournalDb.GetLines(entryId))
                _lines.Add(line);

            gridLines.ItemsSource = _lines;
            UpdateBalance();

            if (_viewOnly)
            {
                gridLines.IsReadOnly   = true;
                btnSave.Visibility     = Visibility.Collapsed;
                btnAddLine.Visibility  = Visibility.Collapsed;
            }
        }

        private void SetupSources()
        {
            cmbSource.Items.Clear();
            foreach (var s in new[] { "يدوي", "مبيعات", "مشتريات", "رواتب", "أخرى" })
                cmbSource.Items.Add(s);
            cmbSource.SelectedIndex = 0;
        }

        private void SetupDefaultLines(int count)
        {
            for (int i = 1; i <= count; i++)
                _lines.Add(new JournalLine { LineNo = i, AccountCode = "", AccountName = "", Debit = 0, Credit = 0, Notes = "" });
        }

        private void UpdateBalance()
        {
            decimal totalDebit  = 0;
            decimal totalCredit = 0;

            foreach (var line in _lines)
            {
                totalDebit  += line.Debit;
                totalCredit += line.Credit;
            }

            decimal diff = totalDebit - totalCredit;

            txtTotalDebit.Text  = totalDebit.ToString("N2");
            txtTotalCredit.Text = totalCredit.ToString("N2");
            txtDiff.Text        = Math.Abs(diff).ToString("N2");

            if (diff == 0 && totalDebit > 0)
            {
                txtDiff.Foreground = new SolidColorBrush(AppTheme.Success);
                txtBalanceStatus.Text = "✔ القيد متوازن";
                txtBalanceStatus.Foreground = new SolidColorBrush(AppTheme.Success);
            }
            else if (totalDebit == 0 && totalCredit == 0)
            {
                txtDiff.Foreground = new SolidColorBrush(AppTheme.TextMuted);
                txtBalanceStatus.Text = "أدخل بيانات القيد";
                txtBalanceStatus.Foreground = new SolidColorBrush(AppTheme.TextMuted);
            }
            else
            {
                txtDiff.Foreground = new SolidColorBrush(AppTheme.Danger);
                txtBalanceStatus.Text = "✖ القيد غير متوازن";
                txtBalanceStatus.Foreground = new SolidColorBrush(AppTheme.Danger);
            }
        }

        private void gridLines_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit) return;
            if (e.Row.Item is not JournalLine line) return;

            string colName = e.Column.Header?.ToString();

            if (colName == "كود الحساب")
            {
                var tb   = e.EditingElement as TextBox;
                string code = tb?.Text?.Trim() ?? "";

                if (!string.IsNullOrEmpty(code))
                {
                    var account = AccountDb.GetByCode(code);
                    if (account != null)
                        line.AccountName = account.Name;
                }
            }

            Dispatcher.BeginInvoke(new Action(() =>
            {
                gridLines.Items.Refresh();
                UpdateBalance();
            }));
        }

        private void gridLines_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (_viewOnly) e.Cancel = true;
        }

        private void btnAddLine_Click(object sender, RoutedEventArgs e)
        {
            int no = _lines.Count + 1;
            _lines.Add(new JournalLine { LineNo = no, AccountCode = "", AccountName = "", Debit = 0, Credit = 0, Notes = "" });
        }

        private void btnDeleteLine_Click(object sender, RoutedEventArgs e)
        {
            if (gridLines.SelectedItem is JournalLine line)
            {
                _lines.Remove(line);
                for (int i = 0; i < _lines.Count; i++)
                    _lines[i].LineNo = i + 1;
                gridLines.Items.Refresh();
                UpdateBalance();
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var entry = new JournalEntry
            {
                EntryNo     = txtEntryNo.Text,
                EntryDate   = txtDate.Text,
                Description = txtDescription.Text,
                Source      = cmbSource.SelectedItem?.ToString() ?? "يدوي"
            };

            foreach (var line in _lines)
                if (!string.IsNullOrEmpty(line.AccountCode))
                    entry.Lines.Add(line);

            var validation = JournalValidator.Validate(entry);
            if (!validation.IsValid)
            {
                MessageBox.Show(validation.FirstError, "تنبيه",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            btnSave.IsEnabled = false;

            try
            {
                if (_entryId > 0)
                    JournalDb.DeleteEntry(_entryId);

                JournalDb.InsertEntry(entry);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                btnSave.IsEnabled = true;
            }
        }

        private void txtDate_GotFocus(object sender, RoutedEventArgs e)
        {
            dateBorder.BorderBrush     = new SolidColorBrush(AppTheme.Primary);
            dateBorder.BorderThickness = new Thickness(2);
        }

        private void txtDate_LostFocus(object sender, RoutedEventArgs e)
        {
            dateBorder.BorderBrush     = new SolidColorBrush(AppTheme.Border);
            dateBorder.BorderThickness = new Thickness(1);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
