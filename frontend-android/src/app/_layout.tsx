import "../global.css";
import { IBMPlexSansArabic_400Regular, IBMPlexSansArabic_500Medium, IBMPlexSansArabic_600SemiBold, IBMPlexSansArabic_700Bold, useFonts } from "@expo-google-fonts/ibm-plex-sans-arabic";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { router, Stack, usePathname } from "expo-router";
import { StatusBar } from "expo-status-bar";
import { useEffect, useState } from "react";
import { ActivityIndicator, View } from "react-native";
import { GestureHandlerRootView } from "react-native-gesture-handler";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { useSession } from "@/features/auth/session";
import { ApiError } from "@/lib/api";
import { useLanguage } from "@/lib/i18n";
import { useServer } from "@/lib/platform";
import { useDirection } from "~/lib/direction";

/** كإعداد الويب في App.tsx */
const client = new QueryClient({
  defaultOptions: {
    queries: { staleTime: 30_000, retry: (failures, error) => !(error instanceof ApiError && error.status < 500) && failures < 2 },
  },
});

/** المخازن تُقرأ من الجهاز قبل أي قرار */
const stores = [useSession, useLanguage, useServer] as const;
const loaded = () => stores.every((s) => s.persist.hasHydrated());

export default function RootLayout() {
  const direction = useDirection();
  const pathname = usePathname();
  const [ready, setReady] = useState(loaded);
  /** اسم العائلة كما في tokens.css (--font-sans)، والأوزان الأخرى للخطّ العريض */
  const [fonts] = useFonts({
    "IBM Plex Sans Arabic": IBMPlexSansArabic_400Regular,
    IBMPlexSansArabic_500Medium,
    IBMPlexSansArabic_600SemiBold,
    IBMPlexSansArabic_700Bold,
  });

  useEffect(() => {
    if (ready) return;
    const offs = stores.map((s) => s.persist.onFinishHydration(() => setReady(loaded())));
    setReady(loaded());
    return () => offs.forEach((off) => off());
  }, [ready]);

  /** انتهت الجلسة ← الدخول — كالويب */
  useEffect(
    () =>
      useSession.subscribe((state, previous) => {
        if (previous.token && !state.token && pathname !== "/login") router.replace("/login");
      }),
    [pathname],
  );

  return (
    <GestureHandlerRootView style={{ flex: 1 }}>
      <SafeAreaProvider>
        <QueryClientProvider client={client}>
          <View className="flex-1 bg-field" style={{ direction }}>
            <StatusBar style="light" />
            {ready && fonts ? (
              <Stack screenOptions={{ headerShown: false, animation: "fade" }} />
            ) : (
              <View className="flex-1 items-center justify-center">
                <ActivityIndicator color="#ffffff" />
              </View>
            )}
          </View>
        </QueryClientProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
