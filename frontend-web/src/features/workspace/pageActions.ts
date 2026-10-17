import type { PageAction, RecordAction } from "@/features/workspace/spec";
import { api } from "@/lib/api";

/** أزرار الصفحة المُعلَنة — تنفيذها مشتركٌ بين الويب والأندرويد */

/** فعل السجل على الخادم، ونصّه إن كان يُعرض مرةً (مفتاح الربط) */
export async function runRecordAction(action: RecordAction, id: number): Promise<string> {
  switch (action.run) {
    case "issueApiKey":
      return (await api.post<{ key: string }>(`/api-keys/${id}/issue`, {})).key;
    case "paymentLink":
      return (await api.post<{ url: string }>(`/payments/invoices/${id}/link`, {})).url;
    case "submitEInvoice":
      return (await api.post<{ url: string }>(`/einvoices/invoices/${id}/submit`, {})).url;
  }
}

/** يُنفَّذ على الخادم، أو يُرجع false ليتولّاه العرض (فتح محرّر رصيد أول المدة) */
export async function runPageAction(action: PageAction): Promise<boolean> {
  if (action.run === "openingStock") return false;
  await api.post(`/assets/depreciation/run?upTo=${new Date().toISOString().slice(0, 10)}`, {});
  return true;
}
