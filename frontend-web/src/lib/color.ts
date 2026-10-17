/** الألوان — حسابٌ خالص مشترك بين الويب والأندرويد */

const channel = (x: number) => Math.round(255 * Math.max(0, Math.min(1, x > 0.0031308 ? 1.055 * x ** (1 / 2.4) - 0.055 : 12.92 * x)));
const hex2 = (n: number) => n.toString(16).padStart(2, "0");

/** oklch(L% C H) ← #rrggbb (معادلات OKLab القياسية) */
const fromOklch = (l: number, c: number, h: number) => {
  const a = c * Math.cos((h * Math.PI) / 180);
  const b = c * Math.sin((h * Math.PI) / 180);
  const l_ = (l + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m_ = (l - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s_ = (l - 0.0894841775 * a - 1.291485548 * b) ** 3;
  const r = 4.0767416621 * l_ - 3.3077115913 * m_ + 0.2309699292 * s_;
  const g = -1.2684380046 * l_ + 2.6097574011 * m_ - 0.3413193965 * s_;
  const bl = -0.0041960863 * l_ - 0.7034186147 * m_ + 1.707614701 * s_;
  return `#${hex2(channel(r))}${hex2(channel(g))}${hex2(channel(bl))}`;
};

/** أي لونٍ من tokens.css ست عشريّاً: hex كما هو، وoklch محسوباً */
export const toHex = (value: string) => {
  const m = /oklch\(\s*([\d.]+)(%?)\s+([\d.]+)\s+([\d.]+)/i.exec(value.trim());
  if (!m) return value.trim();
  const l = Number(m[1]) / (m[2] ? 100 : 1);
  return fromOklch(l, Number(m[3]), Number(m[4]));
};

/** شفافية لونٍ ست عشري */
export const alpha = (hex: string, a: number) => `${toHex(hex)}${Math.round(a * 255).toString(16).padStart(2, "0")}`;
