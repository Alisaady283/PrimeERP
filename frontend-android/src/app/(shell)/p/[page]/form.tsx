import { router, useLocalSearchParams } from "expo-router";
import { ArrowLeft, ArrowRight } from "lucide-react-native";
import { Pressable, Text, View } from "react-native";
import { openingStock, pageByKey } from "@/catalog/pages";
import type { DocumentSpec, ListSpec } from "@/features/workspace/spec";
import { useText } from "@/lib/i18n";
import { useDirection, useStart } from "~/lib/direction";
import { EmptyState } from "~/components/ui/EmptyState";
import { Icon } from "~/components/ui/Icon";
import { DocumentEditor } from "~/features/workspace/DocumentEditor";
import { EntityDialog } from "~/features/workspace/EntityDialog";

/** نموذج الصفحة: العنوان كعنوان حوار الويب */
export default function FormRoute() {
  const t = useText();
  const start = useStart();
  const direction = useDirection();
  const { page, id, editor } = useLocalSearchParams<{ page: string; id?: string; editor?: string }>();
  /** محرّر نوع الصفّ (رصيد أول المدة) بدل محرّر الصفحة */
  const found = editor === openingStock.key ? openingStock : pageByKey.get(page);
  const document = found?.type === "document" ? (found as DocumentSpec) : undefined;
  const spec = found as ListSpec | undefined;
  if (!document && !spec?.form) return <EmptyState message={t("Str.PermissionDenied")} />;
  const titles = spec?.formTitles ?? ["Str.Add", "Str.Edit"];
  return (
    <View className="flex-1 bg-card">
      <View className="flex-row items-center gap-2 border-b border-line px-3 py-2.5">
        <Pressable hitSlop={8} onPress={() => router.back()} className="rounded-lg p-1.5 active:bg-paper">
          <Icon icon={direction === "rtl" ? ArrowRight : ArrowLeft} size={20} tone="ink" />
        </Pressable>
        <Text className={`flex-1 font-sans text-base font-semibold text-ink ${start}`}>{t(id ? titles[1] : titles[0])}</Text>
      </View>
      {document ? <DocumentEditor spec={document} id={id ? Number(id) : null} /> : <EntityDialog spec={spec!} id={id ? Number(id) : null} />}
    </View>
  );
}
