import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { RouterProvider } from "@tanstack/react-router";
import { MotionConfig } from "motion/react";
import { useEffect } from "react";
import { router } from "@/app/router";
import { ConfirmHost } from "@/components/ui/Confirm";
import { ToastHost } from "@/components/ui/Toast";
import { ApiError } from "@/lib/api";
import { directionOf, useLanguage } from "@/lib/i18n";
import { useIdentity } from "@/lib/identity";
import { useSession } from "@/features/auth/session";

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      refetchOnWindowFocus: false,
      retry: (failures, error) => !(error instanceof ApiError && error.status < 500) && failures < 2,
    },
  },
});

export function App() {
  const language = useLanguage((state) => state.language);
  const identity = useIdentity((state) => state.identity);

  useEffect(() => {
    document.documentElement.lang = language;
    document.documentElement.dir = directionOf(language);
    void queryClient.invalidateQueries();
  }, [language]);

  useEffect(() => {
    document.documentElement.dataset.identity = identity;
  }, [identity]);

  /** انتهت الجلسة ← الدخول ثم العودة */
  useEffect(
    () =>
      useSession.subscribe((state, previous) => {
        const location = router.state.location;
        if (previous.token && !state.token && location.pathname !== "/login")
          void router.navigate({ to: "/login", search: { redirect: location.href }, replace: true });
      }),
    [],
  );

  return (
    <QueryClientProvider client={queryClient}>
      <MotionConfig reducedMotion="user">
        <RouterProvider router={router} />
        <ToastHost />
        <ConfirmHost />
      </MotionConfig>
    </QueryClientProvider>
  );
}
