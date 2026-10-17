import { Redirect, Stack } from "expo-router";
import { isSignedIn, useSession } from "@/features/auth/session";
import { AppShell } from "~/components/layout/AppShell";

/** كمسار shell في الويب: الحارس ثم الإطار */
export default function ShellLayout() {
  useSession((s) => s.token);
  if (!isSignedIn()) return <Redirect href="/login" />;
  return (
    <AppShell>
      <Stack screenOptions={{ headerShown: false, animation: "slide_from_right" }} />
    </AppShell>
  );
}
