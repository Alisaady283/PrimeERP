import { useEffect, useState } from "react";
import { ActivityIndicator, FlatList, Pressable, Text, TextInput, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCSSVariable } from "uniwind";
import { chooseAll, pullResult, useCandidates, type Candidate, type Chosen, type Pulled } from "@/features/workspace/pull";
import type { PullSpec } from "@/features/workspace/spec";
import { formatMoney } from "@/lib/format";
import { useText } from "@/lib/i18n";
import { dateOnly } from "@/lib/values";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Card } from "~/components/ui/Card";
import { Checkbox } from "~/components/ui/Checkbox";
import { EmptyState } from "~/components/ui/EmptyState";
import { Sheet } from "~/components/ui/Sheet";
import { toast } from "~/components/ui/Snackbar";

/** نظير PullDialog في الويب: المستندات ثم سطور المختار بكمياتها — والمنطق من `pull.ts` المشترك */
export function PullDialog({ source, header, onPull, onClose }: { source: PullSpec | null; header: Record<string, unknown>; onPull: (pulled: Pulled) => void; onClose: () => void }) {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const brand = useCSSVariable("--color-brand");
  const [picked, setPicked] = useState<Candidate | null>(null);
  const [chosen, setChosen] = useState<Chosen>({});
  const candidates = useCandidates(source, header);

  useEffect(() => {
    setPicked(null);
    setChosen({});
  }, [source]);

  const pull = () => {
    const result = pullResult(picked, chosen, t);
    if ("error" in result) return toast.error(result.error);
    onPull(result.pulled);
    onClose();
  };

  const list = candidates.data ?? [];

  return (
    <Sheet
      open={!!source}
      title={picked ? picked.sourceNo : source ? t(`Str.Pull.From.${source.source}`) : ""}
      onClose={onClose}
      onBack={picked ? () => setPicked(null) : undefined}
      footer={
        picked ? (
          <Button size="lg" onPress={pull}>
            {t("Str.Pull.Pull")}
          </Button>
        ) : undefined
      }
    >

        {candidates.isLoading ? (
          <ActivityIndicator color={String(brand)} style={{ marginTop: 40 }} />
        ) : !picked ? (
          <FlatList
            data={list}
            keyExtractor={(c) => `${c.sourceType}-${c.sourceId}`}
            contentContainerStyle={{ padding: 12, gap: 8, paddingBottom: insets.bottom + 24 }}
            ListHeaderComponent={list.length ? <Text className={`px-1 pb-1 font-sans text-sm font-semibold text-ink-soft ${start}`}>{t("Str.Pull.Documents")}</Text> : null}
            ListEmptyComponent={<EmptyState message={t("Str.Pull.NoDocuments")} />}
            renderItem={({ item }) => (
              <Pressable
                onPress={() => {
                  setPicked(item);
                  setChosen(chooseAll(item));
                }}
                className="active:opacity-80"
              >
                <Card className="gap-1.5 p-3">
                  <View className="flex-row items-center justify-between gap-2">
                    <Text className="font-mono text-sm font-semibold text-ink">{item.sourceNo}</Text>
                    <Text className="font-sans text-xs text-ink-tertiary">{dateOnly(item.docDate)}</Text>
                  </View>
                  <View className="flex-row items-center justify-between gap-2">
                    <Text numberOfLines={1} className={`flex-1 font-sans text-[13px] text-ink-soft ${start}`}>{item.partyName}</Text>
                    <Text className="font-sans text-xs text-ink-tertiary">
                      {t("Str.Pull.OpenLines")}: {item.openLineCount}
                    </Text>
                  </View>
                </Card>
              </Pressable>
            )}
          />
        ) : (
          <FlatList
              data={picked.lines}
              keyExtractor={(l) => String(l.sourceLineId)}
              keyboardShouldPersistTaps="handled"
              contentContainerStyle={{ padding: 12, gap: 8 }}
              ListHeaderComponent={<Text className={`px-1 pb-1 font-sans text-sm font-semibold text-ink-soft ${start}`}>{t("Str.Pull.SelectedLines")}</Text>}
              renderItem={({ item: l }) => {
                const on = chosen[l.sourceLineId] != null;
                return (
                  <Card className="flex-row items-center gap-3 p-3">
                    <Checkbox state={on} onPress={() => setChosen((s) => ({ ...s, [l.sourceLineId]: on ? null : l.remainingQty }))} />
                    <View className="min-w-0 flex-1">
                      <Text numberOfLines={1} className={`font-sans text-sm font-medium text-ink ${start}`}>{l.productName || l.productCode}</Text>
                      <Text className={`font-sans text-xs text-ink-tertiary ${start}`}>
                        {t("Str.Pull.OriginalPulled")}: {formatMoney(l.originalQty)} / {formatMoney(l.pulledQty)}
                      </Text>
                    </View>
                    <TextInput
                      editable={on}
                      keyboardType="decimal-pad"
                      value={chosen[l.sourceLineId] == null ? "" : String(chosen[l.sourceLineId])}
                      onChangeText={(v) => setChosen((s) => ({ ...s, [l.sourceLineId]: v === "" ? 0 : Number(v) }))}
                      className={`h-9 w-20 rounded-lg border border-line px-2 text-center font-sans text-sm text-ink ${on ? "bg-card" : "bg-paper-soft"}`}
                    />
                  </Card>
                );
              }}
            />
        )}
    </Sheet>
  );
}
