import { ChevronDown } from "lucide-react";
import { useState, type ReactNode } from "react";
import { Card } from "@/components/ui/Card";
import { Combobox } from "@/components/ui/Combobox";
import { PageHeader } from "@/components/ui/PageHeader";
import { SearchBox } from "@/components/ui/SearchBox";
import { Skeleton } from "@/components/ui/Skeleton";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";
import { useOptions } from "@/features/workspace/data";
import { usePermissionGroups, type PermissionNode } from "@/features/permissions/permissions";

type Props = {
  sourceLabel: string;
  sourcePage: string;
  sourceId: number | null;
  onSource: (id: number | null) => void;
  actions?: ReactNode;
  renderKey: (key: string) => ReactNode;
  renderGroup?: (group: PermissionNode) => ReactNode;
};

export function PermissionScreen({ sourceLabel, sourcePage, sourceId, onSource, actions, renderKey, renderGroup }: Props) {
  const t = useText();
  const [search, setSearch] = useState("");
  const [open, setOpen] = useState<Record<string, boolean>>({});
  const sources = useOptions({ page: sourcePage });
  const tree = usePermissionGroups(search);

  return (
    <div className="flex h-full flex-col">
      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <PageHeader className="items-center border-b border-line px-3 py-2.5" actions={actions}>
          <Combobox dense className="w-64" placeholder={t(sourceLabel)} value={sourceId} options={sources.data ?? []} onChange={(v) => onSource(v === null ? null : Number(v))} />
          <SearchBox value={search} onChange={setSearch} />
        </PageHeader>
        <div className="min-h-0 flex-1 overflow-y-auto p-4">
          {!sourceId ? null : tree.loading ? (
            <Skeleton className="h-96" />
          ) : (
            <div className="grid gap-3 xl:grid-cols-2">
              {tree.groups.map((group) => {
                  const expanded = tree.searching || open[group.key];
                  return (
                    <div key={group.key} className="rounded-2xl border border-line">
                      <div className="flex items-center gap-3 px-4 py-3">
                        <button type="button" onClick={() => setOpen((o) => ({ ...o, [group.key]: !expanded }))} className="flex flex-1 items-center gap-2 text-start font-semibold text-ink">
                          <ChevronDown className={cn("size-4 text-ink-faint transition-transform", !expanded && "-rotate-90 rtl:rotate-90")} />
                          {group.label}
                        </button>
                        {renderGroup?.(group)}
                      </div>
                      {expanded && (
                        <div className="space-y-1 border-t border-line p-2">
                          {group.children.map((child) => (
                            <div key={child.key} className="flex items-center justify-between gap-3 rounded-lg px-3 py-1.5 hover:bg-paper">
                              <span className="text-sm text-ink-soft">{child.label}</span>
                              {renderKey(child.key)}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}
            </div>
          )}
        </div>
      </Card>
    </div>
  );
}
