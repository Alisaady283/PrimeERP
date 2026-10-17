import { useLocalSearchParams } from "expo-router";
import { pageByKey } from "@/catalog/pages";
import { useVisiblePage } from "@/features/auth/interface";
import { useText } from "@/lib/i18n";
import { EmptyState } from "~/components/ui/EmptyState";
import { PageHost } from "~/features/workspace/Pages";

/** كـPageRoute في الويب */
export default function PageRoute() {
  const t = useText();
  const { page, q } = useLocalSearchParams<{ page: string; q?: string }>();
  const visible = useVisiblePage();
  const spec = pageByKey.get(page);
  if (!spec || !visible(page)) return <EmptyState message={t("Str.PermissionDenied")} />;
  return <PageHost key={`${page}:${q ?? ""}`} spec={spec} />;
}
