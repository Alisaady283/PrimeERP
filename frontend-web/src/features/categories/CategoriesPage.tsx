import { Pencil, Plus, Trash2 } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Combobox } from "@/components/ui/Combobox";
import { confirm } from "@/components/ui/Confirm";
import { Dialog } from "@/components/ui/Dialog";
import { EmptyState } from "@/components/ui/EmptyState";
import { Switch, TextArea, TextField } from "@/components/ui/Field";
import { SearchBox } from "@/components/ui/SearchBox";
import { toast } from "@/components/ui/Toast";
import { Tree, type TreeCommand } from "@/components/ui/Tree";
import { ChildRows, DetailTile, TreePath, TreeSplit, TreeTools } from "@/components/ui/TreeSplit";
import { useCan, usePagePermission } from "@/features/auth/session";
import { messageOf } from "@/lib/api";
import { formatCount } from "@/lib/format";
import { useText } from "@/lib/i18n";
import type { CustomSpec } from "@/features/workspace/spec";
import { blankCategory, editCategory, useCategories, type CategoryDraft } from "@/features/categories/categories";

export default function CategoriesPage({ spec }: { spec: CustomSpec }) {
  const t = useText();
  const gate = usePagePermission(spec);
  const canCreate = useCan(`${gate}.Create`);
  const canEdit = useCan(`${gate}.Edit`);
  const canDelete = useCan(`${gate}.Delete`);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const [command, setCommand] = useState<TreeCommand>();
  const [draft, setDraft] = useState<CategoryDraft | null>(null);
  const [saving, setSaving] = useState(false);

  const { list, nodes, byId, childrenOf, path, options, refresh, ...categories } = useCategories(spec.key);
  const category = selected ? byId.get(selected) : undefined;
  const children = childrenOf(category?.id ?? null);

  const save = async () => {
    if (!draft) return;
    setSaving(true);
    try {
      await categories.save(draft);
      toast.success(t("Str.Success"));
      setDraft(null);
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  const remove = async (id: number) => {
    if (!(await confirm(t("Str.ConfirmDeleteMessage")))) return;
    try {
      await categories.remove(id);
      toast.success(t("Str.Success"));
      setDraft(null);
      if (String(id) === selected) setSelected(null);
    } catch (error) {
      toast.error(messageOf(error));
    }
  };

  return (
    <>
      <TreeSplit
        toolbar={
          <>
            <SearchBox value={search} onChange={setSearch} className="min-w-0 max-w-none" />
            <TreeTools onRefresh={() => void refresh()} refreshing={list.isFetching} onCommand={setCommand} />
          </>
        }
        tree={<Tree nodes={nodes} selectedId={selected} onSelect={(node) => setSelected(node.id)} filter={search} expandLevel={1} command={command} />}
      >
        <div className="space-y-5 p-5">
          <div className="flex flex-wrap items-center gap-3">
            <div className="min-w-0 flex-1">
              <TreePath items={[{ id: "root", label: t(spec.title) }, ...(category ? path(category).map((c) => ({ id: String(c.id), label: c.name })) : [])]} onPick={setSelected} />
            </div>
            {canCreate && (
              <Button icon={<Plus className="size-4" />} onClick={() => setDraft(blankCategory(category?.id ?? null))}>
                {t("Str.Category.Add")}
              </Button>
            )}
          </div>
          {category && (
            <>
              <div className="flex flex-wrap items-start gap-3">
                <div className="min-w-0 flex-1">
                  <h2 className="truncate text-xl font-bold text-ink">{category.name}</h2>
                  <div className="mt-2 flex gap-1.5">
                    <Badge tone={category.isActive ? "success" : "danger"}>{t(category.isActive ? "Str.Active" : "Str.Inactive")}</Badge>
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  {canEdit && (
                    <Button variant="secondary" icon={<Pencil className="size-4" />} onClick={() => setDraft(editCategory(category))}>
                      {t("Str.Category.Edit")}
                    </Button>
                  )}
                  {canDelete && (
                    <Button variant="ghost" className="text-danger hover:bg-danger-soft hover:text-danger" icon={<Trash2 className="size-4" />} onClick={() => remove(category.id)}>
                      {t("Str.Delete")}
                    </Button>
                  )}
                </div>
              </div>
              <div className="grid gap-3 sm:grid-cols-3">
                <DetailTile tone="brand" label={t("Str.Tree.Children")} value={formatCount(children.length)} />
                <DetailTile label={t("Str.Level")} value={formatCount(path(category).length)} />
                <DetailTile label={t("Str.ParentCategory")} value={category.parentName ?? "—"} />
              </div>
              {category.notes && <p className="rounded-xl bg-paper-soft px-4 py-3 text-sm text-ink-soft">{category.notes}</p>}
            </>
          )}
          {children.length > 0 ? (
            <ChildRows
              title={t("Str.Tree.Children")}
              rows={children}
              onOpen={(c) => setSelected(String(c.id))}
              columns={[
                { label: t("Str.Name"), cell: (c) => <span className="font-medium text-ink">{c.name}</span> },
                { label: t("Str.Tree.Children"), cell: (c) => formatCount(childrenOf(c.id).length), end: true },
                { label: t("Str.Status"), cell: (c) => <Badge tone={c.isActive ? "success" : "danger"}>{t(c.isActive ? "Str.Active" : "Str.Inactive")}</Badge>, end: true },
              ]}
            />
          ) : (
            !category && !list.isLoading && <EmptyState message={t("Str.NoData")} />
          )}
        </div>
      </TreeSplit>

      <Dialog
        open={!!draft}
        title={t(draft?.id ? "Str.Category.Edit" : "Str.Category.Add")}
        onClose={() => setDraft(null)}
        footer={
          <>
            {draft?.id && canDelete && (
              <Button variant="ghost" className="me-auto text-danger hover:bg-danger-soft hover:text-danger" icon={<Trash2 className="size-4" />} onClick={() => remove(draft.id!)}>
                {t("Str.Delete")}
              </Button>
            )}
            <Button variant="secondary" onClick={() => setDraft(null)}>{t("Str.Cancel")}</Button>
            <Button onClick={save} loading={saving} disabled={!draft?.name.trim()}>{t("Str.Save")}</Button>
          </>
        }
      >
        {draft && (
          <div className="space-y-4">
            <TextField label={t("Str.Name")} required value={draft.name} maxLength={200} onChange={(e) => setDraft({ ...draft, name: e.target.value })} />
            {!draft.id && (
              <Combobox
                label={t("Str.ParentCategory")}
                value={draft.parentId}
                options={options}
                onChange={(v) => setDraft({ ...draft, parentId: v === null ? null : Number(v) })}
              />
            )}
            <TextArea label={t("Str.Notes")} value={draft.notes} onChange={(e) => setDraft({ ...draft, notes: e.target.value })} />
            <Switch label={t("Str.Active")} checked={draft.isActive} onChange={(isActive) => setDraft({ ...draft, isActive })} />
          </div>
        )}
      </Dialog>
    </>
  );
}
