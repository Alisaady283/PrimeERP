import { Pencil, Plus, Trash2 } from "lucide-react-native";
import { useState } from "react";
import { Alert, RefreshControl, ScrollView, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { editAccount, newAccount, stamp, useAccounts, type Account, type AccountDraft } from "@/features/accounts/accounts";
import { useCan } from "@/features/auth/session";
import type { CustomSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { formatCount, formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Badge } from "~/components/ui/Badge";
import { Button } from "~/components/ui/Button";
import { EmptyState } from "~/components/ui/EmptyState";
import { Switch, TextArea, TextField } from "~/components/ui/Field";
import { Icon } from "~/components/ui/Icon";
import { SearchBox } from "~/components/ui/SearchBox";
import { Sheet } from "~/components/ui/Sheet";
import { Skeleton } from "~/components/ui/Skeleton";
import { toast } from "~/components/ui/Snackbar";
import { ChildRows, DetailTile, TreeChips, TreePath } from "~/components/ui/TreeNav";

/** نظير AccountsPage في الويب: الطريق · الحساب بملخّصه وأزراره · فروعه — والمنطق من `accounts.ts` المشترك */
export default function AccountsPage({ spec }: { spec: CustomSpec }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const canCreate = useCan("Accounts.Create");
  const canEdit = useCan("Accounts.Edit");
  /** حساب النظام (مربوطٌ بالإعدادات) يُعدَّل بصلاحية إعدادات النظام كما يقبله الخادم، ولا يُحذف */
  const canEditSystem = useCan("Settings.System");
  const canDelete = useCan("Accounts.Delete");
  const canBalance = useCan("Accounts.Column.Balance");
  const [type, setType] = useState<number | null>(null);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const [draft, setDraft] = useState<AccountDraft | null>(null);
  const [saving, setSaving] = useState(false);
  const accounts = useAccounts(type);
  const account = selected ? accounts.byId.get(selected) : undefined;
  const found = accounts.find(search);
  const children = search ? found : account ? account.children : accounts.roots;

  const open = (a: Account) => {
    setSearch("");
    setSelected(String(a.id));
  };

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

  const remove = (a: Account) =>
    Alert.alert(t("Str.Delete"), t("Str.ConfirmDeleteMessage"), [
      { text: t("Str.Cancel"), style: "cancel" },
      {
        text: t("Str.Delete"),
        style: "destructive",
        onPress: async () => {
          try {
            await accounts.remove(a.id);
            toast.success(t("Str.Success"));
            setSelected(a.parentId ? String(a.parentId) : null);
          } catch (error) {
            toast.error(messageOf(error));
          }
        },
      },
    ]);

  return (
    <View className="flex-1 bg-paper">
      <View className="gap-2 border-b border-line bg-card px-3 py-2.5">
        <View className="flex-row">
          <SearchBox value={search} onChange={setSearch} />
        </View>
        <TreeChips items={[{ value: null, label: t("Str.All") }, ...accounts.types]} value={type} onChange={setType} />
      </View>
      <ScrollView
        contentContainerStyle={{ padding: 12, gap: 12, paddingBottom: insets.bottom + 24 }}
        refreshControl={<RefreshControl refreshing={accounts.tree.isRefetching} onRefresh={() => void accounts.refresh()} />}
      >
        {!search ? <TreePath items={[{ id: "root", label: t(spec.title) }, ...(account ? accounts.path(account).map((a) => ({ id: String(a.id), label: a.name })) : [])]} onPick={setSelected} /> : null}
        {accounts.tree.isLoading ? (
          <Skeleton className="h-64" />
        ) : account && !search ? (
          <View className="gap-3">
            <View>
              <Text className={`font-mono text-xs text-ink-tertiary ${start}`}>{account.code}</Text>
              <Text className={`font-sans text-xl font-bold text-ink ${start}`}>{account.name}</Text>
              <View className="mt-2 flex-row flex-wrap gap-1.5">
                <Badge tone="brand">{account.typeName}</Badge>
                {account.isLeaf ? <Badge tone="success">{t("Str.AcceptsEntries")}</Badge> : null}
                {!account.isActive ? <Badge tone="danger">{t("Str.Inactive")}</Badge> : null}
              </View>
            </View>
            <View className="flex-row flex-wrap gap-2">
              {canCreate && !account.isLeaf ? (
                <Button size="sm" icon={<Icon icon={Plus} size={16} tone="on-field" />} onPress={() => setDraft(newAccount(account))}>
                  {t("Str.Accounts.Add")}
                </Button>
              ) : null}
              {canEdit && (!account.isSystem || canEditSystem) ? (
                <Button size="sm" variant="secondary" icon={<Icon icon={Pencil} size={16} tone="ink-soft" />} onPress={() => setDraft(editAccount(account))}>
                  {t("Str.Accounts.Edit")}
                </Button>
              ) : null}
              {canDelete && !account.isSystem ? (
                <Button size="sm" variant="ghost" textClassName="text-danger" icon={<Icon icon={Trash2} size={16} tone="danger" />} onPress={() => remove(account)}>
                  {t("Str.Delete")}
                </Button>
              ) : null}
            </View>
            <View className="flex-row flex-wrap gap-2">
              {canBalance ? <DetailTile tone="brand" label={t("Str.Balance")} value={formatMoney(account.balance)} /> : null}
              <DetailTile label={t("Str.Level")} value={formatCount(account.level)} />
              <DetailTile label={t("Str.Tree.Children")} value={formatCount(account.children.length)} />
            </View>
            {account.notes ? <Text className={`rounded-xl bg-paper-soft px-4 py-3 font-sans text-sm text-ink-soft ${start}`}>{account.notes}</Text> : null}
            <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>
              {t("Str.CreatedAt")}: {stamp(account.createdAt)} · {t("Str.UpdatedAt")}: {stamp(account.updatedAt)}
            </Text>
          </View>
        ) : null}
        {children.length > 0 ? (
          <ChildRows
            title={search ? t("Str.Results", formatCount(children.length)) : t("Str.Tree.Children")}
            rows={children}
            keyOf={(a) => String(a.id)}
            label={(a) => a.name}
            hint={(a) => `${a.code} · ${a.typeName}`}
            end={canBalance ? (a) => <Text className="font-sans text-xs text-ink-soft" style={{ fontVariant: ["tabular-nums"] }}>{formatMoney(a.balance)}</Text> : undefined}
            hasChildren={(a) => a.children.length > 0}
            onOpen={open}
          />
        ) : !accounts.tree.isLoading && !account ? (
          <EmptyState message={t("Str.NoData")} />
        ) : null}
      </ScrollView>

      <Sheet
        open={!!draft}
        title={t(draft?.id ? "Str.Accounts.Edit" : "Str.Accounts.Add")}
        onClose={() => setDraft(null)}
        footer={
          <Button size="lg" loading={saving} disabled={!draft?.name.trim()} onPress={() => void save()}>
            {t("Str.Save")}
          </Button>
        }
      >
        {draft ? (
          <ScrollView contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
            {draft.parentId ? (
              <Text className={`font-sans text-sm text-ink-tertiary ${start}`}>
                {t("Str.ParentAccount")}: <Text className="font-semibold text-ink">{accounts.byId.get(String(draft.parentId))?.name}</Text>
              </Text>
            ) : null}
            <TextField label={t("Str.Name")} required value={draft.name} maxLength={200} onChangeText={(name) => setDraft({ ...draft, name })} />
            <TextArea label={t("Str.Notes")} value={draft.notes} onChangeText={(notes) => setDraft({ ...draft, notes })} />
            <Switch label={t("Str.AcceptsEntries")} checked={draft.isLeaf} onChange={(isLeaf) => setDraft({ ...draft, isLeaf })} />
            <Switch label={t("Str.Active")} checked={draft.isActive} onChange={(isActive) => setDraft({ ...draft, isActive })} />
          </ScrollView>
        ) : null}
      </Sheet>
    </View>
  );
}
