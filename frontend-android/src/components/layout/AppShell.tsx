import { router, usePathname } from "expo-router";
import { ChevronDown, KeyRound, Languages, LogOut, Menu as MenuIcon, Search } from "lucide-react-native";
import { useEffect, useMemo, useState, type ReactNode } from "react";
import { BackHandler, Pressable, ScrollView, Text, TextInput, View, useWindowDimensions } from "react-native";
import Animated, { Easing, useAnimatedStyle, useSharedValue, withTiming } from "react-native-reanimated";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useCSSVariable } from "uniwind";
import { sectionOf, sections } from "@/catalog/navigation";
import { pageByKey } from "@/catalog/pages";
import { useInterface, useVisiblePage } from "@/features/auth/interface";
import { changePassword, passwordFields } from "@/features/auth/password";
import { useSession } from "@/features/auth/session";
import { useLanguage, useText } from "@/lib/i18n";
import { duration, easing } from "@/lib/motion";
import { useDirection, useStart } from "~/lib/direction";
import { sectionIcons } from "~/components/layout/sectionIcons";
import { SnackbarHost } from "~/components/ui/Snackbar";
import { NotificationBell } from "~/components/layout/NotificationBell";
import { Icon } from "~/components/ui/Icon";
import { FormDialog } from "~/features/workspace/FormDialog";

/** عرض السايدبار كالويب: w-64 */
const WIDTH = 256;
const [x1, y1, x2, y2] = easing.emphasized;
const emphasized = Easing.bezier(x1, y1, x2, y2);

/** نظير AppShell في الويب: الشريط العلوي بلون السايدبار، والسايدبار يُفتح من البداية */
export function AppShell({ children }: { children: ReactNode }) {
  const t = useText();
  const start = useStart();
  const direction = useDirection();
  const insets = useSafeAreaInsets();
  const { width } = useWindowDimensions();
  const pathname = usePathname();
  const user = useSession((s) => s.user);
  const signOut = useSession((s) => s.signOut);
  const { language, setLanguage } = useLanguage();
  const settings = useInterface();
  const visible = useVisiblePage();
  const [faint] = useCSSVariable(["--color-on-field-faint"]);
  const [filter, setFilter] = useState("");
  const [drawer, setDrawer] = useState(false);
  const [menu, setMenu] = useState(false);
  const [password, setPassword] = useState(false);
  const current = pathname.startsWith("/p/") ? decodeURIComponent(pathname.slice(3).split("/")[0] ?? "") : "";
  const [open, setOpen] = useState<string | null>(() => sectionOf(current)?.key ?? null);
  const shown = useSharedValue(0);

  useEffect(() => {
    shown.value = withTiming(drawer ? 1 : 0, { duration: drawer ? duration.medium4 : duration.short3, easing: emphasized });
  }, [drawer, shown]);

  /** رجوع الجهاز يغلق السايدبار أولاً */
  useEffect(() => {
    if (!drawer) return;
    const sub = BackHandler.addEventListener("hardwareBackPress", () => {
      setDrawer(false);
      return true;
    });
    return () => sub.remove();
  }, [drawer]);

  const side = direction === "rtl" ? 1 : -1;
  const panel = useAnimatedStyle(() => ({ transform: [{ translateX: (1 - shown.value) * WIDTH * side }] }));
  const scrim = useAnimatedStyle(() => ({ opacity: shown.value * 0.4 }));

  const groups = useMemo(() => {
    const q = filter.trim().toLowerCase();
    return sections
      .map((s) => ({ ...s, pages: s.pages.filter((p) => visible(p) && (!q || t(pageByKey.get(p)!.title).toLowerCase().includes(q))) }))
      .filter((s) => s.pages.length > 0);
  }, [visible, filter, t]);

  const section = sectionOf(current);
  const page = pageByKey.get(current);
  const userName = user ? user.displayName || user.username : "";
  const canToggle = (settings?.arabic ?? true) && (settings?.english ?? true);

  const go = (key: string) => {
    setDrawer(false);
    router.navigate({ pathname: "/p/[page]", params: { page: key } });
  };

  return (
    <View className="flex-1 bg-card">
      <View className="flex-row items-center gap-2 border-b border-field-line bg-field px-3" style={{ paddingTop: insets.top, height: insets.top + 56 }}>
        <Pressable hitSlop={8} onPress={() => setDrawer(true)} className="rounded-lg p-2 active:bg-field-hover">
          <Icon icon={MenuIcon} size={20} tone="on-field" />
        </Pressable>
        <Text numberOfLines={1} className={`flex-1 font-sans text-sm text-on-field-faint ${start}`}>
          {section ? t(`Str.Nav.${section.key}`) : ""}
          {page ? <Text className="text-on-field-faint">{"  /  "}</Text> : null}
          {page ? <Text className="font-semibold text-on-field">{t(page.title)}</Text> : null}
        </Text>
        <NotificationBell />
        <Pressable onPress={() => setMenu((m) => !m)} className="size-8 items-center justify-center rounded-full bg-white/15">
          <Text className="font-sans text-sm font-semibold text-on-field">{userName.slice(0, 1)}</Text>
        </Pressable>
      </View>

      {menu ? (
        <View className="absolute end-3 z-50 min-w-48 overflow-hidden rounded-xl border border-line bg-card py-1 shadow-pop" style={{ top: insets.top + 52 }}>
          <Text className={`px-3 py-2 font-sans text-xs text-ink-tertiary ${start}`}>{userName}</Text>
          <Pressable onPress={() => { setMenu(false); setPassword(true); }} className="flex-row items-center gap-2 px-3 py-2.5 active:bg-paper">
            <Icon icon={KeyRound} size={16} tone="ink-soft" />
            <Text className="font-sans text-sm text-ink-soft">{t("Str.TopBar.ChangePassword")}</Text>
          </Pressable>
          {canToggle ? (
            <Pressable onPress={() => { setMenu(false); setLanguage(language === "ar" ? "en" : "ar"); }} className="flex-row items-center gap-2 px-3 py-2.5 active:bg-paper">
              <Icon icon={Languages} size={16} tone="ink-soft" />
              <Text className="font-sans text-sm text-ink-soft">{t("Str.TopBar.Language")}</Text>
            </Pressable>
          ) : null}
          <Pressable onPress={() => { setMenu(false); signOut(); }} className="flex-row items-center gap-2 px-3 py-2.5 active:bg-danger-soft">
            <Icon icon={LogOut} size={16} tone="danger" />
            <Text className="font-sans text-sm text-danger">{t("Str.Logout")}</Text>
          </Pressable>
        </View>
      ) : null}

      <View className="flex-1 bg-card">{children}</View>
      <FormDialog open={password} title={t("Str.TopBar.ChangePassword")} fields={passwordFields} onSubmit={changePassword} onClose={() => setPassword(false)} />
      <SnackbarHost />

      <Animated.View pointerEvents={drawer ? "auto" : "none"} className="absolute inset-0 z-40 bg-brand" style={scrim}>
        <Pressable className="flex-1" onPress={() => setDrawer(false)} />
      </Animated.View>
      <Animated.View pointerEvents={drawer ? "auto" : "none"} className="absolute inset-y-0 start-0 z-50 bg-field" style={[{ width: Math.min(WIDTH, width * 0.82), paddingTop: insets.top }, panel]}>
        <View className="h-14 flex-row items-center gap-2.5 border-b border-field-line px-3">
          <View className="size-8 items-center justify-center rounded-lg bg-white/10">
            <Text className="font-sans text-sm font-bold text-on-field">{t("Str.AppName").slice(0, 1)}</Text>
          </View>
          <Text numberOfLines={1} className={`flex-1 font-sans text-base font-bold text-on-field ${start}`}>{t("Str.AppName")}</Text>
        </View>
        <View className="justify-center px-2 py-2">
          <View className="absolute start-5 z-10">
            <Icon icon={Search} size={16} tone="on-field-faint" />
          </View>
          <TextInput
            value={filter}
            onChangeText={setFilter}
            placeholder={t("Str.SearchPlaceholder")}
            placeholderTextColor={String(faint)}
            className={`h-9 rounded-lg bg-white/10 ps-9 pe-3 font-sans text-sm text-on-field ${start}`}
          />
        </View>
        <ScrollView className="flex-1 px-2" contentContainerStyle={{ paddingBottom: insets.bottom + 16, gap: 2 }}>
          {groups.map((group) => {
            const expanded = !!filter || open === group.key;
            const GroupIcon = sectionIcons[group.icon];
            return (
              <View key={group.key}>
                <Pressable onPress={() => setOpen(expanded ? null : group.key)} className="flex-row items-center gap-2.5 rounded-lg px-2.5 py-2.5 active:bg-field-hover">
                  <Icon icon={GroupIcon} size={18} tone={section?.key === group.key ? "on-field" : "on-field-soft"} />
                  <Text className={`flex-1 font-sans text-sm font-medium ${section?.key === group.key ? "text-on-field" : "text-on-field-soft"} ${start}`}>{t(`Str.Nav.${group.key}`)}</Text>
                  <View style={{ transform: [{ rotate: expanded ? "180deg" : "0deg" }] }}>
                    <Icon icon={ChevronDown} size={16} tone="on-field-faint" />
                  </View>
                </Pressable>
                {expanded ? (
                  <View className="mb-1.5 ms-4 mt-0.5 gap-px border-s border-field-line ps-1.5">
                    {group.pages.map((key) => {
                      const active = current === key;
                      return (
                        <Pressable key={key} onPress={() => go(key)} className={`rounded-md px-2.5 py-2 ${active ? "bg-field-raised" : "active:bg-field-hover"}`}>
                          {active ? <View className="absolute inset-y-1.5 start-0 w-0.5 rounded-full bg-chart-1" /> : null}
                          <Text numberOfLines={1} className={`font-sans text-[13px] ${active ? "font-medium text-on-field" : "text-on-field-soft"} ${start}`}>{t(pageByKey.get(key)!.title)}</Text>
                        </Pressable>
                      );
                    })}
                  </View>
                ) : null}
              </View>
            );
          })}
        </ScrollView>
      </Animated.View>
    </View>
  );
}
