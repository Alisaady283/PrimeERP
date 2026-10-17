import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import { api } from "@/lib/api";

/** دليل الحسابات — منطقه مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

export type Account = {
  id: number; code: string; name: string; parentId: number | null; level: number; isLeaf: boolean; isActive: boolean; type: number; typeName: string; balance: number;
  isSystem: boolean; notes?: string; createdAt?: string; updatedAt?: string; children: Account[];
};

export type AccountDraft = { id: number | null; parentId: number | null; name: string; isLeaf: boolean; isActive: boolean; notes: string };

/** تاريخ ووقت */
export const stamp = (value?: string) => (value ? value.replace("T", " ").slice(0, 16) : "—");

const flatten = (accounts: Account[]): Account[] => accounts.flatMap((a) => [a, ...flatten(a.children)]);

export const newAccount = (parent: Account): AccountDraft => ({ id: null, parentId: parent.id, name: "", isLeaf: true, isActive: true, notes: "" });
export const editAccount = (a: Account): AccountDraft => ({ id: a.id, parentId: a.parentId, name: a.name, isLeaf: a.isLeaf, isActive: a.isActive, notes: a.notes ?? "" });

/** الشجرة مصفّاةً بالنوع، وفهرسها، وطريق الحساب، والحفظ والحذف */
export function useAccounts(type: number | null) {
  const client = useQueryClient();
  const tree = useQuery({ queryKey: ["accounts", "tree"], queryFn: ({ signal }) => api.get<Account[]>("/accounts/tree", undefined, signal) });
  const roots = useMemo(() => (tree.data ?? []).filter((a) => type === null || a.type === type), [tree.data, type]);
  const types = (tree.data ?? []).map((a) => ({ value: a.type as number | null, label: a.typeName }));
  const all = useMemo(() => flatten(roots), [roots]);
  const byId = useMemo(() => new Map(flatten(tree.data ?? []).map((a) => [String(a.id), a])), [tree.data]);
  const path = (a: Account): Account[] => (a.parentId && byId.has(String(a.parentId)) ? [...path(byId.get(String(a.parentId))!), a] : [a]);
  const refresh = () => client.invalidateQueries({ queryKey: ["accounts"] });
  /** البحث بالاسم أو الرمز في الشجرة كلها */
  const find = (term: string) => {
    const q = term.trim().toLowerCase();
    return q ? all.filter((a) => a.name.toLowerCase().includes(q) || a.code.toLowerCase().includes(q)) : [];
  };

  const save = async (draft: AccountDraft) => {
    if (draft.id) await api.put(`/pages/Accounts/${draft.id}`, { id: draft.id, name: draft.name, isLeaf: draft.isLeaf, isActive: draft.isActive, notes: draft.notes });
    else await api.post("/pages/Accounts", { parentId: draft.parentId, name: draft.name, isLeaf: draft.isLeaf, isActive: draft.isActive, notes: draft.notes });
    await refresh();
  };

  const remove = async (id: number) => {
    await api.delete(`/pages/Accounts/${id}`);
    await refresh();
  };

  return { tree, roots, types, byId, path, find, refresh, save, remove };
}
