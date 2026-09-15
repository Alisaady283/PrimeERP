using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Display;
using PrimeERP.UI.Components.Inputs;
using PrimeERP.UI.Components.Layout;
using PrimeERP.UI.Components.Tree;
using PrimeERP.UI.Services;
using Btn = PrimeERP.UI.Components.Actions.AppButton;

namespace PrimeERP.Composition.Renderers
{
    // شجرة قابلة للتأشير مدفوعة بمصدر — تجميع PageHeader/FilterBar/AppTreeView/AppComboBox، صفر عنصر خام.
    public static class TreeCheckListRenderer
    {
        public static FrameworkElement Render(ModuleDefinition definition, IServiceProvider services)
        {
            var def = definition.TreeCheckList
                ?? throw new InvalidOperationException($"الوحدة '{definition.Key}' بتخطيط TreeCheckList بلا TreeCheckListDefinition.");

            var toast = services.GetRequiredService<IToastService>();
            // القائمة تُستبدَل ولا تُفرَّغ: إسناد نفس المرجع لا يُطلق إشعار تغيير في WPF، فتبقى الشجرة
            // معروضة بعُقد التحميل الأول بينما الأزرار والحفظ يعملان على عُقد جديدة لا يراها أحد.
            var nodes = new List<TreeNodeViewModel>();
            var tree = new AppTreeView { CheckMode = def.Mode };

            var sourcePicker = new AppComboBox
            {
                Label = LocalizationService.Get(def.SourceLabelKey),
                DisplayMemberPath = "Display",
                SelectedValuePath = "Id",
                Width = 260
            };
            sourcePicker.ItemsSource = def.SourceItems(services);

            var summary = new TextBlock { Margin = new Thickness(0, 8, 0, 0) };
            summary.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondary");

            void RefreshSummary()
            {
                var keyNodes = nodes.SelectMany(n => n.Children).ToList();
                var granted = keyNodes.Count(n => n.CheckState is NodeCheckState.Checked or NodeCheckState.Granted);
                summary.Text = $"الممنوح {granted} من {keyNodes.Count}";
            }

            int SelectedSourceId() => sourcePicker.SelectedValue is int id ? id : 0;

            void LoadTree()
            {
                var sourceId = SelectedSourceId();
                // الوصف يقرّر ما يُعرض بلا مصدر مختار — شاشة إنشاء البرنامج تعرض الشجرة كاملةً،
                // وشاشات الصلاحيات تُرجع فارغاً لأن لا دور بعد.
                nodes = def.BuildTree(services, sourceId) ?? new List<TreeNodeViewModel>();
                def.ApplyRules?.Invoke(nodes, null);
                tree.ItemsSource = nodes;
                RefreshSummary();
            }

            sourcePicker.SelectionChanged += (_, __) => LoadTree();
            // العقدة التي تغيّرت تصل للقاعدة: بلا معرفتها لا يعرف القسم أنه هو من نُقر فيورّث صفحاته.
            tree.CheckStateChanged += (_, changed) =>
            {
                def.ApplyRules?.Invoke(nodes, changed);
                RefreshSummary();
            };

            var header = new PageHeader();
            var actions = new StackPanel { Orientation = Orientation.Horizontal };

            foreach (var action in def.Actions)
            {
                var button = new Btn { Text = LocalizationService.Get(action.TextKey), Variant = action.Variant, Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
                button.Click += async (_, __) =>
                {
                    // الحارس مُعلَن لا مفروض: ما يحتاج مصدراً يُرفض برسالة، وما لا يحتاجه يمضي.
                    if (action.RequiresSource && SelectedSourceId() == 0)
                    {
                        toast.Info(LocalizationService.Get(def.SourceLabelKey) + " مطلوب أولاً");
                        return;
                    }

                    if (action.RunAsync != null)
                    {
                        button.IsEnabled = false;
                        try
                        {
                            var outcome = await action.RunAsync(services, SelectedSourceId(), nodes);
                            if (outcome.IsSuccess) toast.Success(LocalizationService.Get("Str.Success"));
                            else toast.Error(outcome.ErrorMessage);
                        }
                        finally { button.IsEnabled = true; }
                    }
                    else action.Run(services, SelectedSourceId(), nodes);

                    def.ApplyRules?.Invoke(nodes, null);
                    RefreshSummary();

                    // إجراءٌ قد يُضيف مصدراً (عميلاً جديداً) — فالقائمة تُعاد قراءتها بعده لا تبقى قديمة.
                    sourcePicker.ItemsSource = def.SourceItems(services);
                };
                actions.Children.Add(button);
            }

            var saveButton = new Btn { Text = LocalizationService.Get(def.SaveTextKey), Variant = "primary", Size = "sm", Margin = new Thickness(8, 0, 0, 0) };
            saveButton.Click += async (_, __) =>
            {
                // الحفظ يخصّ مصدراً بعينه دائماً — والرفض يُقال ولا يُصمَت عنه.
                if (SelectedSourceId() == 0)
                {
                    toast.Info(LocalizationService.Get(def.SourceLabelKey) + " مطلوب أولاً");
                    return;
                }

                // الزرّ يُعطَّل أثناء العمل: الحفظ قد يطول، ونقرةٌ ثانية تبدأ العملية مرّتين على نفس المسار.
                saveButton.IsEnabled = false;
                try
                {
                    var result = await def.Save(services, SelectedSourceId(), nodes);
                    if (result.IsSuccess) toast.Success(LocalizationService.Get("Str.Success"));
                    else toast.Error(result.ErrorMessage);
                }
                finally { saveButton.IsEnabled = true; }
            };
            actions.Children.Add(saveButton);
            header.ActionsContent = actions;

            var filterBar = new FilterBar { SearchPlaceholder = LocalizationService.Get("Str.Search") };
            filterBar.Search += (_, text) => tree.SearchText = text;

            var top = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 8, 24, 0) };
            top.Children.Add(sourcePicker);

            if (def.SourceNote != null)
            {
                var note = new AppTextBox
                {
                    Label = LocalizationService.Get("Str.Builder.Serial"),
                    Width = 240, IsReadOnly = true, Margin = new Thickness(12, 0, 0, 0)
                };

                top.Children.Add(note);
                sourcePicker.SelectionChanged += (_, __) => note.Text = def.SourceNote(services, SelectedSourceId());
            }

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Grid.SetRow(header, 0);
            Grid.SetRow(top, 1);
            Grid.SetRow(filterBar, 2);
            Grid.SetRow(tree, 3);
            Grid.SetRow(summary, 4);
            root.Children.Add(header);
            root.Children.Add(top);
            root.Children.Add(filterBar);
            root.Children.Add(tree);
            root.Children.Add(summary);

            root.Loaded += (_, __) =>
            {
                var first = def.SourceItems(services).FirstOrDefault();

                // مصدرٌ موجود يُختار فيُحمّل باختياره؛ وبلا مصادر تُحمَّل الشجرة كما يقرّرها الوصف —
                // وإلّا بقيت الصفحة فارغة إلى أن يوجد أوّل مصدر، وهي أوّل ما يراه المستخدم.
                if (first != null)
                    sourcePicker.SelectedItem = ((List<SourceOption>)sourcePicker.ItemsSource)
                        .FirstOrDefault(o => o.Id == first.Id);
                else
                    LoadTree();
            };

            return root;
        }
    }
}
