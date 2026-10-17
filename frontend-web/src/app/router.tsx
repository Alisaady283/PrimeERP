import { createRootRoute, createRoute, createRouter, lazyRouteComponent, Navigate, Outlet, redirect } from "@tanstack/react-router";
import { AppShell } from "@/components/layout/AppShell";
import { EmptyState } from "@/components/ui/EmptyState";
import { sections } from "@/catalog/navigation";
import { pageByKey } from "@/catalog/pages";
import { useVisiblePage } from "@/features/auth/interface";
import { isSignedIn } from "@/features/auth/session";
import { useText } from "@/lib/i18n";
import { PageHost } from "@/features/workspace/Pages";

const root = createRootRoute({ component: Outlet });

const login = createRoute({
  getParentRoute: () => root,
  path: "/login",
  validateSearch: (search: Record<string, unknown>): { redirect?: string } => ({ redirect: typeof search.redirect === "string" ? search.redirect : undefined }),
  beforeLoad: () => {
    if (isSignedIn()) throw redirect({ to: "/" });
  },
  component: lazyRouteComponent(() => import("@/features/auth/LoginPage"), "LoginPage"),
});

const shell = createRoute({
  getParentRoute: () => root,
  id: "shell",
  beforeLoad: ({ location }) => {
    if (!isSignedIn()) throw redirect({ to: "/login", search: { redirect: location.href } });
  },
  component: AppShell,
});

function Home() {
  const visible = useVisiblePage();
  const first = sections.flatMap((s) => s.pages).find(visible);
  const t = useText();
  return first ? <Navigate to="/p/$page" params={{ page: first }} replace /> : <EmptyState message={t("Str.NoData")} />;
}

function PageRoute() {
  const { page } = workspace.useParams();
  const { q, from, to } = workspace.useSearch();
  const visible = useVisiblePage();
  const t = useText();
  const spec = pageByKey.get(page);
  if (!spec || !visible(page)) return <EmptyState message={t("Str.PermissionDenied")} />;
  return <PageHost key={`${page}:${q ?? ""}:${from ?? ""}:${to ?? ""}`} spec={spec} />;
}

const home = createRoute({ getParentRoute: () => shell, path: "/", component: Home });

const workspace = createRoute({
  getParentRoute: () => shell,
  path: "/p/$page",
  /** ما تُفتح به الصفحة مصفّاةً: نصّ البحث، ونطاق التاريخ */
  validateSearch: (search: Record<string, unknown>): PageSearch => {
    const of = (v: unknown) => (typeof v === "string" && v ? v : undefined);
    return { q: of(search.q), from: of(search.from), to: of(search.to) };
  },
  component: PageRoute,
});

export type PageSearch = { q?: string; from?: string; to?: string };

export const router = createRouter({
  routeTree: root.addChildren([login, shell.addChildren([home, workspace])]),
  defaultPreload: "intent",
});

declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
