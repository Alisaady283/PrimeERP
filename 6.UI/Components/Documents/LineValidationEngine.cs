using System;
using System.Collections.Generic;
using System.Linq;

namespace PrimeERP.UI.Components.Documents
{
    /// <summary>تحقّق سطور المستند</summary>
    public enum DocumentLinesMode
    {
        Journal, SalesInvoice, PurchaseInvoice, Stock
    }

    /// <summary>سياق التحقق</summary>
    public class DocumentLinesContext
    {
        public List<DocumentLine> AllLines         { get; set; } = new();
        public string             DuplicateCheckKey{ get; set; }
        public DocumentLinesMode  Mode             { get; set; } = DocumentLinesMode.SalesInvoice;
    }

    /// <summary>يتحقق من سطر مستند واحد</summary>
    public static class LineValidationEngine
    {
        public static Dictionary<string, string> Validate(DocumentLine line, List<LineColumn> columns, DocumentLinesContext context = null)
        {
            var errors = new Dictionary<string, string>();

            if (line.IsEmpty)
            {
                line.Errors.Clear();
                return errors;
            }

            foreach (var col in columns.Where(c => c.IsRequired))
            {
                if (IsMissing(line[col.Key], col.Type))
                    errors[col.Key] = $"{col.Header} مطلوب";
            }

            if (HasColumn(columns, nameof(DocumentLine.Qty)) && line.Qty <= 0)
                errors[nameof(DocumentLine.Qty)] = "الكمية يجب أن تكون أكبر من صفر";

            if (HasColumn(columns, nameof(DocumentLine.Price)) && line.Price < 0)
                errors[nameof(DocumentLine.Price)] = "السعر لا يمكن أن يكون سالباً";

            if (HasColumn(columns, nameof(DocumentLine.DiscountPercent)) &&
                (line.DiscountPercent < 0 || line.DiscountPercent > 100))
                errors[nameof(DocumentLine.DiscountPercent)] = "نسبة الخصم يجب أن تكون بين 0 و 100";

            if (context?.Mode == DocumentLinesMode.Journal)
                ValidateJournalDebitCredit(line, errors);

            if (context?.DuplicateCheckKey != null)
                ValidateDuplicate(line, context, errors);

            line.Errors.Clear();
            foreach (var kv in errors)
                line.Errors[kv.Key] = kv.Value;

            return errors;
        }

        private static void ValidateJournalDebitCredit(DocumentLine line, Dictionary<string, string> errors)
        {
            bool hasDebit  = line.Debit  > 0;
            bool hasCredit = line.Credit > 0;

            if (hasDebit && hasCredit)
            {
                errors[nameof(DocumentLine.Debit)]  = "لا يمكن إدخال مدين ودائن في نفس السطر";
                errors[nameof(DocumentLine.Credit)] = "لا يمكن إدخال مدين ودائن في نفس السطر";
            }
            else if (!hasDebit && !hasCredit)
            {
                errors[nameof(DocumentLine.Debit)] = "أدخل مدين أو دائن";
            }
        }

        private static void ValidateDuplicate(DocumentLine line, DocumentLinesContext context, Dictionary<string, string> errors)
        {
            var key = context.DuplicateCheckKey;
            var value = line[key]?.ToString();
            if (string.IsNullOrEmpty(value)) return;

            var count = (context.AllLines ?? new List<DocumentLine>())
                .Count(l => !l.IsEmpty && string.Equals(l[key]?.ToString(), value, StringComparison.OrdinalIgnoreCase));

            if (count > 1)
                errors[key] = "هذا العنصر مستخدم في سطر آخر بالفعل";
        }

        private static bool HasColumn(List<LineColumn> columns, string key) => columns.Any(c => c.Key == key);

        private static bool IsMissing(object value, LineColumnType type) => type switch
        {
            LineColumnType.Text or LineColumnType.Picker or LineColumnType.Combo =>
                value == null || string.IsNullOrWhiteSpace(value.ToString()),
            LineColumnType.Integer or LineColumnType.Decimal or LineColumnType.Money or LineColumnType.Percent =>
                value == null || Convert.ToDecimal(value) == 0,
            _ => value == null
        };
    }
}
