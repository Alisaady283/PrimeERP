using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.Services.Cheques;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>شاشة الشيكات</summary>
    public static class ChequeBoardRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            dynamic vm = Resolve.ViewModel(definition, services);
            var cheques = services.GetRequiredService<IChequeService>();
            var toast = services.GetRequiredService<IToastService>();

            var header = new PageHeader();
            var moveCommand = new RelayCommand(_ =>
            {
                var selected = vm.SelectedItem as ChequeDto;
                if (selected == null) { toast.Error("اختر شيكاً أولاً"); return; }

                if (ShowMoveDialog(selected, services, cheques, toast))
                    vm.LoadCommand.Execute(null);
            });

            header.ActionsContent = new ActionToolbar
            {
                ButtonsSource = new List<ToolbarAction>
                {
                    ToolbarAction.Build("move", "تحريك الشيك", "IconRefresh", "primary", moveCommand, $"{definition.PermissionPrefix}.Edit", null, "إيداع / تحصيل / ارتداد / رد"),
                    ToolbarAction.Refresh((ICommand)vm.RefreshCommand),
                }
            };

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            BindingOperations.SetBinding(filterBar, FilterBar.ResultCountProperty, new Binding("TotalCount"));
            filterBar.Search += (_, text) => { vm.SearchText = text; vm.SearchCommand.Execute(null); };

            var grid = new AppDataGrid { ColumnsSource = definition.Columns, ShowRowActions = false, ShowPagination = false };
            BindingOperations.SetBinding(grid, AppDataGrid.ItemsSourceProperty, new Binding("Items"));
            BindingOperations.SetBinding(grid, AppDataGrid.SelectedItemProperty, new Binding("SelectedItem") { Mode = BindingMode.TwoWay });
            BindingOperations.SetBinding(grid, AppDataGrid.IsLoadingProperty, new Binding("IsLoading"));

            var pagination = PaginationBar.For(vm);

            var root = new Grid { DataContext = vm };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(header, 0); Grid.SetRow(filterBar, 1); Grid.SetRow(grid, 2); Grid.SetRow(pagination, 3);
            root.Children.Add(header); root.Children.Add(filterBar); root.Children.Add(grid); root.Children.Add(pagination);

            root.Loaded += async (_, __) => await (Task)vm.LoadAsync();
            return root;
        }

        private static bool ShowMoveDialog(ChequeDto cheque, IServiceProvider services, IChequeService service, IToastService toast)
        {
            var allowed = service.GetAllowedTransitions(cheque.Id);
            if (allowed.IsFailure) { toast.Error(allowed.ErrorMessage); return false; }
            if (allowed.Value.Count == 0) { toast.Info($"الشيك في حالة نهائية ({cheque.StatusName}) — لا حركة بعدها"); return false; }

            var statusPicker = new AppComboBox
            {
                Placeholder = "الحالة الجديدة", DisplayMemberPath = "Display", SelectedValuePath = "Id",
                ItemsSource = allowed.Value.Select(s => new DialogRenderer.PickerRow { Id = (int)s, Display = ChequeService.StatusName(s) }).ToList(),
                Margin = new Thickness(0, 0, 0, 12)
            };

            var treasuryField = new FieldDefinition { Key = "TreasuryId", LabelKey = "الخزينة", Kind = FieldKind.Picker, PickerType = "Treasury" };
            var treasuryPicker = (AppComboBox)DialogRenderer.BuildField(treasuryField);
            DialogRenderer.LoadPickerItems(treasuryPicker, treasuryField, services);
            treasuryPicker.Margin = new Thickness(0, 0, 0, 12);

            var datePicker = new AppDatePicker { SelectedDate = DateTime.Today, Margin = new Thickness(0, 0, 0, 12) };
            var notes = new AppTextBox { Placeholder = LocalizationService.Get("Str.Notes") };

            var body = new StackPanel { Width = 380, Margin = new Thickness(4) };
            var summary = new TextBlock { Text = $"شيك {cheque.ChequeNo} — {cheque.Amount:N2} — الحالة الحالية: {cheque.StatusName}", Margin = new Thickness(0, 0, 0, 12), TextWrapping = TextWrapping.Wrap };
            summary.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            body.Children.Add(summary);
            body.Children.Add(statusPicker);
            body.Children.Add(treasuryPicker);
            body.Children.Add(datePicker);
            body.Children.Add(notes);

            var btnCancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var btnMove = new Btn { Text = "تنفيذ", Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            var footer = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnCancel, btnMove } };
            var window = new ComposedDialogWindow("تحريك شيك", body, footer);

            var moved = false;
            btnCancel.Click += (_, __) => window.Close();
            btnMove.Click += (_, __) =>
            {
                if (statusPicker.SelectedValue == null) { toast.Error("اختر الحالة الجديدة"); return; }

                var result = service.Move(new MoveChequeDto
                {
                    ChequeId = cheque.Id,
                    ToStatus = Convert.ToInt32(statusPicker.SelectedValue),
                    MovementDate = datePicker.SelectedDate ?? DateTime.Today,
                    TreasuryId = treasuryPicker.SelectedValue == null ? null : Convert.ToInt32(treasuryPicker.SelectedValue),
                    Notes = notes.Text
                });

                if (result.IsFailure) { toast.Error(result.ErrorMessage); return; }

                toast.Success(LocalizationService.Get("Str.Success"));
                moved = true;
                window.Close();
            };

            var frame = new DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            Dispatcher.PushFrame(frame);

            return moved;
        }
    }
}
