using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Actions;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    public static class PosRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var saleDef = AtShift(DocumentRenderer.Shown(definition.DocumentDialog ?? throw new InvalidOperationException(definition.Key), services)) with { PullSources = new() };
            var returnDef = AtShift(DocumentRenderer.Shown(definition.ReturnDialog ?? throw new InvalidOperationException(definition.Key), services));
            var toast = services.GetRequiredService<IToastService>();
            var pos = services.GetRequiredService<IPosService>();

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var header = new PageHeader { Title = LocalizationService.Get(definition.TitleKey) };
            var body = new ContentControl();
            Grid.SetRow(body, 1);
            root.Children.Add(header);
            root.Children.Add(body);

            SalePanel current = null;

            void Show()
            {
                var shift = pos.Current();
                if (shift.IsSuccess) ShowShift(shift.Value);
                else ShowOpening();
            }

            void ShowOpening()
            {
                current = null;
                header.Subtitle = LocalizationService.Get("Str.Pos.NoShift");
                header.ActionsContent = null;

                var opening = new AppNumericBox { Label = LocalizationService.Get("Str.Pos.Opening") };
                var fields = new List<FieldDefinition>();
                var controls = new Dictionary<string, FrameworkElement>();
                var panel = new StackPanel { Width = 360, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 32, 0, 0) };
                panel.Children.Add(opening);

                if (AppSession.BranchId == null)
                    foreach (var field in new[]
                    {
                        new FieldDefinition { Key = "TreasuryId", LabelKey = "Str.Treasury", Kind = FieldKind.Picker, PickerType = "Treasury", IsRequired = true },
                        new FieldDefinition { Key = "WarehouseId", LabelKey = "Str.Warehouse", Kind = FieldKind.Picker, PickerType = "Warehouse", IsRequired = true }
                    })
                    {
                        var picker = (AppComboBox)DialogRenderer.BuildField(field);
                        DialogRenderer.LoadPickerItems(picker, field, services);
                        picker.Margin = new Thickness(0, 12, 0, 0);
                        fields.Add(field);
                        controls[field.Key] = picker;
                        panel.Children.Add(picker);
                    }

                var open = new Btn { Text = LocalizationService.Get("Str.Pos.OpenShift"), Variant = "primary", Margin = new Thickness(0, 20, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
                open.Click += (_, __) =>
                {
                    if (!FieldValidation.Validate(fields, controls)) return;
                    var opened = pos.Open(opening.Value,
                        controls.GetValueOrDefault("TreasuryId") is AppComboBox t ? t.SelectedValue as int? : null,
                        controls.GetValueOrDefault("WarehouseId") is AppComboBox w ? w.SelectedValue as int? : null);
                    if (opened.IsFailure) { toast.Error(opened.ErrorMessage); return; }
                    ShowShift(opened.Value);
                };
                panel.Children.Add(open);
                body.Content = panel;
            }

            void ShowShift(PosShift shift)
            {
                header.Subtitle = LocalizationService.Get("Str.Pos.ShiftInfo", shift.UserName, shift.OpenedAt);
                PosReceipt last = null;

                current = Sale(saleDef, services, toast, pos, receipt => last = receipt);
                var tabs = new AppTabControl
                {
                    Tabs = new[]
                    {
                        new AppTabItem { Header = LocalizationService.Get("Str.Pos.SaleTab"), Content = current.View },
                        new AppTabItem { Header = LocalizationService.Get("Str.Pos.ReturnTab"), Content = Return(returnDef, services, toast, pos) }
                    }
                };

                var btnHeld = new Btn { Text = LocalizationService.Get("Str.Pos.HeldTitle"), Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                var btnHold = new Btn { Text = LocalizationService.Get("Str.Pos.Hold"), Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                var btnNew = new Btn { Text = LocalizationService.Get("Str.Pos.NewSale"), Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                var btnPrint = new Btn { Text = LocalizationService.Get("Str.Pos.PrintLast"), Variant = "secondary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                var btnClose = new Btn { Text = LocalizationService.Get("Str.Pos.CloseShift"), Variant = "danger", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };

                btnHeld.Click += (_, __) => { if (HeldList(pos, toast) is { } restored) current.Reset(restored); };
                btnHold.Click += (_, __) => current.Hold();
                btnNew.Click += (_, __) => current.Reset(null);
                btnPrint.Click += (_, __) =>
                {
                    if (last == null) return;
                    var invoice = services.GetRequiredService<ISalesInvoiceService>().GetById(last.DocumentId);
                    if (invoice.IsSuccess) DocumentPrinter.PrintSelected(definition, services, invoice.Value, roll: true);
                };
                btnClose.Click += (_, __) => { if (CloseShift(shift, pos, toast)) Show(); };

                header.ActionsContent = new StackPanel { Orientation = Orientation.Horizontal, Children = { btnHeld, btnHold, btnNew, btnPrint, btnClose } };
                body.Content = tabs;
                current.Focus();
            }

            root.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.F8 && current != null) { current.Hold(); e.Handled = true; }
            };

            root.Loaded += (_, __) => { if (body.Content == null) Show(); };
            return root;
        }

        private static DocumentDialogDefinition AtShift(DocumentDialogDefinition def) =>
            def with { HeaderFields = def.HeaderFields.Where(f => f.PickerType != "Warehouse").ToList() };

        private sealed record SalePanel(FrameworkElement View, Action Focus, Action Hold, Action<CreateSalesInvoiceDto> Reset);

        private static SalePanel Sale(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, IPosService pos, Action<PosReceipt> sold)
        {
            var products = services.GetRequiredService<IProductService>();
            DocumentRenderer.DocumentEditor editor = null;

            var barcode = new AppTextBox { Label = LocalizationService.Get("Str.Pos.Scan") };
            var lines = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 12, 0, 0) };
            var paid = new AppNumericBox { Label = LocalizationService.Get("Str.Pos.Paid") };
            var header = new ContentControl();
            var result = new TextBlock { Margin = new Thickness(0, 16, 0, 0), TextWrapping = TextWrapping.Wrap };
            result.SetResourceReference(TextBlock.FontSizeProperty, "P.Font.Size.500");
            result.SetResourceReference(TextBlock.ForegroundProperty, "Success");
            var pay = new Btn { Text = LocalizationService.Get("Str.Pos.Pay"), Variant = "primary", Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };

            void Focus() => barcode.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => barcode.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));

            void Reset(CreateSalesInvoiceDto restore)
            {
                var keep = editor?.HeaderControls.ToDictionary(c => c.Key, c => DialogRenderer.GetControlValue(c.Value, def.HeaderFields.First(f => f.Key == c.Key).Kind));
                editor = DocumentRenderer.BuildEditor(def, services, toast, null);
                if (editor == null) return;
                ((Panel)editor.Body).Children.Remove(editor.Header);
                editor.Header.Children.Clear();
                header.Content = DialogRenderer.BuildGrid(def.HeaderFields, 1, editor.HeaderControls, width: null);
                lines.Content = editor.Body;

                foreach (var field in def.HeaderFields.Where(f => f.Kind == FieldKind.Picker))
                {
                    var value = restore != null ? typeof(CreateSalesInvoiceDto).GetProperty(field.Key)?.GetValue(restore) : keep?.GetValueOrDefault(field.Key);
                    if (value != null) DialogRenderer.SelectPickerItem((AppComboBox)editor.HeaderControls[field.Key], value,
                        DialogRenderer.ValueFieldOf(def.DtoType, field.Key, field.PickerValueField));
                }

                foreach (var line in restore?.Lines ?? new List<CreateSalesInvoiceLineDto>())
                    editor.AddLine(def.LineFields.ToDictionary(f => f.Key, f => typeof(CreateSalesInvoiceLineDto).GetProperty(f.Key)?.GetValue(line)));

                paid.Value = 0;
                Focus();
            }

            void Scan()
            {
                var raw = barcode.Text?.Trim();
                barcode.Text = "";
                if (string.IsNullOrEmpty(raw) || editor == null) return;

                var scanned = products.Scan(raw);
                if (scanned.IsFailure) { toast.Error(scanned.ErrorMessage); return; }

                var (product, code) = scanned.Value;
                var same = code.Price == null
                    ? editor.Rows.FirstOrDefault(r => Equals(DialogRenderer.GetControlValue(r.Controls["ProductCode"], FieldKind.Picker), product.Code)
                                                     && (code.Batch == null || !r.Controls.ContainsKey("BatchNo")
                                                         || Equals(DialogRenderer.GetControlValue(r.Controls["BatchNo"], FieldKind.Text), code.Batch)))
                    : null;

                if (same != null)
                {
                    var qty = Convert.ToDecimal(DialogRenderer.GetControlValue(same.Controls["Qty"], FieldKind.Number) ?? 0m);
                    DocumentRenderer.SetRowValue(same, def, "Qty", qty + (code.Qty ?? 1));
                    return;
                }

                editor.AddLine(new Dictionary<string, object>
                {
                    ["ProductCode"] = product.Code, ["Qty"] = code.Qty ?? 1m, ["UnitPrice"] = code.Price ?? product.SalePrice, ["BatchNo"] = code.Batch
                });
            }

            void Pay()
            {
                if (editor == null || DocumentRenderer.ReadDto(def, toast, editor) is not CreateSalesInvoiceDto dto) return;
                var done = pos.Sell(dto, paid.Value);
                if (done.IsFailure) { toast.Error(done.ErrorMessage); return; }

                sold(done.Value);
                result.Text = LocalizationService.Get("Str.Pos.Done", done.Value.DocumentNo, done.Value.Net, done.Value.Change);
                Reset(null);
            }

            void Hold()
            {
                if (editor == null || DocumentRenderer.ReadDto(def, toast, editor) is not CreateSalesInvoiceDto dto) return;
                var held = pos.Hold(dto, null);
                if (held.IsFailure) { toast.Error(held.ErrorMessage); return; }
                Reset(null);
            }

            barcode.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter) { Scan(); e.Handled = true; } };
            pay.Click += (_, __) => Pay();

            var payment = new Border { Width = 300, Padding = new Thickness(16), Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Top, Child = new StackPanel { Children = { header, paid, pay, result } } };
            payment.SetResourceReference(Border.BackgroundProperty, "SurfaceSunken");
            payment.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Md");

            var view = new Grid { Margin = new Thickness(24, 12, 24, 12) };
            view.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            view.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            view.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            view.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(lines, 1);
            Grid.SetColumn(payment, 1);
            Grid.SetRowSpan(payment, 2);
            view.Children.Add(barcode);
            view.Children.Add(lines);
            view.Children.Add(payment);

            view.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.F12) { Pay(); e.Handled = true; }
                else if (e.Key == Key.F2) { Focus(); e.Handled = true; }
            };

            Reset(null);
            return new SalePanel(view, Focus, Hold, Reset);
        }

        private static FrameworkElement Return(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, IPosService pos)
        {
            DocumentRenderer.DocumentEditor editor = null;
            var lines = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0, 12, 0, 0) };
            var result = new TextBlock { Margin = new Thickness(0, 12, 0, 0) };
            result.SetResourceReference(TextBlock.ForegroundProperty, "Success");

            void Reset()
            {
                editor = DocumentRenderer.BuildEditor(def, services, toast, null);
                if (editor != null) lines.Content = editor.Body;
            }

            var save = new Btn { Text = LocalizationService.Get("Str.Pos.SaveReturn"), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            save.Click += (_, __) =>
            {
                if (editor == null || DocumentRenderer.ReadDto(def, toast, editor) is not CreateSalesReturnDto dto) return;
                var done = pos.Return(dto);
                if (done.IsFailure) { toast.Error(done.ErrorMessage); return; }
                result.Text = LocalizationService.Get("Str.Pos.Refunded", done.Value.DocumentNo, done.Value.Collected);
                Reset();
            };

            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            foreach (var pull in DocumentRenderer.BuildPullButtons(def, services, () => editor)) actions.Children.Add(pull);
            actions.Children.Add(save);

            var view = new DockPanel { Margin = new Thickness(24, 12, 24, 12) };
            DockPanel.SetDock(actions, Dock.Top);
            DockPanel.SetDock(result, Dock.Bottom);
            view.Children.Add(actions);
            view.Children.Add(result);
            view.Children.Add(lines);

            Reset();
            return view;
        }

        private static CreateSalesInvoiceDto HeldList(IPosService pos, IToastService toast)
        {
            CreateSalesInvoiceDto chosen = null;
            var list = new StackPanel();
            ComposedDialogWindow window = null;

            void Fill()
            {
                list.Children.Clear();
                var held = pos.Held();
                if (held.IsFailure) { toast.Error(held.ErrorMessage); return; }

                foreach (var entry in held.Value)
                {
                    var open = new Btn { Text = $"{entry.Label}  ·  {entry.HeldAt:HH:mm}", Variant = "secondary", HorizontalAlignment = HorizontalAlignment.Stretch };
                    var trash = new AppIconButton { Icon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconDelete"), Variant = "danger", Margin = new Thickness(8, 0, 0, 0) };
                    open.Click += (_, __) =>
                    {
                        var taken = pos.Take(entry.Id);
                        if (taken.IsFailure) { toast.Error(taken.ErrorMessage); return; }
                        chosen = taken.Value;
                        window.Close();
                    };
                    trash.Click += (_, __) =>
                    {
                        var removed = pos.DeleteHeld(entry.Id);
                        if (removed.IsFailure) toast.Error(removed.ErrorMessage);
                        Fill();
                    };

                    var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
                    DockPanel.SetDock(trash, Dock.Right);
                    row.Children.Add(trash);
                    row.Children.Add(open);
                    list.Children.Add(row);
                }
            }

            Fill();
            var close = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            window = new ComposedDialogWindow(LocalizationService.Get("Str.Pos.HeldTitle"), new ScrollViewer { Content = list, MaxHeight = 420 }, close, width: 420);
            close.Click += (_, __) => window.Close();

            var frame = new DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            Dispatcher.PushFrame(frame);
            return chosen;
        }

        private static bool CloseShift(PosShift shift, IPosService pos, IToastService toast)
        {
            var counted = new AppNumericBox { Label = LocalizationService.Get("Str.Pos.Counted") };
            var expected = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 12),
                Text = $"{LocalizationService.Get("Str.Pos.Expected")}: {LineCalc.Drawer(shift.OpeningCash, shift.CashIn, shift.CashOut):N2}"
            };
            var confirm = new Btn { Text = LocalizationService.Get("Str.Pos.CloseShift"), Variant = "danger", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            var cancel = new Btn { Text = LocalizationService.Get("Str.Cancel"), Variant = "secondary", Size = "sm" };
            var window = new ComposedDialogWindow(LocalizationService.Get("Str.Pos.CloseShift"),
                new StackPanel { Children = { expected, counted } },
                new StackPanel { Orientation = Orientation.Horizontal, Children = { cancel, confirm } }, width: 380);

            cancel.Click += (_, __) => window.Close();
            confirm.Click += (_, __) =>
            {
                var closed = pos.Close(counted.Value);
                if (closed.IsFailure) { toast.Error(closed.ErrorMessage); return; }
                toast.Success(LocalizationService.Get("Str.Pos.ShiftClosed", closed.Value.Difference ?? 0));
                window.Saved = true;
                window.Close();
            };

            var frame = new DispatcherFrame();
            window.Closed += (_, __) => frame.Continue = false;
            window.Show();
            Dispatcher.PushFrame(frame);
            return window.Saved;
        }
    }
}
