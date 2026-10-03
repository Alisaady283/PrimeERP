using PrimeERP.Application.Legacy.Backup;
using PrimeERP.Application.Legacy.Admin;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Services.Ledger;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Settings;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>تصيير صفحة الإعدادات</summary>
    public static class SettingsPageRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var settingsService = services.GetRequiredService<ISettingsService>();
            var toast = services.GetRequiredService<IToastService>();
            var allDefs = SettingKeys.All();

            var header = new PageHeader { Subtitle = LocalizationService.Get("Str.Settings.Version", PrimeERP.Platform.AppInfo.Version) };
            var updateButton = new Btn { Text = LocalizationService.Get("Str.Settings.CheckUpdate"), Variant = "secondary", Size = "sm" };
            updateButton.Click += async (_, __) => await UpdateFlow.RunAsync(services);
            header.ActionsContent = updateButton;
            var controls = new Dictionary<string, (FieldDefinition Field, FrameworkElement Control)>();
            var accountRows = new Lazy<List<DialogRenderer.PickerRow>>(() => DialogRenderer.AccountRows(services) ?? new());

            var tabs = SettingsTabs.Visible(services.GetRequiredService<IModuleRegistry>().Manifest).Select(cat =>
            {
                var panel = new WrapPanel { Margin = new Thickness(24) };
                foreach (var def in allDefs.Where(d => d.Category == cat.Category))
                {
                    var isAccount = cat.Category == "Accounts" && def.DataType == "string";
                    var field = new FieldDefinition
                    {
                        Key = def.Key, LabelKey = LabelKeyOf(def.Key),
                        Kind = isAccount ? FieldKind.Picker
                             : def.Key == SettingKeys.Company.LogoData ? FieldKind.Image
                             : def.DataType switch { "bool" => FieldKind.Check, "int" => FieldKind.Number, _ => FieldKind.Text },
                        PickerType = isAccount ? "Account" : null,
                        PickerValueField = isAccount ? "Code" : "Id",
                    };

                    var control = DialogRenderer.BuildField(field);
                    if (isAccount) ((AppComboBox)control).ItemsSource = accountRows.Value.ToList();
                    if (field.Kind == FieldKind.Image) control.Width = 420; else control.Width = 260;
                    control.Margin = new Thickness(0, 0, 16, 16);

                    var currentValue = settingsService.Get<string>(def.Key, def.DefaultValue);
                    object typedValue = field.Kind switch
                    {
                        FieldKind.Check => bool.TryParse(currentValue, out var b) && b,
                        FieldKind.Number => decimal.TryParse(currentValue, out var n) ? n : 0,
                        FieldKind.Picker => currentValue,
                        FieldKind.Image => currentValue,
                        _ => currentValue
                    };
                    if (field.Kind == FieldKind.Picker) DialogRenderer.SelectPickerItem((AppComboBox)control, typedValue, "Code");
                    else DialogRenderer.SetControlValue(control, field, typedValue);

                    controls[def.Key] = (field, control);
                    panel.Children.Add(control);
                }

                if (cat.Category == "Backup") panel.Children.Add(BuildBackupPanel(services, toast));
                if (cat.Category == "Print") panel.Children.Add(BuildChequeCalibration(services, toast));

                return new AppTabItem { Header = LocalizationService.Get(cat.TitleKey), Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
            }).ToList();

            var tabControl = new AppTabControl { Tabs = tabs };

            var saveButton = new Btn { Text = LocalizationService.Get("Str.Save"), Variant = "primary", Size = "sm", Margin = new Thickness(24) };
            saveButton.Click += (_, __) =>
            {
                var values = new Dictionary<string, object>();
                foreach (var (key, (field, control)) in controls)
                    values[key] = DialogRenderer.GetControlValue(control, field.Kind);

                var accounts = services.GetRequiredService<PrimeERP.Application.Legacy.Accounting.IAccountService>();
                foreach (var rootKey in SettingKeys.Accounts.LinkedRoots)
                {
                    if (!values.TryGetValue(rootKey, out var code) || code is not string text || string.IsNullOrWhiteSpace(text)) continue;

                    var account = accounts.GetByCode(text);
                    if (account.IsSuccess && account.Value.IsLeaf)
                    {
                        toast.Error(LocalizationService.Get("Str.Settings.RootMustBeGroup", LocalizationService.Get(LabelKeyOf(rootKey))));
                        return;
                    }
                }

                var result = settingsService.SetMany(values);
                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }
                toast.Success(LocalizationService.Get("Str.Success"));
            };

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(header, 0);
            Grid.SetRow(tabControl, 1);
            Grid.SetRow(saveButton, 2);
            root.Children.Add(header);
            root.Children.Add(tabControl);
            root.Children.Add(saveButton);

            return root;
        }

        private static FrameworkElement BuildChequeCalibration(IServiceProvider services, IToastService toast)
        {
            var button = new Btn { Text = LocalizationService.Get("Str.Settings.ChequeCalibrationPrint"), Variant = "secondary", Size = "sm", Margin = new Thickness(0, 8, 0, 0) };
            button.Click += (_, __) =>
            {
                var settings = services.GetRequiredService<ISettingsService>();
                var layout = new PrimeERP.UI.Services.ChequeLayout
                {
                    OffsetX = ReadNumber(settings, SettingKeys.Print.ChequeOffsetX),
                    OffsetY = ReadNumber(settings, SettingKeys.Print.ChequeOffsetY),
                    Fields =
                    {
                        new() { Text = LocalizationService.Get("Str.Cheque.Payee"), X = 3.0, Y = 1.6 },
                        new() { Text = LocalizationService.Get("Str.Cheque.AmountDigits"), X = 12.5, Y = 1.6 },
                        new() { Text = LocalizationService.Get("Str.Cheque.AmountWords"), X = 3.0, Y = 2.8 },
                        new() { Text = LocalizationService.Get("Str.Date"), X = 12.5, Y = 0.8 },
                    }
                };

                var sheet = services.GetRequiredService<PrimeERP.UI.Services.IChequePrinter>().BuildCalibrationSheet(layout);
                if (sheet.IsFailure) { toast.Error(sheet.ErrorMessage); return; }

                services.GetRequiredService<PrimeERP.UI.Services.IPrintService>()
                    .DialogHost?.ShowPreview(sheet.Value, LocalizationService.Get("Str.Settings.ChequeCalibration"));
            };

            return new StackPanel { Width = 500, Children = { button } };
        }

        private static double ReadNumber(ISettingsService settings, string key) =>
            double.TryParse(settings.Get<string>(key, "0"), out var value) ? value : 0;

        private static FrameworkElement BuildBackupPanel(IServiceProvider services, IToastService toast)
        {
            var backup = services.GetRequiredService<IBackupService>();
            var dialogs = services.GetRequiredService<IDialogService>();

            var panel = new StackPanel { Width = 500 };
            var createBtn = new Btn { Text = LocalizationService.Get("Str.Backup.CreateNow"), Variant = "primary", Size = "sm", Margin = new Thickness(0, 0, 0, 12) };
            var listPanel = new StackPanel();

            void RefreshList()
            {
                listPanel.Children.Clear();
                foreach (var b in backup.List())
                {
                    var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var text = new TextBlock { Text = $"{System.IO.Path.GetFileName(b.FilePath)} — {b.CreatedAt:yyyy-MM-dd HH:mm}", VerticalAlignment = VerticalAlignment.Center };
                    text.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
                    var restoreBtn = new Btn { Text = LocalizationService.Get("Str.Backup.Restore"), Variant = "secondary", Size = "sm" };
                    restoreBtn.Click += async (_, __) =>
                    {
                        var confirmed = await dialogs.ConfirmAsync(LocalizationService.Get("Str.Backup.Restore"), LocalizationService.Get("Str.Backup.RestoreConfirm"), isDangerous: true);
                        if (!confirmed) return;

                        PrimeERP.Domain.Results.Result result;
                        using (dialogs.ShowProgress(LocalizationService.Get("Str.Backup.Restore"),
                                                    LocalizationService.Get("Str.PleaseWait")))
                            result = await Task.Run(() => backup.Restore(b.FilePath));

                        if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }

                        await dialogs.ShowMessageAsync(LocalizationService.Get("Str.Backup.Restore"),
                            LocalizationService.Get("Str.Backup.RestoredRestart"), PrimeERP.Domain.Results.StatusVariant.Success);
                        System.Windows.Application.Current.Shutdown();
                    };

                    Grid.SetColumn(text, 0);
                    Grid.SetColumn(restoreBtn, 1);
                    row.Children.Add(text);
                    row.Children.Add(restoreBtn);
                    listPanel.Children.Add(row);
                }
            }

            createBtn.Click += async (_, __) =>
            {
                using var handle = dialogs.ShowProgress(LocalizationService.Get("Str.Backup.Create"),
                                                       LocalizationService.Get("Str.PleaseWait"));
                var result = await Task.Run(() => backup.Create());

                if (!result.IsSuccess) { toast.Error(result.ErrorMessage); return; }
                toast.Success(LocalizationService.Get("Str.Success"));
                RefreshList();
            };

            RefreshList();
            panel.Children.Add(createBtn);
            panel.Children.Add(listPanel);
            return panel;
        }

        private static string LabelKeyOf(string settingKey) => $"Str.Settings.Label.{settingKey}";
    }
}
