import { useEffect, useState } from 'react'
import { Button, Pill } from './ui'
import { fetchInbox, markAnnouncementRead } from '../lib/announcements'
import type { InboxAnnouncement } from '../types'

function audienceTone(label: string): 'green' | 'gold' | 'gray' {
  if (label === 'Teachers') return 'green'
  if (label === 'Parents') return 'gold'
  return 'gray'
}

// Shown once per session over the app: after login, any unread announcements pop up as a small
// stack the reader steps through. Dismissing marks each read on the server, so it won't reappear.
// Mounted in the app shell so it surfaces no matter which page the user lands on.
export function AnnouncementPopup() {
  const [queue, setQueue] = useState<InboxAnnouncement[]>([])
  const [index, setIndex] = useState(0)
  const [shown, setShown] = useState(false)

  useEffect(() => {
    // Only fetch once per mount (the shell mounts once after login).
    if (shown) return
    setShown(true)
    fetchInbox()
      .then((items) => setQueue(items.filter((i) => !i.isRead)))
      .catch(() => {
        /* stay silent — the Announcements tab is the reliable path */
      })
  }, [shown])

  if (queue.length === 0 || index >= queue.length) return null

  const current = queue[index]
  const isLast = index === queue.length - 1

  async function dismissCurrent() {
    const id = queue[index].id
    try {
      await markAnnouncementRead(id)
    } catch {
      /* if the mark fails it simply pops up again next login */
    }
    if (isLast) setQueue([])
    else setIndex((i) => i + 1)
  }

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
      <div className="w-full max-w-lg overflow-hidden rounded-3xl bg-white shadow-2xl">
        <div className="flex items-center gap-3 bg-gradient-to-br from-grove to-leaf px-6 py-5">
          <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-cream/15 text-2xl" aria-hidden>
            📣
          </span>
          <div className="min-w-0">
            <p className="text-[0.7rem] font-semibold uppercase tracking-[0.2em] text-gold-soft">
              {queue.length > 1 ? `Announcement ${index + 1} of ${queue.length}` : 'New announcement'}
            </p>
            <h2 className="truncate font-display text-2xl text-oncream">{current.title}</h2>
          </div>
        </div>

        <div className="px-6 py-5">
          <div className="mb-3 flex flex-wrap items-center gap-2 text-xs text-ink/50">
            <Pill tone={audienceTone(current.audienceLabel)}>{current.audienceLabel}</Pill>
            <span>
              From {current.createdByName} ·{' '}
              {new Date(current.createdAtUtc).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })}
            </span>
          </div>
          <p className="max-h-72 overflow-y-auto whitespace-pre-wrap text-sm leading-relaxed text-ink/80">{current.body}</p>
        </div>

        <div className="flex justify-end gap-2 border-t border-cream-deep px-6 py-4">
          <Button onClick={dismissCurrent}>{isLast ? 'Got it' : 'Next'}</Button>
        </div>
      </div>
    </div>
  )
}
