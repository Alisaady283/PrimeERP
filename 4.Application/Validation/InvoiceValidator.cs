using PrimeERP.Domain.Contracts;
using System;
using PrimeERP.Domain.Entities;

namespace PrimeERP.Application.Validation
{
    /// <summary>يتحقق من فواتير المبيعات والمشتريات معاً — نفس القواعد المشتركة بين النوعين.</summary>
    public static class InvoiceValidator
    {
        public static ValidationResult ValidateSales(SalesInvoice invoice)
        {
            var result = new ValidationResult();
            ValidateCommon(result, invoice.InvoiceDate, invoice.Lines.Count, invoice.NetTotal);
            return result;
        }

        public static ValidationResult ValidatePurchase(PurchaseInvoice invoice)
        {
            var result = new ValidationResult();
            ValidateCommon(result, invoice.InvoiceDate, invoice.Lines.Count, invoice.NetTotal);
            return result;
        }

        private static void ValidateCommon(ValidationResult result, DateTime invoiceDate, int lineCount, decimal netTotal)
        {
            if (invoiceDate == default)
                result.AddError("InvoiceDate", "تاريخ الفاتورة مطلوب");

            if (lineCount == 0)
                result.AddError("Lines", "الفاتورة يجب أن تحتوي على سطر واحد على الأقل");

            if (netTotal < 0)
                result.AddError("NetTotal", "إجمالي الفاتورة لا يمكن أن يكون سالباً");
        }
    }
}
