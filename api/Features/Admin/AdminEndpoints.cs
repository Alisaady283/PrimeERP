using PrimeERP.Api.Infrastructure;
using PrimeERP.Application.PageServices.Admin;
using PrimeERP.Platform.Localization;
using PrimeERP.Platform.Permissions;
using PrimeERP.Platform.Settings;

namespace PrimeERP.Api.Features.Admin
{
    public sealed record PermissionNode(string Key, string Label, IReadOnlyList<PermissionNode> Children);

    public sealed record UserPermission(string Key, PermissionState State, bool FromRole);

    public sealed record SettingItem(string Key, string Category, string DataType, string Value, bool IsSystem, string[] Options, string ShownWhen);

    /// <summary>CashCustomerId وPosCustomerEditable: عميل نقطة البيع الافتراضي، وهل يُغيَّر</summary>
    public sealed record InterfaceSettings(string Identity, string CompanyName, bool SimplifiedFlow, IReadOnlyList<string> Manifest, bool Arabic, bool English, int? CashCustomerId, bool PosCustomerEditable);

    /// <summary>الصلاحيات والإعدادات وإعداد الواجهة — المنح بصلاحية إدارة الأدوار، والحفظ بخدمة الإعدادات وصلاحياتها</summary>
    public static class AdminEndpoints
    {
        public static RouteGroupBuilder MapAdmin(this RouteGroupBuilder api)
        {
            var permissions = api.MapGroup("/permissions").WithTags("Permissions").RequireAuthorization();

            // الشجرة نفسها في الدسكتوب (PermissionCatalog): مجموعةٌ لكل صفحةٍ باسمها، والعرض أولاً، والأعمدة «عمود: …»
            permissions.MapGet("/tree", (IPermissionService access) => !access.Can(PermissionKeys.Users.ManageRoles) ? Forbidden() :
                Results.Ok(PermissionCatalog.Groups()
                    .Select(g => new PermissionNode(g.Key, g.Name, g.Actions.Select(a => new PermissionNode(a.Key, a.Name, Array.Empty<PermissionNode>())).ToList()))
                    .ToList()));

            permissions.MapGet("/roles/{roleId:int}", (int roleId, IPermissionService access, IPermissionStore store) =>
                !access.Can(PermissionKeys.Users.ManageRoles) ? Forbidden() : Results.Ok(store.GetRolePermissions(roleId)));

            permissions.MapPut("/roles/{roleId:int}", (int roleId, List<string> keys, IPermissionService access, IPermissionAdminService admin) =>
            {
                if (!access.Can(PermissionKeys.Users.ManageRoles)) return Forbidden();
                var known = PermissionKeys.All().ToHashSet();
                admin.SetRolePermissions(roleId, keys.Where(known.Contains));
                return Results.NoContent();
            });

            permissions.MapGet("/users/{userId:int}", (int userId, IPermissionService access, IPermissionAdminService admin) =>
                !access.Can(PermissionKeys.Users.ManageRoles) ? Forbidden() :
                    Results.Ok(PermissionKeys.All().Select(k => new UserPermission(k, admin.GetState(userId, k), admin.IsInheritedFromRole(userId, k))).ToList()));

            permissions.MapPut("/users/{userId:int}", (int userId, List<UserPermission> states, IPermissionService access, IPermissionAdminService admin) =>
            {
                if (!access.Can(PermissionKeys.Users.ManageRoles)) return Forbidden();
                var known = PermissionKeys.All().ToHashSet();
                foreach (var state in states.Where(s => known.Contains(s.Key))) admin.SetUserPermission(userId, state.Key, state.State);
                return Results.NoContent();
            });

            var settings = api.MapGroup("/settings").WithTags("Settings").RequireAuthorization();

            settings.MapGet("/", (IPermissionService access, ISettingsService values) => !access.Can("Settings.View") ? Forbidden() :
                Results.Ok(SettingKeys.All().Select(d => new SettingItem(d.Key, d.Category, d.DataType, SettingKeys.IsSecret(d.Key) ? "" : values.Get(d.Key, d.DefaultValue), d.IsSystem, d.Options, d.ShownWhen)).ToList()));

            settings.MapPut("/", (Dictionary<string, string> changes, ISettingsService values) =>
            {
                var known = SettingKeys.All().ToDictionary(d => d.Key);
                return values.SetMany(changes.Where(c => known.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => (object)c.Value)).ToHttp();
            });

            settings.MapGet("/interface", (ISettingsService values) =>
            {
                var manifest = values.Get(SettingKeys.UI.Manifest, "");
                return Results.Ok(new InterfaceSettings(
                    values.Get(SettingKeys.UI.Identity, "Navy"),
                    values.Get(SettingKeys.Company.Name, "PrimeERP"),
                    values.Get(SettingKeys.Documents.SimplifiedFlow, true),
                    manifest.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                    values.Get(SettingKeys.Edition.Arabic, true),
                    values.Get(SettingKeys.Edition.English, true),
                    int.TryParse(values.Get(SettingKeys.Documents.CashCustomer, ""), out var cash) ? cash : null,
                    values.Get(SettingKeys.Documents.PosCustomerEditable, false)));
            });

            return api;
        }

        private static IResult Forbidden() =>
            Results.Problem(detail: LocalizationService.Get("Str.PermissionDenied"), statusCode: StatusCodes.Status403Forbidden);
    }
}
