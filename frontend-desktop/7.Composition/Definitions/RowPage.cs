using PrimeERP.Application.PageServices.Builder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels;
using PrimeERP.UI.ViewModels.Base;
using PrimeERP.Domain.Results;

namespace PrimeERP.Composition.Definitions
{
    /// <summary>صفحة صفوفٍ من خدمتها</summary>
    public static class RowPage
    {
        public static object ViewModel<TService>(IServiceProvider services, string permissionPrefix)
            where TService : class, IRowService =>
            new DynamicViewModel(services.GetRequiredService<TService>(), permissionPrefix,
                services.GetRequiredService<IPermissionService>(),
                services.GetRequiredService<IToastService>(),
                services.GetRequiredService<IDialogService>());

        public static object Documents<TDto, TFilter>(IServiceProvider services, string permissionPrefix,
            Func<int, int, TFilter, Result<PagedResult<TDto>>> page, Func<int, Result> delete, Func<TDto, int> id, Action<TFilter, string> search)
            where TFilter : new() =>
            new DocumentListViewModel<TDto, TFilter>(permissionPrefix, page, delete, id, search,
                services.GetRequiredService<IPermissionService>(),
                services.GetRequiredService<IToastService>(),
                services.GetRequiredService<IDialogService>());

        public static DialogDefinition Dialog<TService>(string title, List<FieldDefinition> fields,
            string titleEdit = null, int gridColumns = 2) where TService : class => new()
        {
            TitleKey = title, TitleEditKey = titleEdit ?? title, GridColumns = gridColumns,
            ServiceType = typeof(TService),
            CreateDtoType = typeof(ExpandoObject),
            UpdateDtoType = typeof(ExpandoObject),
            Fields = fields
        };
    }
}
