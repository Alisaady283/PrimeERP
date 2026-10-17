import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useNavigate } from "@tanstack/react-router";
import { Banknote, CalendarClock, ExternalLink, Filter, Hash, Percent, Play, X } from "lucide-react";
import { motion } from "motion/react";
import { lazy, Suspense, useEffect, useRef, useState, type CSSProperties } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { DataGrid, GridTotals } from "@/components/ui/DataGrid";
import { PageHeader } from "@/components/ui/PageHeader";
import { Skeleton } from "@/components/ui/Skeleton";
import { StatCard } from "@/components/ui/StatCard";
import { pageByKey } from "@/catalog/pages";
import { filterOf, openSearch, toggleFilter, type DashboardFilter } from "@/features/workspace/dashboardFilter";
import type { PageSearch } from "@/app/router";
import { toast } from "@/components/ui/Toast";
import { openingStock } from "@/catalog/pages";
import { useCan, usePagePermission } from "@/features/auth/session";
import { api, messageOf } from "@/lib/api";
import { cn } from "@/lib/cn";
import { formatCount } from "@/lib/format";
import { duration, ENTRANCE_STEP, enterAt } from "@/lib/motion";
import { palette } from "@/lib/palette";
import { camel } from "@/lib/values";
import { mark, report } from "@/lib/perf";
import { useText } from "@/lib/i18n";
import { analyticsTable, outputTable, reportTotals } from "@/lib/output";
import { DocumentEditor } from "@/features/workspace/DocumentEditor";
import { editorFor } from "@/features/workspace/documentRules";
import { runPageAction } from "@/features/workspace/pageActions";
import { EntityDialog } from "@/features/workspace/EntityDialog";
import { emptyValue, FieldInput } from "@/features/workspace/FieldInput";
import { ListFrame, useColumns, useVisibleColumns } from "@/features/workspace/ListPage";
import { OutputActions } from "@/features/workspace/OutputActions";
import { analyticsOptions, hasData, yearToDate, type Row } from "@/features/workspace/data";
import { loadChart } from "@/features/workspace/charts";
import type { CustomSpec, DashboardSpec, DocumentSpec, ListSpec, PageAction, PageSpec, ReportSpec } from "@/features/workspace/spec";
import { screens } from "@/features/workspace/screens";

/** زرّ صفحة */
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
    <Button variant="secondary" loading={busy} onClick={() => void run()}>
      {t(action.label)}
    </Button>
  );
}

const pageActions = (actions?: PageAction[], onOpeningStock?: () => void) =>
  actions?.map((action) => <ActionButton key={action.key} action={action} onOpeningStock={onOpeningStock} />);

export function EntityPage({ spec }: { spec: ListSpec }) {
  const gate = usePagePermission(spec);
  const canEdit = useCan(`${gate}.Edit`);
  const [dialog, setDialog] = useState<{ open: boolean; id: number | null; row?: Row }>({ open: false, id: null });
  const editable = !!spec.form && !spec.readOnly;
  return (
    <>
      <ListFrame
        pageKey={spec.key}
        title={spec.title}
        permission={gate}
        columns={spec.columns}
        filters={spec.filters}
        gallery={spec.gallery}
        actions={pageActions(spec.pageActions)}
        addLabel={spec.formTitles?.[0]}
        onAdd={editable ? () => setDialog({ open: true, id: null }) : undefined}
        onOpen={editable && canEdit ? (row) => setDialog({ open: true, id: Number(row.id), row }) : undefined}
      />
      {editable && <EntityDialog spec={spec} open={dialog.open} id={dialog.id} row={dialog.row} onClose={() => setDialog({ open: false, id: null })} />}
    </>
  );
}

function DocumentPage({ spec }: { spec: DocumentSpec }) {
  const gate = usePagePermission(spec);
  const [editor, setEditor] = useState<{ spec: DocumentSpec; id: number | null } | null>(null);
  return (
    <>
      <ListFrame
        pageKey={spec.key}
        title={spec.title}
        permission={gate}
        columns={spec.columns}
        filters={spec.filters}
        actions={pageActions(spec.pageActions, () => setEditor({ spec: openingStock, id: null }))}
        onAdd={() => setEditor({ spec, id: null })}
        onOpen={(row) => setEditor({ spec: editorFor(spec, row), id: Number(row.id) })}
      />
      {editor && <DocumentEditor spec={editor.spec} open id={editor.id} onClose={() => setEditor(null)} />}
    </>
  );
}

const Chart = lazy(() => loadChart().then((m) => ({ default: m.Chart })));

type ReportResponse = { rows: Row[]; totals: Record<string, string> };

function ReportPage({ spec }: { spec: ReportSpec }) {
  const t = useText();
  const [params, setParams] = useState<Record<string, unknown>>(() => Object.fromEntries(spec.params.map((p) => [p.key, emptyValue(p)])));
  const [applied, setApplied] = useState<Record<string, unknown> | null>(spec.params.every((p) => !p.required) ? params : null);
  const columns = useColumns(spec.columns);
  const visible = useVisibleColumns(spec.columns);
  const report = useQuery({
    queryKey: ["report", spec.key, applied],
    enabled: applied !== null,
    queryFn: ({ signal }) => api.get<ReportResponse>(`/reports/${spec.key}`, applied as Record<string, string>, signal),
  });

  const totals = reportTotals(spec.columns, report.data?.totals);
  const table = () => outputTable(visible, report.data?.rows ?? [], totals);

  return (
    <Card className="flex h-full min-h-0 flex-col overflow-hidden">
      <PageHeader
        className="border-b border-line px-3 py-2.5"
        meta={report.data ? t("Str.Results", formatCount(report.data.rows.length)) : undefined}
        actions={<OutputActions title={t(spec.title)} table={table} />}
      >
        {spec.params.map((p) => (
          <div key={p.key} className="w-44">
            <FieldInput compact field={p} value={params[p.key]} onChange={(v) => setParams((s) => ({ ...s, [p.key]: v }))} />
          </div>
        ))}
        {spec.params.length > 0 && (
          <Button icon={<Play className="size-4" />} onClick={() => setApplied({ ...params })} loading={report.isFetching}>
            {t("Str.Refresh")}
          </Button>
        )}
      </PageHeader>
      <div className="min-h-0 flex-1">
        <DataGrid
          columns={columns}
          data={report.data?.rows ?? []}
          getRowId={(row) => JSON.stringify(row)}
          loading={report.isFetching}
          emptyMessage={t("Str.NoData")}
          storageKey={`report.${spec.key}`}
        />
      </div>
      <GridTotals items={totals} />
    </Card>
  );
}




/** رسمٌ فيه قيمةٌ غير صفرية */

const kindIcon = { money: Banknote, count: Hash, percent: Percent, days: CalendarClock } as const;

function DashboardPage({ spec }: { spec: DashboardSpec }) {
  const t = useText();
  const navigate = useNavigate();
  const [range, setRange] = useState(yearToDate);
  /** تصفية BI: ما نُقر عليه في رسم، وصفحة التفصيل المرتبطة به */
  const [filter, setFilter] = useState<(DashboardFilter & { drill?: string }) | null>(null);
  const data = useQuery({ ...analyticsOptions(spec.key, range, filter), placeholderData: keepPreviousData });
  const done = useRef(new Set<string>());
  useEffect(() => {
    mark("dash:start");
    done.current = new Set();
    void loadChart().then(() => mark("dash:chunk"));
  }, [range]);
  useEffect(() => {
    if (data.data) mark("dash:data");
  }, [data.data]);
  /** نهاية حركة كل الرسوم */
  const finished = (key: string) => {
    if (done.current.has(key)) return;
    done.current.add(key);
    if (done.current.size === 1) mark("dash:first-chart");
    if (done.current.size !== spec.charts.filter((c) => hasData(data.data?.charts[camel(c.key)])).length) return;
    mark("dash:all-charts");
    report("dash", "dash:start", ["dash:chunk", "dash:data", "dash:first-chart", "dash:all-charts"]);
  };
  /** الرسوم تبدأ بعد انتهاء آخر بطاقة */
  const chartsStart = Math.max(0, spec.cards.length - 1) * ENTRANCE_STEP + duration.long2;
  const drill = (key?: string, search: PageSearch = {}) => (key ? () => void navigate({ to: "/p/$page", params: { page: key }, search }) : undefined);
  /** النقر المزدوج يفتح صفحة العنصر مصفّاةً به: الشهر بنطاقه، وغيره بنصّه */
  const open = (chart: (typeof spec.charts)[number]) => (index: number) => {
    const series = data.data?.charts[camel(chart.key)];
    drill(chart.drill, openSearch(filterOf(series, index), series?.labels[index]))?.();
  };
  /** النقر على عنصرٍ يصفّي اللوحة كلها، وعلى المختار نفسه يلغي — والفتح للمزدوج وحده */
  const pick = (chart: (typeof spec.charts)[number]) => (index: number) => {
    const next = filterOf(data.data?.charts[camel(chart.key)], index);
    if (!next) return;
    setFilter((current) => {
      const toggled = toggleFilter(current, next);
      return toggled ? { ...toggled, drill: chart.drill } : null;
    });
  };

  const table = () => analyticsTable(spec, data.data);

  return (
    <div className="scroll-thin h-full space-y-4 overflow-y-auto pb-4">
      <PageHeader className="items-center" actions={<OutputActions title={t(spec.title)} table={table} />}>
        <div className="w-[calc(50%-1rem)] sm:w-40"><FieldInput compact field={{ key: "from", label: "Str.DateFrom", kind: "date" }} value={range.from} onChange={(v) => setRange((r) => ({ ...r, from: String(v) }))} /></div>
        <span className="text-ink-faint">—</span>
        <div className="w-[calc(50%-1rem)] sm:w-40"><FieldInput compact field={{ key: "to", label: "Str.DateTo", kind: "date" }} value={range.to} onChange={(v) => setRange((r) => ({ ...r, to: String(v) }))} /></div>
        <p className="min-w-0 basis-full truncate text-sm text-ink-tertiary sm:basis-auto sm:ps-2">{t(`Str.Analytics.${spec.key}.Question`)}</p>
      </PageHeader>
      {filter ? (
        <motion.div {...enterAt(0)} className="flex flex-wrap items-center gap-2 rounded-xl border border-brand/20 bg-brand-soft px-3 py-2">
          <Filter className="size-4 text-brand" />
          <span className="text-sm font-medium text-ink">{t("Str.Dashboard.FilteredBy", filter.label)}</span>
          <button type="button" onClick={() => setFilter(null)} aria-label={t("Str.Dashboard.ClearFilter")} className="grid size-6 place-items-center rounded-md text-ink-tertiary hover:bg-card hover:text-ink">
            <X className="size-4" />
          </button>
          {filter.drill ? (
            <Button variant="secondary" size="sm" className="ms-auto" icon={<ExternalLink className="size-4" />} onClick={drill(filter.drill, openSearch(filter))}>
              {t("Str.Dashboard.OpenFiltered", t(pageByKey.get(filter.drill)?.title ?? ""))}
            </Button>
          ) : null}
        </motion.div>
      ) : null}
      <div
        className="grid grid-cols-2 gap-2 sm:gap-3 md:grid-cols-3 lg:grid-cols-[repeat(var(--cols),minmax(0,1fr))]"
        style={{ "--cols": Math.ceil(spec.cards.length / Math.ceil(spec.cards.length / 6)) } as CSSProperties}
      >
        {spec.cards.map((card, index) => {
          const Icon = kindIcon[card.kind ?? "money"];
          return data.isLoading ? (
            <Skeleton key={card.key} className="h-28" />
          ) : (
            <motion.div key={card.key} {...enterAt(index)}>
              <StatCard
                label={t(`Str.Analytics.${spec.key}.${card.key}`)}
                value={data.data?.cards[camel(card.key)]?.value ?? 0}
                kind={card.kind}
                delay={index * ENTRANCE_STEP}
                change={data.data?.cards[camel(card.key)]?.change}
                lowerIsBetter={card.lowerIsBetter}
                icon={<Icon className="size-5" />}
                accent={palette[index % palette.length]}
                onOpen={drill(card.drill, openSearch(filter))}
              />
            </motion.div>
          );
        })}
      </div>
      <div className="grid gap-3 md:grid-cols-2 lg:grid-cols-3">
        {spec.charts.map((chart, index) => {
          const series = data.data?.charts[camel(chart.key)];
          return (
            <div
              key={chart.key}
              className={cn("rounded-2xl border border-line/70 bg-card p-4 shadow-card transition-shadow hover:shadow-pop", chart.span === 2 && "md:col-span-2 lg:col-span-2", chart.span === 3 && "md:col-span-2 lg:col-span-3")}
            >
              <button type="button" onDoubleClick={drill(chart.drill, openSearch(filter))} disabled={!chart.drill} className="mb-3 block text-start">
                <h3 className="font-semibold text-ink">{t(`Str.Analytics.${spec.key}.${chart.key}`)}</h3>
                <p className="text-xs text-ink-tertiary">{t(`Str.Analytics.${spec.key}.${chart.key}.Question`)}</p>
              </button>
              {data.isLoading || !series ? <Skeleton className="h-64" /> : !hasData(series) ? <p className="grid h-[260px] place-items-center text-sm text-ink-faint">{t("Str.NoData")}</p> : <Suspense fallback={<Skeleton className="h-64" />}><Chart kind={chart.kind} tone={chart.tone} smooth={chart.smooth} data={series} onPick={pick(chart)} onOpen={open(chart)} onFinished={() => finished(chart.key)} delay={chartsStart + index * ENTRANCE_STEP} /></Suspense>}
            </div>
          );
        })}
      </div>
    </div>
  );
}

function CustomPage({ spec }: { spec: CustomSpec }) {
  const Component = screens[spec.screen]!;
  return (
    <Suspense fallback={<Skeleton className="h-full" />}>
      <Component spec={spec} />
    </Suspense>
  );
}

export function PageHost({ spec }: { spec: PageSpec }) {
  switch (spec.type) {
    case "list":
      return <EntityPage key={spec.key} spec={spec} />;
    case "document":
      return <DocumentPage key={spec.key} spec={spec} />;
    case "report":
      return <ReportPage key={spec.key} spec={spec} />;
    case "dashboard":
      return <DashboardPage key={spec.key} spec={spec} />;
    case "custom":
      return <CustomPage key={spec.key} spec={spec} />;
  }
}
