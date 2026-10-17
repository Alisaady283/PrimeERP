/** تصوير الباركود — قواعده مشتركة بين كاميرا الويب والأندرويد */

/** أنواع الباركود المقروءة: باركودات المتاجر والأكواد الشائعة */
export const BARCODE_TYPES = ["ean13", "ean8", "upc_a", "upc_e", "code128", "code39", "code93", "itf14", "qr"] as const;

/** المدّة التي يُتجاهل فيها الكود نفسه بعد قراءته — الكاميرا تقرؤه مراراً في اللقطة الواحدة */
export const SCAN_REPEAT_MS = 1500;

/** بوّابة القراءة: تقبل الكود الجديد، وتردّ تكراره القريب */
export function scanGate(windowMs = SCAN_REPEAT_MS) {
  let last = "";
  let at = 0;
  return (code: string) => {
    const now = Date.now();
    if (code === last && now - at < windowMs) return false;
    last = code;
    at = now;
    return true;
  };
}
