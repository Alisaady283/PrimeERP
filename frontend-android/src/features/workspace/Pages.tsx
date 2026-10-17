import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { router } from "expo-router";
import { Banknote, CalendarClock, ExternalLink, Filter, Hash, Percent, Play, X } from "lucide-react-native";
import { useState } from "react";
import { FlatList, Pressable, ScrollView, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCSSVariable } from "uniwind";
import { useCan, usePagePermission, useSession } from "@/features/auth/session";
import { analyticsOptions, hasData, yearToDate, type Row } from "@/features/workspace/data";
import { editorFor } from "@/features/workspace/documentRules";
import { filterOf, toggleFilter, type DashboardFilter } from "@/features/workspace/dashboardFilter";
import { pageByKey } from "@/catalog/pages";
import { runPageAction } from "@/features/workspace/pageActions";
import type { DashboardSpec, DocumentSpec, ListSpec, PageAction, PageSpec, ReportSpec } from "@/features/workspace/spec";
import { api, messageOf } from "@/lib/api";
import { formatCard, formatCount } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { duration, ENTRANCE_STEP } from "@/lib/motion";
import { analyticsTable, outputTable, reportTotals } from "@/lib/output";
import { palette } from "@/lib/palette";
import { allowedColumns, camel, emptyValue, plainValue } from "@/lib/values";
import { useStart } from "~/lib/direction";
import { AnimatedNumber } from "~/components/ui/AnimatedNumber";
import { Button } from "~/components/ui/Button";
import { Card } from "~/components/ui/Card";
import { Chart } from "~/components/ui/Chart";
import { EmptyState } from "~/components/ui/EmptyState";
import { Enter } from "~/components/ui/Enter";
import { GridTotals } from "~/components/ui/GridTotals";
import { Icon } from "~/components/ui/Icon";
import { PageHeader } from "~/components/ui/PageHeader";
import { Skeleton } from "~/components/ui/Skeleton";
import { toast } from "~/components/ui/Snackbar";
import { StatCard } from "~/components/ui/StatCard";
import { FieldInput } from "~/features/workspace/FieldInput";
import { ListFrame } from "~/features/workspace/ListPage";
import { OutputActions } from "~/features/workspace/OutputActions";
import { screens } from "~/features/workspace/screens";

const form = (page: string, id?: number, editor?: string) =>
  router.push({ pathname: "/p/[page]/form", params: { page, ...(id ? { id: String(id) } : {}), ...(editor ? { editor } : {}) } });

/** نظير ActionButton في الويب: التنفيذ من `pageActions` المشترك */
function ActionButton({ action, onOpeningStock }: { action: PageAction; onOpeningStock?: () => void }) {
  const t = useText();
  const can = useCan(action.permission ?? "");
  const [busy, setBusy] = useState(false);
  if (action.permission && !can) return null;
  const run = async () => {
    setBusy(true);
    try {
      if (await runPageAction(action)) toast.success(t("Str.Success"));
      else onOpeningStock?.();
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setBusy(false);
    }
  };
  return (
    <Button variant="secondary" size="sm" loading={busy} onPress={() => void run()}>
      {t(action.label)}
    </Button>
  );
}

const pageActions = (actions?: PageAction[], onOpeningStock?: () => void) =>
  actions?.map((action) => <ActionButton key={action.key} action={action} onOpeningStock={onOpeningStock} />);

/** نظير EntityPage في الويب */
export function EntityPage({ spec }: { spec: ListSpec }) {
  const gate = usePagePermission(spec);
  const canEdit = useCan(`${gate}.Edit`);
  const editable = !!spec.form && !spec.readOnly;
  return (
    <ListFrame
      pageKey={spec.key}
      title={spec.title}
      permission={gate}
      columns={spec.columns}
      filters={spec.filters}
      gallery={spec.gallery}
      actions={pageActions(spec.pageActions)}
      onAdd={editable ? () => form(spec.key) : undefined}
      onOpen={editable && canEdit ? (row) => form(spec.key, Number(row.id)) : undefined}
    />
  );
}

/** نظير DocumentPage في الويب: الإضافة والفتح إلى المحرّر بالسطور، وصفّ رصيد أول المدة بمحرّره */
function DocumentPage({ spec }: { spec: DocumentSpec }) {
  const gate = usePagePermission(spec);
  return (
    <ListFrame
      pageKey={spec.key}
      title={spec.title}
      permission={gate}
      columns={spec.columns}
      filters={spec.filters}
      actions={pageActions(spec.pageActions, () => form(spec.key, undefined, "OpeningStock"))}
      onAdd={() => form(spec.key)}
      onOpen={(row) => {
        const editor = editorFor(spec, row);
        form(spec.key, Number(row.id), editor === spec ? undefined : editor.key);
      }}
    />
  );
}

type ReportResponse = { rows: Row[]; totals: Record<string, string> };

/** نظير ReportPage في الويب: المعاملات في الصفّ ثم الصفوف والمجاميع */
function ReportPage({ spec }: { spec: ReportSpec }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const permissions = useSession((s) => s.user?.permissions);
  const [params, setParams] = useState<Record<string, unknown>>(() => Object.fromEntries(spec.params.map((p) => [p.key, emptyValue(p)])));
  const [applied, setApplied] = useState<Record<string, unknown> | null>(spec.params.every((p) => !p.required) ? params : null);
  const report = useQuery({
    queryKey: ["report", spec.key, applied],
    enabled: applied !== null,
    queryFn: ({ signal }) => api.get<ReportResponse>(`/reports/${spec.key}`, applied as Record<string, string>, signal),
  });
  const columns = allowedColumns(spec.columns, permissions);
  const [head, ...rest] = columns;
  const totals = reportTotals(spec.columns, report.data?.totals);
  const table = () => outputTable(columns, report.data?.rows ?? [], totals);

  return (
    <View className="flex-1 bg-paper">
      <View className="gap-2 border-b border-line bg-card px-3 py-2.5">
        {spec.params.map((p) => (
          <FieldInput key={p.key} field={p} value={params[p.key]} onChange={(v) => setParams((s) => ({ ...s, [p.key]: v }))} />
        ))}
        <PageHeader meta={report.data ? t("Str.Results", formatCount(report.data.rows.length)) : undefined} actions={<OutputActions title={t(spec.title)} table={table} />}>
          {spec.params.length > 0 ? (
            <Button size="sm" icon={<Icon icon={Play} size={16} tone="on-field" />} loading={report.isFetching} onPress={() => setApplied({ ...params })}>
              {t("Str.Refresh")}
            </Button>
          ) : null}
        </PageHeader>
      </View>
      <FlatList
        data={report.data?.rows ?? []}
        keyExtractor={(_, i) => String(i)}
        contentContainerStyle={{ padding: 12, gap: 8, paddingBottom: insets.bottom + 24 }}
        ListEmptyComponent={report.isFetching ? <Skeleton className="h-40" /> : <EmptyState message={t("Str.NoData")} />}
        renderItem={({ item }) => (
          <Card className="gap-1.5 p-3">
            {head ? <Text numberOfLines={1} className={`font-sans text-[15px] font-semibold text-ink ${start}`}>{plainValue(item[head.key], head.kind) || "—"}</Text> : null}
            {rest.slice(0, 5).map((c) => (
              <View key={c.key} className="flex-row justify-between gap-3">
                <Text className="font-sans text-xs text-ink-tertiary">{t(c.label)}</Text>
                <Text numberOfLines={1} className="flex-shrink font-sans text-[13px] text-ink">{plainValue(item[c.key], c.kind) || "—"}</Text>
              </View>
            ))}
          </Card>
        )}
      />
      <GridTotals items={totals} />
    </View>
  );
}

const kindIcon = { money: Banknote, count: Hash, percent: Percent, days: CalendarClock } as const;

/** نظير DashboardPage في الويب: البطاقات بزمنها ثم محتوى الرسوم بعد آخرها */
function DashboardPage({ spec }: { spec: DashboardSpec }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const [range, setRange] = useState(yearToDate);
  /** تصفية BI بما لُمس في رسم، وصفحة تفصيله؛ وdismiss يُخفي بطاقات الرسوم عند التمرير */
  const [filter, setFilter] = useState<(DashboardFilter & { drill?: string }) | null>(null);
  const [dismiss, setDismiss] = useState(0);
  const data = useQuery({ ...analyticsOptions(spec.key, range, filter), placeholderData: keepPreviousData });
  const accents = useCSSVariable(palette.map((p) => p.slice(4, -1))).map(String);
  const drill = (key?: string, q?: string) => (key ? () => router.navigate({ pathname: "/p/[page]", params: q ? { page: key, q } : { page: key } }) : undefined);
  /** لمس عنصرٍ يصفّي اللوحة كلها ولمسه ثانيةً يلغي — وما لا مفتاح له يفتح تفصيله */
  const pick = (chart: (typeof spec.charts)[number]) => (index: number) => {
    const next = filterOf(data.data?.charts[camel(chart.key)], index);
    if (!next) return drill(chart.drill)?.();
    setFilter((current) => {
      const toggled = toggleFilter(current, next);
      return toggled ? { ...toggled, drill: chart.drill } : null;
    });
  };
  /** زرّ البطاقة: فتح التفصيل مُصفّى باسم ما لُمس */
  const open = (chart: (typeof spec.charts)[number]) =>
    chart.drill ? { label: t("Str.Dashboard.OpenFiltered", t(pageByKey.get(chart.drill)?.title ?? "")), run: (index: number) => drill(chart.drill, data.data?.charts[camel(chart.key)]?.labels[index])?.() } : undefined;
  const chartsStart = Math.max(0, spec.cards.length - 1) * ENTRANCE_STEP + duration.long2;

  return (
    <ScrollView className="flex-1 bg-paper" contentContainerStyle={{ padding: 12, gap: 12, paddingBottom: insets.bottom + 24 }} onScrollBeginDrag={() => setDismiss((d) => d + 1)}>
      <PageHeader actions={<OutputActions title={t(spec.title)} table={() => analyticsTable(spec, data.data)} />}>
        <View className="w-32">
          <FieldInput compact field={{ key: "from", label: "Str.DateFrom", kind: "date" }} value={range.from} onChange={(v) => setRange((r) => ({ ...r, from: String(v) }))} />
        </View>
        <View className="w-32">
          <FieldInput compact field={{ key: "to", label: "Str.DateTo", kind: "date" }} value={range.to} onChange={(v) => setRange((r) => ({ ...r, to: String(v) }))} />
        </View>
      </PageHeader>
      <Text className={`font-sans text-sm text-ink-tertiary ${start}`}>{t(`Str.Analytics.${spec.key}.Question`)}</Text>
      {filter ? (
        <Enter index={0}>
          <View className="flex-row flex-wrap items-center gap-2 rounded-xl border border-brand-soft bg-brand-soft px-3 py-2">
            <Icon icon={Filter} size={16} tone="brand" />
            <Text className={`flex-1 font-sans text-sm font-medium text-ink ${start}`}>{t("Str.Dashboard.FilteredBy", filter.label)}</Text>
            <Pressable hitSlop={10} accessibilityLabel={t("Str.Dashboard.ClearFilter")} onPress={() => setFilter(null)} className="rounded-md p-1 active:bg-card">
              <Icon icon={X} size={16} tone="ink-tertiary" />
            </Pressable>
            {filter.drill ? (
              <Button size="sm" variant="secondary" icon={<Icon icon={ExternalLink} size={14} tone="ink-soft" />} onPress={drill(filter.drill, filter.label)}>
                {t("Str.Dashboard.OpenFiltered", t(pageByKey.get(filter.drill)?.title ?? ""))}
              </Button>
            ) : null}
          </View>
        </Enter>
      ) : null}
      <View className="flex-row flex-wrap gap-2">
        {spec.cards.map((card, index) => {
          const Glyph = kindIcon[card.kind ?? "money"];
          const accent = accents[index % accents.length]!;
          const value = data.data?.cards[camel(card.key)];
          return (
            <View key={card.key} style={{ width: "48.8%" }}>
              {data.isLoading ? (
                <Skeleton className="h-24" />
              ) : (
                <Enter index={index}>
                  <StatCard
                    label={t(`Str.Analytics.${spec.key}.${card.key}`)}
                    value={<AnimatedNumber className={`font-sans text-xl font-bold text-ink ${start}`} value={value?.value ?? 0} format={(v) => formatCard(v, card.kind)} delay={index * ENTRANCE_STEP} />}
                    change={value?.change}
                    lowerIsBetter={card.lowerIsBetter}
                    icon={<Icon icon={Glyph} size={18} tone={`chart-${(index % palette.length) + 1}`} />}
                    accent={accent}
                    onPress={drill(card.drill)}
                  />
                </Enter>
              )}
            </View>
          );
        })}
      </View>
      {spec.charts.map((chart, index) => {
        const series = data.data?.charts[camel(chart.key)];
        return (
          <Card key={chart.key} className="gap-3 p-4">
            <View>
              <Text className={`font-sans text-[15px] font-semibold text-ink ${start}`} onPress={drill(chart.drill)}>{t(`Str.Analytics.${spec.key}.${chart.key}`)}</Text>
              <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>{t(`Str.Analytics.${spec.key}.${chart.key}.Question`)}</Text>
            </View>
            {data.isLoading || !series ? (
              <Skeleton className="h-52" />
            ) : !hasData(series) ? (
              <Text className="py-10 text-center font-sans text-sm text-ink-faint">{t("Str.NoData")}</Text>
            ) : (
              <Chart kind={chart.kind} tone={chart.tone} smooth={chart.smooth} data={series} delay={chartsStart + index * ENTRANCE_STEP} onPick={pick(chart)} action={open(chart)} dismiss={dismiss} />
            )}
          </Card>
        );
      })}
    </ScrollView>
  );
}

/** نظير PageHost في الويب */
export function PageHost({ spec }: { spec: PageSpec }) {
  const t = useText();
  switch (spec.type) {
    case "list":
      return <EntityPage key={spec.key} spec={spec} />;
    case "document":
      return <DocumentPage key={spec.key} spec={spec} />;
    case "report":
      return <ReportPage key={spec.key} spec={spec} />;
    case "dashboard":
      return <DashboardPage key={spec.key} spec={spec} />;
    case "custom": {
      const Screen = screens[spec.screen];
      return Screen ? <Screen key={spec.key} spec={spec} /> : <EmptyState message={t("Str.Mobile.WebOnly")} />;
    }
  }
}
