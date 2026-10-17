import { ChevronLeft, ChevronRight, Folder, FolderOpen, Minus, Check } from "lucide-react";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { cn } from "@/lib/cn";
import { useLanguage } from "@/lib/i18n";
import { checkState, matches, parents, pathTo, type TreeNode } from "@/lib/tree";

export type { TreeNode };

type TreeProps = {
  nodes: TreeNode[];
  selectedId?: string | null;
  onSelect?: (node: TreeNode) => void;
  checked?: Set<string>;
  onToggle?: (node: TreeNode, checked: boolean) => void;
  filter?: string;
  expandLevel?: number;
  /** توسيع الكل أو طيّه */
  command?: TreeCommand;
};

export type TreeCommand = { expand: boolean; at: number };

export function Tree({ nodes, selectedId, onSelect, checked, onToggle, filter = "", expandLevel = 1, command }: TreeProps) {
  const [open, setOpen] = useState<Record<string, boolean>>({});
  const rtl = useLanguage((s) => s.language) === "ar";
  const q = filter.trim().toLowerCase();
  useEffect(() => {
    if (command) setOpen(Object.fromEntries(parents(nodes).map((id) => [id, command.expand])));
  }, [command]);
  const ancestors = useMemo(() => new Set(selectedId ? pathTo(nodes, selectedId) ?? [] : []), [nodes, selectedId]);

  const render = (list: TreeNode[], depth: number): ReactNode =>
    list
      .filter((n) => !q || matches(n, q))
      .map((node) => {
        const hasChildren = !!node.children?.length;
        const expanded = q ? true : open[node.id] ?? (ancestors.has(node.id) || depth < expandLevel);
        const state = checked ? checkState(node, checked) : null;
        const Arrow = rtl ? ChevronLeft : ChevronRight;
        const Icon = expanded ? FolderOpen : Folder;
        const selected = selectedId === node.id;

        return (
          <li key={node.id}>
            <div
              onClick={() => onSelect?.(node)}
              className={cn(
                "group flex h-8 cursor-pointer items-center gap-1.5 rounded-md pe-2 ps-1 text-[13px] transition-colors",
                selected ? "bg-brand-soft font-medium text-brand" : "text-ink-soft hover:bg-paper hover:text-ink",
              )}
            >
              <button
                type="button"
                onClick={(e) => { e.stopPropagation(); setOpen((o) => ({ ...o, [node.id]: !expanded })); }}
                className={cn("grid size-5 shrink-0 place-items-center rounded text-ink-faint hover:text-ink", !hasChildren && "invisible")}
              >
                <Arrow className={cn("size-3.5 transition-transform", expanded && (rtl ? "-rotate-90" : "rotate-90"))} />
              </button>
              {state && (
                <button
                  type="button"
                  onClick={(e) => { e.stopPropagation(); onToggle?.(node, state !== "all"); }}
                  className={cn("grid size-4 shrink-0 place-items-center rounded border transition-colors", state === "none" ? "border-line-strong bg-card" : "border-brand bg-brand text-on-field")}
                >
                  {state === "all" && <Check className="size-3" />}
                  {state === "some" && <Minus className="size-3" />}
                </button>
              )}
              {hasChildren && <Icon className={cn("size-4 shrink-0", selected ? "text-brand" : "text-ink-faint")} />}
              <span className="flex-1 truncate">{node.label}</span>
              {node.hint && <span className="font-mono text-[11px] text-ink-faint">{node.hint}</span>}
              {node.extra}
            </div>
            {hasChildren && expanded && <ul className="ms-[13px] border-s border-line ps-1.5">{render(node.children!, depth + 1)}</ul>}
          </li>
        );
      });

  const tree = useMemo(() => render(nodes, 0), [nodes, open, selectedId, checked, q, rtl, ancestors]);
  return <ul className="select-none space-y-px">{tree}</ul>;
}

