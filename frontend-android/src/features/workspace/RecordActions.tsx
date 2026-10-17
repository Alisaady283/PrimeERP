import { useState } from "react";
import { Share, Text, View } from "react-native";
import { usePagePermission, useSession } from "@/features/auth/session";
import { runRecordAction } from "@/features/workspace/pageActions";
import type { RecordAction } from "@/features/workspace/spec";
import { messageOf } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { Button } from "~/components/ui/Button";
import { Sheet } from "~/components/ui/Sheet";
import { toast } from "~/components/ui/Snackbar";

type Spec = { key: string; permission: string; recordActions?: RecordAction[] };

/** أفعال السجل المسموحة وتنفيذها — نظير RecordActions في الويب، والتنفيذ نفسه (runRecordAction) */
export function useRecordActions(spec: Spec, id: number | null) {
  const t = useText();
  const gate = usePagePermission(spec);
  const permissions = useSession((s) => s.user?.permissions);
  const [shown, setShown] = useState<{ label: string; text: string } | null>(null);

  const actions = id === null ? [] : (spec.recordActions ?? []).filter((a) => permissions?.includes(a.permission ?? `${gate}.${a.action}`));
  const run = async (action: RecordAction) => {
    if (id === null) return;
    try { setShown({ label: action.label, text: await runRecordAction(action, id) }); } catch (failure) { toast.error(messageOf(failure)); }
  };

  /** ما يُعاد (مفتاحٌ أو رابط) يُعرض مرةً، ويُشارَك أو يُنسخ من ورقة المشاركة */
  const sheet = (
    <Sheet open={shown !== null} title={t(shown?.label ?? "")} onClose={() => setShown(null)}
      footer={<View className="p-4"><Button size="lg" onPress={() => void Share.share({ message: shown?.text ?? "" })}>{t("Str.Copy")}</Button></View>}>
      <View className="p-4">
        <Text selectable className="rounded-lg border border-line bg-paper px-3 py-2.5 font-mono text-sm text-ink">{shown?.text}</Text>
      </View>
    </Sheet>
  );

  return { actions, run, sheet, label: (a: RecordAction) => t(a.label) };
}
