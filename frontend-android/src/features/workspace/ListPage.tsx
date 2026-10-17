import AsyncStorage from "@react-native-async-storage/async-storage";
import { useLocalSearchParams } from "expo-router";
import { FilterX, LayoutGrid, List, Package, Plus } from "lucide-react-native";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { ActivityIndicator, FlatList, Image, Pressable, ScrollView, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCSSVariable } from "uniwind";
import { useCan, useSession } from "@/features/auth/session";
import { usePageRows, type Query, type Row } from "@/features/workspace/data";
import type { ColumnSpec, FieldSpec, GallerySpec } from "@/features/workspace/spec";
import { formatCount, formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { imageUrl } from "@/lib/image";
import { useDebounced } from "@/lib/useDebounced";
import { outputTable } from "@/lib/output";
import { allowedColumns, blankValues, columnTotal, plainValue, toneOf } from "@/lib/values";
import { Badge } from "~/components/ui/Badge";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Card } from "~/components/ui/Card";
import { EmptyState } from "~/components/ui/EmptyState";
import { GridTotals } from "~/components/ui/GridTotals";
import { Icon } from "~/components/ui/Icon";
import { PageHeader } from "~/components/ui/PageHeader";
import { SearchBox } from "~/components/ui/SearchBox";
import { FieldInput } from "~/features/workspace/FieldInput";
import { OutputActions } from "~/features/workspace/OutputActions";
import { resolveField } from "@/features/workspace/fieldRules";

/** أعمدة البطاقة بعد العنوان */
const DETAIL = 4;

type ListFrameProps = {
  pageKey: string;
  title: string;
  permission: string;
  columns: ColumnSpec[];
  query?: Query;
  filters?: FieldSpec[];
  onAdd?: () => void;
  onOpen?: (row: Row) => void;
  actions?: ReactNode;
  gallery?: GallerySpec;
};

type Mode = "tiles" | "details";

/** العرض المحفوظ — كالويب لكل صفحة */
const useView = (pageKey: string, enabled: boolean) => {
  const [view, setView] = useState<Mode>("details");
  useEffect(() => {
    if (enabled) void AsyncStorage.getItem(`view.${pageKey}`).then((v) => v === "tiles" && setView("tiles"));
  }, [pageKey, enabled]);
  const choose = (next: Mode) => {
    setView(next);
    void AsyncStorage.setItem(`view.${pageKey}`, next);
  };
  return [enabled ? view : "details", choose] as const;
};

/** نظير ListFrame في الويب: صفٌّ واحد ثم الصفوف بتمريرٍ لا نهائي */
export function ListFrame({ pageKey, title: pageTitle, permission, columns, query, filters = [], onAdd, onOpen, actions, gallery }: ListFrameProps) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const brand = useCSSVariable("--color-brand");
  const canCreate = useCan(`${permission}.Create`);
  const canView = useCan(`${permission}.View`);
  const permissions = useSession((s) => s.user?.permissions);
  /** q: فتح القائمة مُصفّاةً بما نُقر عليه في اللوحة */
  const { q } = useLocalSearchParams<{ q?: string }>();
  const [search, setSearch] = useState(q ?? "");
  const blank = () => blankValues(filters);
  const [filterValues, setFilterValues] = useState<Record<string, unknown>>(blank);
  const chosen = Object.fromEntries(Object.entries(filterValues).filter(([, v]) => v !== null && v !== undefined && v !== "")) as Query;
  const rowsQuery = usePageRows(pageKey, { ...query, ...chosen, search: useDebounced(search.trim()) });
  const rows = useMemo(() => rowsQuery.data?.pages.flatMap((p) => p.items) ?? [], [rowsQuery.data]);
  const total = rowsQuery.data?.pages[0]?.total ?? 0;
  const [view, setView] = useView(pageKey, !!gallery);

  const visible = useMemo(() => allowedColumns(columns, permissions), [columns, permissions]);
  const title = visible.find((c) => !c.kind || c.kind === "text") ?? visible[0];
  const code = visible.find((c) => c.kind === "code" && c !== title);
  const details = visible.filter((c) => c !== title && c !== code).slice(0, DETAIL);
  const totals = visible.filter((c) => c.total);
  const totalOf = (key: string) => formatMoney(columnTotal(rows, key));
  const totalItems = totals.map((c): [string, string] => [t(c.label), totalOf(c.key)]);
  const table = () => outputTable(visible, rows, totalItems);
  const more = () => rowsQuery.hasNextPage && !rowsQuery.isFetchingNextPage && void rowsQuery.fetchNextPage();

  const details$ = ({ item }: { item: Row }) => (
    <Pressable disabled={!onOpen} onPress={() => onOpen?.(item)} className="active:opacity-80">
      <Card className="gap-2 p-3">
        <View className="flex-row items-center gap-2">
          <Text numberOfLines={1} className={`flex-1 font-sans text-[15px] font-semibold text-ink ${start}`}>{title ? plainValue(item[title.key], title.kind) || "—" : "—"}</Text>
          {code ? <Text className="font-sans text-xs text-ink-faint">{plainValue(item[code.key], code.kind)}</Text> : null}
        </View>
        {details.map((c) => (
          <View key={c.key} className="flex-row items-center justify-between gap-3">
            <Text className="font-sans text-xs text-ink-tertiary">{t(c.label)}</Text>
            {c.kind === "status" && item[c.key]
              ? <Badge tone={toneOf(item[c.key.replace(/Text$/, "Variant")])}>{String(item[c.key])}</Badge>
              : <Text numberOfLines={1} className="flex-shrink font-sans text-[13px] text-ink" style={{ fontVariant: ["tabular-nums"] }}>{plainValue(item[c.key], c.kind) || "—"}</Text>}
          </View>
        ))}
      </Card>
    </Pressable>
  );

  const tiles$ = ({ item }: { item: Row }) => {
    const image = gallery ? imageUrl(item[gallery.image]) : undefined;
    return (
      <Pressable disabled={!onOpen} onPress={() => onOpen?.(item)} className="flex-1 active:scale-[0.97]">
        <View className="overflow-hidden rounded-xl border border-line bg-card">
          <View className="aspect-square items-center justify-center bg-paper">
            {image ? <Image source={{ uri: image }} className="size-full" resizeMode="cover" /> : <Icon icon={Package} size={30} tone="ink-faint" />}
          </View>
          <View className="gap-0.5 px-3 py-2">
            <Text numberOfLines={1} className={`font-sans text-sm font-medium text-ink ${start}`}>{gallery ? String(item[gallery.title] ?? "") : ""}</Text>
            <View className="flex-row items-center justify-between gap-2">
              <Text numberOfLines={1} className="font-sans text-xs text-ink-faint">{gallery?.hint ? String(item[gallery.hint] ?? "") : ""}</Text>
              <Text className="font-sans text-xs font-semibold text-brand">{gallery?.value ? formatMoney(Number(item[gallery.value] ?? 0)) : ""}</Text>
            </View>
          </View>
        </View>
      </Pressable>
    );
  };

  const tiles = gallery && view === "tiles";

  return (
    <View className="flex-1 bg-paper">
      <View className="gap-2 border-b border-line bg-card px-3 py-2.5">
        <PageHeader
          meta={t("Str.Results", formatCount(total))}
          actions={
            <>
              {actions}
              {canView ? <OutputActions title={t(pageTitle)} table={table} /> : null}
              {gallery ? (
                <View className="flex-row rounded-lg border border-line bg-paper-soft p-0.5">
                  {(
                    [
                      ["details", List],
                      ["tiles", LayoutGrid],
                    ] as const
                  ).map(([key, Glyph]) => (
                    <Pressable key={key} accessibilityLabel={t(key === "tiles" ? "Str.View.Tiles" : "Str.View.Details")} onPress={() => setView(key)} className={`size-8 items-center justify-center rounded-md ${view === key ? "bg-card shadow-card" : ""}`}>
                      <Icon icon={Glyph} size={16} tone={view === key ? "brand" : "ink-tertiary"} />
                    </Pressable>
                  ))}
                </View>
              ) : null}
            </>
          }
        >
          <SearchBox value={search} onChange={setSearch} />
        </PageHeader>
        {filters.length > 0 ? (
          <ScrollView horizontal showsHorizontalScrollIndicator={false} contentContainerStyle={{ gap: 8 }}>
            {filters.map((f) => (
              <View key={f.key} className="w-44">
                <FieldInput compact field={{ ...resolveField(f, filterValues), required: false }} value={filterValues[f.key]} onChange={(v) => setFilterValues((s) => ({ ...s, [f.key]: v }))} />
              </View>
            ))}
            {Object.keys(chosen).length > 0 ? (
              <Button variant="ghost" size="sm" icon={<Icon icon={FilterX} size={16} tone="ink-soft" />} onPress={() => setFilterValues(blank())}>
                {t("Str.ClearFilter")}
              </Button>
            ) : null}
          </ScrollView>
        ) : null}
      </View>

      <FlatList
        key={tiles ? "tiles" : "details"}
        data={rows}
        numColumns={tiles ? 2 : 1}
        keyExtractor={(row, i) => String(row.id ?? i)}
        renderItem={tiles ? tiles$ : details$}
        columnWrapperStyle={tiles ? { gap: 12 } : undefined}
        contentContainerStyle={{ padding: 12, gap: tiles ? 12 : 8, paddingBottom: insets.bottom + 96 }}
        onEndReachedThreshold={0.5}
        onEndReached={more}
        ListEmptyComponent={rowsQuery.isFetching ? null : <EmptyState message={t("Str.NoData")} />}
        ListFooterComponent={rowsQuery.isFetching ? <ActivityIndicator color={String(brand)} style={{ marginVertical: 16 }} /> : null}
        refreshing={rowsQuery.isRefetching && !rowsQuery.isFetchingNextPage}
        onRefresh={() => void rowsQuery.refetch()}
      />

      {rows.length > 0 ? <GridTotals items={totalItems} /> : null}

      {onAdd && canCreate ? (
        <Pressable
          accessibilityLabel={t("Str.Add")}
          onPress={onAdd}
          className="absolute end-4 size-14 items-center justify-center rounded-full bg-brand shadow-pop active:scale-[0.94]"
          style={{ bottom: insets.bottom + (totals.length > 0 && rows.length > 0 ? 64 : 20) }}
        >
          <Icon icon={Plus} size={26} tone="on-field" />
        </Pressable>
      ) : null}
    </View>
  );
}
