using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Common;
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
            // المرتجع من الفواتير التي تسمح بها سياسة المرتجع (Documents.PosReturnPolicy)
            returnDef = returnDef with { PullSources = returnDef.PullSources.Select(s => s with { Scope = PrimeERP.Application.PageServices.Sales.PosReturnScope.Key }).ToList() };
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
                header.Visibility = Visibility.Visible;
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
                header.Visibility = Visibility.Collapsed;
                PosReceipt last = null;
                FrameworkElement returnView = null;

                void ShowSale() { body.Content = current.View; current.Focus(); }
                void ShowReturn() => body.Content = returnView ??= Return(returnDef, services, toast, pos, ShowSale);

                current = Sale(saleDef, services, toast, pos, new SaleHooks(
                    ShiftInfo: LocalizationService.Get("Str.Pos.ShiftInfo", shift.UserName, shift.OpenedAt),
                    Sold: receipt => last = receipt,
                    PrintLast: () =>
                    {
                        if (last == null) return;
                        var invoice = services.GetRequiredService<ISalesInvoiceService>().GetById(last.DocumentId);
                        if (invoice.IsSuccess) DocumentPrinter.PrintSelected(definition, services, invoice.Value, roll: true);
                    },
                    CloseShift: () => { if (CloseShift(shift, pos, toast)) Show(); },
                    ShowReturn: ShowReturn));
                ShowSale();
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

        /// <summary>ما يأخذه البيع من الصفحة: سطر الوردية، وما بعد البيع، وطباعة الأخيرة، والإغلاق، والانتقال إلى المرتجع</summary>
        private sealed record SaleHooks(string ShiftInfo, Action<PosReceipt> Sold, Action PrintLast, Action CloseShift, Action ShowReturn);

        /// <summary>مفاتيح سطر الفاتورة التي تقرؤها السلّة</summary>
        private const string ProductKey = "ProductCode", QtyKey = "Qty", PriceKey = "UnitPrice";

        /// <summary>طريقة عرض الأصناف المختارة — تبقى طوال الجلسة</summary>
        private static GalleryView _view = GalleryView.Tiles;

        /// <summary>
        /// البيع كتوزيع نقطة بيع الويب: الأصناف تملأ الشاشة (مسحٌ وبحث · شرائح التصنيفات · مربّعات أو تفاصيل)،
        /// والسلّة لوحةٌ بعرضٍ ثابت (الرأس · السطور · الدفع). الإضافة من المربّع والمسح طريقٌ واحد (AddProduct).
        /// </summary>
        private static SalePanel Sale(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, IPosService pos, SaleHooks hooks)
        {
            var products = services.GetRequiredService<IProductService>();
            var categories = services.GetRequiredService<ICategoryService>();
            DocumentRenderer.DocumentEditor editor = null;
            int? category = null;
            List<Product> shown = new();

            var barcode = new AppTextBox { Placeholder = LocalizationService.Get("Str.Pos.Scan"), PrefixIcon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconScan") };
            var hidden = new ContentControl { Visibility = Visibility.Collapsed };
            var cartList = new AppCartList { Margin = new Thickness(12, 0, 12, 0), EmptyText = LocalizationService.Get("Str.NoData") };
            var names = new Dictionary<string, string>();
            var totalText = new TextBlock { HorizontalAlignment = HorizontalAlignment.Right };
            totalText.SetResourceReference(TextBlock.FontSizeProperty, "P.Font.Size.700");
            totalText.SetResourceReference(TextBlock.FontWeightProperty, "P.Font.Weight.Bold");
            totalText.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimary");
            var totalLabel = new TextBlock { Text = LocalizationService.Get("Str.NetTotal"), VerticalAlignment = VerticalAlignment.Center };
            totalLabel.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiary");
            var paid = new AppNumericBox { Label = LocalizationService.Get("Str.Pos.Paid") };
            var header = new ContentControl();
            var result = new TextBlock { Margin = new Thickness(0, 12, 0, 0), TextWrapping = TextWrapping.Wrap };
            result.SetResourceReference(TextBlock.FontSizeProperty, "P.Font.Size.300");
            result.SetResourceReference(TextBlock.ForegroundProperty, "Success");
            var pay = new Btn { Text = LocalizationService.Get("Str.Pos.Pay"), Variant = "primary", Margin = new Thickness(0, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            var holdBtn = new Btn { Text = LocalizationService.Get("Str.Pos.Hold"), Variant = "secondary", Margin = new Thickness(0, 8, 4, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            var recallBtn = new Btn { Variant = "secondary", Margin = new Thickness(4, 8, 0, 0), HorizontalAlignment = HorizontalAlignment.Stretch };
            var printLast = new Btn { Text = LocalizationService.Get("Str.Pos.PrintLast"), Variant = "ghost", Size = "sm", Visibility = Visibility.Collapsed, HorizontalAlignment = HorizontalAlignment.Left };
            var change = new TextBlock { Margin = new Thickness(0, 4, 0, 0) };
            change.SetResourceReference(TextBlock.ForegroundProperty, "Success");
            var closeBtn = new Btn { Text = LocalizationService.Get("Str.Pos.CloseShift"), Variant = "ghost", Size = "sm" };
            var returnBtn = new Btn { Text = LocalizationService.Get("Str.Pos.ReturnTab"), Variant = "ghost", Size = "sm" };
            var newSale = new AppIconButton { Icon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconRefresh"), TooltipText = LocalizationService.Get("Str.Pos.NewSale") };
            var shiftText = new TextBlock { Text = hooks.ShiftInfo, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            shiftText.SetResourceReference(TextBlock.ForegroundProperty, "TextTertiary");
            shiftText.SetResourceReference(TextBlock.FontSizeProperty, "P.Font.Size.100");
            var gallery = new AppGallery { View = _view };
            var chips = new AppChipBar();
            var tiles = new AppIconButton { Icon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconGrid"), TooltipText = LocalizationService.Get("Str.View.Tiles") };
            var details = new AppIconButton { Icon = (System.Windows.Media.Geometry)System.Windows.Application.Current.FindResource("IconList"), TooltipText = LocalizationService.Get("Str.View.Details") };

            void Focus() => barcode.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => barcode.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));

            void Reset(CreateSalesInvoiceDto restore)
            {
                var keep = editor?.HeaderControls.ToDictionary(c => c.Key, c => DialogRenderer.GetControlValue(c.Value, def.HeaderFields.First(f => f.Key == c.Key).Kind));
                editor = DocumentRenderer.BuildEditor(def, services, toast, null);
                if (editor == null) return;
                ((Panel)editor.Body).Children.Remove(editor.Header);
                editor.Header.Children.Clear();
                header.Content = DialogRenderer.BuildGrid(def.HeaderFields.Where(f => f.PickerType == "Customer").ToList(), 1, editor.HeaderControls, width: null);
                hidden.Content = editor.Body;
                // السطور الفارغة الافتراضية لا مكان لها في سلّة البيع
                foreach (var empty in editor.Rows.Where(r => DialogRenderer.GetControlValue(r.Controls[ProductKey], FieldKind.Picker) == null).ToList())
                    editor.RemoveLine(empty);

                foreach (var field in def.HeaderFields.Where(f => f.Kind == FieldKind.Picker))
                {
                    var value = restore != null ? typeof(CreateSalesInvoiceDto).GetProperty(field.Key)?.GetValue(restore) : keep?.GetValueOrDefault(field.Key);
                    if (value != null) DialogRenderer.SelectPickerItem((AppComboBox)editor.HeaderControls[field.Key], value,
                        DialogRenderer.ValueFieldOf(def.DtoType, field.Key, field.PickerValueField));
                }

                // العميل النقدي افتراضاً لكل بيعٍ جديد، ومُقفلٌ إلا إن سمحت الإعدادات — والخادم يفرضه كذلك (PosService.Sell)
                var customerField = def.HeaderFields.FirstOrDefault(f => f.PickerType == "Customer");
                if (customerField != null && editor.HeaderControls.TryGetValue(customerField.Key, out var customerControl) && customerControl is AppComboBox customerPicker)
                {
                    var settings = services.GetRequiredService<PrimeERP.Platform.Settings.ISettingsProvider>();
                    var editable = settings.Get(PrimeERP.Platform.Settings.SettingKeys.Documents.PosCustomerEditable, false);
                    if ((restore == null || !editable) && int.TryParse(settings.Get(PrimeERP.Platform.Settings.SettingKeys.Documents.CashCustomer, ""), out var cash))
                        DialogRenderer.SelectPickerItem(customerPicker, cash, DialogRenderer.ValueFieldOf(def.DtoType, customerField.Key, customerField.PickerValueField));
                    customerPicker.IsEnabled = editable;
                }

                foreach (var line in restore?.Lines ?? new List<CreateSalesInvoiceLineDto>())
                    editor.AddLine(def.LineFields.ToDictionary(f => f.Key, f => typeof(CreateSalesInvoiceLineDto).GetProperty(f.Key)?.GetValue(line)));

                paid.Value = 0;
                RefreshCart();
                RefreshHeld();
                Focus();
            }

            /// <summary>عدد المعلّقة على زرّها</summary>
            void RefreshHeld()
            {
                var held = pos.Held();
                recallBtn.Text = LocalizationService.Get("Str.Pos.Recall", held.IsSuccess ? held.Value.Count : 0);
            }

            /// <summary>الباقي للعميل حين يزيد المدفوع على الصافي — كالويب</summary>
            void RefreshChange()
            {
                var due = editor?.Rows.Sum(Net) ?? 0m;
                change.Text = paid.Value > due ? $"{LocalizationService.Get("Str.Pos.Change")}: {paid.Value - due:N2}" : "";
            }

            decimal Number(DocumentRenderer.EditorRow row, string key) =>
                key != null && row.Controls.TryGetValue(key, out var control) ? Convert.ToDecimal(DialogRenderer.GetControlValue(control, FieldKind.Number) ?? 0m) : 0m;

            /// <summary>صافي السطر بالقاعدة نفسها التي يحسبه بها المحرّر (LineCalc.ForLine)</summary>
            decimal Net(DocumentRenderer.EditorRow row)
            {
                var math = def.LineMath;
                return math == null
                    ? Number(row, QtyKey) * Number(row, PriceKey)
                    : LineCalc.ForLine(Number(row, math.QtyKey), Number(row, math.PriceKey), Number(row, math.DiscountPercentKey), Number(row, math.VatPercentKey), Number(row, math.WithholdingPercentKey)).Net;
            }

            string NameOf(string code)
            {
                if (code == null) return "—";
                if (names.TryGetValue(code, out var name)) return name;
                var found = products.GetByCode(code);
                return names[code] = found.IsSuccess ? found.Value.Name : code;
            }

            /// <summary>السلّة من سطور المحرّر: الاسم والسعر والكمية والصافي، والإجمالي تحتها</summary>
            void RefreshCart()
            {
                if (editor == null) return;
                cartList.Lines = editor.Rows.Select(r => new CartLine(r, NameOf(DialogRenderer.GetControlValue(r.Controls[ProductKey], FieldKind.Picker)?.ToString()),
                    Number(r, PriceKey).ToString("N2"), Number(r, QtyKey), Net(r).ToString("N2"))).ToList();
                totalText.Text = editor.Rows.Sum(Net).ToString("N2");
                RefreshChange();
            }

            /// <summary>الصنف في السلّة: يزيد سطره القائم بالصنف والتشغيلة نفسيهما، وإلا سطرٌ جديد</summary>
            void AddProduct(Product product, decimal qty, decimal? price, string batch)
            {
                if (editor == null) return;
                names[product.Code] = product.Name;
                var same = price == null
                    ? editor.Rows.FirstOrDefault(r => Equals(DialogRenderer.GetControlValue(r.Controls[ProductKey], FieldKind.Picker), product.Code)
                                                     && (batch == null || !r.Controls.ContainsKey("BatchNo")
                                                         || Equals(DialogRenderer.GetControlValue(r.Controls["BatchNo"], FieldKind.Text), batch)))
                    : null;

                if (same != null)
                {
                    DocumentRenderer.SetRowValue(same, def, QtyKey, Number(same, QtyKey) + qty);
                    RefreshCart();
                    return;
                }

                editor.AddLine(new Dictionary<string, object>
                {
                    [ProductKey] = product.Code, [QtyKey] = qty, [PriceKey] = price ?? product.SalePrice, ["BatchNo"] = batch
                });
                RefreshCart();
            }

            /// <summary>الأصناف بالبحث والتصنيف — كصفحة الويب: 60 صنفاً نشطاً</summary>
            void Load()
            {
                var page = products.GetPaged(1, 60, new ProductFilter { SearchText = barcode.Text?.Trim(), CategoryId = category, IsActive = true });
                shown = page.IsSuccess ? page.Value.Items : new List<Product>();
                gallery.Items = shown.Select(p => new GalleryItem(p, p.Name, p.Code, p.SalePrice.ToString("N2"), p.ImageData, Details(p))).ToList();
            }

            /// <summary>المسح: الكود أو الباركود، وإلا الصنف الوحيد في نتيجة البحث</summary>
            void Scan()
            {
                var raw = barcode.Text?.Trim();
                if (string.IsNullOrEmpty(raw) || editor == null) return;

                var scanned = products.Scan(raw);
                if (scanned.IsSuccess)
                {
                    var (product, code) = scanned.Value;
                    AddProduct(product, code.Qty ?? 1m, code.Price, code.Batch);
                }
                else if (shown.Count == 1) AddProduct(shown[0], 1m, null, null);
                else { toast.Error(scanned.ErrorMessage); return; }
                barcode.Text = "";
            }

            void Pay()
            {
                if (editor == null || DocumentRenderer.ReadDto(def, toast, editor) is not CreateSalesInvoiceDto dto) return;
                var done = pos.Sell(dto, paid.Value);
                if (done.IsFailure) { toast.Error(done.ErrorMessage); return; }

                hooks.Sold(done.Value);
                result.Text = LocalizationService.Get("Str.Pos.Done", done.Value.DocumentNo, done.Value.Net, done.Value.Change);
                printLast.Visibility = Visibility.Visible;
                Reset(null);
            }

            void Hold()
            {
                if (editor == null || DocumentRenderer.ReadDto(def, toast, editor) is not CreateSalesInvoiceDto dto) return;
                var held = pos.Hold(dto, null);
                if (held.IsFailure) { toast.Error(held.ErrorMessage); return; }
                Reset(null);
            }

            void ShowView(GalleryView view)
            {
                _view = view;
                gallery.View = view;
                tiles.Variant = view == GalleryView.Tiles ? "brand" : "ghost";
                details.Variant = view == GalleryView.Details ? "brand" : "ghost";
            }

            // البحث أثناء الكتابة بعد توقّفٍ قصير، كالويب
            var typing = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            typing.Tick += (_, __) => { typing.Stop(); Load(); };
            barcode.TextChanged += (_, __) => { typing.Stop(); typing.Start(); };
            barcode.PreviewKeyDown += (_, e) => { if (e.Key == Key.Enter) { typing.Stop(); Scan(); e.Handled = true; } };
            pay.Click += (_, __) => Pay();
            holdBtn.Click += (_, __) => Hold();
            recallBtn.Click += (_, __) => { if (HeldList(pos, toast) is { } restored) Reset(restored); else RefreshHeld(); };
            printLast.Click += (_, __) => hooks.PrintLast();
            closeBtn.Click += (_, __) => hooks.CloseShift();
            returnBtn.Click += (_, __) => hooks.ShowReturn();
            newSale.Click += (_, __) => Reset(null);
            System.ComponentModel.DependencyPropertyDescriptor.FromProperty(AppNumericBox.ValueProperty, typeof(AppNumericBox)).AddValueChanged(paid, (_, _) => RefreshChange());
            gallery.ItemClick += (_, item) => AddProduct((Product)item.Key, 1m, null, null);
            cartList.Step += (_, change) =>
            {
                var row = (DocumentRenderer.EditorRow)change.Key;
                DocumentRenderer.SetRowValue(row, def, QtyKey, Math.Max(1, Number(row, QtyKey) + change.Step));
                RefreshCart();
            };
            cartList.Remove += (_, key) =>
            {
                editor?.RemoveLine((DocumentRenderer.EditorRow)key);
                RefreshCart();
            };
            chips.SelectionChanged += (_, __) => { category = chips.SelectedValue as int?; Load(); };
            tiles.Click += (_, __) => ShowView(GalleryView.Tiles);
            details.Click += (_, __) => ShowView(GalleryView.Details);

            var list = categories.GetAll("Products");
            chips.Items = new[] { new Chip(null, LocalizationService.Get("Str.All")) }
                .Concat(list.IsSuccess ? list.Value.Where(c => c.IsActive).Select(c => new Chip(c.Id, c.Name)) : Enumerable.Empty<Chip>())
                .ToList();
            chips.SelectedValue = null;
            if (!list.IsSuccess || list.Value.Count == 0) chips.Visibility = Visibility.Collapsed;

            // الأصناف: المسح والعرض · الشرائح · المعرض
            var search = new DockPanel();
            var toggle = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0), Children = { tiles, details } };
            DockPanel.SetDock(toggle, Dock.Right);
            search.Children.Add(toggle);
            search.Children.Add(barcode);

            var shiftRow = new DockPanel { Margin = new Thickness(0, 6, 0, 0) };
            var shiftActions = new StackPanel { Orientation = Orientation.Horizontal, Children = { returnBtn, closeBtn } };
            DockPanel.SetDock(shiftActions, Dock.Right);
            shiftRow.Children.Add(shiftActions);
            shiftRow.Children.Add(shiftText);

            var top = Section(new StackPanel { Children = { search, shiftRow } }, new Thickness(12, 10, 12, 10));
            var chipBar = Section(chips, new Thickness(12, 8, 12, 8));
            chipBar.Visibility = chips.Visibility;
            gallery.Margin = new Thickness(6);

            var catalog = new DockPanel();
            DockPanel.SetDock(top, Dock.Top);
            DockPanel.SetDock(chipBar, Dock.Top);
            catalog.Children.Add(top);
            catalog.Children.Add(chipBar);
            catalog.Children.Add(gallery);
            var catalogCard = Card(catalog);

            // السلّة: الرأس · السطور · الدفع
            var totalRow = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(totalLabel, Dock.Left);
            totalRow.Children.Add(totalLabel);
            totalRow.Children.Add(totalText);
            var holdRow = new Grid();
            holdRow.ColumnDefinitions.Add(new ColumnDefinition());
            holdRow.ColumnDefinitions.Add(new ColumnDefinition());
            Grid.SetColumn(recallBtn, 1);
            holdRow.Children.Add(holdBtn);
            holdRow.Children.Add(recallBtn);
            var payment = new Border { Padding = new Thickness(12), BorderThickness = new Thickness(0, 1, 0, 0), Child = new StackPanel { Children = { totalRow, paid, change, holdRow, pay, result, printLast } } };
            payment.SetResourceReference(Border.BackgroundProperty, "SurfaceSunken");
            payment.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
            // الحوافّ السفلى بحوافّ البطاقة، فلا يخرج لون التذييل عنها
            if (System.Windows.Application.Current.TryFindResource("P.Radius.Lg") is CornerRadius radius)
                payment.CornerRadius = new CornerRadius(0, 0, radius.BottomRight, radius.BottomLeft);
            var cartHead = new DockPanel();
            DockPanel.SetDock(newSale, Dock.Right);
            newSale.VerticalAlignment = VerticalAlignment.Bottom;
            cartHead.Children.Add(newSale);
            cartHead.Children.Add(header);
            cartList.MinHeight = 160;
            var head = Section(cartHead, new Thickness(12, 10, 12, 10));
            var cart = new DockPanel();
            DockPanel.SetDock(head, Dock.Top);
            DockPanel.SetDock(payment, Dock.Bottom);
            cart.Children.Add(head);
            cart.Children.Add(payment);
            cart.Children.Add(hidden);
            cart.Children.Add(cartList);
            var cartCard = Card(cart);
            cartCard.Width = 384; // عرض سلّة الويب (24rem): تبقى للأصناف ثلاثة مربّعاتٍ فأكثر
            cartCard.Margin = new Thickness(16, 0, 0, 0);

            var view = new Grid { Margin = new Thickness(16) };
            view.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            view.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(cartCard, 1);
            view.Children.Add(catalogCard);
            view.Children.Add(cartCard);

            view.PreviewKeyDown += (_, e) =>
            {
                if (e.Key == Key.F12) { Pay(); e.Handled = true; }
                else if (e.Key == Key.F2) { Focus(); e.Handled = true; }
            };

            ShowView(_view);
            Reset(null);
            Load();
            return new SalePanel(view, Focus, Hold, Reset);
        }

        /// <summary>بيانات الصنف عند المرور</summary>
        private static string Details(Product product) =>
            string.Join(Environment.NewLine, new[] { product.Name, $"{LocalizationService.Get("Str.SalePrice")}: {product.SalePrice:N2}", product.Description }
                .Where(line => !string.IsNullOrWhiteSpace(line)));

        /// <summary>بطاقةٌ بسطح الصفحة وحدّها وحوافّها من 5.Design</summary>
        /// <summary>قسمٌ في البطاقة بحشوته وخطٍّ فاصلٍ تحته — كرأس بطاقات الويب (border-b)</summary>
        private static Border Section(UIElement child, Thickness padding)
        {
            var section = new Border { Child = child, Padding = padding, BorderThickness = new Thickness(0, 0, 0, 1) };
            section.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
            return section;
        }

        private static Border Card(UIElement child)
        {
            var card = new Border { Child = child, BorderThickness = new Thickness(1) };
            card.SetResourceReference(Border.BackgroundProperty, "SurfaceDefault");
            card.SetResourceReference(Border.BorderBrushProperty, "OutlineDefault");
            card.SetResourceReference(Border.CornerRadiusProperty, "P.Radius.Lg");
            return card;
        }

        private static FrameworkElement Return(DocumentDialogDefinition def, IServiceProvider services, IToastService toast, IPosService pos, Action back)
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
            var backBtn = new Btn { Text = LocalizationService.Get("Str.Pos.SaleTab"), Variant = "secondary", Size = "sm" };
            backBtn.Click += (_, __) => back();
            actions.Children.Add(backBtn);
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
