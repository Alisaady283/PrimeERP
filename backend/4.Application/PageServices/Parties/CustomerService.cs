using PrimeERP.Domain.Enums;
using PrimeERP.Application.Services.Ledger.Accounts;
using PrimeERP.Application.Services.Core;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Application.DTOs.Parties;
using PrimeERP.Application.PageServices.Accounting;
using PrimeERP.Application.Services.Ledger;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.PageServices.Parties
{
    /// <summary>المالك الوحيد لمنطق العملاء</summary>
    public class CustomerService : PartyServiceBase<Customer, CustomerFilter>, ICustomerService
    {
        protected override string PermissionPrefix => "Customers";
        protected override string StringPrefix => "Str.Customer";
        protected override string EntityName => "Customers";
        protected override string SequenceKey => "Customer";
        protected override string AccountSettingKey => SettingKeys.Accounts.Customers;

        public static readonly Field<Customer>[] Rules = RulesOf("Str.Field.CustomerCode", "Str.Field.CustomerName");

        protected override Field<Customer>[] Fields => Rules;

        private readonly IPartyRepository<Customer> _customers;
        private readonly Cheques.IChequeService _cheques;
        private readonly ISettingStore _settingStore;

        protected override IPartyRepository<Customer> Repository => _customers;
        protected override Cheques.IChequeService Cheques => _cheques;

        public CustomerService(IPermissionService permissions, ISettingsProvider settings, ILocalizationService localization, IAuditLogger audit,
            Statement statement, INumberSequenceService numbers, IPartyRepository<Customer> customers,
            AccountBalances balances, Cheques.IChequeService cheques, AccountCases tree, ISettingStore settingStore)
            : base(permissions, settings, localization, audit, statement, numbers, balances, tree)
        {
            _customers = customers;
            _cheques = cheques;
            _settingStore = settingStore;
        }

        /// <summary>العميل النقدي لنقطة البيع محفوظٌ في الإعدادات فلا يُحذف</summary>
        protected override Result CanErase(Customer customer) =>
            Setting(SettingKeys.Documents.CashCustomer, "") == customer.Id.ToString() ? Fail("CashCustomerLocked", ErrorCode.ValidationFailed) : Result.Ok();

        public Result SeedDefaults()
        {
            var current = Setting(SettingKeys.Documents.CashCustomer, "");
            if (int.TryParse(current, out var id) && _customers.GetById(id) != null) return Result.Ok();

            var first = _customers.GetAll(activeOnly: false).OrderBy(c => c.Id).FirstOrDefault();
            if (first == null)
            {
                var created = Add(new Customer { Name = Msg("Cash"), IsActive = true });
                if (created.IsFailure) return Result.Fail(created.ErrorMessage, created.ErrorCode);
                first = created.Value;
            }

            var setting = _settingStore.GetByKey(SettingKeys.Documents.CashCustomer);
            if (setting == null) return Result.Ok();
            setting.Value = first.Id.ToString();
            setting.ModifiedAt = System.DateTime.Now;
            _settingStore.Upsert(setting);
            Settings.Reload();
            return Result.Ok();
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
