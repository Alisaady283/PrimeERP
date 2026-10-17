using System;
using System.Collections.Generic;
using System.Linq;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Platform.Permissions
{
    /// <summary>مجموعة صلاحيات الصفحة: مفتاحها واسمها ومفاتيح أفعالها بأسمائها</summary>
    public sealed record PermissionGroup(string Key, string Name, IReadOnlyList<PermissionAction> Actions);

    public sealed record PermissionAction(string Key, string Name);

    /// <summary>
    /// شجرة الصلاحيات من PermissionKeys — مصدرٌ واحد يقرؤه الدسكتوب (PermissionTreeFactory) والخادم (/permissions/tree) للويب والأندرويد:
    /// مجموعةٌ لكل صفحة بالعرض أولاً، وأعمدة الحقول باسم «عمود: …»، والأسماء من النصوص ثم اسم الوحدة ثم المفتاح.
    /// </summary>
    public static class PermissionCatalog
    {
        /// <summary>include: الصفحات المعروضة (سجلّ الوحدات في الدسكتوب)، وnull للكل</summary>
        public static List<PermissionGroup> Groups(Func<string, bool> include = null) =>
            PermissionKeys.All()
                .Distinct()
                .Where(k => !PageAction(k) || !PermissionKeys.IsDomainOnly(k.Split('.')[0]))
                .GroupBy(PermissionKeys.PageOfKey)
                .Where(g => include == null || include(g.Key))
                .OrderBy(g => PermissionKeys.OrderOf(g.Key)).ThenBy(g => g.Key)
                .Select(g => new PermissionGroup(g.Key, ModuleName(g.Key),
                    g.OrderBy(k => k.StartsWith(g.Key + ".") ? 0 : 1).ThenBy(k => k.EndsWith(".View") ? 0 : 1).ThenBy(k => k)
                        .Select(k => new PermissionAction(k, NameIn(g.Key, k))).ToList()))
                .ToList();

        /// <summary>اسم الفعل في صفحته، وباسم مجموعته إن كانت قائمةً فيها</summary>
        private static string NameIn(string page, string key)
        {
            var group = key.Split('.')[0];
            return group == page || group == PermissionKeys.DomainOf(page) ? ActionName(key) : $"{ModuleName(group)}: {ActionName(key)}";
        }

        public static string ModuleName(string module) =>
            LocalizationService.GetOr($"Str.Permission.{module}", PermissionKeys.TitleOf(module) is { } title
                ? LocalizationService.Pick(title.Title, title.TitleEn, LocalizationService.GetOr($"Str.Module.{module}", module))
                : LocalizationService.GetOr($"Str.Module.{module}", module));

        /// <summary>فعلٌ قياسيّ تحمله الصفحات لا مجالها</summary>
        private static bool PageAction(string key) =>
            key.Split('.') is { Length: 2 } parts && PermissionKeys.PageActions.Contains(parts[1]);

        public static string ActionName(string key)
        {
            var parts = key.Split('.');
            if (parts.Length >= 3 && parts[1] == "Column")
                return LocalizationService.Get("Str.Permission.Column", ActionOf(parts[2]));

            return ActionOf(parts.Length > 1 ? parts[1] : key);
        }

        private static string ActionOf(string action) =>
            LocalizationService.GetOr($"Str.Permission.Action.{action}", LocalizationService.GetOr($"Str.Module.{action}", action));
    }
}
