import AsyncStorage from "@react-native-async-storage/async-storage";
import { File, Paths } from "expo-file-system";
import * as Print from "expo-print";
import * as Sharing from "expo-sharing";
import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";

/** ما يختلف بين العملاء — نسخة الأندرويد من frontend-web/src/lib/platform.ts */

/** تخزينٌ دائم على الجهاز */
export const lasting = createJSONStorage(() => AsyncStorage);

/** لا جلسة متصفح في الجهاز: تبقى حتى الخروج أو انتهاء الرمز */
export const sessionOnly = lasting;

type ServerState = { url: string; setUrl: (url: string) => void };

/** عنوان الخادم يختاره المستخدم عند الدخول */
export const useServer = create<ServerState>()(
  persist((set) => ({ url: "", setUrl: (url) => set({ url: url.trim().replace(/\/+$/, "") }) }), { name: "primeerp.server", storage: lasting }),
);

/** أصل الخادم */
export const apiBase = () => useServer.getState().url;

/** طباعة صفحة HTML: حوار الطباعة في أندرويد (ومنه حفظ PDF) */
export const printHtml = (html: string, _title: string) => void Print.printAsync({ html });

/** حفظ ملفٍ مُصدَّر: يُكتب في المؤقّت ثم ورقة المشاركة (Drive · البريد · الملفات…) */
export async function saveFile(data: Uint8Array | string, name: string, type: string) {
  const file = new File(Paths.cache, name);
  file.write(data);
  await Sharing.shareAsync(file.uri, { mimeType: type, dialogTitle: name });
}
