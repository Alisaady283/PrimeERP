using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>معاملات صفحةٍ تستدعي خدمةً بالاسم: حقولها، ثم الاستدعاء بقيمها</summary>
    internal static class ReportParameters
    {
        internal static (StackPanel Panel, Func<Dictionary<string, object>> Read) Build(IReadOnlyList<ParameterDefinition> parameters, IServiceProvider services)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 16, 24, 0) };
            var controls = new Dictionary<string, FrameworkElement>();
            foreach (var p in parameters)
            {
                var fieldDef = new FieldDefinition
                {
                    Key = p.Key, LabelKey = p.LabelKey, Kind = p.Kind,
                    PickerType = p.PickerType, PickerCategoryModuleKey = p.PickerCategoryModuleKey,
                    PickerLeafOnly = p.PickerLeafOnly
                };
                var control = DialogRenderer.BuildField(fieldDef);
                if (p.Kind == FieldKind.Picker) DialogRenderer.LoadPickerItems((PrimeERP.UI.Components.Inputs.AppComboBox)control, fieldDef, services);
                if (p.DefaultValue != null) DialogRenderer.SetControlValue(control, fieldDef, p.DefaultValue);
                control.Width = 200;
                control.Margin = new Thickness(0, 0, 12, 0);
                controls[p.Key] = control;
                panel.Children.Add(control);
            }

            Dictionary<string, object> Read()
            {
                var values = new Dictionary<string, object>();
                foreach (var p in parameters) values[p.Key] = DialogRenderer.GetControlValue(controls[p.Key], p.Kind);
                return values;
            }

            return (panel, Read);
        }

        internal static Result<T> Invoke<T>(Type serviceType, string methodName, string[] arguments, Dictionary<string, object> values, IServiceProvider services)
        {
            var service = services.GetRequiredService(serviceType);
            var method = serviceType.GetMethod(methodName);
            if (method == null)
                return Result.Fail<T>(LocalizationService.Get("Str.Composition.MissingMethod", serviceType.Name, methodName));

            var signature = method.GetParameters();
            var args = new object[signature.Length];
            for (var i = 0; i < signature.Length; i++)
            {
                var key = i < arguments.Length ? arguments[i] : signature[i].Name;
                values.TryGetValue(key, out var value);

                var target = Nullable.GetUnderlyingType(signature[i].ParameterType) ?? signature[i].ParameterType;
                args[i] = value == null
                    ? (signature[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(signature[i].ParameterType) == null
                        ? Activator.CreateInstance(signature[i].ParameterType) : null)
                    : Convert.ChangeType(value, target);
            }

            return (Result<T>)method.Invoke(service, args);
        }
    }
}
