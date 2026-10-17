import { useMutation } from "@tanstack/react-query";
import { useNavigate, useSearch } from "@tanstack/react-router";
import { AnimatePresence, motion, useAnimationControls } from "motion/react";
import { Check, CircleAlert, CircleCheck, Eye, EyeOff, KeyRound, LogIn, TriangleAlert, User } from "lucide-react";
import { useRef, useState, type FormEvent, type KeyboardEvent } from "react";
import { Button } from "@/components/ui/Button";
import { TextField } from "@/components/ui/Field";
import { api, ApiError } from "@/lib/api";
import { cn } from "@/lib/cn";
import { useText } from "@/lib/i18n";
import { duration, easing, enterAt, LARGE, tween } from "@/lib/motion";
import { useSession, type SessionUser } from "@/features/auth/session";

type SessionResponse = { token: string; expiresAt: string; user: SessionUser };

/** مهلة رسالة النجاح قبل الانتقال */
const VERIFIED_HOLD = 800;

/** مسارٌ داخلي فقط */
const safe = (path?: string) => (path && path.startsWith("/") && !path.startsWith("//") ? path : "/");

/** هزّة الخطأ: إزاحاتٌ متناقصة */
const shake = { x: [0, -10, 10, -6, 6, -2, 0] };

/** وهجٌ يتحرّك ببطء */
function Glow({ className, drift }: { className: string; drift: { x: number[]; y: number[] } }) {
  return <motion.div aria-hidden className={cn("absolute rounded-full blur-3xl", className)} animate={drift} transition={{ duration: 18, ease: "easeInOut", repeat: Infinity, repeatType: "mirror" }} />;
}

export function LoginPage() {
  const t = useText();
  const navigate = useNavigate();
  const { redirect: back } = useSearch({ from: "/login" });
  const signIn = useSession((state) => state.signIn);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [visible, setVisible] = useState(false);
  const [caps, setCaps] = useState(false);
  const [verified, setVerified] = useState(false);
  const form = useAnimationControls();
  const usernameRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const [missing, setMissing] = useState<string | null>(null);

  const login = useMutation({
    mutationFn: () => api.post<SessionResponse>("/auth/login", { username, password }),
    onSuccess: (session) => {
      setVerified(true);
      window.setTimeout(() => {
        signIn(session.token, session.expiresAt, session.user);
        void navigate({ href: safe(back), replace: true });
      }, VERIFIED_HOLD);
    },
    onError: () => {
      setPassword("");
      passwordRef.current?.focus();
      void form.start(shake, tween(duration.medium4, easing.emphasized));
    },
  });

  /** الحقل الفارغ: رسالةٌ وتركيز */
  const demand = (value: string, label: string, ref: typeof usernameRef) => {
    if (value.trim()) return true;
    setMissing(t("Str.Rule.Required", t(label)));
    ref.current?.focus();
    void form.start(shake, tween(duration.medium4, easing.emphasized));
    return false;
  };

  const submit = (event: FormEvent) => {
    event.preventDefault();
    if (verified || login.isPending) return;
    if (!demand(username, "Str.Login.Username", usernameRef) || !demand(password, "Str.Login.Password", passwordRef)) return;
    setMissing(null);
    login.mutate();
  };

  const watchCaps = (event: KeyboardEvent<HTMLInputElement>) => setCaps(event.getModifierState("CapsLock"));
  const failed = !!login.error && !login.isPending;
  const tint = verified ? "border-success/40 bg-success-soft/60" : failed ? "border-danger/50" : undefined;
  const problem = missing ?? (failed ? (login.error instanceof ApiError ? login.error.message : t("Str.Error")) : null);

  return (
    <div className="grid min-h-dvh lg:grid-cols-[minmax(22rem,36%)_1fr]">
      <motion.section className="relative hidden overflow-hidden bg-field lg:block" {...enterAt(0, { x: 24 }, LARGE)}>
        <Glow className="-top-32 -start-32 size-[28rem] bg-chart-1/35" drift={{ x: [0, 60, -20], y: [0, 40, 80] }} />
        <Glow className="-bottom-40 -end-24 size-[30rem] bg-chart-3/30" drift={{ x: [0, -50, 30], y: [0, -60, -20] }} />
        <Glow className="top-1/3 start-1/4 size-64 bg-chart-6/20" drift={{ x: [0, 40, -40], y: [0, -30, 30] }} />
        <div className="relative flex h-full flex-col justify-between p-10 text-on-field">
          <motion.span className="text-2xl font-bold tracking-tight" {...enterAt(1)}>{t("Str.AppName")}</motion.span>
          <motion.p className="max-w-xs text-2xl font-semibold leading-snug" {...enterAt(2)}>{t("Str.Login.Subtitle")}</motion.p>
        </div>
      </motion.section>

      <section className="flex items-center justify-center p-6">
        <motion.form onSubmit={submit} noValidate animate={form} className="w-full max-w-sm space-y-6">
          <motion.div className="space-y-2" {...enterAt(3)}>
            <span className="grid size-12 place-items-center rounded-2xl bg-brand text-on-field shadow-pop">
              <LogIn className="size-5" />
            </span>
            <h1 className="text-2xl font-bold text-ink">{t("Str.Login.Submit")}</h1>
            <p className="text-sm text-ink-tertiary">{t("Str.Login.Subtitle")}</p>
          </motion.div>

          <div className="space-y-4">
            <motion.div {...enterAt(4)}>
              <TextField
                ref={usernameRef}
                label={t("Str.Login.Username")}
                icon={<User className="size-4" />}
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                autoComplete="username"
                autoFocus
                className={tint}
              />
            </motion.div>
            <motion.div {...enterAt(5)}>
              <TextField
                ref={passwordRef}
                label={t("Str.Login.Password")}
                icon={<KeyRound className="size-4" />}
                type={visible ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                onKeyDown={watchCaps}
                onKeyUp={watchCaps}
                autoComplete="current-password"
                className={tint}
                end={
                  <button
                    type="button"
                    title={t(visible ? "Str.Login.HidePassword" : "Str.Login.ShowPassword")}
                    aria-label={t(visible ? "Str.Login.HidePassword" : "Str.Login.ShowPassword")}
                    onClick={() => setVisible((v) => !v)}
                    className="grid size-7 place-items-center rounded-md text-ink-faint transition-colors hover:bg-paper hover:text-ink"
                  >
                    {visible ? <EyeOff className="size-4" /> : <Eye className="size-4" />}
                  </button>
                }
              />
              <AnimatePresence>
                {caps && (
                  <motion.p
                    initial={{ opacity: 0, height: 0 }}
                    animate={{ opacity: 1, height: "auto" }}
                    exit={{ opacity: 0, height: 0 }}
                    transition={tween(duration.short4, easing.decelerate)}
                    className="mt-1.5 flex items-center gap-1.5 overflow-hidden text-xs text-warning"
                  >
                    <TriangleAlert className="size-3.5" />
                    {t("Str.Login.CapsLock")}
                  </motion.p>
                )}
              </AnimatePresence>
            </motion.div>
          </div>

          <AnimatePresence mode="wait">
            {(problem || verified) && (
              <motion.p
                key={verified ? "ok" : "error"}
                role={verified ? "status" : "alert"}
                initial={{ opacity: 0, y: -6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                transition={tween(duration.medium2, easing.decelerate)}
                className={cn("flex items-center gap-2 rounded-lg border p-3 text-sm", verified ? "border-success/25 bg-success-soft text-success" : "border-danger/25 bg-danger-soft text-danger")}
              >
                {verified ? <CircleCheck className="size-4 shrink-0" /> : <CircleAlert className="size-4 shrink-0" />}
                {verified ? t("Str.Login.Redirecting") : problem}
              </motion.p>
            )}
          </AnimatePresence>

          <motion.div {...enterAt(6)}>
            <Button
              type="submit"
              className={cn("w-full", verified && "bg-success/60 from-transparent shadow-none ring-1 ring-success/30 disabled:opacity-100")}
              disabled={verified}
              loading={login.isPending}
              icon={verified ? <Check className="size-4" /> : undefined}
            >
              {verified ? t("Str.Login.Verified") : login.isPending ? t("Str.Login.Checking") : t("Str.Login.Submit")}
            </Button>
          </motion.div>
        </motion.form>
      </section>
    </div>
  );
}
