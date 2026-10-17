import { useVirtualizer } from "@tanstack/react-virtual";
import { LoaderCircle, Package } from "lucide-react";
import { useEffect, useRef, useState, type ReactNode } from "react";
import { EmptyState } from "@/components/ui/EmptyState";
import { cn } from "@/lib/cn";

export type GalleryTile = { id: string; image?: string; title: string; hint?: string; value?: ReactNode };

const TILE = 176;
const ROW = 236;

/** مربّعاتٌ بتمريرٍ لا ينتهي */
export function Gallery<T>({ data, tile, onOpen, loading, emptyMessage, hasMore, onReachEnd }: {
  data: T[];
  tile: (row: T) => GalleryTile;
  onOpen?: (row: T) => void;
  loading?: boolean;
  emptyMessage: string;
  hasMore?: boolean;
  onReachEnd?: () => void;
}) {
  const scrollRef = useRef<HTMLDivElement>(null);
  const [columns, setColumns] = useState(4);

  useEffect(() => {
    const element = scrollRef.current;
    if (!element) return;
    const observer = new ResizeObserver(([entry]) => entry && setColumns(Math.max(2, Math.floor((entry.contentRect.width - 24) / TILE))));
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  const rowCount = Math.ceil(data.length / columns);
  const virtualizer = useVirtualizer({ count: rowCount, getScrollElement: () => scrollRef.current, estimateSize: () => ROW, overscan: 4 });
  const items = virtualizer.getVirtualItems();
  const last = items[items.length - 1];

  useEffect(() => {
    if (last && hasMore && !loading && last.index >= rowCount - 3) onReachEnd?.();
  }, [last, hasMore, loading, rowCount, onReachEnd]);

  return (
    <div ref={scrollRef} className="scroll-thin relative h-full overflow-y-auto overscroll-contain p-3">
      {data.length === 0 && !loading ? (
        <EmptyState message={emptyMessage} />
      ) : (
        <div className="relative" style={{ height: virtualizer.getTotalSize() }}>
          {items.map((item) => (
            <div key={item.key} className="absolute inset-x-0 grid gap-3" style={{ gridTemplateColumns: `repeat(${columns}, minmax(0, 1fr))`, height: ROW - 12, transform: `translateY(${item.start}px)` }}>
              {data.slice(item.index * columns, item.index * columns + columns).map((row) => {
                const view = tile(row);
                return (
                  <button
                    key={view.id}
                    type="button"
                    onClick={onOpen ? () => onOpen(row) : undefined}
                    className={cn("group flex flex-col overflow-hidden rounded-xl border border-line bg-card text-start transition-all", onOpen && "hover:-translate-y-0.5 hover:border-brand hover:shadow-pop")}
                  >
                    <span className="grid min-h-0 flex-1 place-items-center overflow-hidden bg-paper">
                      {view.image ? <img src={view.image} alt="" loading="lazy" className="size-full object-cover transition-transform duration-300 group-hover:scale-105" /> : <Package className="size-8 text-ink-faint transition-transform duration-300 group-hover:scale-110" />}
                    </span>
                    <span className="space-y-0.5 px-3 py-2">
                      <span className="block truncate text-sm font-medium text-ink">{view.title}</span>
                      <span className="flex items-center justify-between gap-2 text-xs">
                        <span className="truncate font-mono text-ink-faint">{view.hint}</span>
                        <span className="font-semibold tabular-nums text-brand">{view.value}</span>
                      </span>
                    </span>
                  </button>
                );
              })}
            </div>
          ))}
        </div>
      )}
      {loading && (
        <div className="flex justify-center py-6 text-ink-faint">
          <LoaderCircle className="size-5 animate-spin" />
        </div>
      )}
    </div>
  );
}
