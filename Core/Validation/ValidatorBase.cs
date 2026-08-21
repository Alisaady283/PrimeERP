using System;
using System.Text.RegularExpressions;

namespace PrimeERP.Core.Validation
{
    /// <summary>قواعد تحقق عامة مشتركة — كل Validator ملموس يرث منها ويستخدمها بدل تكرار نفس المنطق.</summary>
    public abstract class ValidatorBase
    {
        protected void Required(ValidationResult result, string field, string value, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                result.AddError(field, $"{label} مطلوب");
        }

        protected void MinLength(ValidationResult result, string field, string value, int min, string label)
        {
            if (!string.IsNullOrEmpty(value) && value.Length < min)
                result.AddError(field, $"{label} يجب ألا يقل عن {min} حرف");
        }

        protected void MaxLength(ValidationResult result, string field, string value, int max, string label)
        {
            if (!string.IsNullOrEmpty(value) && value.Length > max)
                result.AddError(field, $"{label} يجب ألا يتجاوز {max} حرف");
        }

        protected void Range(ValidationResult result, string field, decimal value, decimal min, decimal max, string label)
        {
            if (value < min || value > max)
                result.AddError(field, $"{label} يجب أن يكون بين {min} و {max}");
        }

        protected void Numeric(ValidationResult result, string field, string value, string label)
        {
            if (!string.IsNullOrEmpty(value) && !decimal.TryParse(value, out _))
                result.AddError(field, $"{label} يجب أن يكون رقماً");
        }

        protected void Positive(ValidationResult result, string field, decimal value, string label)
        {
            if (value < 0)
                result.AddError(field, $"{label} لا يمكن أن يكون سالباً");
        }

        protected void Unique(ValidationResult result, string field, bool alreadyExists, string label)
        {
            if (alreadyExists)
                result.AddError(field, $"{label} مستخدم من قبل");
        }

        protected void Email(ValidationResult result, string field, string value, string label)
        {
            if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                result.AddError(field, $"{label} غير صحيح");
        }

        protected void Phone(ValidationResult result, string field, string value, string label)
        {
            if (!string.IsNullOrEmpty(value))
            {
                var digits = Regex.Replace(value, @"[^\d]", "");
                if (digits.Length < 7 || digits.Length > 15)
                    result.AddError(field, $"{label} غير صحيح");
            }
        }

        protected void DateValid(ValidationResult result, string field, string value, string label)
        {
            if (string.IsNullOrEmpty(value) || !DateTime.TryParse(value, out _))
                result.AddError(field, $"{label} غير صحيح");
        }

        protected void Custom(ValidationResult result, string field, bool condition, string message)
        {
            if (!condition)
                result.AddError(field, message);
        }
    }
}
