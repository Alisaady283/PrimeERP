import type { ChartData } from "@/features/workspace/spec";

/** تصفية اللوحة كـBI — منطقها مشتركٌ بين الويب والأندرويد (والدسكتوب بنظيره في DashboardRenderer) */

/** ما نُقر عليه: بُعده ومفتاحه في الخادم واسمه الظاهر */
export type DashboardFilter = { dimension: string; key: string; label: string };

/** تصفية العنصر المنقور، أو null لما لا يُصفّي (رسمٌ بلا مفاتيح · «أخرى») */
export const filterOf = (chart: ChartData | undefined, index: number): DashboardFilter | null => {
  const key = chart?.keys?.[index];
  return chart?.dimension && key ? { dimension: chart.dimension, key, label: chart.labels[index] ?? key } : null;
};

/** النقر على المختار نفسه يلغيه، وعلى غيره يستبدله */
export const toggleFilter = (current: DashboardFilter | null, next: DashboardFilter | null) =>
  !next ? current : current && current.dimension === next.dimension && current.key === next.key ? null : next;

/** معاملا الطلب */
export const filterParams = (filter: DashboardFilter | null) => (filter ? { dimension: filter.dimension, filterKey: filter.key } : {});

/** ما تُفتح به صفحة التفصيل مصفّاةً بالعنصر: الشهر نطاقُ تاريخه، وغيره نصُّه في البحث */
export const openSearch = (filter: DashboardFilter | null, label?: string): { q?: string; from?: string; to?: string } => {
  if (filter?.dimension === "Month" && /^\d{4}-\d{2}$/.test(filter.key)) {
    const [year, month] = filter.key.split("-").map(Number) as [number, number];
    return { from: `${filter.key}-01`, to: `${filter.key}-${String(new Date(year, month, 0).getDate()).padStart(2, "0")}` };
  }
  const q = filter?.label ?? label;
  return q ? { q } : {};
};
