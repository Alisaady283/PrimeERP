import { Redirect } from "expo-router";
import { sections } from "@/catalog/navigation";
import { useVisiblePage } from "@/features/auth/interface";
import { useText } from "@/lib/i18n";
import { EmptyState } from "~/components/ui/EmptyState";

/** كـHome في الويب: أوّل صفحةٍ مسموحة */
export default function Home() {
  const t = useText();
  const visible = useVisiblePage();
  const first = sections.flatMap((s) => s.pages).find(visible);
  return first ? <Redirect href={{ pathname: "/p/[page]", params: { page: first } }} /> : <EmptyState message={t("Str.NoData")} />;
}
