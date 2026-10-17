using System;
using PrimeERP.Application.Services.Documents;
using PrimeERP.Data.Repositories;
using PrimeERP.Data.Repositories.Base;
using PrimeERP.Domain.Calculations;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Sales
{
    /// <summary>فواتير البيع التي يُرتجَع منها في نقطة البيع حسب Documents.PosReturnPolicy — تضيّق السحب، ويفرضها PosService.Return</summary>
    public sealed class PosReturnScope : IPullScope
    {
        public const string Key = "PosReturn";
        public string Name => Key;

        private readonly ISettingsProvider _settings;
        private readonly IInvoiceRepository<SalesInvoice, SalesInvoiceLine> _invoices;
        private readonly IPosShiftRepository _shifts;

        public PosReturnScope(ISettingsProvider settings, IInvoiceRepository<SalesInvoice, SalesInvoiceLine> invoices, IPosShiftRepository shifts)
        {
            _settings = settings; _invoices = invoices; _shifts = shifts;
        }

        public bool Allows(string sourceKind, int sourceId)
        {
            var policy = _settings.Get(SettingKeys.Documents.PosReturnPolicy, PosReturnRule.Open);
            if (policy == PosReturnRule.Open) return true;
            if (_invoices.GetById(sourceId) is not { } invoice) return false;

            var soldIn = invoice.PosShiftId is int shiftId ? _shifts.GetById(shiftId) : null;
            return PosReturnRule.Allows(policy, _settings.Get(SettingKeys.Documents.PosReturnHours, 24), invoice.CreatedAt,
                soldIn?.Id, soldIn?.UserId, _shifts.OpenOf(AppSession.UserId)?.Id, AppSession.UserId, DateTime.Now);
        }
    }
}
