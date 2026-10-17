import { router } from "expo-router";
import { Bell } from "lucide-react-native";
import { useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { useNotifications } from "@/features/notifications/notifications";
import { useText } from "@/lib/i18n";
import { useStart } from "~/lib/direction";
import { Icon } from "~/components/ui/Icon";
import { Sheet } from "~/components/ui/Sheet";

const dot = { danger: "bg-danger", warning: "bg-warning", info: "bg-brand" } as const;

/** جرس التنبيهات — نظير جرس الويب، ومنطقه نفسه (useNotifications)، والقائمة في ورقةٍ سفلية */
export function NotificationBell() {
  const t = useText();
  const start = useStart();
  const [open, setOpen] = useState(false);
  const { count, items, markRead } = useNotifications(open);

  return (
    <>
      <Pressable hitSlop={8} onPress={() => setOpen(true)} className="rounded-lg p-2 active:bg-field-hover">
        <Icon icon={Bell} size={20} tone="on-field" />
        {count > 0 ? (
          <View className="absolute end-0.5 top-0.5 h-4 min-w-4 items-center justify-center rounded-full bg-danger px-1">
            <Text className="font-sans text-[10px] font-bold text-white">{count > 99 ? "99+" : count}</Text>
          </View>
        ) : null}
      </Pressable>
      <Sheet open={open} title={t("Str.Notifications.Title")} onClose={() => setOpen(false)}
        footer={<Pressable onPress={() => markRead(items.filter((n) => !n.isRead).map((n) => n.id))} className="items-center py-3">
          <Text className="font-sans text-sm text-brand">{t("Str.Notifications.MarkAll")}</Text>
        </Pressable>}>
        <ScrollView>
          {items.length === 0 ? <Text className="py-8 text-center font-sans text-sm text-ink-tertiary">{t("Str.Notifications.Empty")}</Text> : null}
          {items.map((n) => (
            <Pressable key={n.id} className="flex-row gap-3 border-b border-line px-4 py-3 active:bg-paper"
              onPress={() => {
                markRead([n.id]);
                setOpen(false);
                if (n.pageKey) router.navigate({ pathname: "/p/[page]", params: { page: n.pageKey } });
              }}>
              <View className={`mt-1.5 size-2 rounded-full ${n.isRead ? "bg-line" : dot[n.severity] ?? dot.info}`} />
              <View className="flex-1 gap-0.5">
                <Text className={`font-sans text-sm text-ink ${n.isRead ? "" : "font-semibold"} ${start}`}>{n.title}</Text>
                <Text className={`font-sans text-xs text-ink-soft ${start}`}>{n.body}</Text>
                <Text className={`font-sans text-[11px] text-ink-tertiary ${start}`}>{new Date(n.createdAt).toLocaleString()}</Text>
              </View>
            </Pressable>
          ))}
        </ScrollView>
      </Sheet>
    </>
  );
}
