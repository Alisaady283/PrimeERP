using System.Collections.Generic;
using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using PrimeERP.Data.Repositories;
using PrimeERP.Domain.Entities;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Application.Services.Ledger.Accounts
{
    /// <summary>إضافة حساب في الشجرة</summary>
    public sealed class AddTreeAccount
    {
        private const int MaxChildSuffix = 9999;

        public static readonly Field<Account>[] AccountFields =
        {
            new(x => x.Name, "Str.Field.AccountName", Required: true, Max: 150),
            new(x => x.Code, "", Format: FieldFormat.Digits, Message: "Str.Rule.AccountCodeFormat",
                Args: _ => new object[] { LocalizationService.Get("Str.Field.AccountCode") }),
        };

        private readonly IAccountRepository _accounts;
        private readonly Guards _guards;

        public AddTreeAccount(IAccountRepository accounts, Guards guards)
        {
            _accounts = accounts;
            _guards = guards;
        }

        public Result<Account> Run(PrimeDbContext db, Account parent, string name, bool leaf = true,
            string notes = null, bool isActive = true)
        {
            if (parent.IsLeaf && _guards.HasEntries(parent.Code, db: db))
                return Result.Fail<Account>(LocalizationService.Get("Str.Accounts.ParentHasEntries", parent.Name), ErrorCode.ValidationFailed);

            var code = ChildCode(db, parent);
            if (code.IsFailure) return code.As<Account>();

            var account = new Account
            {
                Code = code.Value, Name = name, ParentCode = parent.Code, Level = parent.Level + 1,
                IsLeaf = leaf, Type = parent.Type, Notes = notes, IsActive = isActive
            };
            var check = Check.Valid(account, AccountFields);
            if (check.IsFailure) return check.As<Account>();

            account.Id = _accounts.Insert(account, db);
            if (parent.IsLeaf) _accounts.SetIsLeaf(parent.Code, false, db);
            return Result.Ok(account);
        }

        /// <summary>كود الابن التالي</summary>
        public Result<string> ChildCode(PrimeDbContext db, Account parent) => Next(parent, _accounts.GetAllChildren(parent.Code, db));

        private static Result<string> Next(Account parent, List<Account> children)
        {
            var max = 0;
            foreach (var child in children)
            {
                var suffix = child.Code.Length > parent.Code.Length ? child.Code.Substring(parent.Code.Length) : "";
                if (int.TryParse(suffix, out var n) && n > max) max = n;
            }

            return max + 1 > MaxChildSuffix
                ? Result.Fail<string>(LocalizationService.Get("Str.Accounts.TooManyChildren", parent.Code, MaxChildSuffix), ErrorCode.ValidationFailed)
                : Result.Ok(parent.Code + (max + 1).ToString("D" + parent.Level));
        }
    }
}
