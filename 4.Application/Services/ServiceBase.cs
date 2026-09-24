using PrimeERP.Data.Core;
using System;
using System.Linq;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services
{
    /// <summary>القاعدة المشتركة لكل خدمة</summary>
    public abstract class ServiceBase
    {
        protected abstract string PermissionPrefix { get; }
        protected abstract string StringPrefix { get; }
        protected abstract string EntityName { get; }

        protected readonly IPermissionService Permissions;
        protected readonly ISettingsProvider Settings;
        protected readonly ILocalizationService Localization;
        protected readonly IAuditLogger Audit;

        protected ServiceBase(IPermissionService permissions, ISettingsProvider settings,
                               ILocalizationService localization, IAuditLogger audit)
        {
            Permissions = permissions;
            Settings = settings;
            Localization = localization;
            Audit = audit;
        }

        protected string Msg(string key, params object[] args) =>
            args == null || args.Length == 0 ? Localization.Get($"{StringPrefix}.{key}") : Localization.Get($"{StringPrefix}.{key}", args);

        protected bool Can(string action) => Permissions.Can($"{PermissionPrefix}.{action}");

        protected Result Require(string action) =>
            Can(action) ? Result.Ok() : Result.Fail(Msg("PermissionDenied"), ErrorCode.Unauthorized);

        protected Result<T> Require<T>(string action) =>
            Can(action) ? Result.Ok<T>(default) : Result.Fail<T>(Msg("PermissionDenied"), ErrorCode.Unauthorized);

        protected Result FailDenied() => Result.Fail(Localization.Get("Str.PermissionDenied"), ErrorCode.Unauthorized);

        protected Result<T> FailDenied<T>() => Result.Fail<T>(Localization.Get("Str.PermissionDenied"), ErrorCode.Unauthorized);

        protected Result Fail(string key, ErrorCode code, params object[] args) => Result.Fail(Msg(key, args), code);

        protected Result Fail(string key, params object[] args) => Result.Fail(Msg(key, args), ErrorCode.Unexpected);

        protected Result<T> Fail<T>(string key, ErrorCode code, params object[] args) => Result.Fail<T>(Msg(key, args), code);

        protected Result<T> Fail<T>(string key, params object[] args) => Result.Fail<T>(Msg(key, args), ErrorCode.Unexpected);

        protected Result Ok(string key = null) => Result.Ok();

        protected Result<T> Ok<T>(T value, string key = null) => Result.Ok(value);

        protected T Setting<T>(string key, T defaultValue = default) => Settings.Get(key, defaultValue);

        protected static string CurrentUser => AppSession.Username ?? "Admin";

        protected Result Check<T>(IValidator<T> validator, T dto)
        {
            var result = validator.Validate(dto);
            return result.IsValid ? Result.Ok() : Result.Fail(result.Errors.Values, ErrorCode.ValidationFailed);
        }

        /// <summary>معاملة ذرّية بلا نتيجة ولا</summary>
        protected void Tx(Action<PrimeDbContext> body) => DbContextFactory.RunTransaction(body);

        /// <summary>معاملة ذرّية تُرجع قيمة</summary>
        protected T Tx<T>(Func<PrimeDbContext, T> body) => DbContextFactory.RunTransaction(body);

        protected Result Tx(AuditAction auditAction, Func<PrimeDbContext, Result> body, int recordId = 0)
        {
            Result outcome = null;
            try
            {
                DbContextFactory.RunTransaction(db =>
                {
                    outcome = body(db);
                    if (outcome.IsFailure) throw new TransactionAbortedException();
                });
            }
            catch (TransactionAbortedException)
            {
                return outcome;
            }

            Audit.Log(EntityName, recordId, auditAction);
            return outcome;
        }

        protected Result<T> Tx<T>(AuditAction auditAction, Func<PrimeDbContext, Result<T>> body, Func<T, int> recordIdOf = null)
        {
            Result<T> outcome = null;
            try
            {
                DbContextFactory.RunTransaction(db =>
                {
                    outcome = body(db);
                    if (outcome.IsFailure) throw new TransactionAbortedException();
                });
            }
            catch (TransactionAbortedException)
            {
                return outcome;
            }

            Audit.Log(EntityName, recordIdOf?.Invoke(outcome.Value) ?? 0, auditAction);
            return outcome;
        }

        private sealed class TransactionAbortedException : Exception { }
    }
}
