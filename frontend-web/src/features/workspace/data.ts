import { keepPreviousData, useInfiniteQuery, useMutation, useQuery, useQueryClient, type QueryClient } from "@tanstack/react-query";
import type { ChartData } from "@/features/workspace/spec";
import type { Option } from "@/components/ui/Combobox";
import { api, type PageResponse } from "@/lib/api";
import { text } from "@/lib/i18n";
import type { Source } from "@/features/workspace/spec";
import { actionKeys } from "@/catalog/actions";
import { pageByKey } from "@/catalog/pages";
import { filterParams, type DashboardFilter } from "@/features/workspace/dashboardFilter";

export type Row = Record<string, unknown> & { id?: number };
export type Query = Record<string, string | number | boolean | null | undefined>;

const PAGE_SIZE = 100;

export const pageKeys = {
  all: (key: string) => ["page", key] as const,
  list: (key: string, query: Query) => ["page", key, "list", query] as const,
  record: (key: string, id: number) => ["page", key, "record", id] as const,
};

const rowsOptions = (key: string, query: Query) => ({
  queryKey: pageKeys.list(key, query),
  queryFn: ({ pageParam, signal }: { pageParam: number; signal: AbortSignal }) => api.get<PageResponse<Row>>(`/pages/${key}`, { ...query, page: pageParam, pageSize: PAGE_SIZE }, signal),
  initialPageParam: 1,
  getNextPageParam: (last: PageResponse<Row>) => (last.page * last.pageSize < last.total ? last.page + 1 : undefined),
});

export const usePageRows = (key: string, query: Query) => useInfiniteQuery({ ...rowsOptions(key, query), placeholderData: keepPreviousData });

export type AnalyticsResponse = { cards: Record<string, { value: number; change: number | null; note: string | null }>; charts: Record<string, ChartData> };

/** رسمٌ فيه قيمةٌ غير صفرية */
export const hasData = (chart?: ChartData) => !!chart?.series.some((s) => s.values.some((v) => v !== 0));
export type Range = { from: string; to: string };

/** من أول السنة إلى اليوم */
export const yearToDate = (): Range => ({ from: `${new Date().getFullYear()}-01-01`, to: new Date().toISOString().slice(0, 10) });

export const analyticsOptions = (key: string, range: Range, filter: DashboardFilter | null = null) => ({
  queryKey: ["analytics", key, range, filter?.dimension ?? null, filter?.key ?? null] as const,
  queryFn: ({ signal }: { signal: AbortSignal }) => api.get<AnalyticsResponse>(`/analytics/${key}`, { ...range, ...filterParams(filter) }, signal),
});

/** تحميلٌ مسبق عند المرور */
export const prefetchPage = (client: QueryClient, key: string) => {
  const type = pageByKey.get(key)?.type;
  if (type === "list" || type === "document") void client.prefetchInfiniteQuery({ ...rowsOptions(key, { search: "" }), staleTime: 30_000 });
  if (type === "dashboard") void client.prefetchQuery({ ...analyticsOptions(key, yearToDate()), staleTime: 30_000 });
};

export const usePageRecord = (key: string, id: number | null) =>
  useQuery({
    queryKey: pageKeys.record(key, id ?? 0),
    queryFn: ({ signal }) => api.get<Row>(`/pages/${key}/${id}`, undefined, signal),
    enabled: id !== null && id > 0,
  });

export const useSavePage = (key: string, saveTo?: string) => {
  const client = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: number | null; body: unknown }): Promise<void> => {
      if (saveTo) await api.post<void>(saveTo, body);
      else if (id) await api.put<void>(`/pages/${key}/${id}`, body);
      else await api.post<Row>(`/pages/${key}`, body);
    },
    onSuccess: () => client.invalidateQueries({ queryKey: pageKeys.all(key) }),
  });
};

export const useDeletePage = (key: string) => {
  const client = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => api.delete(`/pages/${key}/${id}`),
    onSuccess: () => client.invalidateQueries({ queryKey: pageKeys.all(key) }),
  });
};

type RemoteOption = { value: string | number; label: string; hint: string | null; id?: number };

const sourceKey = (source: Source) =>
  "page" in source ? `page:${source.page}:${source.by ?? "id"}:${JSON.stringify(source.where ?? {})}` : "categories" in source ? `cat:${source.categories}` : "accounts" in source ? `accounts:${source.by ?? "code"}` : "alertEvents" in source ? "alertEvents" : "enum";

export const optionsQuery = (source: Source | undefined) => ({
  queryKey: ["options", source ? sourceKey(source) : "none"],
  enabled: !!source && !local(source),
  staleTime: 5 * 60_000,
  queryFn: async ({ signal }: { signal: AbortSignal }): Promise<Option[]> => {
    if (!source || local(source)) return [];
    if ("accounts" in source) {
      const list = await api.get<RemoteOption[]>("/accounts/leaves", undefined, signal);
      return list.map((o) => ({ value: source.by === "id" ? (o.id ?? o.value) : o.value, label: o.label, hint: o.hint ?? undefined }));
    }
    if ("alertEvents" in source) {
      const list = await api.get<Array<{ key: string; title: string }>>("/alerts/events", undefined, signal);
      return list.map((e) => ({ value: e.key, label: e.title, hint: e.key }));
    }
    if ("categories" in source) {
      const list = await api.get<Array<{ id: number; name: string }>>(`/categories/${source.categories}`, undefined, signal);
      return list.map((c) => ({ value: c.id, label: c.name }));
    }
    const list = await api.get<RemoteOption[]>(`/pages/${source.page}/options`, source.where, signal);
    return list.map((o) => ({ value: source.by === "code" ? (o.hint ?? String(o.value)) : o.value, label: o.label, hint: o.hint ?? undefined }));
  },
});

export const useOptions = (source: Source | undefined) => useQuery(optionsQuery(source));

/** مصادر المتصفح */
const local = (source: Source) => "enumOf" in source || "pages" in source || "actions" in source;

export const staticOptions = (source: Source | undefined): Option[] | null => {
  if (!source) return null;
  if ("enumOf" in source) return source.enumOf.map(([value, label]) => ({ value, label: text(label) }));
  if ("pages" in source) return [...pageByKey.values()].map((p) => ({ value: p.key, label: text(p.title), hint: p.key }));
  if ("actions" in source) return actionKeys.map(([value, label]) => ({ value, label: text(label), hint: value }));
  return null;
};
