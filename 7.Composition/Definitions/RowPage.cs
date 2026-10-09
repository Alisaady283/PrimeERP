using PrimeERP.Application.PageServices.Builder;
using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Services;
using PrimeERP.UI.ViewModels;

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
