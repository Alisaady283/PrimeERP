using PrimeERP.UI.Services;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.UI.Components.Actions
{
    /// <summary>شريط أدوات ببناء برمجي (ButtonsSource) — صلاحيات تلقائية، اختصارات لوحة مفاتيح، وقائمة "المزيد" عند ضيق المساحة.</summary>
    public partial class ActionToolbar : UserControl
    {
        public static readonly DependencyProperty ButtonsSourceProperty =
            DependencyProperty.Register(nameof(ButtonsSource), typeof(IEnumerable<ToolbarAction>), typeof(ActionToolbar),
                new PropertyMetadata(null, OnButtonsSourceChanged));

        public IEnumerable<ToolbarAction> ButtonsSource
        {
            get => (IEnumerable<ToolbarAction>)GetValue(ButtonsSourceProperty);
            set => SetValue(ButtonsSourceProperty, value);
        }

        private List<ToolbarAction> _actions = new();
        private readonly List<(ToolbarAction Action, FrameworkElement Element, double Width)> _rendered = new();
        private readonly List<KeyBinding> _registeredBindings = new();
        private Window _hostWindow;

        public ActionToolbar()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                _hostWindow = Window.GetWindow(this);
                Rebuild();
                AppSession.PermissionsChanged += OnPermissionsChanged;
            };
            Unloaded += (s, e) =>
            {
                UnregisterShortcuts();
                AppSession.PermissionsChanged -= OnPermissionsChanged;
            };
        }

        private void OnPermissionsChanged(object sender, System.EventArgs e) => Rebuild();

        private static void OnButtonsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((ActionToolbar)d).Rebuild();

        private void Rebuild()
        {
            _actions = (ButtonsSource ?? Enumerable.Empty<ToolbarAction>())
                .Where(a => a.Separator || (a.IsVisible &&
                       (string.IsNullOrEmpty(a.PermissionKey) || UIServices.Permissions.Can(a.PermissionKey))))
                .ToList();

            mainPanel.Children.Clear();
            _rendered.Clear();

            foreach (var action in _actions)
            {
                FrameworkElement element = action.Separator ? BuildSeparator() : BuildButton(action);
                mainPanel.Children.Add(element);
            }

            RegisterShortcuts();

            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, () =>
            {
                CacheWidths();
                ApplyOverflow(ActualWidth);
            });
        }

        private FrameworkElement BuildSeparator() => new Border { Style = (Style)FindResource("ActionToolbarSeparatorStyle") };

        private AppButton BuildButton(ToolbarAction action) => new()
        {
            Text = action.Text,
            Icon = action.Icon,
            Variant = action.Variant,
            Size = "sm",
            Command = action.Command,
            IsEnabled = action.IsEnabled,
            ToolTip = action.Tooltip,
            Margin = (Thickness)FindResource("ActionToolbarItemSpacing")
        };

        private void CacheWidths()
        {
            // Rebuild() تُستدعى مرتين متتاليتين (تغيّر ButtonsSource ثم Loaded)، وكل مرة تُجدوِل CacheWidths
            // عبر BeginInvoke — الاستدعاء الثاني يجد _rendered ممتلئة بالفعل من الأول (Rebuild لا تُصفّرها إلا
            // عند بدايتها هي، لا هنا)، فيضيف فوق القائمة بدل استبدالها = كل عنصر مكرر في قائمة "المزيد".
            _rendered.Clear();
            for (int i = 0; i < mainPanel.Children.Count && i < _actions.Count; i++)
            {
                var el = (FrameworkElement)mainPanel.Children[i];
                el.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                _rendered.Add((_actions[i], el, el.DesiredSize.Width + el.Margin.Left + el.Margin.Right));
            }
        }

        private void ActionToolbar_SizeChanged(object sender, SizeChangedEventArgs e) => ApplyOverflow(e.NewSize.Width);

        private void ApplyOverflow(double availableWidth)
        {
            if (_rendered.Count == 0 || availableWidth <= 0) return;

            const double overflowButtonWidth = 90;
            double total = _rendered.Sum(r => r.Width);

            if (total <= availableWidth)
            {
                foreach (var r in _rendered) r.Element.Visibility = Visibility.Visible;
                overflowBtn.Visibility = Visibility.Collapsed;
                return;
            }

            double budget = availableWidth - overflowButtonWidth;
            double used = 0;
            var overflowItems = new List<ToolbarAction>();
            bool overflowing = false;

            foreach (var r in _rendered)
            {
                if (!overflowing && used + r.Width <= budget)
                {
                    r.Element.Visibility = Visibility.Visible;
                    used += r.Width;
                }
                else
                {
                    overflowing = true;
                    r.Element.Visibility = Visibility.Collapsed;
                    if (!r.Action.Separator) overflowItems.Add(r.Action);
                }
            }

            overflowBtn.Items = overflowItems;
            overflowBtn.DisplayMemberPath = nameof(ToolbarAction.Text);
            overflowBtn.Visibility = overflowItems.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OverflowBtn_ItemSelected(object sender, object item)
        {
            if (item is ToolbarAction action && action.Command?.CanExecute(null) == true)
                action.Command.Execute(null);
        }

        private void RegisterShortcuts()
        {
            if (_hostWindow == null) return;
            UnregisterShortcuts();

            foreach (var action in _actions.Where(a =>
                !a.Separator && !string.IsNullOrEmpty(a.Shortcut) && a.Command != null))
            {
                try
                {
                    if (new KeyGestureConverter().ConvertFromString(action.Shortcut) is not KeyGesture gesture)
                        continue;

                    var binding = new KeyBinding(action.Command, gesture);
                    _hostWindow.InputBindings.Add(binding);
                    _registeredBindings.Add(binding);
                }
                catch
                {
                    // اختصار غير صالح — يُتجاهل بدل تعطيل الشريط بالكامل
                }
            }
        }

        private void UnregisterShortcuts()
        {
            if (_hostWindow == null) return;
            foreach (var binding in _registeredBindings)
                _hostWindow.InputBindings.Remove(binding);
            _registeredBindings.Clear();
        }
    }
}
