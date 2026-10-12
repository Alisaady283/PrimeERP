using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Reporting
{
    public interface IHrReportService
    {
        Result<ReportData> PayrollSummary(DateTime from, DateTime to);
        Result<ReportData> AttendanceSummary(DateTime from, DateTime to);
        Result<ReportData> Advances(DateTime from, DateTime to);
    }

    public class HrReportService : ReportServiceBase, IHrReportService
    {
        private readonly IPayrollRepository _payrolls;
        private readonly IAttendanceRepository _attendance;
        private readonly IEmployeeAdvanceRepository _advances;
        private readonly IEmployeeRepository _employees;

        public HrReportService(IPayrollRepository payrolls, IAttendanceRepository attendance, IEmployeeAdvanceRepository advances, IEmployeeRepository employees,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _payrolls = payrolls; _attendance = attendance; _advances = advances; _employees = employees;
        }

        public Result<ReportData> PayrollSummary(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var rows = _payrolls.Between(from, to).Select(p => new PayrollSummaryRow
            {
                PayrollNo = p.PayrollNo, Period = $"{p.PeriodStart:yyyy-MM-dd} — {p.PeriodEnd:yyyy-MM-dd}",
                Basic = p.TotalBasic, Allowances = p.TotalAllowances, Deductions = p.TotalDeductions, Net = p.NetTotal
            }).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Net):N2}" } });
        }

        public Result<ReportData> AttendanceSummary(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var summary = _attendance.Summary(from, to);
            var employees = _employees.GetByIds(summary.Select(s => s.EmployeeId)).ToDictionary(e => e.Id);
            var rows = summary.Select(s => new AttendanceSummaryRow
            {
                Code = employees.GetValueOrDefault(s.EmployeeId)?.Code, Name = employees.GetValueOrDefault(s.EmployeeId)?.Name,
                Present = s.Present, Absent = s.Absent, Leave = s.Leave, LateMinutes = s.LateMinutes, OvertimeMinutes = s.OvertimeMinutes
            }).OrderBy(r => r.Code).ToList();
            return Result.Ok(new ReportData { Rows = rows });
        }

        public Result<ReportData> Advances(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var rows = _advances.Between(from, to).Select(a => new AdvanceRow
            {
                Date = a.Date.ToString("yyyy-MM-dd"), Code = a.EmployeeCode, Name = a.EmployeeName, Amount = a.Amount,
                Status = LocalizationService.Get(a.PayrollId == null ? "Str.Reports.Advance.Pending" : "Str.Reports.Advance.Settled")
            }).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Amount):N2}" } });
        }
    }

    public interface IPosReportService
    {
        Result<ReportData> Cashiers(DateTime from, DateTime to);
    }

    public class PosReportService : ReportServiceBase, IPosReportService
    {
        private readonly IPosShiftRepository _shifts;

        public PosReportService(IPosShiftRepository shifts, IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit) => _shifts = shifts;

        public Result<ReportData> Cashiers(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var rows = _shifts.CashierTotals(from, to.Date.AddDays(1)).Select(c => new CashierRow
            {
                Cashier = c.Cashier, Shifts = c.Shifts, Sales = c.Sales, Returns = c.Returns, Collected = c.CashIn, Refunded = c.CashOut, Difference = c.Difference
            }).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Collected - r.Refunded):N2}" } });
        }
    }

    public interface IManufacturingReportService
    {
        Result<ReportData> ProductionCost(DateTime from, DateTime to);
        Result<ReportData> Consumption(DateTime from, DateTime to);
        Result<ReportData> WorkCenterLoad(DateTime from, DateTime to);
        Result<ReportData> Scrap(DateTime from, DateTime to);
    }

    public class ManufacturingReportService : ReportServiceBase, IManufacturingReportService
    {
        private readonly IProductionOrderRepository _orders;
        private readonly IStockMovementRepository _movements;
        private readonly IProductRepository _products;
        private readonly ILookupRepository<WorkCenter> _centers;

        public ManufacturingReportService(IProductionOrderRepository orders, IStockMovementRepository movements, IProductRepository products, ILookupRepository<WorkCenter> centers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _orders = orders; _movements = movements; _products = products; _centers = centers;
        }

        public Result<ReportData> ProductionCost(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var rows = _orders.Between(from, to, unbuild: false).Select(o => new ProductionRow
            {
                OrderNo = o.OrderNo, Date = o.OrderDate.ToString("yyyy-MM-dd"), Product = o.ProductName, Qty = o.Qty,
                Materials = o.MaterialsCost, Operations = o.OperationsCost, Total = o.TotalCost, UnitCost = InventoryCosting.UnitCostOf(o.TotalCost, o.Qty)
            }).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Total):N2}" } });
        }

        public Result<ReportData> Consumption(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var rows = _orders.LinesBetween(from, to, BomLineKind.Component)
                .GroupBy(l => new { l.ProductCode, l.ProductName })
                .Select(g => new QuantityCostRow { Code = g.Key.ProductCode, Name = g.Key.ProductName, Qty = g.Sum(l => l.Qty), Cost = g.Sum(l => l.Qty * l.UnitCost) })
                .OrderBy(r => r.Code).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Cost):N2}" } });
        }

        public Result<ReportData> WorkCenterLoad(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var lines = _orders.LinesBetween(from, to, BomLineKind.Operation);
            var names = _centers.NamesOf(lines.Select(l => l.WorkCenterId ?? 0));
            var rows = lines.GroupBy(l => l.WorkCenterId ?? 0)
                .Select(g => new QuantityCostRow { Name = names.GetValueOrDefault(g.Key), Qty = g.Sum(l => l.Qty), Cost = g.Sum(l => InventoryCosting.OperationCost(l.Qty, l.UnitCost)) })
                .OrderBy(r => r.Name).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Cost):N2}" } });
        }

        public Result<ReportData> Scrap(DateTime from, DateTime to)
        {
            var gate = Gate(); if (gate != null) return gate;
            var moves = _movements.BySource("Scrap", from, to.Date.AddDays(1));
            var products = _products.GetByIds(moves.Select(m => m.ProductId)).ToDictionary(p => p.Id);
            var rows = moves.GroupBy(m => m.ProductId)
                .Select(g => new QuantityCostRow { Code = products.GetValueOrDefault(g.Key)?.Code, Name = products.GetValueOrDefault(g.Key)?.Name, Qty = g.Sum(m => m.Qty), Cost = g.Sum(m => m.TotalCost) })
                .OrderBy(r => r.Code).ToList();
            return Result.Ok(new ReportData { Rows = rows, Totals = new() { ["Total"] = $"{LocalizationService.Get("Str.Total")}: {rows.Sum(r => r.Cost):N2}" } });
        }
    }
}
