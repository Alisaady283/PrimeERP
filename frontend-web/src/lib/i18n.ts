import ar from "@strings/ar.json";
import en from "@strings/en.json";
import { create } from "zustand";
import { persist } from "zustand/middleware";
import { lasting } from "@/lib/platform";

export type Language = "ar" | "en";

const tables: Record<Language, Record<string, string>> = { ar, en };

type LanguageState = { language: Language; setLanguage: (language: Language) => void };

export const useLanguage = create<LanguageState>()(
  persist((set) => ({ language: "ar", setLanguage: (language) => set({ language }) }), { name: "primeerp.language", storage: lasting }),
);

export const text = (key: string, ...args: Array<string | number>) => {
  const template = tables[useLanguage.getState().language][key] ?? key;
  return args.length === 0 ? template : template.replace(/\{(\d+)(?::[^}]*)?\}/g, (_, index: string) => String(args[Number(index)] ?? ""));
};

export const useText = () => {
  useLanguage((state) => state.language);
  return text;
};

export const directionOf = (language: Language) => (language === "ar" ? "rtl" : "ltr");
