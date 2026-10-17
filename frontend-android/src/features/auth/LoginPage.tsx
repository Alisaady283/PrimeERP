import { useMutation } from "@tanstack/react-query";
import { router } from "expo-router";
import { Check, CircleAlert, CircleCheck, Eye, EyeOff, KeyRound, LogIn, Server, User } from "lucide-react-native";
import { useRef, useState } from "react";
import { KeyboardAvoidingView, Pressable, ScrollView, Text, View, type TextInput } from "react-native";
import Animated, { useAnimatedStyle, useSharedValue, withSequence, withTiming } from "react-native-reanimated";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useSession, type SessionUser } from "@/features/auth/session";
import { api, ApiError } from "@/lib/api";
import { useText } from "@/lib/i18n";
import { duration } from "@/lib/motion";
import { useServer } from "@/lib/platform";
import { useStart } from "~/lib/direction";
import { Button } from "~/components/ui/Button";
import { Enter } from "~/components/ui/Enter";
import { TextField } from "~/components/ui/Field";
import { Icon } from "~/components/ui/Icon";

type SessionResponse = { token: string; expiresAt: string; user: SessionUser };

/** مهلة رسالة النجاح قبل الانتقال — كالويب */
const VERIFIED_HOLD = 800;

/** نظير LoginPage في الويب بتخطيطه على الهاتف */
export function LoginPage() {
  const t = useText();
  const start = useStart();
  const insets = useSafeAreaInsets();
  const signIn = useSession((s) => s.signIn);
  const server = useServer();
  const [url, setUrl] = useState(server.url);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [visible, setVisible] = useState(false);
  const [missing, setMissing] = useState<string | null>(null);
  const [verified, setVerified] = useState(false);
  const usernameRef = useRef<TextInput>(null);
  const passwordRef = useRef<TextInput>(null);
  const urlRef = useRef<TextInput>(null);
  const shake = useSharedValue(0);
  const shaking = useAnimatedStyle(() => ({ transform: [{ translateX: shake.value }] }));

  /** هزّة الخطأ: إزاحاتٌ متناقصة كالويب، في medium4 */
  const jolt = () => {
    const step = duration.medium4 / 6;
    shake.value = withSequence(...[-10, 10, -6, 6, -2, 0].map((x) => withTiming(x, { duration: step })));
  };

  const login = useMutation({
    mutationFn: () => {
      server.setUrl(url);
      return api.post<SessionResponse>("/auth/login", { username, password });
    },
    onSuccess: (session) => {
      setVerified(true);
      setTimeout(() => {
        signIn(session.token, session.expiresAt, session.user);
        router.replace("/");
      }, VERIFIED_HOLD);
    },
    onError: () => {
      setPassword("");
      passwordRef.current?.focus();
      jolt();
    },
  });

  const demand = (value: string, label: string, ref: typeof urlRef) => {
    if (value.trim()) return true;
    setMissing(t("Str.Rule.Required", t(label)));
    ref.current?.focus();
    jolt();
    return false;
  };

  const submit = () => {
    if (verified || login.isPending) return;
    if (!demand(url, "Str.Login.Server", urlRef) || !demand(username, "Str.Login.Username", usernameRef) || !demand(password, "Str.Login.Password", passwordRef)) return;
    setMissing(null);
    login.mutate();
  };

  const failed = !!login.error && !login.isPending;
  const problem = missing ?? (failed ? (login.error instanceof ApiError ? login.error.message : t("Str.Login.ServerUnreachable")) : null);
  const tint = verified ? "border-success/40 bg-success-soft" : failed ? "border-danger/50" : "";

  return (
    <KeyboardAvoidingView behavior="height" className="flex-1 bg-card">
      <ScrollView contentContainerStyle={{ flexGrow: 1, justifyContent: "center", padding: 24, paddingTop: insets.top + 24 }} keyboardShouldPersistTaps="handled">
        <Animated.View className="w-full max-w-sm gap-6 self-center" style={shaking}>
          <Enter index={0} className="gap-2">
            <View className="size-12 items-center justify-center rounded-2xl bg-brand shadow-pop">
              <Icon icon={LogIn} size={20} tone="on-field" />
            </View>
            <Text className={`font-sans text-2xl font-bold text-ink ${start}`}>{t("Str.Login.Submit")}</Text>
            <Text className={`font-sans text-sm text-ink-tertiary ${start}`}>{t("Str.Login.Subtitle")}</Text>
          </Enter>

          <View className="gap-4">
            <Enter index={1}>
              <TextField ref={urlRef} label={t("Str.Login.Server")} icon={<Icon icon={Server} size={16} tone="ink-faint" />} value={url} onChangeText={setUrl} placeholder="http://192.168.1.10:5080" autoCapitalize="none" autoCorrect={false} keyboardType="url" className={tint} />
            </Enter>
            <Enter index={2}>
              <TextField ref={usernameRef} label={t("Str.Login.Username")} icon={<Icon icon={User} size={16} tone="ink-faint" />} value={username} onChangeText={setUsername} autoCapitalize="none" autoComplete="username" className={tint} />
            </Enter>
            <Enter index={3}>
              <TextField
                ref={passwordRef}
                label={t("Str.Login.Password")}
                icon={<Icon icon={KeyRound} size={16} tone="ink-faint" />}
                value={password}
                onChangeText={setPassword}
                secureTextEntry={!visible}
                autoComplete="current-password"
                onSubmitEditing={submit}
                className={tint}
                end={
                  <Pressable hitSlop={8} accessibilityLabel={t(visible ? "Str.Login.HidePassword" : "Str.Login.ShowPassword")} onPress={() => setVisible((v) => !v)} className="size-7 items-center justify-center rounded-md active:bg-paper">
                    <Icon icon={visible ? EyeOff : Eye} size={16} tone="ink-faint" />
                  </Pressable>
                }
              />
            </Enter>
          </View>

          {problem || verified ? (
            <Enter from={-6} className={`flex-row items-center gap-2 rounded-lg border p-3 ${verified ? "border-success/25 bg-success-soft" : "border-danger/25 bg-danger-soft"}`}>
              <Icon icon={verified ? CircleCheck : CircleAlert} size={16} tone={verified ? "success" : "danger"} />
              <Text className={`flex-1 font-sans text-sm ${verified ? "text-success" : "text-danger"} ${start}`}>{verified ? t("Str.Login.Redirecting") : problem}</Text>
            </Enter>
          ) : null}

          <Enter index={4}>
            <Button
              size="lg"
              variant={verified ? "success" : "primary"}
              className={verified ? "opacity-60" : ""}
              loading={login.isPending}
              disabled={verified}
              icon={verified ? <Icon icon={Check} size={16} tone="on-field" /> : undefined}
              onPress={submit}
            >
              {verified ? t("Str.Login.Verified") : login.isPending ? t("Str.Login.Checking") : t("Str.Login.Submit")}
            </Button>
          </Enter>
        </Animated.View>
      </ScrollView>
    </KeyboardAvoidingView>
  );
}
