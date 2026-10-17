import type { ChartData, ChartTone } from "@/features/workspace/spec";
import { formatMoney } from "@/lib/format";
import { text } from "@/lib/i18n";
import { severity } from "@/lib/palette";

/** محتوى الرسوم — اللون والترتيب والتلميح — مشتركٌ بين ECharts في الويب وSkia في الأندرويد، والرسم لكلٍّ */

export { compact } from "@/lib/format";
export const percent = (value: number) => `${value.toFixed(1)}%`;
const signed = (value: number) => `${value >= 0 ? "+" : "−"}${formatMoney(Math.abs(value))}`;

/** مدّة بقاء الشريحة بعيدةً بعد النقر */
export const PULL_HOLD = 1000;

/** ترتيب القيمة بين قيمها، من الأكبر */
export const rankOf = (values: number[], value: number) => [...values].sort((a, b) => b - a).indexOf(value) + 1;

/** مؤشّر اللون: فهرس لوحة الرسوم (Auto)، أو لونٌ من سلّم الشدّة */
export const colorOf = (index: number, count: number, tone: ChartTone, resolved: string[]) => {
  if (tone === "Auto") return resolved[index % resolved.length]!;
  const scale = tone === "Severity" ? severity : [...severity].reverse();
  return scale[count <= 1 ? 0 : Math.round((index / (count - 1)) * (scale.length - 1))]!;
};

/** لون المقياس: كلما قلّت النسبة اشتدّ */
export const gaugeColor = (value: number) => colorOf(Math.round((100 - value) / 25), 5, "Severity", []);

/** شرائح الدائري من الأكبر إلى الأصغر، إلا ترتيب الشدّة فله معناه */
export const donutOrder = (data: ChartData, tone: ChartTone) => {
  const order = data.labels.map((_, i) => i);
  if (tone === "Auto") order.sort((a, b) => (data.series[0]?.values[b] ?? 0) - (data.series[0]?.values[a] ?? 0));
  return order;
};

/** سطر تلميح: series لنقطة اللون، وtrend لتلوين الفرق */
export type InsightRow = { label: string; value: string; strong?: boolean; series?: number; trend?: "up" | "down" };
export type Insight = { title: string; rows: InsightRow[] };

const deltaRow = (label: string, diff: number, base: number): InsightRow => ({
  label,
  value: `${signed(diff)}${base ? ` (${percent((diff / Math.abs(base)) * 100)})` : ""}`,
  trend: diff >= 0 ? "up" : "down",
});

/** تلميح العمود: القيمة · الحصّة · الترتيب · الفرق عن السلسلة الأخرى */
export const columnInsight = (data: ChartData, seriesIndex: number, index: number): Insight => {
  const s = data.series[seriesIndex]!;
  const v = s.values[index] ?? 0;
  const total = s.values.reduce((a, x) => a + x, 0);
  const other = data.series.find((_, i) => i !== seriesIndex);
  const otherValue = other?.values[index] ?? 0;
  return {
    title: data.labels[index] ?? "",
    rows: [
      { label: s.name, value: formatMoney(v), strong: true, series: seriesIndex },
      { label: text("Str.Chart.Share"), value: percent(total ? (v / total) * 100 : 0) },
      { label: text("Str.Chart.Rank"), value: `${rankOf(s.values, v)} / ${s.values.length}` },
      ...(other ? [deltaRow(`${text("Str.Chart.Vs")} ${other.name}`, v - otherValue, otherValue)] : []),
    ],
  };
};

/** تلميح الخطّ: كل سلسلةٍ بقيمتها والفرق عن السابق */
export const lineInsight = (data: ChartData, index: number): Insight => ({
  title: data.labels[index] ?? "",
  rows: data.series.flatMap((s, k) => {
    const v = s.values[index] ?? 0;
    const previous = index > 0 ? (s.values[index - 1] ?? 0) : null;
    return [{ label: s.name, value: formatMoney(v), strong: true, series: k }, ...(previous === null ? [] : [deltaRow(text("Str.Chart.VsPrevious"), v - previous, previous)])];
  }),
});

/** تلميح الشريحة: القيمة · الحصّة · الترتيب · الفرق عن المتوسط */
export const sliceInsight = (name: string, value: number, values: number[]): Insight => {
  const total = values.reduce((s, v) => s + v, 0);
  const average = values.length ? total / values.length : 0;
  return {
    title: name,
    rows: [
      { label: text("Str.Dashboard.Value"), value: formatMoney(value), strong: true },
      { label: text("Str.Chart.Share"), value: percent(total ? (value / total) * 100 : 0) },
      { label: text("Str.Chart.Rank"), value: `${rankOf(values, value)} / ${values.length}` },
      deltaRow(text("Str.Chart.VsAverage"), value - average, average),
    ],
  };
};
