using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    public interface IBuilderReportService
    {
        Result<ReportData> Rows(string moduleKey, DateTime from, DateTime to);
    }

    /// <summary>
    /// تقرير مبنيّ: يقرأ جدول وحدةٍ أخرى بمداه ويُرجع ReportData — نفس شكل كل تقرير في النظام، فيمرّ
    /// من ReportRenderer.Run بلا مسار ثانٍ.
    /// </summary>
    public class BuilderReportService : IBuilderReportService
    {
        private readonly IBuilderRepository _repo;

        public BuilderReportService(IBuilderRepository repo) => _repo = repo;

        public Result<ReportData> Rows(string moduleKey, DateTime from, DateTime to)
        {
            var module = _repo.Modules().FirstOrDefault(m => m.Key == moduleKey);
            if (module == null || string.IsNullOrWhiteSpace(module.TableName))
                return Result.Fail<ReportData>("مصدر التقرير غير موجود", ErrorCode.NotFound);

            var columns = _repo.Columns(module.Id);
            var (rows, _) = new DynamicRepository(module.TableName, columns).GetPaged(1, int.MaxValue, null, null, false);

            return Result.Ok(new ReportData
            {
                Rows = Within(rows, columns, from, to),
                Totals = Totals(Within(rows, columns, from, to), columns)
            });
        }

        /// <summary>المدى يُطبَّق على أول عمود تاريخ في الجدول — جدولٌ بلا تاريخ يُقرأ كاملاً.</summary>
        private static List<IDictionary<string, object>> Within(List<IDictionary<string, object>> rows,
            List<BuilderColumn> columns, DateTime from, DateTime to)
        {
            var date = columns.FirstOrDefault(c => c.DataType == BuilderDataType.Date && !c.IsLine);
            if (date == null) return rows;

            return rows.Where(r => r.TryGetValue(date.Name, out var raw) && raw != null
                                && DateTime.TryParse(raw.ToString(), out var value)
                                && value.Date >= from.Date && value.Date <= to.Date).ToList();
        }

        /// <summary>الإجماليات من الأعمدة المُعلَن لها إجمالي في الوصف — لا حساب مكتوب هنا.</summary>
        private static Dictionary<string, string> Totals(List<IDictionary<string, object>> rows, List<BuilderColumn> columns) =>
            columns
                .Where(c => !c.IsLine && !string.IsNullOrWhiteSpace(c.Footer) && c.Footer != "None")
                .ToDictionary(
                    c => c.Name,
                    c => $"{c.Header}: {rows.Sum(r => r.TryGetValue(c.Name, out var v) && v != null ? Convert.ToDecimal(v) : 0m):N2}");
    }
}
