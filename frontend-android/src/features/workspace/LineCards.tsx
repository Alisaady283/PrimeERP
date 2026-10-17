import { useQueries } from "@tanstack/react-query";
import * as Haptics from "expo-haptics";
import { Plus, Trash2 } from "lucide-react-native";
import { useEffect, useMemo, useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import ReanimatedSwipeable from "react-native-gesture-handler/ReanimatedSwipeable";
import Animated, { FadeInDown, FadeOutLeft, LinearTransition } from "react-native-reanimated";
import { optionsQuery, staticOptions } from "@/features/workspace/data";
import { imbalance, lineTotals, titleField, tradeParts, tradeSummary, type Line } from "@/features/workspace/documentRules";
import { resolveField, validate } from "@/features/workspace/fieldRules";
import type { DocumentSpec, FieldSpec } from "@/features/workspace/spec";
import { formatCount, formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { duration } from "@/lib/motion";
import { plainValue } from "@/lib/values";
import { useStart } from "~/lib/direction";
import { AnimatedNumber } from "~/components/ui/AnimatedNumber";
import { Badge } from "~/components/ui/Badge";
import { Button } from "~/components/ui/Button";
import { Card } from "~/components/ui/Card";
import { Icon } from "~/components/ui/Icon";
import { Sheet } from "~/components/ui/Sheet";
import { FieldInput } from "~/features/workspace/FieldInput";

/** حركة السطور: دخولٌ من أسفل، وخروجٌ جانبي، وإعادة ترتيبٍ ناعمة — بمدد lib/motion */
const entering = FadeInDown.duration(duration.medium2);
const exiting = FadeOutLeft.duration(duration.short3);
const layout = LinearTransition.duration(duration.medium2);

/** حقول القيمة في السطر التجاري تُعرض «الكمية × السعر» لا شرائح */
const tradeKeys = new Set(["qty", "unitPrice", "unitCost"]);

/** نصّ قيمة حقلٍ في السطر: اسم الخيار، أو النسبة، أو الرقم */
export function useLineValue(spec: DocumentSpec) {
  const fields = spec.lines?.fields ?? [];
  const remote = useQueries({ queries: fields.map((f) => optionsQuery(f.source)) });
  const loaded = remote.map((r) => r.data);
  return useMemo(() => {
    const maps = new Map(fields.map((f, i) => [f.key, new Map((staticOptions(f.source) ?? loaded[i] ?? []).map((o) => [o.value, o.label]))]));
    return (field: FieldSpec, line: Line): string => {
      const value = line[field.key];
      if (value === null || value === undefined || value === "") return "";
      if (field.kind === "select") return maps.get(field.key)?.get(value as string | number) ?? String(value);
      if (field.kind === "number") return field.key.endsWith("Percent") ? `${formatCount(Number(value))}%` : formatCount(Number(value));
      return plainValue(value, field.kind === "date" ? "date" : "text");
    };
  }, [fields, ...loaded]);
}

type CardProps = { spec: DocumentSpec; line: Line; index: number; valueOf: (field: FieldSpec, line: Line) => string; readOnly?: boolean; onOpen: () => void; onRemove: () => void };

/** بطاقة سطر: العنوان · «الكمية × السعر» · الشرائح · الصافي أو المدين/الدائن — ولمسها يحرّرها، وسحبها يحذفها */
export function LineCard({ spec, line, index, valueOf, readOnly, onOpen, onRemove }: CardProps) {
  const t = useText();
  const start = useStart();
  const fields = spec.lines?.fields ?? [];
  const trade = !!spec.lines?.trade;
  const head = titleField(spec);
  const totals = fields.filter((f) => f.total && Number(line[f.key] ?? 0) !== 0);
  const chips = fields.filter((f) => f !== head && !f.total && f.key !== "notes" && !(trade && tradeKeys.has(f.key)) && valueOf(f, line) !== "" && !(f.kind === "number" && Number(line[f.key]) === 0));
  const price = Number(line.unitPrice ?? line.unitCost ?? 0);
  const qty = line.qty;
  const balance = spec.lines?.balance;

  const card = (
    <Pressable disabled={readOnly} onPress={onOpen} className="active:opacity-80">
      <Card className="gap-2 p-3">
        <View className="flex-row items-start gap-3">
          <View className="size-7 items-center justify-center rounded-full bg-brand-soft">
            <Text className="font-sans text-xs font-bold text-brand">{index + 1}</Text>
          </View>
          <View className="min-w-0 flex-1 gap-0.5">
            <Text numberOfLines={2} className={`font-sans text-[15px] font-semibold text-ink ${start}`}>{(head && valueOf(head, line)) || "—"}</Text>
            {qty !== undefined && (trade || price !== 0) ? (
              <Text className={`font-sans text-xs text-ink-tertiary ${start}`} style={{ fontVariant: ["tabular-nums"] }}>
                {formatCount(Number(qty ?? 0))} × {formatMoney(price)}
              </Text>
            ) : null}
          </View>
          <View className="items-end gap-0.5">
            {trade ? <Text className="font-sans text-base font-bold text-ink" style={{ fontVariant: ["tabular-nums"] }}>{formatMoney(tradeParts(line).net)}</Text> : null}
            {totals.map((f) => (
              <Text
                key={f.key}
                className={`font-sans text-[15px] font-bold ${balance ? (f.key === balance[0] ? "text-success" : "text-danger") : "text-ink"}`}
                style={{ fontVariant: ["tabular-nums"] }}
              >
                {balance ? `${t(f.label)} ` : ""}
                {formatMoney(Number(line[f.key]))}
              </Text>
            ))}
          </View>
        </View>
        {chips.length > 0 ? (
          <View className="flex-row flex-wrap gap-1.5">
            {chips.map((f) => (
              <View key={f.key} className="flex-row items-center gap-1 rounded-full bg-paper px-2.5 py-1">
                <Text className="font-sans text-[11px] text-ink-tertiary">{t(f.label)}</Text>
                <Text className="font-sans text-[11px] font-semibold text-ink-soft">{valueOf(f, line)}</Text>
              </View>
            ))}
          </View>
        ) : null}
        {line.notes ? <Text numberOfLines={2} className={`font-sans text-xs italic text-ink-tertiary ${start}`}>{String(line.notes)}</Text> : null}
      </Card>
    </Pressable>
  );

  return (
    <Animated.View entering={entering} exiting={exiting} layout={layout}>
      {readOnly ? (
        card
      ) : (
        <ReanimatedSwipeable
          friction={1.5}
          rightThreshold={72}
          overshootRight={false}
          onSwipeableOpen={onRemove}
          renderRightActions={() => (
            <View className="ms-2 w-20 items-center justify-center rounded-xl bg-danger">
              <Icon icon={Trash2} size={22} tone="on-field" />
            </View>
          )}
        >
          {card}
        </ReanimatedSwipeable>
      )}
    </Animated.View>
  );
}

/** أول سطرٍ في مستندٍ فارغ: بطاقةٌ منقّطة تدعو إلى الإضافة */
export function AddFirstLine({ onAdd }: { onAdd: () => void }) {
  const t = useText();
  return (
    <Pressable onPress={onAdd} className="items-center gap-2 rounded-2xl border-2 border-dashed border-line-strong px-4 py-8 active:bg-brand-soft">
      <View className="size-12 items-center justify-center rounded-full bg-brand-soft">
        <Icon icon={Plus} size={22} tone="brand" />
      </View>
      <Text className="font-sans text-sm font-semibold text-brand">{t("Str.AddLine").replace("+", "").trim()}</Text>
    </Pressable>
  );
}

/** ملخّص المستند: أجزاء الصافي التجاري، أو مجاميع الحقول المُعلَنة وتوازن المدين والدائن */
export function DocumentSummary({ spec, lines }: { spec: DocumentSpec; lines: Line[] }) {
  const t = useText();
  const totals = lineTotals(spec, lines);
  const diff = imbalance(spec, lines);
  if (!spec.lines?.trade && totals.length === 0) return null;
  const parts = spec.lines?.trade ? tradeSummary(lines) : null;
  const rows: Array<[string, number, string?]> = parts
    ? [
        ["Str.Total", parts.gross],
        ["Str.Line.Discount", -parts.discount, "text-danger"],
        ["Str.Print.Vat", parts.vat],
        ["Str.Print.Withholding", -parts.withholding, "text-danger"],
      ].filter(([, v]) => v !== 0) as Array<[string, number, string?]>
    : totals.map(({ field, sum }) => [field.label, sum]);

  return (
    <Animated.View layout={layout}>
      <Card className="gap-2 p-4">
        {rows.map(([label, value, tone]) => (
          <View key={label} className="flex-row items-center justify-between">
            <Text className="font-sans text-sm text-ink-tertiary">{t(label)}</Text>
            <AnimatedNumber className={`font-sans text-sm font-semibold ${tone ?? "text-ink"}`} value={value} format={formatMoney} />
          </View>
        ))}
        {parts ? (
          <View className="mt-1 flex-row items-center justify-between border-t border-line pt-2.5">
            <Text className="font-sans text-base font-semibold text-ink">{t("Str.NetTotal")}</Text>
            <AnimatedNumber className="font-sans text-xl font-bold text-brand" value={parts.net} format={formatMoney} />
          </View>
        ) : null}
        {diff !== null && lines.length > 0 ? (
          <View className="mt-1 flex-row items-center justify-between border-t border-line pt-2.5">
            <Badge tone={diff === 0 ? "success" : "danger"}>{t(diff === 0 ? "Str.Journal.Balanced" : "Str.Difference")}</Badge>
            {diff !== 0 ? <AnimatedNumber className="font-sans text-base font-bold text-danger" value={Math.abs(diff)} format={formatMoney} /> : null}
          </View>
        ) : null}
      </Card>
    </Animated.View>
  );
}

type SheetProps = { spec: DocumentSpec; line: Line | null; adding: boolean; onSave: (line: Line, again: boolean) => void; onDelete?: () => void; onClose: () => void };

/** محرّر السطر في ورقة: الحقول، والصافي حيّاً، و«حفظ وإضافة آخر» لإدخالٍ متتابع */
export function LineSheet({ spec, line, adding, onSave, onDelete, onClose }: SheetProps) {
  const t = useText();
  const [values, setValues] = useState<Line>({});
  const [errors, setErrors] = useState<Record<string, string>>({});
  const fields = spec.lines?.fields ?? [];

  useEffect(() => {
    if (line) {
      setValues({ ...line });
      setErrors({});
    }
  }, [line]);

  const save = (again: boolean) => {
    const found = validate(fields, values, t);
    setErrors(found);
    if (Object.keys(found).length > 0) return void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
    onSave(values, again);
  };

  return (
    <Sheet
      open={!!line}
      title={t(adding ? "Str.Line.New" : "Str.Line.Edit")}
      onClose={onClose}
      footer={
        <>
          {spec.lines?.trade ? (
            <View className="flex-row items-center justify-between pb-1">
              <Text className="font-sans text-sm text-ink-tertiary">{t("Str.NetTotal")}</Text>
              <AnimatedNumber className="font-sans text-xl font-bold text-brand" value={tradeParts(values).net} format={formatMoney} />
            </View>
          ) : null}
          <View className="flex-row gap-2">
            {adding ? (
              <Button size="lg" variant="secondary" className="flex-1" onPress={() => save(true)}>
                {t("Str.SaveAndNew")}
              </Button>
            ) : null}
            <Button size="lg" className="flex-1" onPress={() => save(false)}>
              {t("Str.Save")}
            </Button>
          </View>
          {onDelete ? (
            <Button size="lg" variant="ghost" textClassName="text-danger" icon={<Icon icon={Trash2} size={16} tone="danger" />} onPress={onDelete}>
              {t("Str.RemoveLine")}
            </Button>
          ) : null}
        </>
      }
    >
      <ScrollView contentContainerStyle={{ padding: 16, gap: 16 }} keyboardShouldPersistTaps="handled">
        {fields.map((f) => (
          <FieldInput key={f.key} field={resolveField(f, values)} value={values[f.key]} error={errors[f.key]} onChange={(v) => setValues((s) => ({ ...s, [f.key]: v }))} />
        ))}
      </ScrollView>
    </Sheet>
  );
}
