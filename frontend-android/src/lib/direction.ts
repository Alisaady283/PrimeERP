import { directionOf, useLanguage } from "@/lib/i18n";

/** اتجاه الواجهة بحسب اللغة — يعمل بلا إعادة تشغيل */
export const useDirection = () => directionOf(useLanguage((s) => s.language));

/** محاذاة البداية للنص */
export const useStart = () => (useDirection() === "rtl" ? "text-right" : "text-left");
