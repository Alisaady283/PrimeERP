using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PrimeERP.Composition.Definitions;
using PrimeERP.Platform.Localization;
using PrimeERP.UI.Components.Inputs;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>شريط فلاتر الصفحة المُعلَنة</summary>
    public static class FilterControls
    {
        public static FrameworkElement Build(List<FilterDefinition> filters, dynamic vm, IServiceProvider services)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var controls = new Dictionary<string, FrameworkElement>();

            foreach (var filter in filters)
            {
                var field = Field(filter);
                var control = DialogRenderer.BuildField(field);
                control.Margin = new Thickness(0, 0, 8, 0);

                if (control is AppComboBox combo)
                {
                    combo.Label = null;
                    combo.Placeholder = LocalizationService.Get(filter.LabelKey);
                    combo.Width = filter.Width;
                    combo.AllowClear = true;
                    DialogRenderer.LoadPickerItems(combo, field, services);
                }

                var captured = filter;
                DialogRenderer.OnChanged(control, () =>
                {
                    DialogRenderer.WriteValue((object)vm.Filter, captured.Key,
                        DialogRenderer.GetControlValue(control, Field(captured).Kind));
                    vm.SearchCommand.Execute(null);
                });

                controls[filter.Key] = control;
                panel.Children.Add(control);
            }

            foreach (var filter in filters.Where(f => !string.IsNullOrEmpty(f.PickerFilterField)))
            {
                if (!controls.TryGetValue(filter.PickerFilterField, out var source)) continue;
                if (!controls.TryGetValue(filter.Key, out var found) || found is not AppComboBox target) continue;

                var captured = filter;
                DialogRenderer.OnChanged(source, () =>
                {
                    target.SelectedItem = null;
                    DialogRenderer.LoadPickerItems(target, Field(captured), services,
                        DialogRenderer.GetControlValue(source, Field(filters.First(f => f.Key == captured.PickerFilterField)).Kind));
                });
            }

            return panel;
        }

        private static FieldDefinition Field(FilterDefinition filter) => new()
        {
            Key = filter.Key,
            LabelKey = filter.LabelKey,
            Kind = filter.Kind == FilterKind.Toggle ? FieldKind.Check : FieldKind.Picker,
            PickerType = filter.PickerType,
            PickerCategoryModuleKey = filter.PickerCategoryModuleKey
        };
    }
}
