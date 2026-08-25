using System;
using System.Data.Common;
using System.Linq;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;
using Db = PrimeERP.Data.Core.DbHelper;

namespace PrimeERP.Application.Services
{
    /// <summary>
    /// القاعدة المشتركة لكل خدمة — صلاحية/رسالة/معاملة/تدقيق بدل تكرارها في كل خدمة (55 فحص صلاحية +
    /// 81 Result.Fail + 24 معاملة + 25 audit قبل R6). كل خدمة تحدّد PermissionPrefix/StringPrefix/EntityName
    /// فقط، وتستخدم الدوال الجاهزة هنا.
    /// </summary>
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

        /// <summary>معاملة مخصّصة لعمليات لا تطابق شكل WriteOperation القياسي (Post/CloseYear متعددة الجداول). body يرجع فشلاً بدل رمي استثناء — التراجع (Rollback) يحدث دون كسر تدفّق الاستثناءات العادي.</summary>
        protected Result Tx(AuditAction auditAction, Func<DbConnection, DbTransaction, Result> body, int recordId = 0)
        {
            Result outcome = null;
            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    outcome = body(conn, tx);
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

        protected Result<T> Tx<T>(AuditAction auditAction, Func<DbConnection, DbTransaction, Result<T>> body, Func<T, int> recordIdOf = null)
        {
            Result<T> outcome = null;
            try
            {
                Db.RunTransaction((conn, tx) =>
                {
                    outcome = body(conn, tx);
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
