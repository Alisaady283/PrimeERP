using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Localization;

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
                    errors[col.Key] = LocalizationService.Get("Str.Rule.Required", col.Header);
            }

            if (HasColumn(columns, nameof(DocumentLine.Qty)) && line.Qty <= 0)
                errors[nameof(DocumentLine.Qty)] = LocalizationService.Get("Str.Document.QtyPositive");

            if (HasColumn(columns, nameof(DocumentLine.Price)) && line.Price < 0)
                errors[nameof(DocumentLine.Price)] = LocalizationService.Get("Str.Rule.NonNegative", LocalizationService.Get("Str.Line.Price"));

            if (HasColumn(columns, nameof(DocumentLine.DiscountPercent)) &&
                (line.DiscountPercent < 0 || line.DiscountPercent > 100))
                errors[nameof(DocumentLine.DiscountPercent)] = LocalizationService.Get("Str.Rule.Range", LocalizationService.Get("Str.Line.DiscountPercent"), 0, 100);

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
                errors[nameof(DocumentLine.Debit)]  = LocalizationService.Get("Str.Journal.BothDebitAndCredit");
                errors[nameof(DocumentLine.Credit)] = LocalizationService.Get("Str.Journal.BothDebitAndCredit");
            }
            else if (!hasDebit && !hasCredit)
            {
                errors[nameof(DocumentLine.Debit)] = LocalizationService.Get("Str.Line.DebitOrCredit");
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
                errors[key] = LocalizationService.Get("Str.Line.Duplicate");
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
