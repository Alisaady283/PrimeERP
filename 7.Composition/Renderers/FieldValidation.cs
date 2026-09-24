using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using PrimeERP.Composition.Definitions;
using PrimeERP.UI.Components.Inputs;

namespace PrimeERP.Composition.Renderers
{
    /// <summary>تحقّق واجهة واحد لكل الشاشات</summary>
    internal static class FieldValidation
    {
        internal static bool Validate(List<FieldDefinition> fieldDefs, Dictionary<string, FrameworkElement> controls)
        {
            var valid = true;

            foreach (var field in fieldDefs)
            {
                if (!controls.TryGetValue(field.Key, out var control)) continue;
                if (control.Visibility != Visibility.Visible) { SetError(control, null); continue; }

                var error = Check(field, DialogRenderer.GetControlValue(control, field.Kind));
                SetError(control, error);
                if (error != null) valid = false;
            }

            return valid;
        }

        private static string Check(FieldDefinition field, object value)
        {
            var label = string.IsNullOrEmpty(field.LabelKey) ? field.Key : PrimeERP.Platform.Localization.LocalizationService.Get(field.LabelKey);

            var isEmpty = value == null
                || (value is string text && string.IsNullOrWhiteSpace(text))
                || (field.Kind == FieldKind.Date && value is DateTime date && date == default);

            if (isEmpty) return field.IsRequired ? $"{label} مطلوب" : null;

            return field.Kind switch
            {
                FieldKind.Number => CheckNumber(field, label, value),
                FieldKind.Date   => CheckDate(field, label, value),
                _                => CheckText(field, label, value)
            };
        }

        private static string CheckNumber(FieldDefinition field, string label, object value)
        {
            if (!decimal.TryParse(value.ToString(), out var number)) return $"{label} يجب أن يكون رقماً";
            if (field.Min != null && number < field.Min) return $"{label} لا يقل عن {field.Min:N2}";
            if (field.Max != null && number > field.Max) return $"{label} لا يزيد عن {field.Max:N2}";

            return null;
        }

        private static string CheckDate(FieldDefinition field, string label, object value)
        {
            if (value is not DateTime date) return $"{label} تاريخ غير صالح";

            if (date.Year < 1900) return $"{label} تاريخ غير صالح";
            if (field.MinDate != null && date < field.MinDate) return $"{label} لا يسبق {field.MinDate:yyyy-MM-dd}";
            if (field.MaxDate != null && date > field.MaxDate) return $"{label} لا يتجاوز {field.MaxDate:yyyy-MM-dd}";

            return null;
        }

        private static string CheckText(FieldDefinition field, string label, object value)
        {
            var text = value.ToString();
            if (field.MinLength > 0 && text.Length < field.MinLength) return $"{label} لا يقل عن {field.MinLength} حرفاً";
            if (field.MaxLength > 0 && text.Length > field.MaxLength) return $"{label} لا يزيد عن {field.MaxLength} حرفاً";
            if (!string.IsNullOrEmpty(field.Pattern) && !Regex.IsMatch(text, field.Pattern))
                return field.PatternMessage ?? $"صيغة {label} غير صحيحة";

            return null;
        }

        internal static void SetError(FrameworkElement control, string error)
        {
            switch (control)
            {
                case AppTextBox box:      box.ErrorText = error; break;
                case AppTextArea area:    area.ErrorText = error; break;
                case AppNumericBox num:   num.ErrorText = error; break;
                case AppDatePicker date:  date.ErrorText = error; break;
                case AppComboBox combo:   combo.ErrorText = error; break;
                case AppPasswordBox pass: pass.ErrorText = error; break;
            }
        }

        internal static void ClearErrors(Dictionary<string, FrameworkElement> controls)
        {
            foreach (var control in controls.Values) SetError(control, null);
        }
    }
}
