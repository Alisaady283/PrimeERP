using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Application.DTOs.Documents;
using PrimeERP.Application.DTOs.Inventory;
using PrimeERP.Application.PageServices.Documents;
using PrimeERP.Application.PageServices.Inventory;
using PrimeERP.Application.Reporting;
using PrimeERP.Application.Services.Entities;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Tests.Helpers;
using Xunit;

namespace PrimeERP.Tests.Services
{
    /// <summary>تقارير متابعة السحب</summary>
    [Collection("Database")]
    public class FlowReportTests
    {
        private readonly TestDatabaseFixture _db;

        public FlowReportTests(TestDatabaseFixture db)
        {
            _db = db;
            AppSession.DevMode = true;
        }

        private string SeedProduct() =>
            _db.Services.GetRequiredService<IProductService>().Create(new Product
            { Name = $"صنف {Guid.NewGuid():N}", CostPrice = 10, SalePrice = 15, IsActive = true }).Value.Code;

        private static CreateCycleDocumentDto Doc(string code, decimal qty) => new()
        {
            DocDate = DateTime.Today, PartyId = 1,
            Lines = new List<CreateCycleDocumentLineDto> { new() { ProductCode = code, Qty = qty, UnitPrice = 12 } }
        };

        private static FlowRow RowOf(Domain.Results.Result<ReportData> report, string docNo)
        {
            Assert.True(report.IsSuccess, report.ErrorMessage);
            return report.Value.Rows.Cast<FlowRow>().Single(r => r.DocNo == docNo);
        }

        [Fact]
        public void PurchaseOrderFollowUp_ShowsWhatTheStockInPulled()
        {
            var code = SeedProduct();
            var order = _db.Services.GetRequiredService<IPurchaseOrderService>().Create(Doc(code, 10));
            Assert.True(order.IsSuccess, order.ErrorMessage);
            var line = order.Value.Lines.Single();

            var stockIn = _db.Services.GetRequiredService<IStockInService>().Create(new CreateStockAdjustmentDto
            {
                MovementDate = DateTime.Today,
                WarehouseId = _db.Services.GetRequiredService<Lookup<Warehouse>>().Add($"مخزن {Guid.NewGuid():N}"),
                Lines = new List<CreateStockAdjustmentLineDto>
                {
                    new()
                    {
                        ProductCode = code, Qty = 6, UnitCost = 12,
                        SourceType = "PurchaseOrder", SourceId = order.Value.Id, SourceNo = order.Value.DocNo, SourceLineId = line.Id
                    }
                }
            });
            Assert.True(stockIn.IsSuccess, stockIn.ErrorMessage);

            var row = RowOf(_db.Services.GetRequiredService<IFlowReportService>().PurchaseOrders(DateTime.Today, DateTime.Today, null), order.Value.DocNo);

            Assert.Equal(10m, row.Qty);
            Assert.Equal(6m, row.Done);
            Assert.Equal(0m, row.Closed);
            Assert.Equal(4m, row.Remaining);
            Assert.Equal(LocalizationService.Get("Str.Flow.State.Partial"), row.Status);
        }

        [Fact]
        public void QuotationFollowUp_IsCompleteOnceFullyOrdered()
        {
            var code = SeedProduct();
            var quotation = _db.Services.GetRequiredService<IQuotationService>().Create(Doc(code, 5)).Value;
            var line = quotation.Lines.Single();

            var order = _db.Services.GetRequiredService<ISalesOrderService>().Create(new CreateCycleDocumentDto
            {
                DocDate = DateTime.Today, PartyId = 1,
                Lines = new List<CreateCycleDocumentLineDto>
                {
                    new() { ProductCode = code, Qty = 5, UnitPrice = 12, SourceType = "Quotation", SourceId = quotation.Id, SourceNo = quotation.DocNo, SourceLineId = line.Id }
                }
            });
            Assert.True(order.IsSuccess, order.ErrorMessage);

            var row = RowOf(_db.Services.GetRequiredService<IFlowReportService>().Quotations(DateTime.Today, DateTime.Today, (int)FlowState.Complete), quotation.DocNo);

            Assert.Equal(0m, row.Remaining);
            Assert.Equal(LocalizationService.Get("Str.Flow.State.Complete"), row.Status);
        }

        [Theory]
        [InlineData(10, 0, null, FlowState.Open)]
        [InlineData(10, 4, null, FlowState.Partial)]
        [InlineData(10, 10, null, FlowState.Complete)]
        [InlineData(10, 10, 3.0, FlowState.Awaiting)]
        [InlineData(10, 10, 10.0, FlowState.Complete)]
        public void Progress_FollowsEachStage(double qty, double done, double? closed, FlowState expected) =>
            Assert.Equal(expected, LineCalc.Progress((decimal)qty, (decimal)done, closed is double c ? (decimal)c : null));
    }
}
