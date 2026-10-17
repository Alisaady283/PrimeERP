import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useMemo } from "react";
import type { TreeNode } from "@/lib/tree";
import { api } from "@/lib/api";

/** التصنيفات (الأصناف · العلامات · فئات الأصول) — منطقها مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

export type Category = { id: number; name: string; parentId: number | null; parentName: string | null; isActive: boolean; notes: string | null };
export type CategoryDraft = { id: number | null; name: string; parentId: number | null; isActive: boolean; notes: string };

const moduleOf: Record<string, string> = { Categories: "Products", Brands: "Brands", AssetCategories: "AssetCategories" };

const build = (list: Category[], parent: number | null): TreeNode[] =>
  list.filter((c) => c.parentId === parent).map((c) => ({ id: String(c.id), label: c.name, children: build(list, c.id) }));

export const blankCategory = (parentId: number | null): CategoryDraft => ({ id: null, name: "", parentId, isActive: true, notes: "" });
export const editCategory = (c: Category): CategoryDraft => ({ id: c.id, name: c.name, parentId: c.parentId, isActive: c.isActive, notes: c.notes ?? "" });

/** القائمة وشجرتها وفهرسها وفروع كلّ عقدة وطريقها، والحفظ والحذف */
export function useCategories(pageKey: string) {
  const client = useQueryClient();
  const module = moduleOf[pageKey] ?? pageKey;
  const list = useQuery({ queryKey: ["categories", module], queryFn: ({ signal }) => api.get<Category[]>(`/categories/${module}`, undefined, signal) });
  const nodes = useMemo(() => build(list.data ?? [], null), [list.data]);
  const byId = useMemo(() => new Map((list.data ?? []).map((c) => [String(c.id), c])), [list.data]);
  const childrenOf = (id: number | null) => (list.data ?? []).filter((c) => c.parentId === id);
  const path = (c: Category): Category[] => (c.parentId && byId.has(String(c.parentId)) ? [...path(byId.get(String(c.parentId))!), c] : [c]);
  const options = (list.data ?? []).map((c) => ({ value: c.id, label: c.name }));
  /** البحث بالاسم في الشجرة كلها */
  const find = (term: string) => {
    const q = term.trim().toLowerCase();
    return q ? (list.data ?? []).filter((c) => c.name.toLowerCase().includes(q)) : [];
  };
  const refresh = () => Promise.all([client.invalidateQueries({ queryKey: ["categories", module] }), client.invalidateQueries({ queryKey: ["options"] })]);

  const save = async (draft: CategoryDraft) => {
    const body = { name: draft.name, parentId: draft.parentId, isActive: draft.isActive, notes: draft.notes };
    if (draft.id) await api.put(`/categories/${module}/${draft.id}`, body);
    else await api.post(`/categories/${module}`, body);
    await refresh();
  };

  const remove = async (id: number) => {
    await api.delete(`/categories/${module}/${id}`);
    await refresh();
  };

  return { list, nodes, byId, childrenOf, path, options, find, refresh, save, remove };
}
