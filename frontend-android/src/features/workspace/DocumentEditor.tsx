import * as Haptics from "expo-haptics";
import { router } from "expo-router";
import { Download, MoreHorizontal, Plus, Printer, RefreshCw, Trash2 } from "lucide-react-native";
import { useEffect, useRef, useState } from "react";
import { Alert, KeyboardAvoidingView, ScrollView, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useInterface } from "@/features/auth/interface";
import { useCan, usePagePermission, useSession } from "@/features/auth/session";
import { useDeletePage, usePageRecord, useSavePage } from "@/features/workspace/data";
import { blankLine, documentBody, documentTotal, imbalance, mergePulled, openSheet, pick, pullSources, recordLines, type Line } from "@/features/workspace/documentRules";
import { documentPaper } from "@/features/workspace/paper";
import { resolveField, validate } from "@/features/workspace/fieldRules";
import type { DocumentSpec, PullSpec } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { formatCount, formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { printPaper } from "@/lib/output";
import { useStart } from "~/lib/direction";
import { useRecordActions } from "~/features/workspace/RecordActions";
import { AnimatedNumber } from "~/components/ui/AnimatedNumber";
import { Button } from "~/components/ui/Button";
import { Card } from "~/components/ui/Card";
import { Enter } from "~/components/ui/Enter";
import { Icon } from "~/components/ui/Icon";
import { Menu, type MenuItem } from "~/components/ui/Menu";
import { Skeleton } from "~/components/ui/Skeleton";
import { toast } from "~/components/ui/Snackbar";
import { FieldInput } from "~/features/workspace/FieldInput";
import { AddFirstLine, DocumentSummary, LineCard, LineSheet, useLineValue } from "~/features/workspace/LineCards";
import { PullDialog } from "~/features/workspace/PullDialog";

/** السطور بمفاتيح ثابتة لحركة الدخول والخروج — المفاتيح للعرض وحده ولا تُرسل */
function useKeyedLines() {
  const next = useRef(0);
  const [items, setItems] = useState<Array<{ key: number; line: Line }>>([]);
  const wrap = (line: Line) => ({ key: next.current++, line });
  return {
    lines: items.map((i) => i.line),
    items,
    reset: (lines: Line[]) => setItems(lines.map(wrap)),
    add: (line: Line) => setItems((all) => [...all, wrap(line)]),
    update: (key: number, line: Line) => setItems((all) => all.map((i) => (i.key === key ? { ...i, line } : i))),
    remove: (key: number) => setItems((all) => all.filter((i) => i.key !== key)),
    insert: (at: number, item: { key: number; line: Line }) => setItems((all) => [...all.slice(0, at), item, ...all.slice(at)]),
  };
}

/** نظير DocumentEditor في الويب: معاملات الفتح · الرأس · السحب · السطور بطاقاتٍ · الملخّص · شريط الصافي والحفظ — والمنطق من `documentRules` و`pull` المشتركين؛ والعنوان من المسار */
export function DocumentEditor({ spec, id }: { spec: DocumentSpec; id: number | null }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const record = usePageRecord(spec.key, id);
  const save = useSavePage(spec.key, spec.saveTo);
  const remove = useDeletePage(spec.key);
  const records = useRecordActions(spec, id);
  const canDelete = useCan(`${usePagePermission(spec)}.Delete`);
  const [header, setHeader] = useState<Record<string, unknown>>({});
  const keyed = useKeyedLines();
  const { lines } = keyed;
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [editing, setEditing] = useState<{ key: number | null; line: Line } | null>(null);
  const [params, setParams] = useState<Record<string, unknown>>(() => pick(undefined, spec.open?.params ?? []));
  const [opening, setOpening] = useState(false);
  const [pulling, setPulling] = useState<PullSpec | null>(null);
  const settings = useInterface();
  const permissions = useSession((s) => s.user?.permissions);
  const sources = pullSources(spec, settings?.simplifiedFlow ?? true, permissions);
  const valueOf = useLineValue(spec);
  const readOnly = !!id && spec.noUpdate;

  useEffect(() => {
    if (id && !record.data) return;
    const source = id ? record.data : undefined;
    setHeader({ ...(source ?? {}), ...pick(source, spec.header) });
    keyed.reset(recordLines(spec, source));
  }, [id, record.data]);

  const load = async () => {
    setOpening(true);
    try {
      const sheet = await openSheet(spec, params);
      setHeader(sheet.header);
      keyed.reset(sheet.lines);
    } catch (error) {
      toast.error(messageOf(error));
    } finally {
      setOpening(false);
    }
  };

  const print = async () => {
    try {
      printPaper(await documentPaper(spec, { id: id ?? 0 }, settings?.companyName));
    } catch (error) {
      toast.error(messageOf(error));
    }
  };

  const submit = () => {
    const found = validate(spec.header, header, t);
    setErrors(found);
    if (Object.keys(found).length > 0) return void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Error);
    save.mutate(
      { id, body: documentBody(spec, id, header, lines) },
      {
        onSuccess: () => {
          void Haptics.notificationAsync(Haptics.NotificationFeedbackType.Success);
          toast.success(t("Str.Success"));
          router.back();
        },
      },
    );
  };

  const confirmDelete = () =>
    Alert.alert(t("Str.Delete"), t("Str.ConfirmDeleteMessage"), [
      { text: t("Str.Cancel"), style: "cancel" },
      { text: t("Str.Delete"), style: "destructive", onPress: () => id && remove.mutate(id, { onSuccess: () => router.back() }) },
    ]);

  /** حذفٌ بالسحب أو من الورقة، ومعه «تراجع» يعيد السطر مكانه */
  const removeLine = (key: number) => {
    const at = keyed.items.findIndex((i) => i.key === key);
    const item = keyed.items[at];
    if (!item) return;
    void Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Medium);
    keyed.remove(key);
    setEditing(null);
    toast.undo(t("Str.Line.Removed"), t("Str.Undo"), () => keyed.insert(at, item));
  };

  const addLine = () => setEditing({ key: null, line: blankLine(spec) });
  const total = documentTotal(spec, lines);
  const diff = imbalance(spec, lines);
  const error = save.error ?? remove.error;
  const more: MenuItem[] = [
    ...(id !== null ? [{ label: t("Str.Print"), icon: <Icon icon={Printer} size={20} tone="ink-soft" />, onSelect: () => void print() }] : []),
    ...records.actions.map((a) => ({ label: records.label(a), onSelect: () => void records.run(a) })),
    ...(id !== null && canDelete ? [{ label: t("Str.Delete"), icon: <Icon icon={Trash2} size={20} tone="danger" />, danger: true, onSelect: confirmDelete }] : []),
  ];

  if (id && record.isLoading)
    return (
      <View className="gap-3 p-4">
        <Skeleton className="h-40" />
        <Skeleton className="h-24" />
        <Skeleton className="h-24" />
      </View>
    );

  return (
    <KeyboardAvoidingView behavior="height" className="flex-1 bg-paper">
      <ScrollView contentContainerStyle={{ padding: 12, gap: 12, paddingBottom: 24 }} keyboardShouldPersistTaps="handled">
        {!id && spec.open ? (
          <View className="gap-3 rounded-2xl bg-brand-soft p-3">
            {spec.open.params.map((f) => (
              <FieldInput key={f.key} field={f} value={params[f.key]} onChange={(v) => setParams((s) => ({ ...s, [f.key]: v }))} />
            ))}
            <Button icon={<Icon icon={RefreshCw} size={16} tone="on-field" />} loading={opening} onPress={() => void load()}>
              {t("Str.Refresh")}
            </Button>
          </View>
        ) : null}

        <Card className="gap-4 p-4">
          {spec.header.map((f, i) => (
            <Enter key={f.key} index={i}>
              <FieldInput field={resolveField(f, header)} value={header[f.key]} error={errors[f.key]} disabled={readOnly} onChange={(v) => setHeader((h) => ({ ...h, [f.key]: v }))} />
            </Enter>
          ))}
        </Card>

        {spec.lines ? (
          <>
            <View className="flex-row items-center gap-2 px-1 pt-1">
              <Text className={`flex-1 font-sans text-base font-bold text-ink ${start}`}>
                {t("Str.Lines")} <Text className="font-normal text-ink-tertiary">· {formatCount(lines.length)}</Text>
              </Text>
              {!readOnly && sources.length > 0 ? (
                <Menu
                  icon={<Icon icon={Download} size={16} tone="ink-soft" />}
                  label={t("Str.Pull.Pull")}
                  items={sources.map((source) => ({ label: t(`Str.Pull.From.${source.source}`), onSelect: () => setPulling(source) }))}
                />
              ) : null}
              {!readOnly && lines.length > 0 ? (
                <Button size="sm" icon={<Icon icon={Plus} size={16} tone="on-field" />} onPress={addLine}>
                  {t("Str.AddLine").replace("+", "").trim()}
                </Button>
              ) : null}
            </View>
            {keyed.items.map((item, i) => (
              <LineCard
                key={item.key}
                spec={spec}
                line={item.line}
                index={i}
                valueOf={valueOf}
                readOnly={readOnly}
                onOpen={() => setEditing({ key: item.key, line: item.line })}
                onRemove={() => removeLine(item.key)}
              />
            ))}
            {lines.length === 0 && !readOnly ? <AddFirstLine onAdd={addLine} /> : null}
            {lines.length > 0 && !readOnly ? <Text className="text-center font-sans text-[11px] text-ink-faint">{t("Str.Line.SwipeHint")}</Text> : null}
            <DocumentSummary spec={spec} lines={lines} />
          </>
        ) : null}

        {error ? (
          <View className="rounded-xl bg-danger-soft px-4 py-3">
            <Text className={`font-sans text-sm text-danger ${start}`}>{messageOf(error)}</Text>
          </View>
        ) : null}
      </ScrollView>

      <View className="flex-row items-center gap-3 border-t border-line bg-card px-4 pt-3" style={{ paddingBottom: insets.bottom + 12 }}>
        {total !== null ? (
          <View className="flex-1">
            <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>{t("Str.NetTotal")}</Text>
            <AnimatedNumber className={`font-sans text-lg font-bold text-ink ${start}`} value={total} format={formatMoney} />
          </View>
        ) : diff !== null && diff !== 0 ? (
          <View className="flex-1">
            <Text className={`font-sans text-xs text-danger ${start}`}>{t("Str.Difference")}</Text>
            <AnimatedNumber className={`font-sans text-lg font-bold text-danger ${start}`} value={Math.abs(diff)} format={formatMoney} />
          </View>
        ) : (
          <View className="flex-1" />
        )}
        {more.length > 0 ? <Menu icon={<Icon icon={MoreHorizontal} size={16} tone="ink-soft" />} label={t("Str.More")} items={more} /> : null}
        {!readOnly ? (
          <Button size="lg" className="min-w-32" loading={save.isPending} onPress={submit}>
            {t("Str.Save")}
          </Button>
        ) : null}
      </View>

      <PullDialog
        source={pulling}
        header={header}
        onPull={(pulled) => {
          const merged = mergePulled(spec, lines, header, pulled);
          keyed.reset(merged.lines);
          setHeader(merged.header);
        }}
        onClose={() => setPulling(null)}
      />
      <LineSheet
        spec={spec}
        line={editing?.line ?? null}
        adding={editing?.key === null}
        onClose={() => setEditing(null)}
        onSave={(line, again) => {
          void Haptics.impactAsync(Haptics.ImpactFeedbackStyle.Light);
          if (editing?.key === null || editing?.key === undefined) keyed.add(line);
          else keyed.update(editing.key, line);
          setEditing(again ? { key: null, line: blankLine(spec) } : null);
        }}
        onDelete={editing && editing.key !== null ? () => removeLine(editing.key!) : undefined}
      />
      {records.sheet}
    </KeyboardAvoidingView>
  );
}
