using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;

namespace PrimeERP.Modules
{
    /// <summary>شاشتا الصلاحيات — تكوين فقط فوق TreeCheckListRenderer، الشجرة من PermissionKeys.All() آلياً.</summary>
    public static class PermissionModuleRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "RolePermissions",
                TitleKey = "صلاحيات الأدوار",
                PermissionPrefix = "Users",
                LayoutKind = LayoutKind.TreeCheckList,
                TreeCheckList = new TreeCheckListDefinition
                {
                    TitleKey = "صلاحيات الأدوار",
                    SourceLabelKey = "الدور",
                    Mode = TreeCheckMode.TwoState,
                    SourceItems = _ => PermissionDb.GetAllRoles()
                        .Select(r => new SourceOption { Id = r.Id, Display = r.NameAr })
                        .ToList(),
                    BuildTree = (_, roleId) =>
                    {
                        var granted = PermissionDb.GetRolePermissions(roleId).ToHashSet();
                        var roots = PermissionTreeFactory.Build();
                        foreach (var node in PermissionTreeFactory.KeyNodes(roots))
                            node.CheckState = granted.Contains(node.Id) ? NodeCheckState.Checked : NodeCheckState.Unchecked;

                        SyncModules(roots, NodeCheckState.Checked);
                        return roots;
                    },
                    Save = (services, roleId, nodes) =>
                    {
                        var keys = PermissionTreeFactory.KeyNodes(nodes)
                            .Where(n => n.CheckState == NodeCheckState.Checked)
                            .Select(n => n.Id);
                        services.GetRequiredService<IPermissionAdminService>().SetRolePermissions(roleId, keys);
                        return Result.Ok();
                    },
                    OnCheckChanged = (node, roots) => ApplyViewGate(node, roots, on: NodeCheckState.Checked),
                    Actions = new List<TreeCheckListAction>
                    {
                        new()
                        {
                            TextKey = "تحديد الكل",
                            Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Checked)
                        },
                        new()
                        {
                            TextKey = "إلغاء الكل",
                            Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Unchecked)
                        },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "UserPermissions",
                TitleKey = "صلاحيات المستخدمين",
                PermissionPrefix = "Users",
                LayoutKind = LayoutKind.TreeCheckList,
                TreeCheckList = new TreeCheckListDefinition
                {
                    TitleKey = "صلاحيات المستخدمين",
                    SourceLabelKey = "المستخدم",
                    Mode = TreeCheckMode.ThreeState,
                    SourceItems = _ => PermissionDb.GetAllUsers()
                        .Select(u => new SourceOption { Id = u.Id, Display = u.DisplayName })
                        .ToList(),
                    BuildTree = (services, userId) =>
                    {
                        var admin = services.GetRequiredService<IPermissionAdminService>();
                        var roots = PermissionTreeFactory.Build();

                        foreach (var node in PermissionTreeFactory.KeyNodes(roots))
                        {
                            node.CheckState = admin.GetState(userId, node.Id) switch
                            {
                                PermissionState.Granted => NodeCheckState.Granted,
                                PermissionState.Revoked => NodeCheckState.Revoked,
                                _ => NodeCheckState.Inherited
                            };

                            if (node.CheckState == NodeCheckState.Inherited)
                                node.InheritedHint = admin.IsInheritedFromRole(userId, node.Id) ? "(الدور: مسموح)" : "(الدور: ممنوع)";
                        }

                        SyncModules(roots, NodeCheckState.Granted);
                        return roots;
                    },
                    Save = (services, userId, nodes) =>
                    {
                        var admin = services.GetRequiredService<IPermissionAdminService>();
                        foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                        {
                            admin.SetUserPermission(userId, node.Id, node.CheckState switch
                            {
                                NodeCheckState.Granted => PermissionState.Granted,
                                NodeCheckState.Revoked => PermissionState.Revoked,
                                _ => PermissionState.Inherited
                            });
                        }
                        return Result.Ok();
                    },
                    OnCheckChanged = (node, roots) => ApplyViewGate(node, roots, on: NodeCheckState.Granted),
                    Actions = new List<TreeCheckListAction>
                    {
                        new()
                        {
                            TextKey = "إعادة الكل للموروث",
                            Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Inherited)
                        },
                    }
                }
            });
        }

        /// <summary>
        /// العرض بوّابة القسم: باقي إجراءاته معطَّلة حتى يُؤشَّر، فلا تُبنى حالة خاطئة أصلاً بدل تصحيحها
        /// لاحقاً بصمت. ورفعه يُنزل إجراءاته معه، والقسم يتبع أبناءه فيظهر مؤشَّراً بأوّل إجراء.
        /// </summary>
        public static void SyncModules(List<TreeNodeViewModel> roots, NodeCheckState on)
        {
            foreach (var module in roots) SyncModule(module, on);
        }

        private static void ApplyViewGate(TreeNodeViewModel node, List<TreeNodeViewModel> roots, NodeCheckState on)
        {
            var module = roots.FirstOrDefault(r => r.Children.Contains(node)) ?? roots.FirstOrDefault(r => r == node);
            if (module != null) SyncModule(module, on);
        }

        private static void SyncModule(TreeNodeViewModel module, NodeCheckState on)
        {
            var view = module.Children.FirstOrDefault(child => child.Id == PermissionRules.ViewKeyOf(child.Id));
            if (view == null) return;

            var open = view.CheckState == on;

            foreach (var action in module.Children.Where(child => child != view))
            {
                action.IsCheckEnabled = open;
                if (!open) action.CheckState = view.CheckState;
            }

            module.CheckState = module.Children.Any(child => child.CheckState == on) ? on : view.CheckState;
        }

        private static void SetAll(List<TreeNodeViewModel> nodes, NodeCheckState state)
        {
            foreach (var node in PermissionTreeFactory.KeyNodes(nodes))
                node.CheckState = state;
        }
    }
}
