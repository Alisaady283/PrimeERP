using PrimeERP.Domain.Enums;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.Legacy.Accounting;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Legacy.Parties
{
    /// <summary>المالك الوحيد لمنطق الموردين</summary>
    public class SupplierService : PartyServiceBase<Supplier, SupplierFilter>, ISupplierService
    {
        protected override string PermissionPrefix => "Suppliers";
        protected override string StringPrefix => "Str.Supplier";
        protected override string EntityName => "Suppliers";
        protected override string SequenceKey => "Supplier";
        protected override string AccountSettingKey => SettingKeys.Accounts.Suppliers;

        public static readonly Field<Supplier>[] Rules =
        {
            new(x => x.Code, "Str.Field.SupplierCode", Required: true),
            new(x => x.Name, "Str.Field.SupplierName", Required: true, Max: 150),
            new(x => x.Phone, "Str.Field.Phone", Format: FieldFormat.Phone),
            new(x => x.Email, "Str.Email", Format: FieldFormat.Email),
            new(x => x.CreditLimit, "Str.CreditLimit", From: 0),
        };

        protected override Field<Supplier>[] Fields => Rules;

        private readonly IPartyRepository<Supplier> _suppliers;
        private readonly Cheques.IChequeService _cheques;

        protected override IPartyRepository<Supplier> Repository => _suppliers;
        protected override Cheques.IChequeService Cheques => _cheques;

        public SupplierService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            Statement statement, INumberSequenceService numbers, IPartyRepository<Supplier> suppliers,
            IJournalRepository journalRepo, Cheques.IChequeService cheques, AccountCases tree)
            : base(permissions, settings, localization, audit, statement, numbers, journalRepo, tree)
        {
            _suppliers = suppliers;
            _cheques = cheques;
        }

        protected override (List<Supplier> Items, int Total) FindPaged(int page, int pageSize, SupplierFilter filter)
        {
            filter ??= new SupplierFilter();
            return _suppliers.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.HasBalance,
                filter.OverCreditLimit, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

    }
}
