using PrimeERP.UI.Components.Documents;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.UI.Converters
{
    /// <summary>يقرأ قيمة خلية بالمفتاح الديناميكي</summary>
    public class LineCellValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return null;
            if (values[0] is not DocumentLine line) return null;
            if (values[1] is not string key || string.IsNullOrEmpty(key)) return null;
            return line[key];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException("الكتابة تتم عبر معالجات الأحداث لا عبر ConvertBack.");
    }

    /// <summary>values[0]=Errors</summary>
    public class CellHasErrorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return false;
            if (values[0] is not Dictionary<string, string> errors) return false;
            if (values[1] is not string key || string.IsNullOrEmpty(key)) return false;
            return errors.ContainsKey(key);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>مثل CellHasErrorConverter لكنه يرجع نص</summary>
    public class CellErrorMessageConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2) return null;
            if (values[0] is not Dictionary<string, string> errors) return null;
            if (values[1] is not string key || string.IsNullOrEmpty(key)) return null;
            return errors.TryGetValue(key, out var message) ? message : null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>يحوّل HorizontalAlignment</summary>
    public class HorizontalToTextAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            HorizontalAlignment.Center => TextAlignment.Center,
            HorizontalAlignment.Right  => TextAlignment.Right,
            _ => TextAlignment.Left
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
