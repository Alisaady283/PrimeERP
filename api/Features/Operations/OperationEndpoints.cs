using PrimeERP.Api.Infrastructure;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.DTOs.Cheques;
using PrimeERP.Application.DTOs.Sales;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.PageServices.Assets;
using PrimeERP.Application.PageServices.Cheques;
using PrimeERP.Application.PageServices.HR;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.PageServices.Manufacturing;
using PrimeERP.Application.PageServices.Sales;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Permissions;

namespace PrimeERP.Api.Features.Operations
{
    public sealed record ScanResponse(Product Product, ScannedCode Code);

    public sealed record SaleRequest(CreateSalesInvoiceDto Sale, decimal Paid);

    public sealed record HoldRequest(CreateSalesInvoiceDto Sale, string Label);

    public sealed record OpenShiftRequest(decimal OpeningCash, int? TreasuryId, int? WarehouseId);

    public sealed record PullRequest(string SourceKind, List<string> MatchFields, Dictionary<string, string> MatchValues, string Scope);

    /// <summary>العمليات التي ليست قراءةً أو حفظاً: الترحيل وحركة الشيك وفتح الكشوف والإهلاك والشجرة والباركود ونقطة البيع — كلٌّ بخدمته وصلاحياتها</summary>
    public static class OperationEndpoints
    {
        public static RouteGroupBuilder MapOperations(this RouteGroupBuilder api)
        {
            var ops = api.MapGroup("").RequireAuthorization();

            ops.MapGet("/accounts/tree", (string search, AccountType? type, IAccountService accounts, IPermissionService access) =>
                accounts.GetTree(new AccountTreeFilter { SearchText = search, TypeFilter = type }).ToHttp(tree =>
                    access.Can(PermissionKeys.Accounts.ColumnBalance) ? tree : Hide(tree))).WithTags("Accounts");

            ops.MapGet("/accounts/leaves", (AccountType? type, IAccountService accounts) =>
                accounts.GetLeaves(type).ToHttp(list => list.Select(a => new { value = a.Code, label = a.Name, hint = a.Code, a.Id }).ToList())).WithTags("Accounts");

            ops.MapGet("/accounts/{parentId:int}/next-code", (int parentId, IAccountService accounts) =>
                accounts.GenerateChildCode(parentId).ToHttp(code => new { code })).WithTags("Accounts");

            ops.MapPost("/journals/{id:int}/post", (int id, IJournalService journals) => journals.Post(id).ToHttp()).WithTags("Journals");
            ops.MapPost("/journals/{id:int}/unpost", (int id, IJournalService journals) => journals.Unpost(id).ToHttp()).WithTags("Journals");

            ops.MapGet("/cheques/{id:int}/transitions", (int id, IChequeService cheques) => cheques.GetAllowedTransitions(id).ToHttp(s => s)).WithTags("Cheques");
            ops.MapPost("/cheques/move", (MoveChequeDto move, IChequeService cheques) => cheques.Move(move).ToHttp()).WithTags("Cheques");

            ops.MapGet("/attendance/open", (DateTime? date, IAttendanceService attendance) =>
                (date.HasValue ? attendance.Open(date.Value) : attendance.Open()).ToHttp(s => s)).WithTags("HR");
            ops.MapGet("/payroll/open", (int? month, int? year, IPayrollService payroll) =>
                (month.HasValue && year.HasValue ? payroll.Open(month.Value, year.Value) : payroll.Open()).ToHttp(s => s)).WithTags("HR");
            ops.MapGet("/production/open", (int bomId, decimal qty, IProductionOrderService orders) => orders.Open(bomId, qty).ToHttp(s => s)).WithTags("Manufacturing");

            ops.MapPost("/pull/available", (PullRequest request, PrimeERP.Application.Services.Documents.IPullCandidates candidates) =>
                candidates.Available(request.SourceKind, request.MatchFields ?? new(), (request.MatchValues ?? new()).ToDictionary(v => v.Key, v => (object)v.Value), request.Scope)
                    .ToHttp(list => list)).WithTags("Documents");

            ops.MapPost("/assets/depreciation/run", (DateTime upTo, IAssetDepreciationService depreciation) =>
                depreciation.RunFor(upTo).ToHttp(count => new { count })).WithTags("Assets");

            ops.MapGet("/products/scan", (string code, IProductService products) =>
                products.Scan(code).ToHttp(found => new ScanResponse(found.Product, found.Code))).WithTags("Products");

            var pos = ops.MapGroup("/pos").WithTags("Pos");
            pos.MapGet("/shift", (IPosService service) => service.Current().ToHttp(s => s));
            pos.MapPost("/shift/open", (OpenShiftRequest r, IPosService service) => service.Open(r.OpeningCash, r.TreasuryId, r.WarehouseId).ToHttp(s => s));
            pos.MapPost("/shift/close", (decimal countedCash, IPosService service) => service.Close(countedCash).ToHttp(s => s));
            pos.MapPost("/sell", (SaleRequest r, IPosService service) => service.Sell(r.Sale, r.Paid).ToHttp(s => s));
            pos.MapPost("/return", (CreateSalesReturnDto sale, IPosService service) => service.Return(sale).ToHttp(s => s));
            pos.MapPost("/hold", (HoldRequest r, IPosService service) => service.Hold(r.Sale, r.Label).ToHttp());
            pos.MapGet("/held", (IPosService service) => service.Held().ToHttp(s => s));
            pos.MapPost("/held/{id:int}/take", (int id, IPosService service) => service.Take(id).ToHttp(s => s));
            pos.MapDelete("/held/{id:int}", (int id, IPosService service) => service.DeleteHeld(id).ToHttp());

            return api;
        }

        private static List<AccountTreeNode> Hide(List<AccountTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.Balance = 0;
                Hide(node.Children);
            }
            return nodes;
        }
    }
}
