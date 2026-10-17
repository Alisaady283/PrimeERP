import { Pencil, Plus, Trash2 } from "lucide-react";
import { useMemo, useState } from "react";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { confirm } from "@/components/ui/Confirm";
import { Dialog } from "@/components/ui/Dialog";
import { EmptyState } from "@/components/ui/EmptyState";
import { Switch, TextArea, TextField } from "@/components/ui/Field";
import { SearchBox } from "@/components/ui/SearchBox";
import { Skeleton } from "@/components/ui/Skeleton";
import { toast } from "@/components/ui/Toast";
import { Tree, type TreeCommand, type TreeNode } from "@/components/ui/Tree";
import { ChildRows, DetailTile, TreeChips, TreePath, TreeSplit, TreeTools } from "@/components/ui/TreeSplit";
import { useCan } from "@/features/auth/session";
import { messageOf } from "@/lib/api";
import { formatCount, formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import type { CustomSpec } from "@/features/workspace/spec";
import { editAccount, newAccount, stamp, useAccounts, type Account, type AccountDraft } from "@/features/accounts/accounts";

const toNodes = (accounts: Account[], balance: boolean): TreeNode[] =>
  accounts.map((a) => ({
    id: String(a.id),
    label: a.name,
    hint: a.code,
    extra: balance ? <span className="ms-1 text-[11px] tabular-nums text-ink-tertiary">{formatMoney(a.balance)}</span> : undefined,
    children: a.children.length ? toNodes(a.children, balance) : undefined,
  }));

export default function AccountsPage({ spec }: { spec: CustomSpec }) {
  const t = useText();
  const canCreate = useCan("Accounts.Create");
  const canEdit = useCan("Accounts.Edit");
  /** حساب النظام (مربوطٌ بالإعدادات) يُعدَّل بصلاحية إعدادات النظام كما يقبله الخادم، ولا يُحذف */
  const canEditSystem = useCan("Settings.System");
  const canDelete = useCan("Accounts.Delete");
  const canBalance = useCan("Accounts.Column.Balance");
  const [type, setType] = useState<number | null>(null);
  const [command, setCommand] = useState<TreeCommand>();
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const [draft, setDraft] = useState<AccountDraft | null>(null);
  const [saving, setSaving] = useState(false);

  const { tree, roots, types, byId, path, refresh, ...accounts } = useAccounts(type);
  const nodes = useMemo(() => toNodes(roots, canBalance), [roots, canBalance]);
  const account = selected ? byId.get(selected) : undefined;

  const save = async () => {
    if (!draft) return;
    setSaving(true);
    try {
      await accounts.save(draft);
      toast.success(t("Str.Success"));
      setDraft(null);
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setSaving(false);
    }
  };

  const remove = async () => {
    if (!account || !(await confirm(t("Str.ConfirmDeleteMessage")))) return;
    try {
      await accounts.remove(account.id);
      toast.success(t("Str.Success"));
      setSelected(account.parentId ? String(account.parentId) : null);
    } catch (error) {
      toast.error(messageOf(error));
    }
  };

  const children = account ? account.children : roots;

  return (
    <>
      <TreeSplit
        toolbar={
          <>
            <SearchBox value={search} onChange={setSearch} className="min-w-0 max-w-none" />
            <TreeTools onRefresh={() => void refresh()} refreshing={tree.isFetching} onCommand={setCommand} />
          </>
        }
        filters={<TreeChips items={[{ value: null, label: t("Str.All") }, ...types]} value={type} onChange={setType} />}
        tree={tree.isLoading ? <Skeleton className="h-96" /> : <Tree nodes={nodes} selectedId={selected} onSelect={(n) => setSelected(n.id)} filter={search} expandLevel={2} command={command} />}
      >
        <div className="space-y-5 p-5">
          <TreePath items={[{ id: "root", label: t(spec.title) }, ...(account ? path(account).map((a) => ({ id: String(a.id), label: a.name })) : [])]} onPick={setSelected} />
          {account ? (
            <>
              <div className="flex flex-wrap items-start gap-3">
                <div className="min-w-0 flex-1">
                  <p className="font-mono text-xs text-ink-tertiary">{account.code}</p>
                  <h2 className="truncate text-xl font-bold text-ink">{account.name}</h2>
                  <div className="mt-2 flex flex-wrap gap-1.5">
                    <Badge tone="brand">{account.typeName}</Badge>
                    {account.isLeaf && <Badge tone="success">{t("Str.AcceptsEntries")}</Badge>}
                    {!account.isActive && <Badge tone="danger">{t("Str.Inactive")}</Badge>}
                  </div>
                </div>
                <div className="flex flex-wrap gap-2">
                  {canCreate && !account.isLeaf && (
                    <Button icon={<Plus className="size-4" />} onClick={() => setDraft(newAccount(account))}>
                      {t("Str.Accounts.Add")}
                    </Button>
                  )}
                  {canEdit && (!account.isSystem || canEditSystem) && (
                    <Button variant="secondary" icon={<Pencil className="size-4" />} onClick={() => setDraft(editAccount(account))}>
                      {t("Str.Accounts.Edit")}
                    </Button>
                  )}
                  {canDelete && !account.isSystem && (
                    <Button variant="ghost" className="text-danger hover:bg-danger-soft hover:text-danger" icon={<Trash2 className="size-4" />} onClick={remove}>
                      {t("Str.Delete")}
                    </Button>
                  )}
                </div>
              </div>
              <div className="grid gap-3 sm:grid-cols-3">
                {canBalance && <DetailTile tone="brand" label={t("Str.Balance")} value={formatMoney(account.balance)} />}
                <DetailTile label={t("Str.Level")} value={formatCount(account.level)} />
                <DetailTile label={t("Str.Tree.Children")} value={formatCount(account.children.length)} />
              </div>
              {account.notes && <p className="rounded-xl bg-paper-soft px-4 py-3 text-sm text-ink-soft">{account.notes}</p>}
              <p className="flex flex-wrap gap-x-5 gap-y-1 text-xs text-ink-tertiary">
                <span>{t("Str.CreatedAt")}: <b className="font-medium tabular-nums text-ink-soft">{stamp(account.createdAt)}</b></span>
                <span>{t("Str.UpdatedAt")}: <b className="font-medium tabular-nums text-ink-soft">{stamp(account.updatedAt)}</b></span>
              </p>
            </>
          ) : (
            !tree.isLoading && children.length === 0 && <EmptyState message={t("Str.NoData")} />
          )}
          {children.length > 0 && (
            <ChildRows
              title={t("Str.Tree.Children")}
              rows={children}
              onOpen={(a) => setSelected(String(a.id))}
              columns={[
                { label: t("Str.Code"), cell: (a) => <span className="font-mono text-xs text-ink-tertiary">{a.code}</span> },
                { label: t("Str.Name"), cell: (a) => <span className="font-medium text-ink">{a.name}</span> },
                { label: t("Str.AccountType"), cell: (a) => a.typeName },
                ...(canBalance ? [{ label: t("Str.Balance"), cell: (a: Account) => formatMoney(a.balance), end: true }] : []),
              ]}
            />
          )}
        </div>
      </TreeSplit>

      <Dialog
        open={!!draft}
        title={t(draft?.id ? "Str.Accounts.Edit" : "Str.Accounts.Add")}
        onClose={() => setDraft(null)}
        footer={
          <>
            <Button variant="secondary" onClick={() => setDraft(null)}>{t("Str.Cancel")}</Button>
            <Button onClick={save} loading={saving} disabled={!draft?.name.trim()}>{t("Str.Save")}</Button>
          </>
        }
      >
        {draft && (
          <div className="space-y-4">
            {draft.parentId && <p className="text-sm text-ink-tertiary">{t("Str.ParentAccount")}: <b className="text-ink">{byId.get(String(draft.parentId))?.name}</b></p>}
            <TextField label={t("Str.Name")} required value={draft.name} maxLength={200} onChange={(e) => setDraft({ ...draft, name: e.target.value })} />
            <TextArea label={t("Str.Notes")} value={draft.notes} onChange={(e) => setDraft({ ...draft, notes: e.target.value })} />
            <div className="flex gap-6">
              <Switch label={t("Str.AcceptsEntries")} checked={draft.isLeaf} onChange={(isLeaf) => setDraft({ ...draft, isLeaf })} />
              <Switch label={t("Str.Active")} checked={draft.isActive} onChange={(isActive) => setDraft({ ...draft, isActive })} />
            </div>
          </div>
        )}
      </Dialog>
    </>
  );
}
