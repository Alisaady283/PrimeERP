using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Rules;
using System.Dynamic;

namespace PrimeERP.Application.Validation
{
    /// <summary>
    /// قواعد صفٍّ في جدول مبنيّ. القواعد مُعلَنة على أعمدته لا مكتوبة هنا، فتمرّ عبر .Custom — وهي
    /// المُعلَن أنها تغطّي أي قاعدة مركّبة. والنتيجة ValidationResult كأي متحقّق، فتترجمها ServiceBase.Check.
    /// </summary>
    public class BuilderRowValidator : IValidator<IDictionary<string, object>>
    {
        private readonly List<BuilderColumn> _columns;
        private readonly System.Func<string, object, bool> _exists;

        public BuilderRowValidator(List<BuilderColumn> columns, System.Func<string, object, bool> exists)
        {
            _columns = columns;
            _exists = exists;
        }

        public ValidationResult Validate(IDictionary<string, object> row) =>
            Rules.For<IDictionary<string, object>>()
                .Custom((values, result) =>
                {
                    foreach (var column in _columns.Where(c => c.Aggregate == BuilderAggregate.None && c.ShowInForm))
                    {
                        values.TryGetValue(column.Name, out var value);
                        var text = value?.ToString();

                        if (column.IsRequired && string.IsNullOrWhiteSpace(text))
                            result.AddError(column.Name, $"{column.Header} مطلوب");

                        else if (column.MaxLength is > 0 && text?.Length > column.MaxLength)
                            result.AddError(column.Name, $"{column.Header} أطول من {column.MaxLength}");

                        else if (column.IsUnique && !string.IsNullOrWhiteSpace(text) && _exists(column.Name, value))
                            result.AddError(column.Name, $"{column.Header} مكرَّر");
                    }
                })
                .Validate(row);
    }
}
