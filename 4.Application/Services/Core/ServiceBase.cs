using PrimeERP.Application.Validation;
using PrimeERP.Data.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Domain.Contracts;
using PrimeERP.Domain.Enums;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Audit;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Application.Services.Core
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

        /// <summary>صفحةٌ لا تتجاوز حجمها</summary>
        protected static PagedResult<TOut> Paged<TIn, TOut>(List<TIn> items, int total, int page, int pageSize,
            Func<List<TIn>, List<TOut>> map)
        {
            var cut = pageSize > 0 && items.Count > pageSize
                ? items.Skip(Math.Max(0, page - 1) * pageSize).Take(pageSize).ToList()
                : items;
            return new PagedResult<TOut> { Items = map(cut), TotalCount = total, Page = page, PageSize = pageSize };
        }



        /// <summary>معاملة ذرّية بلا نتيجة ولا</summary>
        protected void Tx(Action<PrimeDbContext> body) => DbContextFactory.RunTransaction(body);

        /// <summary>معاملة ذرّية تُرجع قيمة</summary>
        protected T Tx<T>(Func<PrimeDbContext, T> body) => DbContextFactory.RunTransaction(body);

        /// <summary>معاملةٌ تُلغى بنتيجتها</summary>
        protected static Result<T> Commit<T>(Func<PrimeDbContext, Result<T>> body)
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
            catch (TransactionAbortedException) { }
            catch (InvalidOperationException aborted)
            {
                outcome = Result.Fail<T>(aborted.Message, ErrorCode.ValidationFailed);
            }
            catch (DbUpdateException refused)
            {
                outcome = Result.Fail<T>((refused.InnerException ?? refused).Message,
                    refused is DbUpdateConcurrencyException ? ErrorCode.ConcurrencyConflict : ErrorCode.Conflict);
            }
            catch (System.Data.Common.DbException concurrent)
            {
                outcome = Result.Fail<T>(concurrent.Message, ErrorCode.ConcurrencyConflict);
            }

            return outcome;
        }

        protected static Result Commit(Func<PrimeDbContext, Result> body)
        {
            var outcome = Commit(db => body(db) is { IsFailure: true } failed ? failed.As<bool>() : Result.Ok(true));
            return outcome.IsSuccess ? Result.Ok() : outcome;
        }

        private sealed class TransactionAbortedException : Exception { }
    }
}
