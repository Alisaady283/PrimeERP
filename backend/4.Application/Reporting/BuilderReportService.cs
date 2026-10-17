using PrimeERP.Platform.Settings;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Audit;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;

namespace PrimeERP.Application.Reporting
{
    /// <summary>تقرير مبنيّ</summary>
    public interface IBuilderReportService
    {
        Result<ReportData> Rows(string moduleKey, DateTime from, DateTime to);
    }

    public class BuilderReportService : ReportServiceBase, IBuilderReportService
    {
        private readonly IBuilderRepository _repo;

        public BuilderReportService(IBuilderRepository repo, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _repo = repo;

        public Result<ReportData> Rows(string moduleKey, DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;

            var module = _repo.Modules().FirstOrDefault(m => m.Key == moduleKey);
            if (module == null || string.IsNullOrWhiteSpace(module.TableName))
                return Result.Fail<ReportData>(Msg("SourceMissing"), ErrorCode.NotFound);

            var columns = _repo.Columns(module.Id);
            var (rows, _) = new DynamicRepository(module.TableName, columns).GetPaged(1, int.MaxValue, null, null, false);

            return Result.Ok(new ReportData
            {
                Rows = Within(rows, columns, from, to),
                Totals = Totals(Within(rows, columns, from, to), columns)
            });
        }

        private static List<IDictionary<string, object>> Within(List<IDictionary<string, object>> rows,
            List<BuilderColumn> columns, DateTime from, DateTime to)
        {
            var date = columns.FirstOrDefault(c => c.DataType == BuilderDataType.Date && !c.IsLine);
            if (date == null) return rows;

            return rows.Where(r => r.TryGetValue(date.Name, out var raw) && raw != null
                                && DateTime.TryParse(raw.ToString(), out var value)
                                && value.Date >= from.Date && value.Date <= to.Date).ToList();
        }

        private static Dictionary<string, string> Totals(List<IDictionary<string, object>> rows, List<BuilderColumn> columns) =>
            columns
                .Where(c => !c.IsLine && !string.IsNullOrWhiteSpace(c.Footer) && c.Footer != "None")
                .ToDictionary(
                    c => c.Name,
                    c => $"{c.Header}: {rows.Sum(r => r.TryGetValue(c.Name, out var v) && v != null ? Convert.ToDecimal(v) : 0m):N2}");
    }
}
