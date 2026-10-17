import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect } from "react";
import { useSession } from "@/features/auth/session";
import { api } from "@/lib/api";
import { apiBase } from "@/lib/platform";

/** التنبيهات — منطقٌ مشترك بين الويب والأندرويد، والعرض لكلٍّ */
export type NotificationItem = {
  id: number;
  event: string;
  title: string;
  body: string;
  pageKey: string | null;
  recordId: number | null;
  severity: "info" | "warning" | "danger";
  createdAt: string;
  isRead: boolean;
};

const key = ["notifications"] as const;

/**
 * الجرس: العدد والقائمة من الخادم بصلاحيات المستخدم، وقناة SignalR لا تحمل بيانات بل تقول «تغيّر» فيُعاد الجلب —
 * والجلب كل دقيقة احتياطٌ إن انقطعت القناة.
 */
export function useNotifications(open: boolean) {
  const client = useQueryClient();
  const token = useSession((s) => s.token);

  const count = useQuery({
    queryKey: [...key, "count"],
    queryFn: ({ signal }) => api.get<number>("/notifications/count", undefined, signal),
    refetchInterval: 60_000,
    enabled: !!token,
  });
  const list = useQuery({
    queryKey: [...key, "list"],
    queryFn: ({ signal }) => api.get<NotificationItem[]>("/notifications", { take: 30 }, signal),
    enabled: !!token && open,
  });
  const markRead = useMutation({
    mutationFn: (ids: number[]) => api.post("/notifications/read", ids),
    onSuccess: () => client.invalidateQueries({ queryKey: key }),
  });

  useEffect(() => {
    if (!token) return;
    const connection = new HubConnectionBuilder()
      .withUrl(`${apiBase()}/hubs/notifications`, { accessTokenFactory: () => useSession.getState().token ?? "", withCredentials: false })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
    connection.on("changed", () => void client.invalidateQueries({ queryKey: key }));
    void connection.start().catch(() => undefined);
    return () => void connection.stop();
  }, [token, client]);

  return { count: count.data ?? 0, items: list.data ?? [], loading: list.isLoading, markRead: (ids: number[]) => markRead.mutate(ids) };
}
