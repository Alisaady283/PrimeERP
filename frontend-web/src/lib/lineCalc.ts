const round = (value: number) => Math.round((value + Number.EPSILON) * 100) / 100;

/** أجزاء السطر التجاري: الإجمالي · الخصم · الضريبة · الخصم من المنبع · الصافي */
export const lineParts = (qty: number, unitPrice: number, discountPercent = 0, vatPercent = 0, withholdingPercent = 0) => {
  const gross = round(qty * unitPrice);
  const discount = round((gross * discountPercent) / 100);
  const taxable = gross - discount;
  const vat = round((taxable * vatPercent) / 100);
  const withholding = round(((taxable + vat) * withholdingPercent) / 100);
  return { gross, discount, vat, withholding, net: taxable + vat - withholding };
};

export const lineNet = (qty: number, unitPrice: number, discountPercent = 0, vatPercent = 0, withholdingPercent = 0) =>
  lineParts(qty, unitPrice, discountPercent, vatPercent, withholdingPercent).net;
