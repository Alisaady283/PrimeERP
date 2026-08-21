using System;
using System.Windows;
using System.Windows.Media;
using PrimeERP.Core;
using PrimeERP.Database;
using PrimeERP.Models;
using PrimeERP.Validators;

namespace PrimeERP.Views.Dialogs
{
    public partial class AccountDialog : Window
    {
        private string _parentCode;
        private string _editCode;
        private bool   _editMode;

        public AccountDialog(string parentCode)
        {
            InitializeComponent();
            _parentCode = parentCode;
            _editMode   = false;
            LoadForAdd(parentCode);
        }

        public AccountDialog(string code, bool editMode)
        {
            InitializeComponent();
            _editCode = code;
            _editMode = true;
            LoadForEdit(code);
        }

        private void LoadForAdd(string parentCode)
        {
            string newCode = AccountDb.GenerateChildCode(parentCode);
            int    type    = AccountDb.GetTypeOf(parentCode);
            int    level   = AccountDb.GetLevel(parentCode) + 1;

            txtTitle.Text      = "إضافة حساب جديد";
            txtSubtitle.Text   = $"إضافة حساب تحت: {parentCode}";
            txtHeaderIcon.Text = "➕";

            txtParentCode.Text = GetParentDisplay(parentCode);
            txtCode.Text       = newCode;
            txtLevel.Text      = $"المستوى {level}";

            SetupTypeCombo(type);
            SetFooterInfo($"سيتم إنشاء الحساب برقم: {newCode}");

            pnlLeaf.Visibility = Visibility.Visible;
        }

        private void LoadForEdit(string code)
        {
            txtTitle.Text      = "تعديل حساب";
            txtHeaderIcon.Text = "✏️";

            var account = AccountDb.GetByCode(code);
            if (account != null)
            {
                txtSubtitle.Text   = $"تعديل بيانات الحساب {code}";
                txtParentCode.Text = GetParentDisplay(account.ParentCode);
                txtCode.Text       = code;
                txtLevel.Text      = $"المستوى {account.Level}";
                txtName.Text       = account.Name;
                txtNotes.Text      = account.Notes ?? "";
                chkLeaf.IsChecked  = account.IsLeaf;

                SetupTypeCombo(account.Type);
                SetFooterInfo($"آخر تعديل: {account.UpdatedAt}");

                pnlLeaf.Visibility = Visibility.Visible;
            }
        }

        private string GetParentDisplay(string parentCode)
        {
            if (string.IsNullOrEmpty(parentCode)) return "—";
            var parent = AccountDb.GetByCode(parentCode);
            return parent != null ? $"{parent.Name} ({parentCode})" : parentCode;
        }

        private void SetupTypeCombo(int type)
        {
            cmbType.Items.Clear();
            var types = new[] { "أصول", "خصوم", "حقوق ملكية", "إيرادات", "مصروفات" };
            foreach (var t in types)
                cmbType.Items.Add(t);

            cmbType.SelectedIndex = type - 1;
        }

        private void SetFooterInfo(string text)
        {
            txtFooterInfo.Text = text;
        }

        private void txtName_GotFocus(object sender, RoutedEventArgs e)
        {
            nameBorder.BorderBrush     = new SolidColorBrush(AppTheme.Primary);
            nameBorder.BorderThickness = new Thickness(2);
            lblNameError.Visibility    = Visibility.Collapsed;
        }

        private void txtName_LostFocus(object sender, RoutedEventArgs e)
        {
            nameBorder.BorderBrush     = new SolidColorBrush(AppTheme.Border);
            nameBorder.BorderThickness = new Thickness(1);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            var account = new Account
            {
                Code   = _editMode ? _editCode : AccountDb.GenerateChildCode(_parentCode),
                Name   = txtName.Text,
                Notes  = txtNotes.Text ?? "",
                IsLeaf = chkLeaf.IsChecked ?? true,
                Type   = _editMode ? AccountDb.GetByCode(_editCode).Type : AccountDb.GetTypeOf(_parentCode),
                ParentCode = _editMode ? AccountDb.GetByCode(_editCode).ParentCode : _parentCode
            };

            var validation = AccountValidator.Validate(account, _editMode);
            if (!validation.IsValid)
            {
                lblNameError.Text          = validation["Name"] ?? validation.FirstError;
                lblNameError.Visibility    = Visibility.Visible;
                nameBorder.BorderBrush     = new SolidColorBrush(AppTheme.Danger);
                nameBorder.BorderThickness = new Thickness(2);
                txtName.Focus();
                return;
            }

            btnSave.IsEnabled = false;
            btnSave.Content   = "جارٍ الحفظ...";

            try
            {
                if (_editMode)
                    AccountDb.Update(account);
                else
                    AccountDb.Insert(account);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الحفظ:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                btnSave.IsEnabled = true;
                btnSave.Content   = "💾 حفظ";
            }
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
