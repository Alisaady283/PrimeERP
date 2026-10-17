import { ImageIcon, Trash2, Upload } from "lucide-react";
import { useRef, useState } from "react";
import { Button } from "@/components/ui/Button";
import { useText } from "@/lib/i18n";
import { encodeImage, imageUrl } from "@/lib/image";

/** اختيار صورةٍ مصغَّرة */
export function ImageInput({ label, value, onChange, size = 256, disabled }: { label?: string; value: unknown; onChange: (value: string) => void; size?: number; disabled?: boolean }) {
  const t = useText();
  const input = useRef<HTMLInputElement>(null);
  const [error, setError] = useState<string | null>(null);
  const url = imageUrl(value);

  const pick = async (file?: File) => {
    if (!file) return;
    try {
      onChange(await encodeImage(file, size));
      setError(null);
    } catch {
      setError(t("Str.Image.ReadFailed", file.name));
    }
  };

  return (
    <div className="flex flex-col gap-1.5">
      {label && <span className="text-xs font-medium text-ink-soft">{label}</span>}
      <div
        onDragOver={(e) => e.preventDefault()}
        onDrop={(e) => {
          e.preventDefault();
          if (!disabled) void pick(e.dataTransfer.files[0]);
        }}
        className="flex items-center gap-4 rounded-xl border border-dashed border-line-strong bg-paper-soft p-3"
      >
        <span className="grid size-20 shrink-0 place-items-center overflow-hidden rounded-lg border border-line bg-card">
          {url ? <img src={url} alt="" className="size-full object-cover" /> : <ImageIcon className="size-6 text-ink-faint" />}
        </span>
        <div className="flex flex-wrap gap-2">
          <Button size="sm" variant="secondary" icon={<Upload className="size-4" />} disabled={disabled} onClick={() => input.current?.click()}>
            {t("Str.Image.Pick")}
          </Button>
          {url && !disabled && (
            <Button size="sm" variant="ghost" className="text-danger hover:bg-danger-soft hover:text-danger" icon={<Trash2 className="size-4" />} onClick={() => onChange("")}>
              {t("Str.Image.Remove")}
            </Button>
          )}
        </div>
        <input
          ref={input}
          type="file"
          accept="image/*"
          hidden
          onChange={(e) => {
            void pick(e.target.files?.[0]);
            e.target.value = "";
          }}
        />
      </div>
      {error && <span className="text-xs text-danger">{error}</span>}
    </div>
  );
}
