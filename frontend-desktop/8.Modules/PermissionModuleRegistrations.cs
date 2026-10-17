using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PrimeERP.Composition.Definitions;
using PrimeERP.Composition.Registry;
using PrimeERP.Domain.Results;
using PrimeERP.Platform.Permissions;
using PrimeERP.UI.Components.Tree;
using PrimeERP.Platform.Localization;

namespace PrimeERP.Modules
{
    /// <summary>شاشتا الصلاحيات</summary>
    public static class PermissionModuleRegistrations
    {
        public static void RegisterAll(IModuleRegistry registry)
        {
            registry.Register(new ModuleDefinition
            {
                Key = "RolePermissions",
                TitleKey = "Str.Module.RolePermissions",
                PermissionPrefix = "Users",
                LayoutKind = LayoutKind.TreeCheckList,
                TreeCheckList = new TreeCheckListDefinition
                {
                    TitleKey = "Str.Module.RolePermissions",
                    SourceLabelKey = "Str.Role",
                    Mode = TreeCheckMode.TwoState,
                    SourceItems = services => services.GetRequiredService<IPermissionStore>().GetAllRoles()
                        .Select(r => new SourceOption { Id = r.Id, Display = r.NameAr })
                        .ToList(),
                    BuildTree = (services, roleId) =>
                    {
                        var granted = services.GetRequiredService<IPermissionStore>().GetRolePermissions(roleId).ToHashSet();
                        var roots = PermissionTreeFactory.Build(services.GetRequiredService<IModuleRegistry>());
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
                        return Task.FromResult(Result.Ok());
                    },
                    ApplyRules = (roots, _) => SyncModules(roots, NodeCheckState.Checked),
                    Actions = new List<TreeCheckListAction>
                    {
                        new()
                        {
                            TextKey = "Str.SelectAll",
                            Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Checked)
                        },
                        new()
                        {
                            TextKey = "Str.ClearAll",
                            Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Unchecked)
                        },
                    }
                }
            });

            registry.Register(new ModuleDefinition
            {
                Key = "UserPermissions",
                TitleKey = "Str.Module.UserPermissions",
                PermissionPrefix = "Users",
                LayoutKind = LayoutKind.TreeCheckList,
                TreeCheckList = new TreeCheckListDefinition
                {
                    TitleKey = "Str.Module.UserPermissions",
                    SourceLabelKey = "Str.User",
                    Mode = TreeCheckMode.ThreeState,
                    SourceItems = services => services.GetRequiredService<IPermissionStore>().GetAllUsers()
                        .Select(u => new SourceOption { Id = u.Id, Display = u.DisplayName })
                        .ToList(),
                    BuildTree = (services, userId) =>
                    {
                        var admin = services.GetRequiredService<IPermissionAdminService>();
                        var roots = PermissionTreeFactory.Build(services.GetRequiredService<IModuleRegistry>());

                        foreach (var node in PermissionTreeFactory.KeyNodes(roots))
                        {
                            node.CheckState = admin.GetState(userId, node.Id) switch
                            {
                                PermissionState.Granted => NodeCheckState.Granted,
                                PermissionState.Revoked => NodeCheckState.Revoked,
                                _ => NodeCheckState.Inherited
                            };

                            if (node.CheckState != NodeCheckState.Inherited) continue;

                            node.InheritedAllowed = admin.IsInheritedFromRole(userId, node.Id);
                            node.InheritedHint = node.InheritedAllowed ? LocalizationService.Get("Str.Permissions.FromRole") : "";
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
                        return Task.FromResult(Result.Ok());
                    },
                    ApplyRules = (roots, _) => SyncModules(roots, NodeCheckState.Granted),
                    Actions = new List<TreeCheckListAction>
                    {
                        new()
                        {
                            TextKey = "Str.Permissions.ResetInherited",
                            Run = (_, __, nodes) => SetAll(nodes, NodeCheckState.Inherited)
                        },
                    }
                }
            });
        }

        public static void SyncModules(List<TreeNodeViewModel> roots, NodeCheckState on)
        {
            foreach (var module in roots) SyncModule(module, on);
        }

        private static void SyncModule(TreeNodeViewModel module, NodeCheckState on)
        {
            var view = module.Children.FirstOrDefault(child => child.Id == PermissionRules.ViewKeyOf(child.Id));
            if (view == null) return;

            var open = view.CheckState == on ||
                       (view.CheckState == NodeCheckState.Inherited && view.InheritedAllowed);

            foreach (var action in module.Children.Where(child => child != view))
            {
                action.IsCheckEnabled = open;
                if (!open) action.CheckState = view.CheckState;
            }

            module.CheckState = module.Children.Any(child => child.CheckState == on) ? on : view.CheckState;
        }

        private static void SetAll(List<TreeNodeViewModel> nodes, NodeCheckState state)
        {
            foreach (var module in nodes)
            {
                module.CheckState = state;
                foreach (var node in module.Children)
                {
                    node.IsCheckEnabled = true;
                    node.CheckState = state;
                }
            }
        }
    }
}
