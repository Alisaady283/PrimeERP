import { Pencil, Plus, Trash2 } from "lucide-react-native";
import { useState } from "react";
import { Alert, RefreshControl, ScrollView, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCan, usePagePermission } from "@/features/auth/session";
import { blankCategory, editCategory, useCategories, type Category, type CategoryDraft } from "@/features/categories/categories";
import type { CustomSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { formatCount } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Badge } from "~/components/ui/Badge";
import { Button } from "~/components/ui/Button";
import { Combobox } from "~/components/ui/Combobox";
import { EmptyState } from "~/components/ui/EmptyState";
import { Switch, TextArea, TextField } from "~/components/ui/Field";
import { Icon } from "~/components/ui/Icon";
import { SearchBox } from "~/components/ui/SearchBox";
import { Sheet } from "~/components/ui/Sheet";
import { Skeleton } from "~/components/ui/Skeleton";
import { toast } from "~/components/ui/Snackbar";
import { ChildRows, DetailTile, TreePath } from "~/components/ui/TreeNav";

/** نظير CategoriesPage في الويب (الأصناف · العلامات · فئات الأصول): الطريق · التصنيف · فروعه — والمنطق من `categories.ts` المشترك */
export default function CategoriesPage({ spec }: { spec: CustomSpec }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const gate = usePagePermission(spec);
  const canCreate = useCan(`${gate}.Create`);
  const canEdit = useCan(`${gate}.Edit`);
  const canDelete = useCan(`${gate}.Delete`);
  const [search, setSearch] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const [draft, setDraft] = useState<CategoryDraft | null>(null);
  const [saving, setSaving] = useState(false);
  const categories = useCategories(spec.key);
  const category = selected ? categories.byId.get(selected) : undefined;
  const children = search ? categories.find(search) : categories.childrenOf(category?.id ?? null);

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

  const remove = (c: Category) =>
    Alert.alert(t("Str.Delete"), t("Str.ConfirmDeleteMessage"), [
      { text: t("Str.Cancel"), style: "cancel" },
      {
        text: t("Str.Delete"),
        style: "destructive",
        onPress: async () => {
          try {
            await categories.remove(c.id);
            toast.success(t("Str.Success"));
            setDraft(null);
            if (String(c.id) === selected) setSelected(c.parentId ? String(c.parentId) : null);
          } catch (error) {
            toast.error(messageOf(error));
          }
        },
      },
    ]);

  return (
    <View className="flex-1 bg-paper">
      <View className="flex-row items-center gap-2 border-b border-line bg-card px-3 py-2.5">
        <SearchBox value={search} onChange={setSearch} />
        {canCreate ? (
          <Button size="sm" icon={<Icon icon={Plus} size={16} tone="on-field" />} onPress={() => setDraft(blankCategory(category?.id ?? null))}>
            {t("Str.Category.Add")}
          </Button>
        ) : null}
      </View>
      <ScrollView
        contentContainerStyle={{ padding: 12, gap: 12, paddingBottom: insets.bottom + 24 }}
        refreshControl={<RefreshControl refreshing={categories.list.isRefetching} onRefresh={() => void categories.refresh()} />}
      >
        {!search ? <TreePath items={[{ id: "root", label: t(spec.title) }, ...(category ? categories.path(category).map((c) => ({ id: String(c.id), label: c.name })) : [])]} onPick={setSelected} /> : null}
        {categories.list.isLoading ? (
          <Skeleton className="h-64" />
        ) : category && !search ? (
          <View className="gap-3">
            <View>
              <Text className={`font-sans text-xl font-bold text-ink ${start}`}>{category.name}</Text>
              <View className="mt-2 flex-row">
                <Badge tone={category.isActive ? "success" : "danger"}>{t(category.isActive ? "Str.Active" : "Str.Inactive")}</Badge>
              </View>
            </View>
            <View className="flex-row flex-wrap gap-2">
              {canEdit ? (
                <Button size="sm" variant="secondary" icon={<Icon icon={Pencil} size={16} tone="ink-soft" />} onPress={() => setDraft(editCategory(category))}>
                  {t("Str.Category.Edit")}
                </Button>
              ) : null}
              {canDelete ? (
                <Button size="sm" variant="ghost" textClassName="text-danger" icon={<Icon icon={Trash2} size={16} tone="danger" />} onPress={() => remove(category)}>
                  {t("Str.Delete")}
                </Button>
              ) : null}
            </View>
            <View className="flex-row flex-wrap gap-2">
              <DetailTile tone="brand" label={t("Str.Tree.Children")} value={formatCount(children.length)} />
              <DetailTile label={t("Str.Level")} value={formatCount(categories.path(category).length)} />
              <DetailTile label={t("Str.ParentCategory")} value={category.parentName ?? "—"} />
            </View>
            {category.notes ? <Text className={`rounded-xl bg-paper-soft px-4 py-3 font-sans text-sm text-ink-soft ${start}`}>{category.notes}</Text> : null}
          </View>
        ) : null}
        {children.length > 0 ? (
          <ChildRows
            title={search ? t("Str.Results", formatCount(children.length)) : t("Str.Tree.Children")}
            rows={children}
            keyOf={(c) => String(c.id)}
            label={(c) => c.name}
            end={(c) => <Badge tone={c.isActive ? "success" : "danger"}>{t(c.isActive ? "Str.Active" : "Str.Inactive")}</Badge>}
            hasChildren={(c) => categories.childrenOf(c.id).length > 0}
            onOpen={(c) => {
              setSearch("");
              setSelected(String(c.id));
            }}
          />
        ) : !categories.list.isLoading && !category ? (
          <EmptyState message={t("Str.NoData")} />
        ) : null}
      </ScrollView>

      <Sheet
        open={!!draft}
        title={t(draft?.id ? "Str.Category.Edit" : "Str.Category.Add")}
        onClose={() => setDraft(null)}
        footer={
          <Button size="lg" loading={saving} disabled={!draft?.name.trim()} onPress={() => void save()}>
            {t("Str.Save")}
          </Button>
        }
      >
        {draft ? (
          <ScrollView contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
            <TextField label={t("Str.Name")} required value={draft.name} maxLength={200} onChangeText={(name) => setDraft({ ...draft, name })} />
            {!draft.id ? <Combobox label={t("Str.ParentCategory")} value={draft.parentId} options={categories.options} onChange={(v) => setDraft({ ...draft, parentId: v === null ? null : Number(v) })} /> : null}
            <TextArea label={t("Str.Notes")} value={draft.notes} onChangeText={(notes) => setDraft({ ...draft, notes })} />
            <Switch label={t("Str.Active")} checked={draft.isActive} onChange={(isActive) => setDraft({ ...draft, isActive })} />
          </ScrollView>
        ) : null}
      </Sheet>
    </View>
  );
}
