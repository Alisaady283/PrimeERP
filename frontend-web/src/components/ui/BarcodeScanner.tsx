import { CameraOff, ScanLine } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/Button";
import { Dialog } from "@/components/ui/Dialog";
import { useText } from "@/lib/i18n";
import { scanGate } from "@/lib/scan";

type Detector = { detect: (source: HTMLVideoElement) => Promise<Array<{ rawValue: string }>> };
type DetectorClass = new (options: { formats: string[] }) => Detector;

/** نغمةٌ قصيرة واهتزازٌ عند القراءة — كأجهزة نقاط البيع */
const confirm = () => {
  navigator.vibrate?.(40);
  try {
    const audio = new AudioContext();
    const tone = audio.createOscillator();
    tone.frequency.value = 1320;
    tone.connect(audio.destination);
    tone.start();
    tone.stop(audio.currentTime + 0.08);
  } catch {
    /* بلا صوت */
  }
};

/**
 * تصوير الباركود بكاميرا الجهاز (الخلفية في الهاتف): قراءةٌ متتابعة، وكل كودٍ جديد يُبلَّغ مرّةً (scanGate).
 * BarcodeDetector الأصلي إن وُجد (أسرع، في Chrome على الهاتف)، وإلا ZXing يُحمَّل عند الفتح فقط.
 */
export function BarcodeScanner({ open, onClose, onScan }: { open: boolean; onClose: () => void; onScan: (code: string) => void }) {
  const t = useText();
  const video = useRef<HTMLVideoElement>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) return;
    setError(null);
    const accept = scanGate();
    const read = (code: string) => {
      if (!accept(code)) return;
      confirm();
      onScan(code);
    };
    let stop = () => {};
    let cancelled = false;

    const start = async () => {
      if (!navigator.mediaDevices?.getUserMedia) return setError(t("Str.Scan.NoCamera"));
      const Native = (window as unknown as { BarcodeDetector?: DetectorClass }).BarcodeDetector;
      try {
        if (Native) {
          const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } });
          if (cancelled || !video.current) return stream.getTracks().forEach((track) => track.stop());
          video.current.srcObject = stream;
          await video.current.play();
          const detector = new Native({ formats: ["ean_13", "ean_8", "upc_a", "upc_e", "code_128", "code_39", "code_93", "itf", "qr_code"] });
          let frame = 0;
          const tick = async () => {
            if (cancelled || !video.current) return;
            const found = await detector.detect(video.current).catch(() => []);
            if (found[0]) read(found[0].rawValue);
            frame = requestAnimationFrame(() => void tick());
          };
          void tick();
          stop = () => {
            cancelAnimationFrame(frame);
            stream.getTracks().forEach((track) => track.stop());
          };
        } else {
          const { BrowserMultiFormatReader } = await import("@zxing/browser");
          if (cancelled || !video.current) return;
          const controls = await new BrowserMultiFormatReader().decodeFromConstraints({ video: { facingMode: "environment" } }, video.current, (result) => {
            if (result) read(result.getText());
          });
          stop = () => controls.stop();
        }
      } catch (e) {
        setError(e instanceof DOMException && e.name === "NotAllowedError" ? t("Str.Scan.Denied") : t("Str.Scan.NoCamera"));
      }
    };

    void start();
    return () => {
      cancelled = true;
      stop();
    };
  }, [open]);

  return (
    <Dialog open={open} title={t("Str.Scan.Camera")} onClose={onClose} footer={<Button variant="secondary" onClick={onClose}>{t("Str.Close")}</Button>}>
      {error ? (
        <div className="flex flex-col items-center gap-3 py-10 text-center text-sm text-ink-tertiary">
          <CameraOff className="size-8 text-ink-faint" />
          {error}
        </div>
      ) : (
        <div className="space-y-3">
          <div className="relative overflow-hidden rounded-2xl bg-black">
            <video ref={video} muted playsInline className="aspect-[4/3] w-full object-cover" />
            {/* إطار التصويب وخطّه المتحرّك */}
            <div className="pointer-events-none absolute inset-x-[12%] inset-y-[22%] rounded-xl border-2 border-white/80 shadow-[0_0_0_9999px_rgba(0,0,0,0.35)]">
              <div className="absolute inset-x-3 top-1/2 h-0.5 animate-pulse rounded-full bg-chart-5" />
            </div>
          </div>
          <p className="flex items-center justify-center gap-2 text-sm text-ink-tertiary">
            <ScanLine className="size-4 text-brand" />
            {t("Str.Scan.Hint")}
          </p>
        </div>
      )}
    </Dialog>
  );
}
