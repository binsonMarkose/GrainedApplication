import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { formatDate } from '../lib/format'
import { useToast } from './Toast'
import { Pill } from './ui'
import type { LessonListItem } from '../types'

// Drag-to-reorder (with up/down arrow fallback for touch/keyboard) the teaching order of a class
// group's lessons. Already-taught lessons drop to a static "Taught" section at the bottom so the
// upcoming curriculum stays on top. Order persists per group via PUT /lessons/order.
export function LessonReorderList({
  lessons,
  classGroupId,
  onReordered,
  onOpen,
}: {
  lessons: LessonListItem[]
  classGroupId: string
  onReordered: () => void
  onOpen?: (id: string) => void
}) {
  const toast = useToast()
  // Taught lessons are pinned at the bottom (not draggable); only the upcoming ones are reordered.
  const taught = lessons.filter((l) => l.lastCompletedAtUtc)
  const [upcoming, setUpcoming] = useState<LessonListItem[]>(lessons.filter((l) => !l.lastCompletedAtUtc))
  const [dragIndex, setDragIndex] = useState<number | null>(null)
  const [saving, setSaving] = useState(false)

  useEffect(() => setUpcoming(lessons.filter((l) => !l.lastCompletedAtUtc)), [lessons])

  async function persist(nextUpcoming: LessonListItem[]) {
    const previous = upcoming
    setUpcoming(nextUpcoming)
    setSaving(true)
    try {
      await api('/lessons/order', {
        method: 'PUT',
        // Upcoming first (their new order), taught kept at the end.
        body: JSON.stringify({ classGroupId, lessonIds: [...nextUpcoming, ...taught].map((l) => l.id) }),
      })
      onReordered()
    } catch (e) {
      toast.error((e as Error).message)
      setUpcoming(previous)
    } finally {
      setSaving(false)
    }
  }

  function move(from: number, to: number) {
    if (to < 0 || to >= upcoming.length || from === to) return
    const next = [...upcoming]
    const [item] = next.splice(from, 1)
    next.splice(to, 0, item)
    persist(next)
  }

  return (
    <div className="space-y-2">
      {upcoming.map((l, i) => (
        <div
          key={l.id}
          draggable
          onDragStart={() => setDragIndex(i)}
          onDragOver={(e) => e.preventDefault()}
          onDrop={() => {
            if (dragIndex !== null) move(dragIndex, i)
            setDragIndex(null)
          }}
          onDragEnd={() => setDragIndex(null)}
          className={`flex items-center gap-3 rounded-xl border bg-white p-3 shadow-sm transition ${
            dragIndex === i ? 'border-gold opacity-50' : 'border-cream-deep'
          }`}
        >
          <span className="cursor-grab select-none text-lg leading-none text-ink/30" title="Drag to reorder" aria-hidden>
            ⠿
          </span>
          <span className="grid size-7 shrink-0 place-items-center rounded-full bg-grove text-xs font-bold text-oncream">
            {i + 1}
          </span>
          <button type="button" onClick={() => onOpen?.(l.id)} className="min-w-0 flex-1 text-left" title="Open lesson">
            <div className="truncate font-medium text-ink hover:text-accent">{l.title}</div>
            <div className="truncate text-xs text-ink/50">{l.bibleReference}</div>
          </button>
          {l.status !== 2 && <Pill tone="gray">{l.status === 1 ? 'In review' : 'Draft'}</Pill>}
          <div className="flex flex-col gap-0.5">
            <button
              type="button"
              onClick={() => move(i, i - 1)}
              disabled={i === 0 || saving}
              aria-label="Move up"
              className="grid size-6 place-items-center rounded-md border border-cream-deep text-ink/60 transition enabled:hover:bg-cream disabled:opacity-30"
            >
              ▲
            </button>
            <button
              type="button"
              onClick={() => move(i, i + 1)}
              disabled={i === upcoming.length - 1 || saving}
              aria-label="Move down"
              className="grid size-6 place-items-center rounded-md border border-cream-deep text-ink/60 transition enabled:hover:bg-cream disabled:opacity-30"
            >
              ▼
            </button>
          </div>
        </div>
      ))}

      {upcoming.length === 0 && taught.length > 0 && (
        <p className="rounded-xl border border-dashed border-cream-deep px-4 py-4 text-center text-sm text-ink/50">
          🎉 Every lesson in this group has been taught.
        </p>
      )}

      {taught.length > 0 && (
        <div className="pt-2">
          <div className="mb-1 flex items-center gap-2 text-xs font-semibold uppercase tracking-wide text-ink/45">
            <span aria-hidden>✅</span> Already taught
          </div>
          <div className="space-y-2">
            {taught.map((l) => (
              <div
                key={l.id}
                className="flex items-center gap-3 rounded-xl border border-cream-deep bg-cream/40 p-3 opacity-70"
              >
                <span className="text-lg leading-none text-leaf" aria-hidden>
                  ✅
                </span>
                <button type="button" onClick={() => onOpen?.(l.id)} className="min-w-0 flex-1 text-left" title="Open lesson">
                  <div className="truncate font-medium text-ink hover:text-accent">{l.title}</div>
                  <div className="truncate text-xs text-ink/50">{l.bibleReference}</div>
                </button>
                {l.lastCompletedAtUtc && <Pill tone="green">Taught {formatDate(l.lastCompletedAtUtc)}</Pill>}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
