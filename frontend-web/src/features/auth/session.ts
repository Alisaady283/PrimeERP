import { create } from "zustand";
import { persist } from "zustand/middleware";
import { sessionOnly } from "@/lib/platform";

export type SessionUser = {
  id: number;
  username: string;
  displayName: string;
  roleName: string;
  branchId: number | null;
  permissions: string[];
  /** الصفحات التي لها صلاحياتها باسمها في الخادم */
  pages?: string[];
};

type SessionState = {
  token: string | null;
  expiresAt: string | null;
  user: SessionUser | null;
  signIn: (token: string, expiresAt: string, user: SessionUser) => void;
  signOut: () => void;
};

export const useSession = create<SessionState>()(
  persist(
    (set) => ({
      token: null,
      expiresAt: null,
      user: null,
      signIn: (token, expiresAt, user) => set({ token, expiresAt, user }),
      signOut: () => set({ token: null, expiresAt: null, user: null }),
    }),
    { name: "primeerp.session", storage: sessionOnly },
  ),
);

export const isSignedIn = () => {
  const { token, expiresAt } = useSession.getState();
  return !!token && !!expiresAt && new Date(expiresAt).getTime() > Date.now();
};

export const useCan = (key: string) => useSession((state) => state.user?.permissions.includes(key) ?? false);

/** بوّابة الصفحة: مفتاحها إن سجّل الخادم صلاحياتها، وإلا مجالها — PagePermission في الدسكتوب */
export const pageGate = (pages: string[] | undefined, key: string, permission: string) =>
  pages?.includes(key) ? key : permission;

export const usePagePermission = (spec: { key: string; permission: string }) =>
  useSession((state) => pageGate(state.user?.pages, spec.key, spec.permission));
