import { BlurMask, Canvas, Circle, DashPathEffect, Group, LinearGradient, Line as SkLine, Path, Skia, useFont, vec } from "@shopify/react-native-skia";
import * as Haptics from "expo-haptics";
import { createContext, useContext, useEffect, useMemo, useRef, useState } from "react";
import { ExternalLink, X } from "lucide-react-native";
import { Pressable, Text, View, type GestureResponderEvent } from "react-native";
import Animated, { Easing, FadeIn, FadeOut, runOnJS, useAnimatedReaction, useAnimatedStyle, useDerivedValue, useSharedValue, withDelay, withSequence, withTiming, type SharedValue } from "react-native-reanimated";
import { useCSSVariable } from "uniwind";
import { Area, Bar, CartesianChart, Line, useChartPressState } from "victory-native";
import type { ChartData, ChartKind, ChartTone } from "@/features/workspace/spec";
import { colorOf, columnInsight, compact, donutOrder, gaugeColor, lineInsight, percent, PULL_HOLD, rankOf, sliceInsight, type Insight } from "@/lib/chartInsights";
import { alpha, toHex } from "@/lib/color";
import { formatMoney } from "@/lib/format";
import { duration, easing, staggerDelay } from "@/lib/motion";
import { palette, shades } from "@/lib/palette";
import { useStart } from "~/lib/direction";
import { AnimatedNumber } from "~/components/ui/AnimatedNumber";
import { Icon } from "~/components/ui/Icon";

const plexArabic = require("@expo-google-fonts/ibm-plex-sans-arabic/400Regular/IBMPlexSansArabic_400Regular.ttf");

const bezier = ([a, b, c, d]: readonly number[]) => Easing.bezier(a!, b!, c!, d!);
const decelerate = bezier(easing.decelerate);
const emphasized = bezier(easing.emphasized);

/** Skia لا يشكّل العربية: داخل اللوحة أرقامٌ لاتينية فقط، والأسماء خارجها */
const compactLatin = (value: number) => new Intl.NumberFormat("en", { notation: "compact", maximumFractionDigits: 1 }).format(value);
const latinOnly = (labels: string[]) => labels.every((l) => /^[\x20-\x7e]*$/.test(l));

/** ألوان اللوحة ودرجتا تدرّج كل لون — من tokens.css كالويب */
function usePalette() {
  const base = useCSSVariable(palette.map((p) => p.slice(4, -1))).map((v) => toHex(String(v ?? "#2b7fff")));
  const light = useCSSVariable(shades.map((s) => s.light.slice(4, -1))).map((v) => toHex(String(v ?? "#60a5fa")));
  const dark = useCSSVariable(shades.map((s) => s.dark.slice(4, -1))).map((v) => toHex(String(v ?? "#1d4ed8")));
  return { base, ramp: (i: number, count: number, tone: ChartTone): [string, string] => (tone === "Auto" ? [light[i % light.length]!, dark[i % dark.length]!] : [colorOf(i, count, tone, base), alpha(colorOf(i, count, tone, base), 0.8)]) };
}

/** عرض بطاقة التلميح الطافية */
const TIP_WIDTH = 200;

/** بطاقة التلميح — قطعةٌ واحدة تطفو فوق الرسم عند الإصبع كتلميح الويب، ومحتواها مقاييس محسوبةٌ مرّة (chartInsights) */
type ChartAction = { label: string; run: (index: number) => void };

function InsightCard({ insight, colorFor, action, onClose }: { insight: Insight; colorFor: (series: number) => string; action?: () => void; onClose?: () => void }) {
  const start = useStart();
  const pinned = !!(action || onClose);
  return (
    <Animated.View entering={FadeIn.duration(duration.short3)} exiting={FadeOut.duration(duration.short3)} pointerEvents={pinned ? "auto" : "none"} className="gap-1 rounded-xl border border-line bg-card px-3 py-2.5 shadow-pop" style={{ width: TIP_WIDTH }}>
      <Text numberOfLines={1} className={`font-sans text-[13px] font-bold text-ink ${start}`}>{insight.title}</Text>
      {insight.rows.map((r, i) => (
        <View key={`${r.label}-${i}`} className="flex-row items-center justify-between gap-4">
          <View className="flex-1 flex-row items-center gap-1.5">
            {r.series !== undefined ? <View className="size-2 rounded-full" style={{ backgroundColor: colorFor(r.series) }} /> : null}
            <Text numberOfLines={1} className={`font-sans text-xs text-ink-tertiary ${start}`}>{r.label}</Text>
          </View>
          <Text className={`font-sans text-xs ${r.trend === "up" ? "font-semibold text-success" : r.trend === "down" ? "font-semibold text-danger" : r.strong ? "font-bold text-ink" : "text-ink-soft"}`} style={{ fontVariant: ["tabular-nums"] }}>
            {r.value}
          </Text>
        </View>
      ))}
      {pinned ? <CardFooter action={action} onClose={onClose} /> : null}
    </Animated.View>
  );
}

/** تذييل البطاقة المثبّتة: الانتقال إلى التفصيل مُصفّى، والإغلاق */
function CardFooter({ action, onClose }: { action?: () => void; onClose?: () => void }) {
  const label = useActionLabel();
  return (
    <View className="mt-1.5 flex-row items-center gap-2 border-t border-line pt-2">
      {action && label ? (
        <Pressable hitSlop={6} onPress={action} className="flex-1 flex-row items-center gap-1.5 active:opacity-70">
          <Icon icon={ExternalLink} size={14} tone="brand" />
          <Text numberOfLines={1} className="flex-1 font-sans text-xs font-semibold text-brand">{label}</Text>
        </Pressable>
      ) : (
        <View className="flex-1" />
      )}
      {onClose ? (
        <Pressable hitSlop={10} onPress={onClose} className="rounded-md p-0.5 active:bg-paper">
          <Icon icon={X} size={14} tone="ink-tertiary" />
        </Pressable>
      ) : null}
    </View>
  );
}

/** نصّ زرّ الانتقال من الرسم الأب */
const ActionLabel = createContext<string | undefined>(undefined);
const useActionLabel = () => useContext(ActionLabel);

/** خطّ المؤشّر ونقطته عند اللمس */
function Indicator({ x, y, top, bottom, color }: { x: SharedValue<number>; y: SharedValue<number>; top: number; bottom: number; color: string }) {
  const p1 = useDerivedValue(() => vec(x.value, top));
  const p2 = useDerivedValue(() => vec(x.value, bottom));
  return (
    <>
      <SkLine p1={p1} p2={p2} color={alpha(color, 0.5)} strokeWidth={1}>
        <DashPathEffect intervals={[4, 4]} />
      </SkLine>
      <Circle cx={x} cy={y} r={8} color={alpha(color, 0.2)} />
      <Circle cx={x} cy={y} r={4.5} color={color} />
      <Circle cx={x} cy={y} r={2} color="#ffffff" />
    </>
  );
}

/** الأعمدة والخطوط والمساحات: تنمو من الصفر، والسحب عليها يعرض التلميح */
/** ما يمرّره الأب للمس: onPick يصفّي اللوحة، وaction ينتقل إلى التفصيل، وdismiss عدّادٌ يُخفي البطاقة (تمرير الصفحة) */
type Touch = { onPick?: (index: number) => void; action?: ChartAction; dismiss?: number };

function Cartesian({ kind, data, tone, smooth, height, delay, onPick, action, dismiss }: { kind: ChartKind; data: ChartData; tone: ChartTone; smooth: boolean; height: number; delay: number } & Touch) {
  const font = useFont(plexArabic, 10);
  const [ink, line] = useCSSVariable(["--color-ink-tertiary", "--color-line"]).map((v) => toHex(String(v ?? "#888888")));
  const colors = usePalette();
  const perItem = tone !== "Auto" && data.series.length === 1;
  const colorFor = (k: number) => colorOf(k, data.series.length, tone, colors.base);
  const [grown, setGrown] = useState(false);
  const [picked, setPicked] = useState<number | null>(null);
  const keys = data.series.map((_, k) => `y${k}`);
  const { state, isActive } = useChartPressState({ x: 0, y: Object.fromEntries(keys.map((k) => [k, 0])) });
  const latin = latinOnly(data.labels);

  useEffect(() => {
    const timer = setTimeout(() => setGrown(true), delay);
    return () => clearTimeout(timer);
  }, [delay]);

  const [box, setBox] = useState(0);

  /** مقاييس كل نقطةٍ تُحسب مرّةً مع البيانات، ويُعرض منها ما تحت الإصبع */
  const insights = useMemo(() => data.labels.map((_, i) => (kind === "Column" && data.series.length === 1 ? columnInsight(data, 0, i) : lineInsight(data, i))), [data, kind]);

  /** البطاقة المثبّتة بعد رفع الإصبع: لقطةٌ من مقاييسها وموضعها، تبقى بعد إعادة تحميل اللوحة مُصفّاة */
  const [pinned, setPinned] = useState<{ index: number; insight: Insight; x: number } | null>(null);
  const last = useRef(-1);

  /** أثناء السحب: البطاقة تتبع الإصبع بنقرةٍ خفيفة لكل نقطة؛ وعند الرفع: تُثبَّت وتُصفّى اللوحة — والنقر على المثبّت نفسه يلغي */
  const choose = (index: number) => {
    if (index >= 0) {
      last.current = index;
      setPicked(index);
      void Haptics.selectionAsync();
      return;
    }
    const at = last.current;
    setPicked(null);
    if (at < 0 || !insights[at]) return;
    const x = Math.max(0, Math.min(box - TIP_WIDTH, ((at + 0.5) / Math.max(data.labels.length, 1)) * box - TIP_WIDTH / 2));
    setPinned((current) => (current?.index === at ? null : { index: at, insight: insights[at]!, x }));
    onPick?.(at);
  };
  useAnimatedReaction(
    () => (state.isActive.value ? state.matchedIndex.value : -1),
    (index, previous) => {
      if (index !== previous) runOnJS(choose)(index);
    },
  );
  useEffect(() => setPinned(null), [dismiss]);

  /** البطاقة تتبع الإصبع أفقياً داخل حدود الرسم */
  const float = useAnimatedStyle(() => ({ transform: [{ translateX: Math.max(0, Math.min(box - TIP_WIDTH, state.x.position.value - TIP_WIDTH / 2)) }] }));

  const rows = useMemo(
    () => data.labels.map((_, i) => Object.fromEntries([["x", i], ...data.series.map((s, k) => [`y${k}`, grown ? (s.values[i] ?? 0) : 0])]) as { x: number } & Record<string, number>),
    [data, grown],
  );
  const animate = { type: "timing" as const, duration: duration.long2, easing: decelerate };

  return (
    <View className="gap-2">
      <View style={{ height }} onLayout={(e) => setBox(e.nativeEvent.layout.width)}>
        {font ? (
          <CartesianChart
            data={rows}
            xKey="x"
            yKeys={keys as never}
            chartPressState={state as never}
            padding={{ left: 4, right: 4, top: 8, bottom: 4 }}
            domainPadding={kind === "Column" ? { left: 18, right: 18, top: 16 } : { top: 16 }}
            axisOptions={{
              font,
              labelColor: ink,
              lineColor: line,
              tickCount: { x: latin ? Math.min(6, data.labels.length) : 0, y: 4 },
              formatXLabel: (v) => (latin ? (data.labels[Number(v)] ?? "") : ""),
              formatYLabel: (v) => compactLatin(Number(v)),
            }}
          >
            {({ points, chartBounds }) => (
              <>
                {data.series.map((s, k) => {
                  const pts = (points as Record<string, never>)[`y${k}`]!;
                  const [light, dark] = colors.ramp(perItem ? 0 : k, perItem ? 1 : data.series.length, tone);
                  if (kind === "Column")
                    return (
                      <Bar key={s.name} points={pts} chartBounds={chartBounds} roundedCorners={{ topLeft: 5, topRight: 5 }} innerPadding={data.series.length > 1 ? 0.35 : 0.3} animate={animate} opacity={s.comparison ? 0.55 : 1}>
                        <LinearGradient start={vec(0, chartBounds.top)} end={vec(0, chartBounds.bottom)} colors={[light, dark]} />
                      </Bar>
                    );
                  return (
                    <Group key={s.name}>
                      {kind === "Area" ? (
                        <Area points={pts} y0={chartBounds.bottom} curveType={smooth ? "natural" : "linear"} animate={animate}>
                          <LinearGradient start={vec(0, chartBounds.top)} end={vec(0, chartBounds.bottom)} colors={[alpha(light, 0.45), alpha(light, 0.02)]} />
                        </Area>
                      ) : null}
                      <Line points={pts} color={alpha(dark, 0.35)} strokeWidth={6} curveType={smooth ? "natural" : "linear"} animate={animate}>
                        <BlurMask blur={6} style="normal" />
                      </Line>
                      <Line points={pts} color={colorFor(k)} strokeWidth={2.5} curveType={smooth ? "natural" : "linear"} animate={animate} />
                    </Group>
                  );
                })}
                {isActive ? <Indicator x={state.x.position} y={state.y[keys[0]!]!.position} top={chartBounds.top} bottom={chartBounds.bottom} color={colorFor(0)} /> : null}
              </>
            )}
          </CartesianChart>
        ) : null}
        {picked !== null && insights[picked] ? (
          <Animated.View pointerEvents="none" style={[{ position: "absolute", top: 4, left: 0 }, float]}>
            <InsightCard insight={insights[picked]!} colorFor={colorFor} />
          </Animated.View>
        ) : pinned ? (
          <View style={{ position: "absolute", top: 4, left: pinned.x }}>
            <ActionLabel.Provider value={action?.label}>
              <InsightCard insight={pinned.insight} colorFor={colorFor} action={action ? () => action.run(pinned.index) : undefined} onClose={() => setPinned(null)} />
            </ActionLabel.Provider>
          </View>
        ) : null}
      </View>
      {!latin ? (
        <View className="flex-row justify-between">
          <Text className="font-sans text-[11px] text-ink-tertiary">{data.labels[0]}</Text>
          <Text className="font-sans text-[11px] text-ink-tertiary">{data.labels[data.labels.length - 1]}</Text>
        </View>
      ) : null}
      {data.series.length > 1 ? (
        <View className="flex-row flex-wrap gap-x-4 gap-y-1">
          {data.series.map((s, k) => (
            <View key={s.name} className="flex-row items-center gap-1.5">
              <View className="size-2 rounded-full" style={{ backgroundColor: colorFor(k) }} />
              <Text className="font-sans text-xs text-ink-soft">{s.name}</Text>
            </View>
          ))}
        </View>
      ) : null}
    </View>
  );
}

/** شريحةٌ تدور مع دخول الحلقة، وتبتعد عند اختيارها */
function Slice({ path, from, sweep, mid, colors, progress, pulse }: { path: ReturnType<typeof Skia.Path.Make>; from: number; sweep: number; mid: number; colors: [string, string]; progress: SharedValue<number>; pulse: number }) {
  const pulled = useSharedValue(0);
  useEffect(() => {
    if (pulse) pulled.value = withSequence(withTiming(1, { duration: duration.medium2, easing: emphasized }), withDelay(PULL_HOLD, withTiming(0, { duration: duration.medium2, easing: emphasized })));
  }, [pulse, pulled]);
  const end = useDerivedValue(() => Math.max(0, Math.min(1, (progress.value * 360 - from) / sweep)));
  const transform = useDerivedValue(() => [{ translateX: Math.cos(mid) * pulled.value * 10 }, { translateY: Math.sin(mid) * pulled.value * 10 }]);
  return (
    <Group transform={transform}>
      <Path path={path} style="stroke" strokeWidth={30} start={0} end={end}>
        <LinearGradient start={vec(0, 0)} end={vec(240, 240)} colors={colors} />
      </Path>
    </Group>
  );
}

/** الدائري بـSkia: يدور من الأكبر، والنقر على شريحةٍ أو مفتاحها يبعدها لحظةً ويعرض حصّتها */
function Donut({ data, tone, height, delay, onPick, action, dismiss }: { data: ChartData; tone: ChartTone; height: number; delay: number } & Touch) {
  const colors = usePalette();
  const order = donutOrder(data, tone);
  const values = order.map((i) => Math.max(0, data.series[0]?.values[i] ?? 0));
  const labels = order.map((i) => data.labels[i] ?? "");
  const total = values.reduce((s, v) => s + v, 0);
  const size = height;
  const radius = size / 2 - 22;
  const center = size / 2;
  const progress = useSharedValue(0);
  const [selected, setSelected] = useState<number | null>(null);
  /** نبضة كل شريحة: تزيد عند اختيارها فتبتعد ثم تعود */
  const [pulses, setPulses] = useState<Record<number, number>>({});

  useEffect(() => {
    progress.value = withDelay(delay, withTiming(1, { duration: duration.long2 + 200, easing: decelerate }));
  }, [delay, progress]);

  /** الزوايا من الأعلى مع عقارب الساعة */
  const arcs = useMemo(() => {
    let at = 0;
    return values.map((v, i) => {
      const sweep = total ? (v / total) * 360 : 0;
      const from = at;
      at += sweep;
      const gap = sweep > 3 ? 1.5 : 0;
      const path = Skia.Path.Make();
      path.addArc({ x: center - radius, y: center - radius, width: radius * 2, height: radius * 2 }, from - 90 + gap / 2, Math.max(0.1, sweep - gap));
      return { path, from, sweep: Math.max(sweep, 0.0001), mid: ((from + sweep / 2 - 90) * Math.PI) / 180, colors: colors.ramp(tone === "Auto" ? i : order[i]!, data.labels.length, tone) };
    });
  }, [values.join(","), total, radius, center, tone]);

  /** الاختيار يُبعد الشريحة لحظةً (PULL_HOLD) ويُثبّت حصّتها وبطاقتها ويصفّي اللوحة — والنقر عليها ثانيةً يلغي */
  const [snapshot, setSnapshot] = useState<{ insight: Insight; color: string; share: string; label: string } | null>(null);
  const pick = (i: number) => {
    setPulses((p) => ({ ...p, [i]: (p[i] ?? 0) + 1 }));
    void Haptics.selectionAsync();
    const again = selected === i;
    setSelected(again ? null : i);
    setSnapshot(again ? null : {
      insight: sliceInsight(labels[i] ?? "", values[i] ?? 0, values),
      color: arcs[i]?.colors[1] ?? "#888",
      share: percent(total ? ((values[i] ?? 0) / total) * 100 : 0),
      label: labels[i] ?? "",
    });
    onPick?.(order[i]!);
  };
  useEffect(() => {
    setSelected(null);
    setSnapshot(null);
  }, [dismiss]);

  const touch = (e: GestureResponderEvent) => {
    const dx = e.nativeEvent.locationX - center;
    const dy = e.nativeEvent.locationY - center;
    const distance = Math.hypot(dx, dy);
    if (distance < radius - 24 || distance > radius + 24) return;
    const angle = ((Math.atan2(dy, dx) * 180) / Math.PI + 90 + 360) % 360;
    const i = arcs.findIndex((a) => angle >= a.from && angle < a.from + a.sweep);
    if (i >= 0) pick(i);
  };

  return (
    <View className="gap-3">
      <View className="items-center">
        <Pressable onPress={touch} style={{ width: size, height: size }}>
          <Canvas style={{ width: size, height: size }}>
            {arcs.map((a, i) => (
              <Slice key={labels[i]} path={a.path} from={a.from} sweep={a.sweep} mid={a.mid} colors={a.colors} progress={progress} pulse={pulses[i] ?? 0} />
            ))}
          </Canvas>
          {snapshot ? (
            <View className="absolute inset-x-0 top-0 z-10 items-center">
              <ActionLabel.Provider value={action?.label}>
                <InsightCard
                  insight={snapshot.insight}
                  colorFor={() => snapshot.color}
                  action={action && selected !== null ? () => action.run(order[selected]!) : undefined}
                  onClose={() => {
                    setSelected(null);
                    setSnapshot(null);
                  }}
                />
              </ActionLabel.Provider>
            </View>
          ) : null}
          <View className="absolute inset-0 items-center justify-center px-12" pointerEvents="none">
            {!snapshot ? (
              <AnimatedNumber className="text-center font-sans text-xl font-bold text-ink" value={total} format={compact} delay={delay} />
            ) : (
              <>
                <Text className="text-center font-sans text-xl font-bold text-ink">{snapshot.share}</Text>
                <Text numberOfLines={2} className="text-center font-sans text-[11px] text-ink-tertiary">{snapshot.label}</Text>
              </>
            )}
          </View>
        </Pressable>
      </View>
      <View className="flex-row flex-wrap gap-x-4 gap-y-2">
        {labels.map((label, i) => (
          <Pressable key={label} onPress={() => pick(i)} className={`flex-row items-center gap-1.5 rounded-full px-1.5 py-0.5 ${selected === i ? "bg-paper" : ""}`}>
            <View className="size-2.5 rounded-full" style={{ backgroundColor: arcs[i]?.colors[1] }} />
            <Text className="font-sans text-xs text-ink-soft">{label}</Text>
            <Text className="font-sans text-xs text-ink-tertiary">{total ? `${(((values[i] ?? 0) / total) * 100).toFixed(0)}%` : ""}</Text>
          </Pressable>
        ))}
      </View>
    </View>
  );
}

/** المقياس: قوسٌ 240° يمتلئ بتدرّجٍ وتوهّج، والرقم يعدّ معه */
function Gauge({ value, delay }: { value: number; delay: number }) {
  const [paper] = useCSSVariable(["--color-paper"]).map((v) => toHex(String(v ?? "#eef1f5")));
  const size = 200;
  const stroke = 16;
  const r = size / 2 - stroke;
  const color = gaugeColor(value);
  const progress = useSharedValue(0);
  const track = useMemo(() => {
    const p = Skia.Path.Make();
    p.addArc({ x: stroke, y: stroke, width: r * 2, height: r * 2 }, 150, 240);
    return p;
  }, [r]);
  useEffect(() => {
    progress.value = withDelay(delay, withTiming(value / 100, { duration: duration.long2, easing: decelerate }));
  }, [value, delay, progress]);
  return (
    <View className="items-center">
      <Canvas style={{ width: size, height: size * 0.8 }}>
        <Path path={track} style="stroke" strokeWidth={stroke} strokeCap="round" color={paper} />
        <Path path={track} style="stroke" strokeWidth={stroke} strokeCap="round" start={0} end={progress} color={alpha(color, 0.35)}>
          <BlurMask blur={8} style="normal" />
        </Path>
        <Path path={track} style="stroke" strokeWidth={stroke} strokeCap="round" start={0} end={progress}>
          <LinearGradient start={vec(0, size)} end={vec(size, 0)} colors={[alpha(color, 0.55), color]} />
        </Path>
      </Canvas>
      <View className="absolute inset-x-0 items-center" style={{ top: size * 0.3 }} pointerEvents="none">
        <AnimatedNumber className="font-sans text-3xl font-bold text-ink" value={value} format={(v) => `${v.toFixed(0)}%`} delay={delay} />
      </View>
    </View>
  );
}

/** الأعمدة الأفقية للأسماء العربية: تنمو بالتتابع، ولمس الصفّ يعرض حصّته وترتيبه */
function Bars({ data, tone, delay, onPick, action, dismiss }: { data: ChartData; tone: ChartTone; delay: number } & Touch) {
  const start = useStart();
  const colors = usePalette();
  const series = data.series[0]!;
  const values = series.values;
  const max = Math.max(...values.map(Math.abs), 1);
  const total = values.reduce((s, v) => s + v, 0);
  const [open, setOpen] = useState<number | null>(null);
  useEffect(() => setOpen(null), [dismiss]);
  return (
    <View className="gap-3">
      {data.labels.map((label, i) => {
        const [light, dark] = colors.ramp(tone === "Auto" ? 0 : i, tone === "Auto" ? 1 : data.labels.length, tone);
        const v = values[i] ?? 0;
        return (
          <Pressable
            key={label}
            onPress={() => {
              setOpen(open === i ? null : i);
              void Haptics.selectionAsync();
              onPick?.(i);
            }}
            className="gap-1"
          >
            <View className="flex-row justify-between gap-3">
              <Text numberOfLines={1} className={`flex-1 font-sans text-xs text-ink-soft ${start}`}>{label}</Text>
              <Text className="font-sans text-xs font-semibold text-ink" style={{ fontVariant: ["tabular-nums"] }}>{formatMoney(v)}</Text>
            </View>
            <Grow ratio={Math.abs(v) / max} from={light} to={dark} delay={delay + staggerDelay(i, data.labels.length)} />
            {open === i ? (
              <Animated.View entering={FadeIn.duration(duration.short3)} className="flex-row items-center gap-3">
                <Text className="font-sans text-[11px] text-ink-tertiary">{percent(total ? (v / total) * 100 : 0)}</Text>
                <Text className="font-sans text-[11px] text-ink-tertiary">#{rankOf(values, v)} / {values.length}</Text>
                {action ? (
                  <Pressable hitSlop={6} onPress={() => action.run(i)} className="ms-auto flex-row items-center gap-1 active:opacity-70">
                    <Icon icon={ExternalLink} size={12} tone="brand" />
                    <Text className="font-sans text-[11px] font-semibold text-brand">{action.label}</Text>
                  </Pressable>
                ) : null}
              </Animated.View>
            ) : null}
          </Pressable>
        );
      })}
    </View>
  );
}

/** شريطٌ ينمو من بدايته بتدرّج لونه */
function Grow({ ratio, from, to, delay }: { ratio: number; from: string; to: string; delay: number }) {
  const [width, setWidth] = useState(0);
  const size = useSharedValue(0);
  const [paper] = useCSSVariable(["--color-paper"]).map((v) => toHex(String(v ?? "#eef1f5")));
  useEffect(() => {
    size.value = withDelay(delay, withTiming(ratio, { duration: duration.long2, easing: decelerate }));
  }, [ratio, delay, size]);
  const end = useDerivedValue(() => vec(Math.max(6, size.value * width), 5));
  const rect = useDerivedValue(() => {
    const p = Skia.Path.Make();
    p.addRRect(Skia.RRectXY(Skia.XYWHRect(0, 0, Math.max(0, size.value * width), 10), 5, 5));
    return p;
  });
  return (
    <View className="h-2.5 overflow-hidden rounded-full" style={{ backgroundColor: paper }} onLayout={(e) => setWidth(e.nativeEvent.layout.width)}>
      {width ? (
        <Canvas style={{ width, height: 10 }}>
          <Path path={rect}>
            <LinearGradient start={vec(0, 5)} end={end} colors={[from, to]} />
          </Path>
        </Canvas>
      ) : null}
    </View>
  );
}

/** نظير Chart في الويب: الأنواع نفسها والتلميح نفسه (chartInsights) — Skia وReanimated للرسم والحركة، والنص العربي خارج اللوحة */
export function Chart({ kind, data, tone = "Auto", smooth = true, height = 220, delay = 0, onPick, action, dismiss }: { kind: ChartKind; data: ChartData; tone?: ChartTone; smooth?: boolean; height?: number; delay?: number } & Touch) {
  const series = data.series[0];
  if (!series) return null;
  if (kind === "Gauge") return <Gauge value={Math.max(0, Math.min(100, series.values[0] ?? 0))} delay={delay} />;
  const touch = { onPick, action, dismiss };
  if (kind === "Bar") return <Bars data={data} tone={tone} delay={delay} {...touch} />;
  if (kind === "Donut") return <Donut data={data} tone={tone} height={height} delay={delay} {...touch} />;
  return <Cartesian kind={kind} data={data} tone={tone} smooth={smooth} height={height} delay={delay} {...touch} />;
}
