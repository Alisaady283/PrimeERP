/** ترتيبٌ ثابت مُتحقَّق: blue-500 · amber-600 · violet-500 · emerald-600 · rose-500 · cyan-600 */
export const palette = [1, 2, 3, 4, 5, 6].map((n) => `var(--color-chart-${n})`);

/** درجتا التدرّج لكل لون */
export const shades = [1, 2, 3, 4, 5, 6].map((n) => ({ light: `var(--color-chart-${n}-light)`, dark: `var(--color-chart-${n}-dark)` }));

/** درجات Tailwind orange 300→700 للشدّة */
export const severity = ["#ffb86a", "#ff8904", "#ff6900", "#f54900", "#ca3500"];
