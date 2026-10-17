import { text, useLanguage } from "@/lib/i18n";
import { useSession } from "@/features/auth/session";
import { apiBase } from "@/lib/platform";

export type PageResponse<T> = { items: T[]; total: number; page: number; pageSize: number };

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly errors: string[] = [],
  ) {
    super(message);
  }
}

type Query = Record<string, string | number | boolean | null | undefined>;

const urlOf = (path: string, query?: Query) => {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query ?? {})) if (value !== undefined && value !== null && value !== "") params.set(key, String(value));
  const search = params.toString();
  return `${apiBase()}/api${path}${search ? `?${search}` : ""}`;
};

async function request<T>(method: string, path: string, options: { query?: Query; body?: unknown; signal?: AbortSignal; binary?: boolean } = {}): Promise<T> {
  const token = useSession.getState().token;
  const response = await fetch(urlOf(path, options.query), {
    method,
    signal: options.signal,
    headers: {
      Accept: "application/json",
      "Accept-Language": useLanguage.getState().language,
      ...(options.body === undefined ? {} : { "Content-Type": "application/json" }),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (response.status === 401 && token) useSession.getState().signOut();
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as { detail?: string; title?: string; errors?: string[] };
    throw new ApiError(response.status, problem.detail ?? problem.title ?? response.statusText, problem.errors ?? []);
  }
  if (options.binary) return (await response.arrayBuffer()) as T;
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T);
}

export const api = {
  get: <T>(path: string, query?: Query, signal?: AbortSignal) => request<T>("GET", path, { query, signal }),
  post: <T>(path: string, body: unknown) => request<T>("POST", path, { body }),
  put: <T>(path: string, body: unknown) => request<T>("PUT", path, { body }),
  delete: (path: string) => request<void>("DELETE", path),
  /** ملفٌّ ثنائي */
  file: (method: "GET" | "POST", path: string, body?: unknown) => request<ArrayBuffer>(method, path, { body, binary: true }),
};

/** رسالة الخطأ للمستخدم: رسالة الخادم وإلا نصّ البديل («خطأ») */
export const messageOf = (error: unknown, fallback = "Str.Error") => (error instanceof ApiError ? error.message : text(fallback));
