import { useEffect, useState } from 'react'
import { PageHeader, Button, EmptyState, ErrorBanner, Loading, Pill } from '../components/ui'
import { fetchInbox, markAllAnnouncementsRead, markAnnouncementRead, onAnnouncementsChanged } from '../lib/announcements'
import type { InboxAnnouncement } from '../types'

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}

function audienceTone(label: string): 'green' | 'gold' | 'gray' {
  if (label === 'Teachers') return 'green'
  if (label === 'Parents') return 'gold'
  return 'gray'
}

// The Announcements tab for teachers and parents: every message from their church, newest first,
// with unread ones highlighted. Opening the page marks nothing automatically — the reader dismisses
// each (or all) so the login pop-up stops showing it.
export function Inbox() {
  const [items, setItems] = useState<InboxAnnouncement[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const load = () => fetchInbox().then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
    return onAnnouncementsChanged(load)
  }, [])

  const unread = items?.filter((i) => !i.isRead).length ?? 0

  async function markOne(id: string) {
    setItems((prev) => prev?.map((i) => (i.id === id ? { ...i, isRead: true } : i)) ?? prev)
    try {
      await markAnnouncementRead(id)
    } catch (e) {
      setError((e as Error).message)
      load()
    }
  }

  async function markAll() {
    setItems((prev) => prev?.map((i) => ({ ...i, isRead: true })) ?? prev)
    try {
      await markAllAnnouncementsRead()
    } catch (e) {
      setError((e as Error).message)
      load()
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader
        title="Announcements"
        subtitle="Messages from your church"
        action={
          unread > 0 ? (
            <Button variant="outline" onClick={markAll}>
              Mark all read
            </Button>
          ) : undefined
        }
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading label="Loading announcements…" />}

      {items && items.length === 0 && (
        <EmptyState icon="📣" title="No announcements yet" hint="Messages from your church will appear here." />
      )}

      {items && items.length > 0 && (
        <div className="space-y-3">
          {items.map((a) => (
            <article
              key={a.id}
              className={[
                'rounded-2xl border bg-white p-5 shadow-sm transition',
                a.isRead ? 'border-cream-deep' : 'border-l-4 border-l-gold border-cream-deep bg-gold-soft/[0.06]',
              ].join(' ')}
            >
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div className="flex items-center gap-2">
                  {!a.isRead && <span className="size-2.5 rounded-full bg-gold" aria-label="Unread" />}
                  <h2 className="font-display text-xl text-heading">{a.title}</h2>
                </div>
                <Pill tone={audienceTone(a.audienceLabel)}>{a.audienceLabel}</Pill>
              </div>
              <p className="mt-2 whitespace-pre-wrap text-sm leading-relaxed text-ink/80">{a.body}</p>
              <div className="mt-4 flex flex-wrap items-center justify-between gap-3 border-t border-cream-deep pt-3 text-xs text-ink/50">
                <span>
                  {a.createdByName} · {formatDateTime(a.createdAtUtc)}
                </span>
                {!a.isRead && (
                  <button onClick={() => markOne(a.id)} className="font-semibold text-accent transition hover:text-heading">
                    Mark as read
                  </button>
                )}
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}
