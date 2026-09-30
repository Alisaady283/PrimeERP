using PrimeERP.Domain.Enums;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using System.Collections.Generic;
using System.Linq;
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
    /// <summary>المالك الوحيد لمنطق العملاء</summary>
    public class CustomerService : PartyServiceBase<Customer, CustomerFilter>, ICustomerService
    {
        protected override string PermissionPrefix => "Customers";
        protected override string StringPrefix => "Str.Customer";
        protected override string EntityName => "Customers";
        protected override string SequenceKey => "Customer";
        protected override string AccountSettingKey => SettingKeys.Accounts.Customers;

        public static readonly Field<Customer>[] Rules =
        {
            new(x => x.Code, "Str.Field.CustomerCode", Required: true),
            new(x => x.Name, "Str.Field.CustomerName", Required: true, Max: 150),
            new(x => x.Phone, "Str.Field.Phone", Format: FieldFormat.Phone),
            new(x => x.Email, "Str.Email", Format: FieldFormat.Email),
            new(x => x.CreditLimit, "Str.CreditLimit", From: 0),
        };

        protected override Field<Customer>[] Fields => Rules;

        private readonly IPartyRepository<Customer> _customers;
        private readonly Cheques.IChequeService _cheques;

        protected override IPartyRepository<Customer> Repository => _customers;
        protected override Cheques.IChequeService Cheques => _cheques;

        public CustomerService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            Statement statement, INumberSequenceService numbers, IPartyRepository<Customer> customers,
            IJournalRepository journalRepo, Cheques.IChequeService cheques, AccountCases tree)
            : base(permissions, settings, localization, audit, statement, numbers, journalRepo, tree)
        {
            _customers = customers;
            _cheques = cheques;
        }

        protected override (List<Customer> Items, int Total) FindPaged(int page, int pageSize, CustomerFilter filter)
        {
            filter ??= new CustomerFilter();
            return _customers.GetPaged(page, pageSize, filter.SearchText, filter.IsActive, filter.HasBalance,
                filter.OverCreditLimit, filter.CategoryId, filter.SortBy, filter.SortDescending);
        }

        protected override string CreateDetails(Customer c)
        {
            var warnings = string.Join(" ", new[]
            {
                Setting(SettingKeys.Financial.WarnOnDuplicateCustomerName, true) && _customers.ExistsName(c.Name, c.Id)
                    ? Msg("DuplicateName") : null,
                Setting(SettingKeys.Financial.WarnOnDuplicatePhone, true) && _customers.ExistsPhone(c.Phone, c.Id)
                    ? Msg("DuplicatePhone") : null
            }.Where(w => w != null));

            return warnings.Length == 0 ? null : warnings;
        }
    }
}
