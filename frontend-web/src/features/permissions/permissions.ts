import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { api } from "@/lib/api";

/** الصلاحيات (الأدوار · المستخدمون) — منطقها مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

export type PermissionNode = { key: string; label: string; children: PermissionNode[] };
export type PermissionState = 0 | 1 | 2;
export type UserPermission = { key: string; state: PermissionState; fromRole: boolean };

/** شجرة الصلاحيات: مجموعاتٌ وأبناؤها، مصفّاةً بالبحث */
export function usePermissionGroups(search: string) {
  const tree = useQuery({ queryKey: ["permissions", "tree"], queryFn: ({ signal }) => api.get<PermissionNode[]>("/permissions/tree", undefined, signal), staleTime: Infinity });
  const q = search.trim().toLowerCase();
  const groups = (tree.data ?? [])
    .map((g) => ({ ...g, children: g.children.filter((c) => !q || c.label.toLowerCase().includes(q) || g.label.toLowerCase().includes(q)) }))
    .filter((g) => g.children.length > 0);
  return { loading: tree.isLoading, groups, searching: !!q };
}

/** صلاحيات الدور: الممنوح، وتبديل مفتاحٍ أو مجموعة، والحفظ */
export function useRolePermissions(roleId: number | null) {
  const role = useQuery({ queryKey: ["permissions", "role", roleId], enabled: !!roleId, queryFn: ({ signal }) => api.get<string[]>(`/permissions/roles/${roleId}`, undefined, signal) });
  const [granted, setGranted] = useState<Set<string>>(new Set());

  useEffect(() => {
    setGranted(new Set(role.data ?? []));
  }, [role.data]);

  const toggle = (keys: string[], on: boolean) =>
    setGranted((current) => {
      const next = new Set(current);
      keys.forEach((k) => (on ? next.add(k) : next.delete(k)));
      return next;
    });

  /** المجموعة ممنوحةٌ كلها */
  const allOf = (group: PermissionNode) => group.children.every((c) => granted.has(c.key));
  const toggleGroup = (group: PermissionNode) => toggle(group.children.map((c) => c.key), !allOf(group));

  const save = async () => {
    await api.put(`/permissions/roles/${roleId}`, [...granted]);
    await role.refetch();
  };

  return { granted, toggle, allOf, toggleGroup, save };
}

/** صلاحيات المستخدم: موروثة · ممنوحة · مسحوبة لكل مفتاح، والإرجاع للموروث، والحفظ */
export function useUserPermissions(userId: number | null) {
  const user = useQuery({ queryKey: ["permissions", "user", userId], enabled: !!userId, queryFn: ({ signal }) => api.get<UserPermission[]>(`/permissions/users/${userId}`, undefined, signal) });
  const [states, setStates] = useState<Record<string, UserPermission>>({});

  useEffect(() => {
    setStates(Object.fromEntries((user.data ?? []).map((p) => [p.key, p])));
  }, [user.data]);

  const set = (key: string, state: PermissionState) => setStates((s) => (s[key] ? { ...s, [key]: { ...s[key], state } } : s));
  const resetInherited = () => setStates((s) => Object.fromEntries(Object.entries(s).map(([k, p]) => [k, { ...p, state: 0 as PermissionState }])));

  const save = async () => {
    await api.put(`/permissions/users/${userId}`, Object.values(states));
    await user.refetch();
  };

  return { states, set, resetInherited, save };
}

/** خيارات حال المستخدم */
export const stateItems = (t: (key: string) => string, fromRole: boolean) => [
  { value: 0 as PermissionState, label: `${t("Str.Permissions.Inherited")}${fromRole ? " ✓" : ""}` },
  { value: 1 as PermissionState, label: t("Str.Permissions.Grant") },
  { value: 2 as PermissionState, label: t("Str.Permissions.Revoke") },
];
