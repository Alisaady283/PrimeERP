import { Link, Outlet, useRouterState } from "@tanstack/react-router";
import { ChevronDown, KeyRound, Languages, LogOut, Menu as MenuIcon, PanelRightClose, PanelRightOpen, Search, X } from "lucide-react";
import { motion } from "motion/react";
import { duration, easing, tween } from "@/lib/motion";
import { useMemo, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { prefetchWebPage } from "@/features/workspace/charts";
import { CommandPalette } from "@/components/layout/CommandPalette";
import { NotificationBell } from "@/components/layout/NotificationBell";
import { FormDialog } from "@/features/workspace/FormDialog";
import { changePassword, passwordFields } from "@/features/auth/password";
import { sectionOf, sections } from "@/catalog/navigation";
import { sectionIcons } from "@/components/layout/sectionIcons";
import { pageByKey } from "@/catalog/pages";
import { Menu, type MenuItem } from "@/components/ui/Menu";
import { useInterface, useVisiblePage } from "@/features/auth/interface";
import { useSession } from "@/features/auth/session";
import { cn } from "@/lib/cn";
import { formatGregorian, formatHijri } from "@/lib/format";
import { useLanguage, useText } from "@/lib/i18n";

export function AppShell() {
  const t = useText();
  const pathname = useRouterState({ select: (state) => state.location.pathname });
  const user = useSession((state) => state.user);
  const signOut = useSession((state) => state.signOut);
  const { language, setLanguage } = useLanguage();
  const settings = useInterface();
  const visible = useVisiblePage();
  const [filter, setFilter] = useState("");
  const client = useQueryClient();
  const [mobile, setMobile] = useState(false);
  const [password, setPassword] = useState(false);
  const [command, setCommand] = useState(false);
  const [rail, setRail] = useState(() => {
    try {
      return localStorage.getItem("sidebar.rail") === "1";
    } catch {
      return false;
    }
  });
  const toggleRail = () =>
    setRail((r) => {
      try {
        localStorage.setItem("sidebar.rail", r ? "0" : "1");
      } catch {
        /* تخزينٌ محجوب */
      }
      return !r;
    });
  const current = pathname.startsWith("/p/") ? decodeURIComponent(pathname.slice(3)) : "";
  const [open, setOpen] = useState<string | null>(() => sectionOf(current)?.key ?? null);

  const groups = useMemo(() => {
    const q = filter.trim().toLowerCase();
    return sections
      .map((s) => ({ ...s, pages: s.pages.filter((p) => visible(p) && (!q || t(pageByKey.get(p)!.title).toLowerCase().includes(q))) }))
      .filter((s) => s.pages.length > 0);
  }, [visible, filter, t]);

  const section = sectionOf(current);
  const page = pageByKey.get(current);
  const canToggle = (settings?.arabic ?? true) && (settings?.english ?? true);
  const userName = user ? user.displayName || user.username : "";
  const today = new Date();
  const userMenu: MenuItem[] = [
    { label: t("Str.TopBar.ChangePassword"), icon: <KeyRound className="size-4" />, onSelect: () => setPassword(true) },
    ...(canToggle ? [{ label: t("Str.TopBar.Language"), icon: <Languages className="size-4" />, onSelect: () => setLanguage(language === "ar" ? "en" : "ar") }] : []),
    {
      label: t("Str.Logout"),
      icon: <LogOut className="size-4" />,
      danger: true,
      onSelect: signOut,
    },
  ];

  const sidebar = (
    <aside className="flex h-full w-64 shrink-0 flex-col bg-field text-on-field-soft">
      <div className="flex h-14 shrink-0 items-center gap-2.5 border-b border-field-line ps-3 pe-2">
        <span className="grid size-8 shrink-0 place-items-center rounded-lg bg-white/10 text-sm font-bold text-on-field">{t("Str.AppName").slice(0, 1)}</span>
        <span className="flex-1 truncate text-base font-bold tracking-tight text-on-field">{t("Str.AppName")}</span>
        <button type="button" title={t(rail ? "Str.Sidebar.Expand" : "Str.Sidebar.Collapse")} aria-label={t(rail ? "Str.Sidebar.Expand" : "Str.Sidebar.Collapse")} onClick={toggleRail} className="hidden size-8 shrink-0 place-items-center rounded-lg text-on-field-faint transition-colors hover:bg-field-hover hover:text-on-field lg:grid">
          <PanelRightClose className="size-[18px] ltr:-scale-x-100" />
        </button>
      </div>
      <div className="px-2 py-2">
        <div className="relative">
          <Search className="pointer-events-none absolute start-3 top-1/2 size-4 -translate-y-1/2 text-on-field-faint" />
          <input
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            placeholder={t("Str.SearchPlaceholder")}
            className="h-9 w-full rounded-lg bg-white/10 ps-9 pe-3 text-sm text-on-field outline-none placeholder:text-on-field-faint focus:bg-white/15"
          />
        </div>
      </div>
      <nav className="scroll-field flex-1 space-y-0.5 overflow-y-auto px-2 py-2">
        {groups.map((group) => {
          const expanded = !!filter || open === group.key;
          const Icon = sectionIcons[group.icon];
          return (
            <div key={group.key}>
              <button
                type="button"
                onClick={() => setOpen(expanded ? null : group.key)}
                className={cn("flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2 text-sm font-medium transition-colors hover:bg-field-hover hover:text-on-field", section?.key === group.key && "text-on-field")}
              >
                <Icon className="size-[18px]" />
                <span className="flex-1 text-start">{t(`Str.Nav.${group.key}`)}</span>
                <ChevronDown className={cn("size-4 transition-transform", expanded && "rotate-180")} />
              </button>
              {expanded && (
                <div className="mt-0.5 mb-1.5 space-y-px border-s border-field-line ms-4 ps-1.5">
                  {group.pages.map((key) => (
                    <Link
                      key={key}
                      to="/p/$page"
                      params={{ page: key }}
                      onClick={() => setMobile(false)}
                      onMouseEnter={() => prefetchWebPage(client, key)}
                      className={cn(
                        "relative block truncate rounded-md px-2.5 py-1.5 text-[13px] transition-colors hover:bg-field-hover hover:text-on-field",
                        current === key && "font-medium text-on-field hover:bg-transparent",
                      )}
                    >
                      {current === key && (
                        <motion.span layoutId="nav-active" transition={tween(duration.medium2)} className="absolute inset-0 rounded-md bg-field-raised">
                          <span className="absolute inset-y-1.5 start-0 w-0.5 rounded-full bg-chart-1" />
                        </motion.span>
                      )}
                      <span className="relative">{t(pageByKey.get(key)!.title)}</span>
                    </Link>
                  ))}
                </div>
              )}
            </div>
          );
        })}
      </nav>
    </aside>
  );

  /** شريط الأيقونات */
  const railbar = (
    <aside className="flex h-full w-16 shrink-0 flex-col items-center bg-field text-on-field-soft">
      <div className="flex h-14 w-full shrink-0 items-center justify-center border-b border-field-line">
        <button type="button" title={t(rail ? "Str.Sidebar.Expand" : "Str.Sidebar.Collapse")} aria-label={t(rail ? "Str.Sidebar.Expand" : "Str.Sidebar.Collapse")} onClick={toggleRail} className="hidden size-8 shrink-0 place-items-center rounded-lg text-on-field-faint transition-colors hover:bg-field-hover hover:text-on-field lg:grid">
          <PanelRightOpen className="size-[18px] ltr:-scale-x-100" />
        </button>
      </div>
      <nav className="scroll-field flex w-full flex-1 flex-col items-center gap-1 overflow-y-auto py-2">
        {groups.map((group) => {
          const Icon = sectionIcons[group.icon];
          return (
            <button
              key={group.key}
              type="button"
              title={t(`Str.Nav.${group.key}`)}
              onClick={() => {
                setOpen(group.key);
                toggleRail();
              }}
              className={cn("grid size-10 place-items-center rounded-lg transition-colors hover:bg-field-hover hover:text-on-field", section?.key === group.key && "bg-field-raised text-on-field")}
            >
              <Icon className="size-[18px]" />
            </button>
          );
        })}
      </nav>
    </aside>
  );

  return (
    <div className="flex h-dvh overflow-hidden">
      <motion.div initial={false} animate={{ width: rail ? 64 : 256 }} transition={tween(duration.medium4)} className="hidden overflow-hidden lg:flex">
        {rail ? railbar : sidebar}
      </motion.div>
      {mobile && (
        <div className="fixed inset-0 z-50 flex lg:hidden">
          {sidebar}
          <button type="button" aria-label="close" onClick={() => setMobile(false)} className="flex-1 bg-brand/40 backdrop-blur-[2px]">
            <X className="m-4 size-5 text-white" />
          </button>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="relative z-30 flex h-14 shrink-0 items-center justify-between gap-3 border-b border-field-line bg-field px-4 text-on-field-soft">
          <div className="flex min-w-0 items-center gap-2">
            <button type="button" onClick={() => setMobile(true)} className="rounded-lg p-2 text-on-field-soft hover:bg-field-hover hover:text-on-field lg:hidden">
              <MenuIcon className="size-5" />
            </button>
            <p className="truncate text-sm text-on-field-faint">
              {section && t(`Str.Nav.${section.key}`)}
              {page && <span className="mx-2 text-on-field-faint/60">/</span>}
              {page && <span className="font-semibold text-on-field">{t(page.title)}</span>}
            </p>
          </div>
          <div className="flex shrink-0 items-center gap-4">
            <button
              type="button"
              onClick={() => setCommand(true)}
              className="hidden h-9 w-56 items-center gap-2 rounded-lg bg-white/10 px-3 text-sm text-on-field-faint transition-colors hover:bg-white/15 hover:text-on-field md:flex"
            >
              <Search className="size-4" />
              <span className="flex-1 text-start">{t("Str.Command.Open")}</span>
              <kbd className="rounded border border-white/15 px-1.5 font-sans text-[11px]">Ctrl K</kbd>
            </button>
            {settings?.companyName && <span className="hidden truncate text-sm text-on-field-soft xl:inline">{settings.companyName}</span>}
            <div className="hidden text-xs leading-tight sm:block">
              <p className="text-on-field">{formatGregorian(today)}</p>
              <p className="text-on-field-faint">{formatHijri(today)}</p>
            </div>
            <NotificationBell />
            <div className="flex items-center gap-2">
              <span className="grid size-8 place-items-center rounded-full bg-white/15 text-sm font-semibold text-on-field">{userName.slice(0, 1)}</span>
              <Menu label={userName} items={userMenu} tone="field" />
            </div>
          </div>
        </header>
        <main className="min-h-0 flex-1 overflow-hidden p-3">
          <motion.div key={pathname} initial={{ opacity: 0 }} animate={{ opacity: 1 }} transition={tween(duration.short3, easing.decelerate)} className="h-full">
            <Outlet />
          </motion.div>
        </main>
      </div>
      <CommandPalette open={command} onOpenChange={setCommand} />
      <FormDialog
        open={password}
        title={t("Str.TopBar.ChangePassword")}
        fields={passwordFields}
        onSubmit={changePassword}
        onClose={() => setPassword(false)}
      />
    </div>
  );
}
