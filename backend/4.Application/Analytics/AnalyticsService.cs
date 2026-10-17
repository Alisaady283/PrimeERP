using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.Reporting;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Analytics
{
    public interface IAnalyticsService
    {
        Result<AnalyticsData> Executive(DateTime from, DateTime to, AnalyticsFilter filter = null);
        Result<AnalyticsData> Sales(DateTime from, DateTime to, AnalyticsFilter filter = null);
        Result<AnalyticsData> Purchases(DateTime from, DateTime to, AnalyticsFilter filter = null);
        Result<AnalyticsData> Inventory(DateTime from, DateTime to, AnalyticsFilter filter = null);
        Result<AnalyticsData> Liquidity(DateTime from, DateTime to, AnalyticsFilter filter = null);
        Result<AnalyticsData> People(DateTime from, DateTime to, AnalyticsFilter filter = null);
        Result<AnalyticsData> Production(DateTime from, DateTime to, AnalyticsFilter filter = null);
    }

    public class AnalyticsService : ReportServiceBase, IAnalyticsService
    {
        private const int Top = 10;
        private const int Slices = 5;
        private const int HorizonDays = 30;
        private const int Weeks = 8;

        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _sales;
        private readonly IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> _purchases;
        private readonly IReturnRepository<SalesReturn, SalesReturnLine> _salesReturns;
        private readonly IReturnRepository<PurchaseReturn, PurchaseReturnLine> _purchaseReturns;
        private readonly IQuotationRepository _quotations;
        private readonly IPurchaseOrderRepository _purchaseOrders;
        private readonly IDocumentLinkRepository _links;
        private readonly IPartyRepository<Customer> _customers;
        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly IProductRepository _products;
        private readonly ICategoryRepository _categories;
        private readonly ILookupRepository<Warehouse> _warehouses;
        private readonly IStockMovementRepository _movements;
        private readonly IJournalRepository _journal;
        private readonly IAccountRepository _accounts;
        private readonly ITreasuryRepository _treasuries;
        private readonly IEmployeeRepository _employees;
        private readonly IPayrollRepository _payrolls;
        private readonly IAttendanceRepository _attendance;
        private readonly IEmployeeAdvanceRepository _advances;
        private readonly ILookupRepository<Department> _departments;
        private readonly IProductionOrderRepository _orders;
        private readonly ILookupRepository<WorkCenter> _centers;

        public AnalyticsService(IInvoiceRepository<SalesInvoice, SalesInvoiceLine> sales, IInvoiceRepository<PurchaseInvoice, PurchaseInvoiceLine> purchases,
            IReturnRepository<SalesReturn, SalesReturnLine> salesReturns, IReturnRepository<PurchaseReturn, PurchaseReturnLine> purchaseReturns,
            IQuotationRepository quotations, IPurchaseOrderRepository purchaseOrders, IDocumentLinkRepository links,
            IPartyRepository<Customer> customers, IPartyRepository<Supplier> suppliers, IProductRepository products, ICategoryRepository categories,
            ILookupRepository<Warehouse> warehouses, IStockMovementRepository movements, IJournalRepository journal, IAccountRepository accounts,
            ITreasuryRepository treasuries, IEmployeeRepository employees, IPayrollRepository payrolls, IAttendanceRepository attendance,
            IEmployeeAdvanceRepository advances, ILookupRepository<Department> departments, IProductionOrderRepository orders, ILookupRepository<WorkCenter> centers,
            IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit)
            : base(permissions, settings, localization, audit)
        {
            _sales = sales; _purchases = purchases; _salesReturns = salesReturns; _purchaseReturns = purchaseReturns;
            _quotations = quotations; _purchaseOrders = purchaseOrders; _links = links;
            _customers = customers; _suppliers = suppliers; _products = products; _categories = categories;
            _warehouses = warehouses; _movements = movements; _journal = journal; _accounts = accounts; _treasuries = treasuries;
            _employees = employees; _payrolls = payrolls; _attendance = attendance; _advances = advances; _departments = departments;
            _orders = orders; _centers = centers;
        }

        public Result<AnalyticsData> Executive(DateTime from, DateTime to, AnalyticsFilter filter = null) => Filtered(PermissionKeys.Analytics.Executive, from, to, filter, (f, t, rest) => ExecutiveOf(f, t, rest));

        private Result<AnalyticsData> ExecutiveOf(DateTime from, DateTime to, AnalyticsFilter filter)
        {
            var months = AnalyticsCalc.Months(from, to);
            var days = AnalyticsCalc.Days(from, to);
            var (prevFrom, prevTo) = AnalyticsCalc.PreviousPeriod(from, to);

            var sales = _sales.Monthly(from, to);
            var purchases = _purchases.Monthly(from, to);
            var revenue = sales.Sum(m => m.Value) - _salesReturns.Total(from, to);
            var previousRevenue = _sales.Monthly(prevFrom, prevTo).Sum(m => m.Value) - _salesReturns.Total(prevFrom, prevTo);

            var cogsCode = Setting(SettingKeys.Accounts.COGS, "");
            var cogs = string.IsNullOrWhiteSpace(cogsCode) ? new List<(string Month, decimal Net)>() : _journal.MonthlyNet(cogsCode, from, to);
            var cogsTotal = cogs.Sum(c => c.Net);
            var grossProfit = StatementCalc.GrossProfit(revenue, cogsTotal);

            var receivables = _sales.Open().Sum(o => o.Remaining);
            var payables = _purchases.Open().Sum(o => o.Remaining);
            var stockValue = _movements.Valuation(to).Sum(v => v.Value);
            var cash = CashBalances().Sum(t => t.Balance);

            var dso = AnalyticsCalc.DaysOf(receivables, sales.Sum(m => m.Value), days);
            var dpo = AnalyticsCalc.DaysOf(payables, purchases.Sum(m => m.Value), days);
            var dio = AnalyticsCalc.DaysOf(stockValue, cogsTotal, days);

            var monthlySales = ByMonth(months, sales.Select(m => (m.Year, m.Month, m.Value)));
            var monthlyCogs = ByMonth(months, cogs.Select(c => (int.Parse(c.Month[..4]), int.Parse(c.Month[5..7]), c.Net)));

            return Result.Ok(new AnalyticsData
            {
                Cards =
                {
                    ["Revenue"] = new Kpi(revenue, AnalyticsCalc.Growth(revenue, previousRevenue)),
                    ["GrossProfit"] = new Kpi(grossProfit, null, Text("MarginNote", AnalyticsCalc.Share(grossProfit, revenue))),
                    ["Cash"] = new Kpi(cash),
                    ["Receivables"] = new Kpi(receivables, null, Text("DaysNote", dso)),
                    ["Payables"] = new Kpi(payables, null, Text("DaysNote", dpo)),
                    ["StockValue"] = new Kpi(stockValue, null, Text("DaysNote", dio)),
                },
                Charts =
                {
                    ["SalesPurchases"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series =
                        {
                            new(Text("Sales"), monthlySales),
                            new(Text("Purchases"), ByMonth(months, purchases.Select(m => (m.Year, m.Month, m.Value)))),
                        }
                    },
                    ["Margin"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series = { new(Text("MarginPercent"), monthlySales.Select((s, i) => AnalyticsCalc.Share(s - monthlyCogs[i], s)).ToList()) }
                    },
                    ["CashCycle"] = new ChartData
                    {
                        Labels = { "DSO", "DIO", "DPO", Text("CashCycle") },
                        Series = { new(Text("Days"), new List<decimal> { dso, dio, dpo, AnalyticsCalc.CashCycle(dso, dio, dpo) }) }
                    },
                    ["Expenses"] = ExpenseMix(from, to, cogsCode, filter?.Dimension == AnalyticsFilter.Account ? filter.Key : null),
                }
            });
        }

        public Result<AnalyticsData> Sales(DateTime from, DateTime to, AnalyticsFilter filter = null) =>
            Filtered(PermissionKeys.Analytics.Sales, from, to, filter, (f, t, rest) => Trade(_sales, _salesReturns, ids => _customers.NamesOf(ids), f, t, _quotations, "Quotation", "SalesOrder", rest));

        public Result<AnalyticsData> Purchases(DateTime from, DateTime to, AnalyticsFilter filter = null) =>
            Filtered(PermissionKeys.Analytics.Purchases, from, to, filter, (f, t, rest) => Trade(_purchases, _purchaseReturns, ids => _suppliers.NamesOf(ids), f, t, _purchaseOrders, "PurchaseOrder", "StockIn", rest));

        /// <summary>
        /// مدخل كل لوحة: تصفية الشهر تضيّق الفترة إلى شهره، وما بقي من تصفيةٍ يمرّ إلى بنائها؛
        /// ثم رسوم الأشهر تحمل مفاتيحها (yyyy-MM) فيُنقر عليها للتصفية.
        /// </summary>
        private Result<AnalyticsData> Filtered(string permission, DateTime from, DateTime to, AnalyticsFilter filter, Func<DateTime, DateTime, AnalyticsFilter, Result<AnalyticsData>> build)
        {
            if (!Permissions.Can(permission)) return FailDenied<AnalyticsData>();
            if (filter?.Dimension == AnalyticsFilter.Month
                && DateTime.TryParseExact($"{filter.Key}-01", "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var month))
            {
                from = month > from ? month : from;
                var end = month.AddMonths(1).AddDays(-1);
                to = end < to ? end : to;
                filter = null;
            }

            var result = build(from, to, filter);
            if (!result.IsSuccess) return result;

            var months = AnalyticsCalc.Months(from, to);
            var labels = MonthLabels(months);
            foreach (var chart in result.Value.Charts.Values.Where(c => c.Keys == null && c.Labels.SequenceEqual(labels)))
            {
                chart.Dimension = AnalyticsFilter.Month;
                chart.Keys = months.Select(m => $"{m.Year:0000}-{m.Month:00}").ToList();
            }
            return result;
        }

        /// <summary>نطاق البيع والشراء من التصفية: الطرف، أو الصنف، أو أصناف التصنيف</summary>
        private TradeScope ScopeOf(AnalyticsFilter filter) =>
            filter == null || !int.TryParse(filter.Key, out var id) ? null
            : filter.Dimension switch
            {
                AnalyticsFilter.Party => new TradeScope(PartyId: id),
                AnalyticsFilter.Product => new TradeScope(ProductIds: new[] { id }),
                AnalyticsFilter.Category => new TradeScope(ProductIds: _products.IdsInCategory(id)),
                _ => null
            };

        private Result<AnalyticsData> Trade<TInvoice, TLine, TReturn, TReturnLine>(IInvoiceRepository<TInvoice, TLine> invoices,
            IReturnRepository<TReturn, TReturnLine> returns, Func<IEnumerable<int>, Dictionary<int, string>> partyNames,
            DateTime from, DateTime to, IFlowLines flowSource, string flowKind, string flowNext, AnalyticsFilter filter)
        {
            var scope = ScopeOf(filter);
            var months = AnalyticsCalc.Months(from, to);
            var (prevFrom, prevTo) = AnalyticsCalc.PreviousPeriod(from, to);
            var (yearFrom, yearTo) = AnalyticsCalc.LastYear(from, to);

            var monthly = invoices.Monthly(from, to, scope);
            var previous = invoices.Monthly(prevFrom, prevTo, scope);
            var lastYear = invoices.Monthly(yearFrom, yearTo, scope).Select(m => (m.Year + 1, m.Month, m.Value));
            var gross = monthly.Sum(m => m.Value);
            var count = monthly.Sum(m => m.Count);
            var previousCount = previous.Sum(m => m.Count);
            var returned = returns.Total(from, to, scope);
            var net = gross - returned;
            var previousNet = previous.Sum(m => m.Value) - returns.Total(prevFrom, prevTo, scope);

            var parties = invoices.ByParty(from, to, scope);
            var products = invoices.ByProduct(from, to, scope);
            var open = invoices.Open(scope);
            var outstanding = open.Sum(o => o.Remaining);
            var overdue = open.Where(o => AnalyticsCalc.Aging(o.DueDate, o.Date, to) != AgingBucket.NotDue).Sum(o => o.Remaining);

            var dealings = invoices.FirstLast(to, scope);
            var states = dealings.Select(p => AnalyticsCalc.Recency(p.Last, to)).ToList();
            var settled = invoices.Settled(from, to, scope);
            var lostInPeriod = dealings.Select(p => AnalyticsCalc.LostOn(p.Last)).Where(d => d >= from.Date && d <= to.Date).ToList();

            var flowLines = flowSource.FlowLines(from, to);
            var flowed = _links.PulledInto(flowKind, flowLines.Select(l => l.LineId), flowNext).Values.Sum();

            var topParties = parties.OrderByDescending(p => p.Value).Take(Top).ToList();
            var partyNameOf = partyNames(topParties.Select(p => p.PartyId));
            var topProducts = products.OrderByDescending(p => p.Value).Take(Top).ToList();
            var productRows = _products.GetByIds(products.Select(p => p.ProductId)).ToDictionary(p => p.Id);

            return Result.Ok(new AnalyticsData
            {
                Cards =
                {
                    ["Net"] = new Kpi(net, AnalyticsCalc.Growth(net, previousNet)),
                    ["Count"] = new Kpi(count, AnalyticsCalc.Growth(count, previousCount)),
                    ["Average"] = new Kpi(count == 0 ? 0 : Math.Round(gross / count, 2)),
                    ["ReturnRate"] = new Kpi(AnalyticsCalc.Share(returned, gross)),
                    ["Outstanding"] = new Kpi(outstanding, null, Text("OverdueNote", AnalyticsCalc.Share(overdue, outstanding))),
                    ["Flow"] = new Kpi(AnalyticsCalc.Share(flowed, flowLines.Sum(l => l.Qty))),
                    ["NewParties"] = new Kpi(dealings.Count(p => p.First >= from.Date && p.First <= to)),
                    ["ActiveParties"] = new Kpi(states.Count(s => s == PartyState.Active)),
                    ["LostParties"] = new Kpi(states.Count(s => s == PartyState.Lost)),
                },
                Charts =
                {
                    ["Trend"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series =
                        {
                            new(Text("CurrentPeriod"), ByMonth(months, monthly.Select(m => (m.Year, m.Month, m.Value)))),
                            new(Text("LastYear"), ByMonth(months, lastYear), Comparison: true),
                        }
                    },
                    ["TopParties"] = Keyed(Bars(topParties.Select(p => (partyNameOf.GetValueOrDefault(p.PartyId) ?? "—", p.Value)), Text("Value")), AnalyticsFilter.Party, topParties.Select(p => p.PartyId.ToString())),
                    ["TopProducts"] = Keyed(Bars(topProducts.Select(p => (productRows.GetValueOrDefault(p.ProductId)?.Name ?? "—", p.Value)), Text("Value")), AnalyticsFilter.Product, topProducts.Select(p => p.ProductId.ToString())),
                    ["ByCategory"] = CategoryDonut(products.Select(p => (p.ProductId, p.Value)), productRows),
                    ["Aging"] = Aging(open, to, Text("Outstanding")),
                    ["PartyFlow"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series =
                        {
                            new(Text("NewParties"), ByMonth(months, dealings.Where(p => p.First >= from.Date && p.First <= to).Select(p => (p.First.Year, p.First.Month, 1m)))),
                            new(Text("LostParties"), ByMonth(months, lostInPeriod.Select(d => (d.Year, d.Month, 1m)))),
                        }
                    },
                    ["PartySegments"] = Donut(Enum.GetValues<PartyState>().Select(s => (Text($"PartyState.{s}"), (decimal)states.Count(x => x == s)))),
                    ["CollectionRate"] = new ChartData { Labels = { Text("Collected") }, Series = { new(Text("Collected"), new List<decimal> { AnalyticsCalc.Share(settled.Paid, settled.Net) }) } },
                }
            });
        }

        public Result<AnalyticsData> Inventory(DateTime from, DateTime to, AnalyticsFilter filter = null) => Filtered(PermissionKeys.Analytics.Inventory, from, to, filter, (f, t, rest) => InventoryOf(f, t, rest));

        private Result<AnalyticsData> InventoryOf(DateTime from, DateTime to, AnalyticsFilter filter)
        {
            // المخزن والصنف والتصنيف تضيّق التقييم فتُحسب اللوحة كلها منه
            var id = int.TryParse(filter?.Key, out var key) ? key : (int?)null;
            var inCategory = filter?.Dimension == AnalyticsFilter.Category && id != null ? _products.IdsInCategory(id.Value).ToHashSet() : null;
            var valuation = _movements.Valuation(to).Where(v => v.Qty != 0)
                .Where(v => id == null || filter.Dimension switch
                {
                    AnalyticsFilter.Warehouse => v.WarehouseId == id,
                    AnalyticsFilter.Product => v.ProductId == id,
                    AnalyticsFilter.Category => inCategory.Contains(v.ProductId),
                    _ => true
                }).ToList();
            var perProduct = valuation.GroupBy(v => v.ProductId).Select(g => (ProductId: g.Key, Qty: g.Sum(v => v.Qty), Value: g.Sum(v => v.Value))).ToList();
            var stockValue = perProduct.Sum(p => p.Value);
            var productRows = _products.GetByIds(perProduct.Select(p => p.ProductId)).ToDictionary(p => p.Id);

            var issued = Merge(_movements.CostByProduct("SalesInvoice", from, to), _movements.CostByProduct("StockOut", from, to), (a, b) => a + b);
            var lastIssue = Merge(_movements.LastMovement("SalesInvoice"), _movements.LastMovement("StockOut"), (a, b) => a > b ? a : b);
            var dead = perProduct.Where(p => AnalyticsCalc.IsDead(p.Qty, lastIssue.TryGetValue(p.ProductId, out var d) ? d : null, to)).ToList();
            var speeds = perProduct.GroupBy(p => AnalyticsCalc.Speed(AnalyticsCalc.Turnover(issued.GetValueOrDefault(p.ProductId), p.Value)))
                                   .ToDictionary(g => g.Key, g => g.Count());
            var abc = AnalyticsCalc.Abc(perProduct.Select(p => (p.ProductId, p.Value)));
            var cogsCode = Setting(SettingKeys.Accounts.COGS, "");
            var cogs = string.IsNullOrWhiteSpace(cogsCode) ? 0m : _journal.MonthlyNet(cogsCode, from, to).Sum(c => c.Net);

            var alertDays = Setting(SettingKeys.Edition.ExpiryAlertDays, 90);
            var expiring = _movements.AllBatches().Count(b => b.Balance > 0 && InventoryCosting.ExpiryState(b.Expiry, to, alertDays) != "Valid");

            var warehouses = valuation.GroupBy(v => v.WarehouseId).Select(g => (Id: g.Key, Value: g.Sum(v => v.Value))).OrderByDescending(w => w.Value).ToList();
            var warehouseNames = _warehouses.NamesOf(warehouses.Select(w => w.Id));
            var classes = Enum.GetValues<AbcClass>();
            var deadTop = dead.OrderByDescending(p => p.Value).Take(Top).ToList();

            return Result.Ok(new AnalyticsData
            {
                Cards =
                {
                    ["StockValue"] = new Kpi(stockValue),
                    ["Items"] = new Kpi(perProduct.Count(p => p.Qty > 0)),
                    ["DeadItems"] = new Kpi(dead.Count, null, Text("ValueNote", dead.Sum(p => p.Value))),
                    ["Dio"] = new Kpi(AnalyticsCalc.DaysOf(stockValue, cogs, AnalyticsCalc.Days(from, to))),
                    ["Expiring"] = new Kpi(expiring),
                },
                Charts =
                {
                    ["ByWarehouse"] = Keyed(Bars(warehouses.Select(w => (warehouseNames.GetValueOrDefault(w.Id) ?? "—", w.Value)), Text("Value")), AnalyticsFilter.Warehouse, warehouses.Select(w => w.Id.ToString())),
                    ["ByCategory"] = CategoryDonut(perProduct.Select(p => (p.ProductId, p.Value)), productRows),
                    ["Abc"] = new ChartData
                    {
                        Labels = classes.Select(c => c.ToString()).ToList(),
                        Series =
                        {
                            new(Text("ItemsShare"), classes.Select(c => AnalyticsCalc.Share(abc.Count(x => x.Value == c), abc.Count)).ToList()),
                            new(Text("ValueShare"), classes.Select(c => AnalyticsCalc.Share(perProduct.Where(p => abc.GetValueOrDefault(p.ProductId) == c && abc.ContainsKey(p.ProductId)).Sum(p => p.Value), stockValue)).ToList()),
                        }
                    },
                    ["Speed"] = new ChartData
                    {
                        Labels = Enum.GetValues<MovementSpeed>().Select(s => Text($"Speed.{s}")).ToList(),
                        Series = { new(Text("Items"), Enum.GetValues<MovementSpeed>().Select(s => (decimal)speeds.GetValueOrDefault(s)).ToList()) }
                    },
                    ["DeadStock"] = Keyed(Bars(deadTop.Select(p => (productRows.GetValueOrDefault(p.ProductId)?.Name ?? "—", p.Value)), Text("Value")), AnalyticsFilter.Product, deadTop.Select(p => p.ProductId.ToString())),
                }
            });
        }

        public Result<AnalyticsData> Liquidity(DateTime from, DateTime to, AnalyticsFilter filter = null) => Filtered(PermissionKeys.Analytics.Liquidity, from, to, filter, (f, t, rest) => LiquidityOf(f, t, rest));

        private Result<AnalyticsData> LiquidityOf(DateTime from, DateTime to, AnalyticsFilter filter)
        {
            // الخزينة تضيّق النقد وحده
            var treasuries = CashBalances().OrderByDescending(t => t.Balance).ToList();
            var cash = treasuries.Where(t => filter?.Dimension != AnalyticsFilter.Treasury || t.Id.ToString() == filter.Key).Sum(t => t.Balance);
            var receivable = _sales.Open();
            var payable = _purchases.Open();
            var horizon = to.Date.AddDays(HorizonDays);

            decimal DueBy(IEnumerable<(int PartyId, DateTime Date, DateTime? DueDate, decimal Remaining)> open, DateTime limit) =>
                open.Where(o => (o.DueDate ?? o.Date).Date <= limit).Sum(o => o.Remaining);

            var inflows = DueBy(receivable, horizon);
            var outflows = DueBy(payable, horizon);

            List<decimal> Weekly(IEnumerable<(int PartyId, DateTime Date, DateTime? DueDate, decimal Remaining)> open) =>
                Enumerable.Range(0, Weeks).Select(w => open.Where(o =>
                {
                    var due = (o.DueDate ?? o.Date).Date;
                    return (w == 0 || due > to.Date.AddDays(7 * w)) && due <= to.Date.AddDays(7 * (w + 1));
                }).Sum(o => o.Remaining)).ToList();

            var buckets = Enum.GetValues<AgingBucket>();

            return Result.Ok(new AnalyticsData
            {
                Cards =
                {
                    ["Cash"] = new Kpi(cash),
                    ["Inflows"] = new Kpi(inflows),
                    ["Outflows"] = new Kpi(outflows),
                    ["Gap"] = new Kpi(cash + inflows - outflows),
                    ["Coverage"] = new Kpi(AnalyticsCalc.Share(cash + inflows, outflows)),
                },
                Charts =
                {
                    ["ByTreasury"] = Keyed(Bars(treasuries.Select(t => (t.Name, t.Balance)), Text("Balance")), AnalyticsFilter.Treasury, treasuries.Select(t => t.Id.ToString())),
                    ["Weekly"] = new ChartData
                    {
                        Labels = Enumerable.Range(1, Weeks).Select(w => Text("Week", w)).ToList(),
                        Series = { new(Text("Inflows"), Weekly(receivable)), new(Text("Outflows"), Weekly(payable)) }
                    },
                    ["Aging"] = new ChartData
                    {
                        Labels = buckets.Select(b => Text($"Aging.{b}")).ToList(),
                        Series =
                        {
                            new(Text("Receivables"), buckets.Select(b => receivable.Where(o => AnalyticsCalc.Aging(o.DueDate, o.Date, to) == b).Sum(o => o.Remaining)).ToList()),
                            new(Text("Payables"), buckets.Select(b => payable.Where(o => AnalyticsCalc.Aging(o.DueDate, o.Date, to) == b).Sum(o => o.Remaining)).ToList()),
                        }
                    },
                }
            });
        }

        public Result<AnalyticsData> People(DateTime from, DateTime to, AnalyticsFilter filter = null) => Filtered(PermissionKeys.Analytics.People, from, to, filter, (f, t, rest) => PeopleOf(f, t, rest));

        private Result<AnalyticsData> PeopleOf(DateTime from, DateTime to, AnalyticsFilter filter)
        {
            var months = AnalyticsCalc.Months(from, to);
            var (prevFrom, prevTo) = AnalyticsCalc.PreviousPeriod(from, to);
            // القسم يضيّق الموظفين وما يتبعهم من رواتب وحضور وسُلف
            var all = _employees.GetAll(activeOnly: false);
            var department = filter?.Dimension == AnalyticsFilter.Department && int.TryParse(filter.Key, out var d) ? d : (int?)null;
            var employees = department == null ? all : all.Where(e => (e.DepartmentId ?? 0) == department).ToList();
            var ids = department == null ? null : employees.Select(e => e.Id).ToHashSet();
            var active = employees.Where(e => e.Status != EmployeeStatus.Inactive && e.HireDate <= to && (e.TerminationDate == null || e.TerminationDate > to)).ToList();

            var payroll = _payrolls.Monthly(from, to, ids);
            var gross = payroll.Sum(p => p.Gross);
            var previousGross = _payrolls.Monthly(prevFrom, prevTo, ids).Sum(p => p.Gross);
            var departmentOf = employees.ToDictionary(e => e.Id, e => e.DepartmentId ?? 0);
            var headcount = active.GroupBy(e => e.DepartmentId ?? 0).OrderByDescending(g => g.Count()).Select(g => (Id: g.Key, Count: (decimal)g.Count())).ToList();
            var cost = _payrolls.CostByEmployee(from, to).Where(c => departmentOf.ContainsKey(c.EmployeeId))
                                .GroupBy(c => departmentOf[c.EmployeeId]).Select(g => (Id: g.Key, Gross: g.Sum(c => c.Gross))).ToList();
            var departmentNames = _departments.NamesOf(employees.Select(e => e.DepartmentId ?? 0));
            string Department(int id) => departmentNames.GetValueOrDefault(id) ?? Text("NoDepartment");

            var attendance = _attendance.Summary(from, to).Where(a => ids == null || ids.Contains(a.EmployeeId)).ToList();
            var present = attendance.Sum(a => a.Present);
            var absent = attendance.Sum(a => a.Absent);
            var leave = attendance.Sum(a => a.Leave);
            var pendingAdvances = _advances.Between(from, to).Where(a => a.PayrollId == null && (ids == null || ids.Contains(a.EmployeeId))).Sum(a => a.Amount);

            return Result.Ok(new AnalyticsData
            {
                Cards =
                {
                    ["Headcount"] = new Kpi(active.Count),
                    ["PayrollCost"] = new Kpi(gross, AnalyticsCalc.Growth(gross, previousGross)),
                    ["AverageSalary"] = new Kpi(active.Count == 0 ? 0 : Math.Round(active.Average(e => e.BasicSalary), 2)),
                    ["AbsenceRate"] = new Kpi(AnalyticsCalc.Share(absent, present + absent + leave)),
                    ["OvertimeHours"] = new Kpi(Math.Round(attendance.Sum(a => a.OvertimeMinutes) / 60m, 1)),
                    ["PendingAdvances"] = new Kpi(pendingAdvances),
                },
                Charts =
                {
                    ["PayrollTrend"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series =
                        {
                            new(Text("Gross"), ByMonth(months, payroll.Select(p => (p.Year, p.Month, p.Gross)))),
                            new(Text("NetPay"), ByMonth(months, payroll.Select(p => (p.Year, p.Month, p.Net)))),
                        }
                    },
                    ["HeadcountByDepartment"] = Keyed(Bars(headcount.Select(g => (Department(g.Id), g.Count)), Text("Employees")), AnalyticsFilter.Department, headcount.Select(g => g.Id.ToString())),
                    ["CostByDepartment"] = Keyed(Donut(cost.Select(g => (Department(g.Id), g.Gross))), AnalyticsFilter.Department, cost.Select(g => g.Id.ToString())),
                    ["Attendance"] = Donut(new[] { (Text("Present"), (decimal)present), (Text("Absent"), (decimal)absent), (Text("Leave"), (decimal)leave) }),
                    ["HiresLeavers"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series =
                        {
                            new(Text("Hires"), ByMonth(months, employees.Where(e => e.HireDate >= from && e.HireDate <= to).Select(e => (e.HireDate.Year, e.HireDate.Month, 1m)))),
                            new(Text("Leavers"), ByMonth(months, employees.Where(e => e.TerminationDate >= from && e.TerminationDate <= to).Select(e => (e.TerminationDate.Value.Year, e.TerminationDate.Value.Month, 1m)))),
                        }
                    },
                }
            });
        }

        public Result<AnalyticsData> Production(DateTime from, DateTime to, AnalyticsFilter filter = null) => Filtered(PermissionKeys.Analytics.Production, from, to, filter, (f, t, _) => ProductionOf(f, t));

        private Result<AnalyticsData> ProductionOf(DateTime from, DateTime to)
        {
            var months = AnalyticsCalc.Months(from, to);
            var (prevFrom, prevTo) = AnalyticsCalc.PreviousPeriod(from, to);
            var monthly = _orders.Monthly(from, to);
            var qty = monthly.Sum(m => m.Qty);
            var materials = monthly.Sum(m => m.Materials);
            var operations = monthly.Sum(m => m.Operations);
            var total = materials + operations;
            var previous = _orders.Monthly(prevFrom, prevTo).Sum(m => m.Materials + m.Operations);
            var scrap = _movements.CostByProduct("Scrap", from, to).Values.Sum();
            var load = _orders.LoadByCenter(from, to);
            var centerNames = _centers.NamesOf(load.Select(l => l.WorkCenterId));

            return Result.Ok(new AnalyticsData
            {
                Cards =
                {
                    ["Produced"] = new Kpi(qty),
                    ["ProductionCost"] = new Kpi(total, AnalyticsCalc.Growth(total, previous)),
                    ["UnitCost"] = new Kpi(Math.Round(InventoryCosting.UnitCostOf(total, qty), 2)),
                    ["Orders"] = new Kpi(monthly.Sum(m => m.Count)),
                    ["ScrapRate"] = new Kpi(AnalyticsCalc.Share(scrap, materials + scrap)),
                    ["WorkHours"] = new Kpi(Math.Round(load.Sum(l => l.Minutes) / 60m, 1)),
                },
                Charts =
                {
                    ["CostTrend"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series =
                        {
                            new(Text("Materials"), ByMonth(months, monthly.Select(m => (m.Year, m.Month, m.Materials)))),
                            new(Text("Operations"), ByMonth(months, monthly.Select(m => (m.Year, m.Month, m.Operations)))),
                        }
                    },
                    ["TopProducts"] = Bars(_orders.ByProduct(from, to).OrderByDescending(p => p.Cost).Take(Top).Select(p => (p.Name ?? "—", p.Cost)), Text("Value")),
                    ["CenterLoad"] = Bars(load.OrderByDescending(l => l.Minutes).Select(l => (centerNames.GetValueOrDefault(l.WorkCenterId) ?? "—", Math.Round(l.Minutes / 60m, 1))), Text("Hours")),
                    ["CostMix"] = Donut(new[] { (Text("Materials"), materials), (Text("Operations"), operations), (Text("Scrap"), scrap) }),
                    ["ScrapTrend"] = new ChartData
                    {
                        Labels = MonthLabels(months),
                        Series = { new(Text("Scrap"), ByMonth(months, _movements.MonthlyCost("Scrap", from, to).Select(m => (m.Year, m.Month, m.Cost)))) }
                    },
                }
            });
        }

        private List<(int Id, string Name, decimal Balance)> CashBalances()
        {
            var treasuries = _treasuries.GetAll();
            var balances = _accounts.GetByCodes(treasuries.Select(t => t.AccountCode).Where(c => !string.IsNullOrWhiteSpace(c)))
                                    .ToDictionary(a => a.Code, a => a.Balance);
            return treasuries.Select(t => (t.Id, t.Name, balances.GetValueOrDefault(t.AccountCode ?? ""))).ToList();
        }

        /// <summary>مزيج المصروفات بمجموعات الحساب «5» أو بفروع الحساب المنقور (account)، وكل شريحةٍ مفتاحها رمزها</summary>
        private ChartData ExpenseMix(DateTime from, DateTime to, string cogsCode, string account)
        {
            var sums = _journal.GetAccountSums(from, to, postedOnly: true);
            var children = _accounts.GetChildren(account ?? "5").Where(a => a.Code != cogsCode).ToList();
            var groups = children.Count > 0 || account == null ? children : _accounts.GetByCodes(new[] { account }).ToList();
            var chart = Donut(groups.Select(g => (g.Name, sums.Where(s => s.AccountCode.StartsWith(g.Code) && !(cogsCode != "" && s.AccountCode.StartsWith(cogsCode)))
                                                          .Sum(s => s.SumDebit - s.SumCredit))));
            return Keyed(chart, AnalyticsFilter.Account, groups.Select(g => g.Code));
        }

        private static ChartData Keyed(ChartData chart, string dimension, IEnumerable<string> keys)
        {
            chart.Dimension = dimension;
            chart.Keys = keys.ToList();
            return chart;
        }

        /// <summary>الدائري بالتصنيفات ومفاتيحها — «أخرى» و«بلا تصنيف» بلا مفتاح فلا يُصفّى بهما</summary>
        private ChartData CategoryDonut(IEnumerable<(int ProductId, decimal Value)> values, IReadOnlyDictionary<int, Product> products)
        {
            var groups = values.GroupBy(v => products.GetValueOrDefault(v.ProductId)?.CategoryId)
                               .Select(g => (Id: g.Key, Value: g.Sum(v => v.Value)))
                               .Where(g => g.Value > 0).OrderByDescending(g => g.Value).ToList();
            var names = _categories.GetAll("Products", includeInactive: true).ToDictionary(c => c.Id, c => c.Name);
            var shown = groups.Take(Slices).ToList();
            var rest = groups.Skip(Slices).Sum(g => g.Value);
            var chart = Bars(shown.Select(g => (g.Id is int id ? names.GetValueOrDefault(id) ?? Text("Uncategorized") : Text("Uncategorized"), g.Value))
                                  .Concat(rest > 0 ? new[] { (Text("Other"), rest) } : Array.Empty<(string, decimal)>()), Text("Value"));
            return Keyed(chart, AnalyticsFilter.Category, shown.Select(g => g.Id?.ToString()).Concat(rest > 0 ? new string[] { null } : Array.Empty<string>()));
        }

        private ChartData Aging(IEnumerable<(int PartyId, DateTime Date, DateTime? DueDate, decimal Remaining)> open, DateTime asOf, string seriesName)
        {
            var buckets = Enum.GetValues<AgingBucket>();
            return new ChartData
            {
                Labels = buckets.Select(b => Text($"Aging.{b}")).ToList(),
                Series = { new(seriesName, buckets.Select(b => open.Where(o => AnalyticsCalc.Aging(o.DueDate, o.Date, asOf) == b).Sum(o => o.Remaining)).ToList()) }
            };
        }

        private static ChartData Bars(IEnumerable<(string Label, decimal Value)> items, string seriesName)
        {
            var list = items.ToList();
            return new ChartData { Labels = list.Select(i => i.Label).ToList(), Series = { new(seriesName, list.Select(i => i.Value).ToList()) } };
        }

        /// <summary>أكبر الحصص وما بعدها «أخرى»</summary>
        private ChartData Donut(IEnumerable<(string Label, decimal Value)> items)
        {
            var ordered = items.Where(i => i.Value > 0).OrderByDescending(i => i.Value).ToList();
            var shown = ordered.Take(Slices).ToList();
            var rest = ordered.Skip(Slices).Sum(i => i.Value);
            if (rest > 0) shown.Add((Text("Other"), rest));
            return Bars(shown, Text("Value"));
        }

        private static List<decimal> ByMonth(List<(int Year, int Month)> months, IEnumerable<(int Year, int Month, decimal Value)> values)
        {
            var map = values.GroupBy(v => (v.Year, v.Month)).ToDictionary(g => g.Key, g => g.Sum(v => v.Value));
            return months.Select(m => map.GetValueOrDefault(m)).ToList();
        }

        private static List<string> MonthLabels(List<(int Year, int Month)> months) =>
            months.Select(m => $"{m.Month:00}/{m.Year % 100:00}").ToList();

        private static Dictionary<int, T> Merge<T>(Dictionary<int, T> a, Dictionary<int, T> b, Func<T, T, T> combine)
        {
            var merged = new Dictionary<int, T>(a);
            foreach (var (key, value) in b) merged[key] = merged.TryGetValue(key, out var existing) ? combine(existing, value) : value;
            return merged;
        }

        private static string Text(string key, params object[] args) => LocalizationService.Get($"Str.Analytics.{key}", args);
    }
}
