import { useQuery } from "@tanstack/react-query";
import { useEffect } from "react";
import { api } from "@/lib/api";
import { useIdentity } from "@/lib/identity";
import { pageGate, useSession } from "@/features/auth/session";
import { pageByKey } from "@/catalog/pages";

/** cashCustomerId وposCustomerEditable: عميل نقطة البيع الافتراضي، وهل يُغيَّر */
export type InterfaceSettings = { identity: string; companyName: string; simplifiedFlow: boolean; manifest: string[]; arabic: boolean; english: boolean; cashCustomerId?: number | null; posCustomerEditable?: boolean };

export const useInterface = () => {
  const signedIn = useSession((s) => !!s.token);
  const setIdentity = useIdentity((s) => s.setIdentity);
  const query = useQuery({
    queryKey: ["interface"],
    enabled: signedIn,
    staleTime: 10 * 60_000,
    queryFn: ({ signal }) => api.get<InterfaceSettings>("/settings/interface", undefined, signal),
  });
  useEffect(() => {
    if (query.data?.identity) setIdentity(query.data.identity);
  }, [query.data?.identity, setIdentity]);
  return query.data;
};

/** مرجعٌ ثابت: مصفوفةٌ جديدة في المحدِّد تعيد الرسم بلا نهاية */
const NONE: string[] = [];

export const useVisiblePage = () => {
  const settings = useInterface();
  const permissions = useSession((s) => s.user?.permissions ?? NONE);
  const pages = useSession((s) => s.user?.pages ?? NONE);
  return (key: string) => {
    const spec = pageByKey.get(key);
    if (!spec) return false;
    if (spec.fullCycleOnly && (settings?.simplifiedFlow ?? true)) return false;
    if (settings?.manifest.length && !settings.manifest.includes(key)) return false;
    return permissions.includes(`${pageGate(pages, key, spec.permission)}.View`);
  };
};
