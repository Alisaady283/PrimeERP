using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using PrimeERP.Domain.Contracts;

namespace PrimeERP.Domain.Rules
{
    public static class Rules
    {
        public static RuleSet<T> For<T>() => new();
    }

    /// <summary>بناء تحقق مرن — يستخرج اسم الحقل من Expression بدل تكراره كنص حر في كل استدعاء (بديل تكرار
    /// ValidatorBase اليدوي). .Custom تُغطّي أي قاعدة أعمال مركّبة (AccountingRules، تفرّد عبر Repository،
    /// حلقات على مجموعات) لا تُختزل في فحص خاصية واحدة — راجع JournalValidator.</summary>
    public class RuleSet<T>
    {
        private readonly System.Collections.Generic.List<Action<T, ValidationResult>> _checks = new();

        public RuleSet<T> Required(Expression<Func<T, string>> selector, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                if (string.IsNullOrWhiteSpace(get(item))) result.AddError(field, $"{label} مطلوب");
            });
        }

        public RuleSet<T> MinLength(Expression<Func<T, string>> selector, int min, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                var value = get(item);
                if (!string.IsNullOrEmpty(value) && value.Length < min) result.AddError(field, $"{label} يجب ألا يقل عن {min} حرف");
            });
        }

        public RuleSet<T> MaxLength(Expression<Func<T, string>> selector, int max, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                var value = get(item);
                if (!string.IsNullOrEmpty(value) && value.Length > max) result.AddError(field, $"{label} يجب ألا يتجاوز {max} حرف");
            });
        }

        public RuleSet<T> Range(Expression<Func<T, decimal>> selector, decimal min, decimal max, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                var value = get(item);
                if (value < min || value > max) result.AddError(field, $"{label} يجب أن يكون بين {min} و {max}");
            });
        }

        public RuleSet<T> Positive(Expression<Func<T, decimal>> selector, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                if (get(item) < 0) result.AddError(field, $"{label} لا يمكن أن يكون سالباً");
            });
        }

        public RuleSet<T> Email(Expression<Func<T, string>> selector, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                var value = get(item);
                if (!string.IsNullOrEmpty(value) && !Regex.IsMatch(value, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                    result.AddError(field, $"{label} غير صحيح");
            });
        }

        public RuleSet<T> Phone(Expression<Func<T, string>> selector, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                var value = get(item);
                if (string.IsNullOrEmpty(value)) return;
                var digits = Regex.Replace(value, @"[^\d]", "");
                if (digits.Length < 7 || digits.Length > 15) result.AddError(field, $"{label} غير صحيح");
            });
        }

        public RuleSet<T> DateValid(Expression<Func<T, string>> selector, string label)
        {
            var (field, get) = Compile(selector);
            return Add((item, result) =>
            {
                var value = get(item);
                if (string.IsNullOrEmpty(value) || !DateTime.TryParse(value, out _)) result.AddError(field, $"{label} غير صحيح");
            });
        }

        /// <summary>alreadyExists يُستدعى فقط لو باقي القاعدة تحتاجه فعلياً — استخدم && قصيرة الدارة داخل الدالة الممرَّرة لتفادي استعلام DB غير ضروري (راجع AccountValidator.Unique).</summary>
        public RuleSet<T> Unique(Expression<Func<T, object>> selector, Func<T, bool> alreadyExists, string label)
        {
            var (field, _) = Compile(selector);
            return Add((item, result) =>
            {
                if (alreadyExists(item)) result.AddError(field, $"{label} مستخدم من قبل");
            });
        }

        public RuleSet<T> Custom(Action<T, ValidationResult> check) => Add(check);

        public ValidationResult Validate(T item)
        {
            var result = new ValidationResult();
            foreach (var check in _checks) check(item, result);
            return result;
        }

        private RuleSet<T> Add(Action<T, ValidationResult> check)
        {
            _checks.Add(check);
            return this;
        }

        private static (string Field, Func<T, TProp> Get) Compile<TProp>(Expression<Func<T, TProp>> selector)
        {
            var body = selector.Body;
            if (body is UnaryExpression unary) body = unary.Operand;

            var member = body as MemberExpression
                ?? throw new ArgumentException("Selector must be a simple property access", nameof(selector));

            return (member.Member.Name, selector.Compile());
        }
    }
}
