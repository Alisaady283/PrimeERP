using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>
    /// يقرأ قيمة خلية بالمفتاح الديناميكي لعمودها: values[0]=DocumentLine (Binding Path="")، values[1]=مفتاح العمود
    /// (يصل عبر RelativeSource إلى DataGridCell.Column.SortMemberPath). أحادي الاتجاه فقط — الكتابة تتم عبر
    /// معالجات أحداث في DocumentLinesGrid.xaml.cs (LostFocus/Checked) لا عبر ConvertBack، لتفادي هشاشة تخزين
    /// آخر قيمة في حقول المحوّل المشترك بين كل الخلايا.
    /// </summary>
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

    /// <summary>values[0]=Errors (Binding Path="Errors")، values[1]=مفتاح العمود — يرجع true لو للخلية خطأ تحقّق حالي.</summary>
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

    /// <summary>مثل CellHasErrorConverter لكنه يرجع نص رسالة الخطأ نفسها (للـ tooltip).</summary>
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

    /// <summary>يحوّل HorizontalAlignment (خاصية DataGridCell.HorizontalContentAlignment) إلى TextAlignment المكافئ لعناصر TextBox.</summary>
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
