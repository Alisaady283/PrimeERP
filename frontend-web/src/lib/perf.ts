/** قياس الظهور */
const enabled = import.meta.env.DEV;

/** علامةٌ زمنية */
export const mark = (name: string) => {
  if (enabled) performance.mark(name);
};

/** يطبع المدد من البداية */
export const report = (scope: string, start: string, marks: string[]) => {
  if (!enabled) return;
  const rows = marks
    .filter((m) => performance.getEntriesByName(m, "mark").length)
    .map((m) => ({ [scope]: m.replace(`${scope}:`, ""), ms: Math.round(performance.measure(`${m}~`, start, m).duration) }));
  console.table(rows);
  performance.clearMarks();
  performance.clearMeasures();
};
