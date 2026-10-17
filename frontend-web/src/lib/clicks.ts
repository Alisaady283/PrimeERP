/** مهلة النقر المزدوج: النقرة الواحدة تنتظرها، فإن تلتها ثانيةٌ كانت فتحاً لا تصفية */
export const DOUBLE_CLICK = 250;

/** يفصل النقرة عن المزدوجة على العنصر نفسه — الواحدة تُنفَّذ بعد المهلة ما لم تأتِ الثانية */
export function splitClicks<T>(single: (value: T) => void, double: (value: T) => void) {
  let timer = 0;
  return {
    click: (value: T) => {
      window.clearTimeout(timer);
      timer = window.setTimeout(() => single(value), DOUBLE_CLICK);
    },
    double: (value: T) => {
      window.clearTimeout(timer);
      double(value);
    },
    cancel: () => window.clearTimeout(timer),
  };
}
