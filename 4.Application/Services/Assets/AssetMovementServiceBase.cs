using PrimeERP.Data.Core;
using System;
using System.Collections.Generic;
using PrimeERP.Application.DTOs.Accounting;
using PrimeERP.Application.Services.Accounting;
using PrimeERP.Domain.Entities.Common;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using PrimeERP.Application.Services.Admin;

namespace PrimeERP.Application.Services.Assets
{
    /// <summary>أساس حركات الأصول</summary>
    public abstract class AssetMovementServiceBase<TEntity, TDto, TFilter>
        : CrudServiceBase<TEntity, TDto, TFilter> where TEntity : BaseModel
    {
        protected readonly IJournalService Journals;
        private readonly ISettingsService _settings;

        protected AssetMovementServiceBase(IPermissionService permissions, ISettingsProvider settings,
            ILocalizationService localization, IAuditLogger audit,
            IJournalService journals, ISettingsService settingsService)
            : base(permissions, settings, localization, audit)
        {
            Journals = journals;
            _settings = settingsService;
        }

        protected override string PermissionPrefix => "Assets";
        protected override string StringPrefix => "Str.Asset";

        protected Result<string> Account(string key) => Required(_settings.Get<string>(key, ""), "AccountsMissing");

        protected int PostEntry(PrimeDbContext db, DateTime date, string description,
            string debitAccount, string creditAccount, decimal amount, string lineNote = null) =>
            Posting.Entry(Journals, db, date, description, EntityName, debitAccount, creditAccount, amount, lineNote);

        protected int PostEntry(PrimeDbContext db, DateTime date, string description,
            List<CreateJournalLineDto> lines) =>
            Posting.Entry(Journals, db, date, description, EntityName, lines);

        protected Result<string> Required(string accountCode, string messageKey)
            => string.IsNullOrWhiteSpace(accountCode)
                ? Result.Fail<string>(Msg(messageKey), ErrorCode.ValidationFailed)
                : Result.Ok(accountCode);

        protected Result EnsureReversible(int? entryId) => Posting.EnsureReversible(Journals, entryId);

        protected void ReverseEntry(PrimeDbContext db, int? entryId) => Posting.Reverse(Journals, db, entryId);
    }
}
