import type { QueryClient } from "@tanstack/react-query";
import { pageByKey } from "@/catalog/pages";
import { prefetchPage } from "@/features/workspace/data";

/** وحدة الرسوم في الويب — خارج data.ts المشترك مع الأندرويد */
export const loadChart = () => import("@/components/ui/Chart");

/** تحميلٌ مسبق للصفحة، ومعها وحدة الرسوم للّوحة */
export const prefetchWebPage = (client: QueryClient, key: string) => {
  prefetchPage(client, key);
  if (pageByKey.get(key)?.type === "dashboard") void loadChart();
};
