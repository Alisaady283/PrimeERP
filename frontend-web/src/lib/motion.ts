/** رموز الحركة — Material 3: material-web/tokens/_md-sys-motion.scss */

/** المدد بالملّي ثانية */
export const duration = { short3: 150, short4: 200, medium2: 300, medium4: 400, long2: 500 } as const;

/** المنحنيات */
export const easing = {
  /** حركةٌ داخل الشاشة */
  emphasized: [0.2, 0, 0, 1],
  /** دخول */
  decelerate: [0.05, 0.7, 0.1, 1],
  /** خروج */
  accelerate: [0.3, 0, 0.8, 0.15],
} as const;

/** ثوانٍ لـmotion */
export const seconds = (ms: number) => ms / 1000;

/** انتقال motion من رمزين */
export const tween = (ms: number, ease: readonly number[] = easing.emphasized, delay = 0) => ({ duration: seconds(ms), ease: ease as [number, number, number, number], delay: seconds(delay) });

/** الفاصل بين علامات الرسم: 50ms (مثال دليل حركة الرسوم: dataIndex × 50) */
export const STAGGER_STEP = 50;
/** أقصى امتدادٍ للتتابع */
export const STAGGER_BUDGET = 600;

/** تأخير العنصر: خطوةٌ ثابتة لا يتجاوز مجموعها الميزانية */
export const staggerDelay = (index: number, count: number) => (count <= 1 ? 0 : index * Math.min(STAGGER_STEP, STAGGER_BUDGET / (count - 1)));

/** منحنى bezier لـECharts */
export const bezier = ([x1, y1, x2, y2]: readonly number[]) => (t: number) => {
  let low = 0;
  let high = 1;
  let u = t;
  for (let i = 0; i < 20; i++) {
    u = (low + high) / 2;
    const x = 3 * (1 - u) ** 2 * u * x1! + 3 * (1 - u) * u ** 2 * x2! + u ** 3;
    if (x < t) low = u;
    else high = u;
  }
  return 3 * (1 - u) ** 2 * u * y1! + 3 * (1 - u) * u ** 2 * y2! + u ** 3;
};

/** مساحةٌ كبيرة (لوحة كاملة): Material extra-long1 */
export const LARGE = 700;
/** الفاصل بين عناصر الظهور: ضمن 70–300ms */
export const ENTRANCE_STEP = 80;

/** دخول عنصرٍ في تتابع الظهور: long2 لكلٍّ، وفاصلٌ ثابت */
export const enterAt = (index: number, from: { x?: number; y?: number; scale?: number } = { y: 16 }, length: number = duration.long2) => ({
  initial: { opacity: 0, ...from },
  animate: { opacity: 1, x: 0, y: 0, scale: 1 },
  transition: tween(length, easing.decelerate, index * ENTRANCE_STEP),
});
