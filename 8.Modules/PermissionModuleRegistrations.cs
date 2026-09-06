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
                    ApplyRules = roots => SyncModules(roots, NodeCheckState.Checked),
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

                            if (node.CheckState != NodeCheckState.Inherited) continue;

                            node.InheritedAllowed = admin.IsInheritedFromRole(userId, node.Id);
                            node.InheritedHint = node.InheritedAllowed ? "(من الدور)" : "";
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
                    ApplyRules = roots => SyncModules(roots, NodeCheckState.Granted),
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

        private static void SyncModule(TreeNodeViewModel module, NodeCheckState on)
        {
            var view = module.Children.FirstOrDefault(child => child.Id == PermissionRules.ViewKeyOf(child.Id));
            if (view == null) return;

            // البوّابة تُقاس بالعرض الفعّال: منحٌ صريح، أو موروث يسمح به الدور. المستخدم يرى القسم في
            // الحالتين، فحجب باقي إجراءاته في الثانية منعٌ بلا سبب.
            var open = view.CheckState == on ||
                       (view.CheckState == NodeCheckState.Inherited && view.InheritedAllowed);

            foreach (var action in module.Children.Where(child => child != view))
            {
                // مفتوح = يُسمح بالتأشير والإلغاء، لا أن يُؤشَّر. مغلق = يُلغى ويُعطَّل.
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
                    // التعطيل يُرفع أولاً وإلا بقيت عقدة معطَّلة على حالتها القديمة بعد «تحديد الكل».
                    node.IsCheckEnabled = true;
                    node.CheckState = state;
                }
            }
        }
    }
}
