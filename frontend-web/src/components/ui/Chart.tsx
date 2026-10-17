import { BarChart, GaugeChart, LineChart, PieChart } from "echarts/charts";
import { DataZoomComponent, GridComponent, LegendComponent, TooltipComponent } from "echarts/components";
import * as echarts from "echarts/core";
import { CanvasRenderer } from "echarts/renderers";
import type { EChartsCoreOption } from "echarts/core";
import { motion } from "motion/react";
import { useEffect, useMemo, useRef, useState } from "react";
import { useLanguage } from "@/lib/i18n";
import { splitClicks } from "@/lib/clicks";
import { colorOf as pickColor, columnInsight, compact, donutOrder, gaugeColor, lineInsight, percent, PULL_HOLD, sliceInsight, type Insight } from "@/lib/chartInsights";
import { bezier, duration, easing, staggerDelay, tween } from "@/lib/motion";
import { alpha, toHex } from "@/lib/color";
import { palette, shades } from "@/lib/palette";
import type { ChartData, ChartKind, ChartTone } from "@/features/workspace/spec";

echarts.use([BarChart, LineChart, PieChart, GaugeChart, GridComponent, LegendComponent, TooltipComponent, DataZoomComponent, CanvasRenderer]);


/** متغيّر CSS لونًا ست عشريًا */
const token = (value: string) => {
  const name = /var\((--[\w-]+)\)/.exec(value)?.[1];
  return toHex(name ? getComputedStyle(document.documentElement).getPropertyValue(name) : value);
};

/** درجتا التدرّج من سلّم Tailwind */
const rampOf = (index: number, count: number, tone: ChartTone): [string, string] => {
  if (tone !== "Auto") {
    const c = colorOf(index, count, tone);
    return [c, alpha(c, 0.8)];
  }
  const shade = shades[index % shades.length]!;
  return [token(shade.light), token(shade.dark)];
};

const colorOf = (index: number, count: number, tone: ChartTone) => pickColor(index, count, tone, palette.map(token));


const dot = (color: string) => `<span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:${color};margin-inline-end:6px"></span>`;
const row = (label: string, value: string, strong = false) =>
  `<div style="display:flex;justify-content:space-between;gap:16px;margin-top:3px"><span style="opacity:.7">${label}</span><span style="font-variant-numeric:tabular-nums;${strong ? "font-weight:700" : ""}">${value}</span></div>`;
/** التلميح المشترك (chartInsights) بـHTML: نقطة لون السلسلة، والفرق أخضر أو أحمر */
const tipHtml = ({ title, rows }: Insight, colorFor: (series: number) => string, titleColor?: string) =>
  `<div style="font-weight:700;margin-bottom:2px">${titleColor ? dot(titleColor) : ""}${title}</div>` +
  rows
    .map((r) => {
      const label = r.series === undefined ? r.label : `${dot(colorFor(r.series))}${r.label}`;
      const value = r.trend ? `<span style="color:${token(r.trend === "up" ? "var(--color-success)" : "var(--color-danger)")};font-weight:600">${r.value}</span>` : r.value;
      return row(label, value, r.strong);
    })
    .join("");

const enter = bezier(easing.decelerate);

const move = bezier(easing.emphasized);

/** خيارات الرسم */
const optionOf = (kind: ChartKind, data: ChartData, tone: ChartTone, smooth: boolean, narrow: boolean, delay: number): EChartsCoreOption => {
  const ink = token("var(--color-ink-tertiary)");
  const line = token("var(--color-line)");
  const surface = token("var(--color-card)");
  const font = token("var(--font-sans)") || "sans-serif";
  const textStyle = { fontFamily: font, color: ink, fontSize: 11 };
  const tooltip = {
    confine: true,
    backgroundColor: surface,
    borderColor: line,
    borderWidth: 1,
    padding: [10, 12],
    textStyle: { fontFamily: font, color: token("var(--color-ink)"), fontSize: 12 },
    extraCssText: "border-radius:12px;box-shadow:var(--shadow-pop);min-width:180px;",
  };
  const legend = data.series.length > 1 ? { bottom: 0, icon: "circle", itemWidth: 8, itemHeight: 8, itemGap: 16, textStyle, type: "scroll" } : undefined;
  const animation = { animationDelay: delay, animationDuration: duration.long2, animationEasing: enter, animationDurationUpdate: duration.medium2, animationEasingUpdate: move, stateAnimation: { duration: duration.short3, easing: "cubicOut" as const } };

  if (kind === "Donut") {
    /** من الأكبر إلى الأصغر، إلا ترتيب الشدّة فله معناه */
    const order = donutOrder(data, tone);
    const values = order.map((i) => data.series[0]?.values[i] ?? 0);
    return {
      ...animation,
      textStyle,
      tooltip: {
        ...tooltip,
        trigger: "item",
        formatter: (p: { name: string; value: number; color: string }) => tipHtml(sliceInsight(p.name, p.value, values), () => p.color, p.color),
      },
      series: [{
        type: "pie",
        radius: narrow ? ["50%", "76%"] : ["56%", "82%"],
        center: ["50%", "50%"],
        selectedMode: "single",
        selectedOffset: 12,
        animationType: "expansion",
        animationDelay: delay,
        itemStyle: { borderColor: surface, borderWidth: 2, borderRadius: 6 },
        label: { show: false, position: "center" },
        emphasis: {
          scale: false,
          focus: "self",
          label: { show: true, formatter: (p: { name: string; percent: number }) => `{v|${percent(p.percent)}}\n{n|${p.name}}`, rich: { v: { fontSize: 20, fontWeight: 700, color: token("var(--color-ink)"), fontFamily: font }, n: { fontSize: 11, color: ink, fontFamily: font, padding: [4, 0, 0, 0] } } },
        },
        blur: { itemStyle: { opacity: 0.35 } },
        data: order.map((i, at) => {
          const [light, dark] = rampOf(tone === "Auto" ? at : i, data.labels.length, tone);
          return {
            name: data.labels[i],
            value: values[at] ?? 0,
            itemStyle: { color: new echarts.graphic.LinearGradient(0, 0, 1, 1, [{ offset: 0, color: light }, { offset: 1, color: dark }]) },
          };
        }),
      }],
    };
  }

  if (kind === "Gauge") {
    const value = Math.max(0, Math.min(100, data.series[0]?.values[0] ?? 0));
    const color = gaugeColor(value);
    return {
      series: [{
        type: "gauge",
        startAngle: 210,
        endAngle: -30,
        min: 0,
        max: 100,
        progress: { show: true, roundCap: true, width: 16, itemStyle: { color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [{ offset: 0, color: alpha(color, 0.55) }, { offset: 1, color }]), shadowBlur: 12, shadowColor: alpha(color, 0.4) } },
        axisLine: { roundCap: true, lineStyle: { width: 16, color: [[1, token("var(--color-paper)")]] } },
        axisTick: { show: false },
        splitLine: { show: false },
        axisLabel: { show: false },
        pointer: { show: false },
        anchor: { show: false },
        title: { show: false },
        detail: { valueAnimation: true, offsetCenter: [0, 0], fontSize: 30, fontWeight: 700, color: token("var(--color-ink)"), fontFamily: font, formatter: "{value}%" },
        animationDelay: delay,
        animationDuration: duration.long2,
        animationEasing: enter,
        data: [{ value: Math.round(value) }],
      }],
    };
  }

  const horizontal = kind === "Bar";
  const bars = kind === "Column" || horizontal;
  const count = data.labels.length;
  const category = { type: "category", data: data.labels, axisLine: { show: false }, axisTick: { show: false }, axisLabel: { ...textStyle, hideOverlap: true, width: horizontal ? 110 : undefined, overflow: "truncate" } };
  const value = { type: "value", splitLine: { lineStyle: { color: line, type: [3, 6] } }, axisLabel: { ...textStyle, formatter: compact } };
  const long = !horizontal && count > 24;
  const single = data.series.length === 1;
  /** تلميح العمود */
  const barTip = (p: { seriesIndex: number; dataIndex: number; color: unknown }) => {
    const color = colorOf(single && tone !== "Auto" ? p.dataIndex : p.seriesIndex, single && tone !== "Auto" ? count : data.series.length, tone);
    return tipHtml(columnInsight(data, p.seriesIndex, p.dataIndex), () => color);
  };

  /** تلميح الخطّ */
  const lineTip = (items: Array<{ seriesIndex: number; dataIndex: number; color: unknown }>) => {
    return tipHtml(lineInsight(data, items[0]?.dataIndex ?? 0), (k) => colorOf(k, data.series.length, tone));
  };

  return {
    ...animation,
    textStyle,
    grid: { top: 16, left: 8, right: 8, bottom: (legend ? 28 : 4) + (long ? 28 : 0), containLabel: true },
    tooltip: bars
      ? { ...tooltip, trigger: "item", formatter: barTip }
      : { ...tooltip, trigger: "axis", axisPointer: { type: "line", lineStyle: { color: token("var(--color-line-strong)"), type: "dashed" } }, formatter: lineTip },
    legend,
    dataZoom: long ? [{ type: "slider", height: 16, bottom: legend ? 26 : 0, borderColor: "transparent", textStyle }] : undefined,
    xAxis: horizontal ? value : { ...category, boundaryGap: bars },
    yAxis: horizontal ? { ...category, inverse: true } : value,
    series: data.series.map((s, i) => {
      const color = colorOf(i, data.series.length, tone);
      if (!bars)
        return {
          type: "line",
          name: s.name,
          data: s.values,
          smooth: smooth ? 0.35 : false,
          showSymbol: count <= 12,
          symbol: "circle",
          symbolSize: 7,
          lineStyle: { width: 2.5, color, type: s.comparison ? "dashed" : "solid", shadowColor: alpha(color, 0.35), shadowBlur: 10, shadowOffsetY: 6 },
          itemStyle: { color, borderColor: surface, borderWidth: 2 },
          areaStyle: kind === "Area" || single ? { color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [{ offset: 0, color: alpha(color, kind === "Area" ? 0.32 : 0.14) }, { offset: 1, color: alpha(color, 0) }]) } : undefined,
          emphasis: { focus: "series", scale: 1.6 },
        };
      const paint = ([light, dark]: [string, string]) => ({
        color: new echarts.graphic.LinearGradient(...((horizontal ? [1, 0, 0, 0] : [0, 0, 0, 1]) as [number, number, number, number]), [{ offset: 0, color: light }, { offset: 1, color: dark }]),
        borderRadius: horizontal ? [0, 6, 6, 0] : [6, 6, 0, 0],
        opacity: s.comparison ? 0.5 : 1,
      });
      return {
        type: "bar",
        name: s.name,
        barMaxWidth: horizontal ? 22 : count <= 4 ? 84 : 56,
        barCategoryGap: count <= 4 ? "28%" : "22%",
        barGap: "10%",
        itemStyle: paint(rampOf(i, data.series.length, tone)),
        label: single && !horizontal && count <= 8 && !narrow ? { show: true, position: "top", formatter: (p: { value: number }) => compact(p.value), color: ink, fontFamily: font, fontSize: 11, fontWeight: 600 } : undefined,
        data: single && tone !== "Auto" ? s.values.map((v, r) => ({ value: v, itemStyle: paint(rampOf(r, s.values.length, tone)) })) : s.values,
        emphasis: { focus: "self", itemStyle: { shadowBlur: 14, shadowColor: alpha(color, 0.45) } },
        blur: { itemStyle: { opacity: 0.35 } },
        animationDelay: (index: number) => delay + staggerDelay(index, count),
      };
    }),
  };
};

export function Chart({ kind, data, tone = "Auto", smooth = true, height = 260, onPick, onOpen, onFinished, delay = 0 }: { kind: ChartKind; data: ChartData; tone?: ChartTone; smooth?: boolean; height?: number; onPick?: (index: number) => void; onOpen?: (index: number) => void; onFinished?: () => void; delay?: number }) {
  const host = useRef<HTMLDivElement>(null);
  const chart = useRef<echarts.ECharts | null>(null);
  const language = useLanguage((s) => s.language);
  /** يُقاس قبل أول رسم: تغييره بعده يُلغي حركة الظهور */
  const [narrow, setNarrow] = useState<boolean | null>(null);
  const [hovering, setHovering] = useState(false);
  const handlers = useRef({ onPick, onOpen });
  handlers.current = { onPick, onOpen };
  const clicks = useMemo(() => splitClicks<number>((i) => handlers.current.onPick?.(i), (i) => handlers.current.onOpen?.(i)), []);
  const total = kind === "Donut" ? (data.series[0]?.values ?? []).reduce((s, v) => s + v, 0) : 0;

  useEffect(() => {
    const element = host.current!;
    chart.current = echarts.init(element, undefined, { renderer: "canvas" });
    let size = `${element.clientWidth}x${element.clientHeight}`;
    setNarrow(element.clientWidth < 420);
    const observer = new ResizeObserver(([entry]) => {
      if (!entry) return;
      const next = `${Math.round(entry.contentRect.width)}x${Math.round(entry.contentRect.height)}`;
      // البلاغ الأول بالحجم نفسه لا يُعيد الرسم
      if (next === size) return;
      size = next;
      setNarrow(entry.contentRect.width < 420);
      chart.current?.resize();
    });
    observer.observe(element);
    return () => {
      observer.disconnect();
      chart.current?.dispose();
      chart.current = null;
    };
  }, []);

  useEffect(() => {
    if (narrow === null) return;
    chart.current?.setOption(optionOf(kind, data, tone, smooth, narrow, delay), true);
  }, [kind, data, tone, smooth, narrow, language, delay]);

  const finished = useRef(onFinished);
  finished.current = onFinished;
  useEffect(() => {
    const instance = chart.current;
    if (!instance) return;
    const once = () => {
      instance.off("finished", once);
      finished.current?.();
    };
    instance.on("finished", once);
    return () => void instance.off("finished", once);
  }, [data]);

  /** الشريحة المنقورة تبتعد ثم تعود */
  useEffect(() => {
    const instance = chart.current;
    if (!instance || kind !== "Donut") return;
    let timer = 0;
    const pull = (p: { dataIndex: number }) => {
      window.clearTimeout(timer);
      timer = window.setTimeout(() => instance.dispatchAction({ type: "unselect", seriesIndex: 0, dataIndex: p.dataIndex }), PULL_HOLD);
    };
    const over = () => setHovering(true);
    const out = () => setHovering(false);
    instance.on("click", pull);
    instance.on("mouseover", over);
    instance.on("mouseout", out);
    return () => {
      window.clearTimeout(timer);
      instance.off("click", pull);
      instance.off("mouseover", over);
      instance.off("mouseout", out);
    };
  }, [kind]);

  useEffect(() => {
    const instance = chart.current;
    if (!instance) return;
    const index = (p: { name: string }) => data.labels.indexOf(p.name);
    const pick = (p: { name: string }) => clicks.click(index(p));
    const open = (p: { name: string }) => clicks.double(index(p));
    instance.on("click", pick);
    instance.on("dblclick", open);
    return () => {
      clicks.cancel();
      instance.off("click", pick);
      instance.off("dblclick", open);
    };
  }, [clicks, kind, data]);

  return (
    <div className="relative">
      <div ref={host} style={{ height }} className="w-full" />
      {kind === "Donut" && !hovering && (
        <motion.span
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={tween(duration.short4, easing.decelerate, delay + duration.long2)}
          style={{ top: height / 2 }}
          className="pointer-events-none absolute inset-x-0 -translate-y-1/2 text-center text-lg font-bold tabular-nums text-ink"
        >
          {compact(total)}
        </motion.span>
      )}
      {kind === "Donut" && (
        <ul className="mt-2 flex flex-wrap justify-center gap-x-3 gap-y-1.5">
          {donutOrder(data, tone).map((i, at) => {
            const name = data.labels[i] ?? "";
            const value = data.series[0]?.values[i] ?? 0;
            return (
              <li key={name}>
                <button type="button" onClick={() => clicks.click(i)} onDoubleClick={() => clicks.double(i)}
                  onMouseEnter={() => chart.current?.dispatchAction({ type: "highlight", seriesIndex: 0, name })}
                  onMouseLeave={() => chart.current?.dispatchAction({ type: "downplay", seriesIndex: 0, name })}
                  className="inline-flex items-center gap-1.5 rounded-full px-1.5 py-0.5 text-xs text-ink-soft transition-colors hover:bg-paper hover:text-ink">
                  <span className="size-2 shrink-0 rounded-full" style={{ background: rampOf(tone === "Auto" ? at : i, data.labels.length, tone)[0] }} />
                  {name}
                  <span className="tabular-nums text-ink-tertiary">{percent(total ? (value / total) * 100 : 0)}</span>
                </button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
