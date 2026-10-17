import { createJSONStorage } from "zustand/middleware";

/** ما يختلف بين العملاء — للأندرويد نسخته في frontend-android/src/lib/platform.ts */

/** تخزينٌ دائم */
export const lasting = createJSONStorage(() => localStorage);

/** تخزينٌ بعمر الجلسة */
export const sessionOnly = createJSONStorage(() => sessionStorage);

/** أصل الخادم: الويب يمرّ بوكيل Vite */
export const apiBase = () => "";

/** تنزيل ملف */
export const download = (blob: Blob, name: string) => {
  const link = Object.assign(document.createElement("a"), { href: URL.createObjectURL(blob), download: name });
  link.click();
  setTimeout(() => URL.revokeObjectURL(link.href), 1000);
};

/** طباعة صفحة HTML: حوار طباعة المتصفح (ومنه PDF) */
export function printHtml(html: string, title: string) {
  const frame = Object.assign(document.createElement("iframe"), { title });
  frame.style.cssText = "position:fixed;inset-inline-end:0;bottom:0;width:0;height:0;border:0";
  document.body.appendChild(frame);
  const doc = frame.contentDocument!;
  doc.open();
  doc.write(html);
  doc.close();
  const done = () => setTimeout(() => frame.remove(), 1000);
  frame.contentWindow!.addEventListener("afterprint", done);
  setTimeout(() => {
    frame.contentWindow!.focus();
    frame.contentWindow!.print();
  }, 250);
}

/** حفظ ملفٍ مُصدَّر: تنزيلٌ في المتصفح */
export const saveFile = async (data: Uint8Array | string, name: string, type: string) => download(new Blob([data as BlobPart], { type }), name);
