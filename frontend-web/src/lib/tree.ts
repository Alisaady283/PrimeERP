import type { ReactNode } from "react";

/** الشجرة — حسابها مشتركٌ بين الويب والأندرويد، والعرض لكلٍّ */

export type TreeNode = { id: string; label: string; hint?: string; children?: TreeNode[]; extra?: ReactNode };

/** معرّفات العقد ذات الفروع */
export const parents = (list: TreeNode[]): string[] => list.flatMap((n) => (n.children?.length ? [n.id, ...parents(n.children)] : []));

/** أوراق العقدة */
export const leafIds = (node: TreeNode): string[] => (node.children?.length ? node.children.flatMap(leafIds) : [node.id]);

/** العقدة أو أحد فروعها يطابق البحث */
export const matches = (node: TreeNode, q: string): boolean =>
  node.label.toLowerCase().includes(q) || (node.hint?.toLowerCase().includes(q) ?? false) || (node.children ?? []).some((c) => matches(c, q));

/** طريق المحدَّد: أسلافه من الجذر */
export const pathTo = (list: TreeNode[], id: string): string[] | null => {
  for (const node of list) {
    if (node.id === id) return [];
    const below = node.children ? pathTo(node.children, id) : null;
    if (below) return [node.id, ...below];
  }
  return null;
};

/** حال التأشير: لا شيء · بعضه · كله */
export const checkState = (node: TreeNode, checked: Set<string>) => {
  const ids = leafIds(node);
  const on = ids.filter((id) => checked.has(id)).length;
  return on === 0 ? "none" : on === ids.length ? "all" : "some";
};
