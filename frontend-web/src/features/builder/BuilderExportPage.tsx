import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FolderDown, Plus, Power, Save, Trash2 } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { Combobox } from "@/components/ui/Combobox";
import { confirm } from "@/components/ui/Confirm";
import { Switch, TextField } from "@/components/ui/Field";
import { PageHeader } from "@/components/ui/PageHeader";
import { SearchBox } from "@/components/ui/SearchBox";
import { toast } from "@/components/ui/Toast";
import { Tree, type TreeNode } from "@/components/ui/Tree";
import { sections } from "@/catalog/navigation";
import { lookupLists, pageByKey } from "@/catalog/pages";
import { useInterface } from "@/features/auth/interface";
import { useCan } from "@/features/auth/session";
import { FormDialog } from "@/features/workspace/FormDialog";
import { dateOnly } from "@/features/workspace/format";
import { api, messageOf } from "@/lib/api";
import { pickFolder, unpackInto } from "@/lib/folder";
import { useText } from "@/lib/i18n";

type License = {
  id: number; customerName: string; location: string | null; serial: string; version: string | null; updatedAt: string;
  moduleKeys: string[]; simplified: boolean; english: boolean; isActivated: boolean; isActive: boolean;
};

const settingsTabs = ["Company", "Financial", "Accounts", "Documents", "UI", "Print", "Backup", "Security", "HR", "System"];
const ALWAYS = "Accounting";

const leavesOf = (node: TreeNode): string[] => (node.children?.length ? node.children.flatMap(leavesOf) : [node.id]);

/** نافذة البناء */
export default function BuilderExportPage() {
  const t = useText();
  const client = useQueryClient();
  const settings = useInterface();
  const canCreate = useCan("BuilderExport.Create");
  const canEdit = useCan("BuilderExport.Edit");
  const canDelete = useCan("BuilderExport.Delete");
  const customers = useQuery({ queryKey: ["builder", "customers"], queryFn: ({ signal }) => api.get<License[]>("/builder/customers", undefined, signal) });
  const [id, setId] = useState<number | null>(null);
  const [checked, setChecked] = useState<Set<string>>(new Set());
  const [notes, setNotes] = useState({ version: "", simplified: false, english: false });
  const [filter, setFilter] = useState("");
  const [busy, setBusy] = useState<string | null>(null);
  const [progress, setProgress] = useState<number | null>(null);
  const [adding, setAdding] = useState(false);

  const license = customers.data?.find((c) => c.id === id) ?? null;
  const bothLanguages = (settings?.arabic ?? true) && (settings?.english ?? true);

  useEffect(() => {
    const first = customers.data?.[0];
    if (id === null && first) setId(first.id);
  }, [customers.data, id]);

  const tree = useMemo<TreeNode[]>(() => {
    const simplified = license?.simplified ?? false;
    return sections
      .filter((s) => s.key !== "Builder")
      .map((s) => ({
        id: s.key,
        label: t(`Str.Nav.${s.key}`),
        children: s.pages
          .map((key) => pageByKey.get(key)!)
          .filter((p) => !(simplified && p.fullCycleOnly))
          .map((p) => ({
            id: p.key,
            label: t(p.title),
            children:
              p.key === "Settings"
                ? settingsTabs.map((c) => ({ id: `Settings.${c}`, label: t(`Str.Settings.${c}`) }))
                : p.key === "HrLists"
                  ? lookupLists.map((l) => ({ id: l.key, label: t(l.title) }))
                  : undefined,
          })),
      }))
      .filter((s) => s.children.length > 0);
  }, [license?.simplified, t]);

  const always = useMemo(() => new Set(tree.filter((s) => s.id === ALWAYS).flatMap(leavesOf)), [tree]);

  useEffect(() => {
    if (!license) return;
    setNotes({ version: license.version ?? "", simplified: license.simplified, english: license.english });
    const keys = new Set(license.moduleKeys);
    const all = tree.flatMap(leavesOf);
    const anyTab = license.moduleKeys.some((k) => k.startsWith("Settings."));
    setChecked(new Set(keys.size === 0 ? all : all.filter((leaf) => always.has(leaf) || keys.has(leaf) || (leaf.startsWith("Settings.") && keys.has("Settings") && !anyTab))));
  }, [license, tree]);

  const toggle = (node: TreeNode, on: boolean) =>
    setChecked((current) => {
      const next = new Set(current);
      for (const leaf of leavesOf(node)) if (!always.has(leaf)) (on ? next.add(leaf) : next.delete(leaf));
      return next;
    });

  const keys = () =>
    tree.flatMap((section) =>
      (section.children ?? []).flatMap((page) => {
        const leaves = leavesOf(page).filter((l) => checked.has(l));
        if (leaves.length === 0) return [];
        return page.children?.length ? [page.id, ...leaves] : [page.id];
      }),
    );

  const pages = tree.flatMap((s) => s.children ?? []);
  const granted = pages.filter((p) => leavesOf(p).some((l) => checked.has(l))).length;

  const guard = async (name: string, work: () => Promise<unknown>) => {
    setBusy(name);
    try {
      await work();
      toast.success(t("Str.Success"));
      void client.invalidateQueries({ queryKey: ["builder"] });
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setBusy(null);
      setProgress(null);
    }
  };

  const save = () =>
    guard("save", async () => {
      if (!license) return;
      if (notes.version !== (license.version ?? "")) await api.put(`/builder/customers/${license.id}/version`, { version: notes.version });
      if (notes.simplified !== license.simplified) await api.put(`/builder/customers/${license.id}/simplified`, { value: notes.simplified });
      if (notes.english !== license.english) await api.put(`/builder/customers/${license.id}/english`, { value: notes.english });
      await api.put(`/builder/customers/${license.id}/manifest`, keys());
    });

  /** ملف الخادم في المجلد */
  const toFolder = (name: string, fetchZip: () => Promise<ArrayBuffer>, fileName: string) =>
    guard(name, async () => {
      const folder = await pickFolder();
      setProgress(0);
      await unpackInto(folder, await fetchZip(), fileName, setProgress);
    });

  const options = (customers.data ?? []).map((c) => ({
    value: c.id,
    label: (c.location ? `${c.customerName} — ${c.location}` : c.customerName) + (c.isActive ? "" : ` (${t("Str.Builder.Stopped")})`),
    hint: c.serial,
  }));

  return (
    <div className="flex h-full flex-col gap-4">
      <Card className="space-y-4 p-4">
        <PageHeader
          actions={
            <>
              {canCreate && (
                <Button variant="secondary" icon={<Plus className="size-4" />} onClick={() => setAdding(true)}>
                  {t("Str.Builder.NewSerial")}
                </Button>
              )}
              {canEdit && (
                <Button icon={<Save className="size-4" />} onClick={() => void save()} loading={busy === "save"} disabled={!license}>
                  {t("Str.Save")}
                </Button>
              )}
            </>
          }
        >
          <div className="w-80">
            <Combobox label={t("Str.Builder.Customer")} value={id} options={options} onChange={(v) => setId(v === null ? null : Number(v))} />
          </div>
          <TextField frameClassName="w-60" label={t("Str.Builder.Serial")} value={license?.serial ?? ""} readOnly />
          <TextField frameClassName="w-36" label={t("Str.Builder.Version")} value={notes.version} onChange={(e) => setNotes((n) => ({ ...n, version: e.target.value }))} />
          <TextField frameClassName="w-36" label={t("Str.Builder.VersionDate")} value={license?.version ? dateOnly(license.updatedAt) : ""} readOnly />
          <div className="flex h-11 items-center gap-5">
            <Switch label={t("Str.Builder.Simplified")} checked={notes.simplified} onChange={(v) => setNotes((n) => ({ ...n, simplified: v }))} />
            {bothLanguages && <Switch label={t("Str.Builder.English")} checked={notes.english} onChange={(v) => setNotes((n) => ({ ...n, english: v }))} />}
          </div>
        </PageHeader>
        <div className="flex flex-wrap items-center gap-2 border-t border-line pt-4">
          {canCreate && (
            <Button
              icon={<FolderDown className="size-4" />}
              disabled={!license}
              loading={busy === "create"}
              onClick={() => void toFolder("create", () => api.file("POST", `/builder/customers/${license!.id}/edition`, { keys: keys() }), "PrimeERP.zip")}
            >
              {t("Str.Builder.CreateProgram")}
            </Button>
          )}
          <Button
            variant="secondary"
            icon={<Download className="size-4" />}
            disabled={!license}
            loading={busy === "installer"}
            onClick={() => void toFolder("installer", () => api.file("GET", `/builder/customers/${license!.id}/installer`), "PrimeERP.Setup.zip")}
          >
            {t("Str.Builder.Installer")}
          </Button>
          {canEdit && (
            <Button variant="secondary" icon={<Power className="size-4" />} disabled={!license} loading={busy === "toggle"} onClick={() => void guard("toggle", () => api.post(`/builder/customers/${license!.id}/toggle`, {}))}>
              {t("Str.Builder.ToggleCustomer")}
            </Button>
          )}
          {canDelete && (
            <Button
              variant="ghost"
              className="text-danger hover:bg-danger-soft hover:text-danger"
              icon={<Trash2 className="size-4" />}
              disabled={!license}
              loading={busy === "delete"}
              onClick={async () => {
                if (!license || !(await confirm(t("Str.Builder.DeleteCustomerConfirm", license.customerName)))) return;
                await guard("delete", async () => {
                  await api.delete(`/builder/customers/${license.id}`);
                  setId(null);
                });
              }}
            >
              {t("Str.Builder.DeleteCustomer")}
            </Button>
          )}
          {progress !== null && (
            <div className="ms-auto flex w-64 items-center gap-2 text-sm text-ink-tertiary">
              <div className="h-2 flex-1 overflow-hidden rounded-full bg-paper">
                <div className="h-full bg-brand transition-[width]" style={{ width: `${progress}%` }} />
              </div>
              <span className="tabular-nums">{progress}%</span>
            </div>
          )}
        </div>
      </Card>

      <Card className="flex min-h-0 flex-1 flex-col overflow-hidden">
        <PageHeader className="items-center border-b border-line px-3 py-2.5" meta={t("Str.Permissions.Granted", granted, pages.length)}>
          <SearchBox value={filter} onChange={setFilter} />
          <Button variant="ghost" size="sm" onClick={() => setChecked(new Set(tree.flatMap(leavesOf)))}>
            {t("Str.SelectAll")}
          </Button>
          <Button variant="ghost" size="sm" onClick={() => setChecked(new Set(always))}>
            {t("Str.ClearAll")}
          </Button>
        </PageHeader>
        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <Tree nodes={tree} checked={checked} onToggle={toggle} filter={filter} expandLevel={2} />
        </div>
      </Card>

      <FormDialog
        open={adding}
        title={t("Str.Builder.NewSerial")}
        fields={[
          { key: "customerName", label: "Str.Builder.Customer", kind: "text", required: true, max: 200 },
          { key: "location", label: "Str.Builder.Location", kind: "text", max: 200 },
          { key: "simplified", label: "Str.Builder.Simplified", kind: "switch" },
          ...(bothLanguages ? [{ key: "english", label: "Str.Builder.English", kind: "switch" as const }] : []),
        ]}
        onSubmit={async (values) => {
          const created = await api.post<License>("/builder/customers", values);
          void client.invalidateQueries({ queryKey: ["builder"] });
          setId(created.id);
        }}
        onClose={() => setAdding(false)}
      />
    </div>
  );
}
