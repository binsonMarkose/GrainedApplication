import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import type { EventListItem } from '../types'
import {
  Button,
  type Column,
  ConfirmDialog,
  DataTable,
  EmptyState,
  ErrorBanner,
  Loading,
  PageHeader,
  Pill,
} from '../components/ui'

// Events store a UTC wall-clock time; format in UTC so it reads back exactly as entered.
function formatWhen(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    timeZone: 'UTC',
  })
}

export function Events() {
  const navigate = useNavigate()
  const toast = useToast()
  const [items, setItems] = useState<EventListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<EventListItem | null>(null)
  const [deletingBusy, setDeletingBusy] = useState(false)

  const load = () =>
    api<EventListItem[]>('/events?includeInactive=true').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  async function toggleActive(ev: EventListItem) {
    try {
      await api(`/events/${ev.id}/active`, { method: 'POST', body: JSON.stringify({ isActive: !ev.isActive }) })
      await load()
      toast.success(ev.isActive ? 'Event disabled' : 'Event enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeletingBusy(true)
    try {
      await api(`/events/${deleting.id}`, { method: 'DELETE' })
      setDeleting(null)
      await load()
      toast.success('Event deleted')
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setDeletingBusy(false)
    }
  }

  const columns: Column<EventListItem>[] = [
    {
      header: 'Event',
      primary: true,
      cell: (e) => (
        <>
          <div className="font-medium text-ink">{e.title}</div>
          <div className="text-xs text-ink/50">
            {formatWhen(e.startDate)}
            {e.location ? ` · ${e.location}` : ''}
          </div>
        </>
      ),
    },
    { header: 'Ticket types', cell: (e) => e.ticketTypeCount },
    { header: 'T-shirt', cell: (e) => (e.enableTshirt ? <Pill tone="gold">On</Pill> : <span className="text-ink/40">—</span>) },
    {
      header: 'Status',
      cell: (e) =>
        !e.isActive ? (
          <Pill tone="gray">Disabled</Pill>
        ) : e.isPublished ? (
          <Pill tone="green">Published</Pill>
        ) : (
          <Pill tone="gray">Draft</Pill>
        ),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Events"
        subtitle="Create events with ticket types for your church to sell."
        action={<Button onClick={() => navigate('/events/new')}>+ New event</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="🎟️" title="No events yet" hint="Create your first event to start selling tickets." />
      )}

      {items && items.length > 0 && (
        <DataTable
          rows={items}
          rowKey={(e) => e.id}
          dim={(e) => !e.isActive}
          columns={columns}
          onRowClick={(e) => navigate(`/events/${e.id}`)}
          actions={(e) => (
            <>
              <Button variant="outline" onClick={() => navigate(`/events/${e.id}`)}>
                Edit
              </Button>
              <Button variant="ghost" onClick={() => toggleActive(e)}>
                {e.isActive ? 'Disable' : 'Enable'}
              </Button>
              <Button variant="danger" onClick={() => setDeleting(e)}>
                Delete
              </Button>
            </>
          )}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title="Delete event?"
        busy={deletingBusy}
        message={
          <>
            Permanently delete <b>{deleting?.title}</b> and its ticket types? An event with registrations can’t be
            deleted — disable it instead. This can’t be undone.
          </>
        }
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />
    </div>
  )
}
