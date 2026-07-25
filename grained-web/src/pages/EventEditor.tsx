import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import { useAuth } from '../auth/AuthContext'
import type { EventDetail, EventForm, EventTicketType } from '../types'
import { Button, Card, ErrorBanner, Field, Input, Loading, Pill, Textarea } from '../components/ui'

// A new event starts with the common attendee categories pre-filled — all editable / removable.
const DEFAULT_TICKETS: EventTicketType[] = [
  { name: 'Adult', price: 0 },
  { name: 'Student', price: 0 },
  { name: 'Child', price: 0 },
  { name: 'Senior citizen', price: 0 },
]

const emptyForm: EventForm = {
  title: '',
  startDate: '',
  endDate: '',
  location: '',
  description: '',
  enableTshirt: false,
  ticketTypes: DEFAULT_TICKETS.map((t) => ({ ...t })),
}

// Events store a UTC wall-clock time; datetime-local wants "YYYY-MM-DDTHH:mm", so slice the ISO.
const toLocalInput = (iso: string) => iso.slice(0, 16)

export function EventEditor() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const toast = useToast()
  const isNew = !id || id === 'new'
  const canEdit = user?.roles.includes('ChurchAdmin') ?? false

  const [detail, setDetail] = useState<EventDetail | null>(null)
  const [form, setForm] = useState<EventForm>({ ...emptyForm })
  const [loading, setLoading] = useState(!isNew)
  const [error, setError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  function populate(d: EventDetail) {
    setDetail(d)
    setForm({
      title: d.title,
      startDate: toLocalInput(d.startDate),
      endDate: toLocalInput(d.endDate),
      location: d.location ?? '',
      description: d.description ?? '',
      enableTshirt: d.enableTshirt,
      ticketTypes: d.ticketTypes.map((t) => ({ id: t.id, name: t.name, price: t.price })),
    })
  }

  async function loadDetail() {
    const d = await api<EventDetail>('/events/' + id)
    populate(d)
  }

  useEffect(() => {
    if (isNew) return
    let active = true
    setLoading(true)
    setError(null)
    api<EventDetail>('/events/' + id)
      .then((d) => active && populate(d))
      .catch((e) => active && setError((e as Error).message))
      .finally(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [id, isNew])

  // ---- Ticket-type rows ----
  function setRow(i: number, patch: Partial<EventTicketType>) {
    setForm((f) => ({ ...f, ticketTypes: f.ticketTypes.map((r, idx) => (idx === i ? { ...r, ...patch } : r)) }))
  }
  function addRow() {
    setForm((f) => ({ ...f, ticketTypes: [...f.ticketTypes, { name: '', price: 0 }] }))
  }
  function removeRow(i: number) {
    setForm((f) => ({ ...f, ticketTypes: f.ticketTypes.filter((_, idx) => idx !== i) }))
  }

  async function saveEvent() {
    setSaving(true)
    setSaveError(null)
    try {
      const body = JSON.stringify(form)
      if (isNew) {
        const created = await api<{ id: string }>('/events', { method: 'POST', body })
        toast.success('Event created')
        navigate('/events/' + created.id)
      } else {
        await api('/events/' + id, { method: 'PUT', body })
        await loadDetail()
        toast.success('Event saved')
      }
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function togglePublish() {
    if (!detail) return
    setError(null)
    const publishing = !detail.isPublished
    try {
      await api(`/events/${id}/${publishing ? 'publish' : 'unpublish'}`, { method: 'POST' })
      await loadDetail()
      toast.success(publishing ? 'Event published' : 'Event unpublished')
    } catch (e) {
      setError((e as Error).message)
      toast.error((e as Error).message)
    }
  }

  if (loading) return <Loading />

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {/* Top bar */}
      <div className="flex items-end justify-between gap-4">
        <div>
          <Link to="/events" className="text-sm font-medium text-accent hover:text-heading">
            ← Events
          </Link>
          <h1 className="mt-1 font-display text-3xl font-medium text-heading">
            {isNew ? 'New event' : detail?.title || 'Event'}
          </h1>
          {!isNew && detail && (
            <div className="mt-2">
              {detail.isPublished ? <Pill tone="green">Published</Pill> : <Pill tone="gray">Draft</Pill>}
            </div>
          )}
        </div>
        {!isNew && canEdit && detail && (
          <Button variant={detail.isPublished ? 'outline' : 'primary'} onClick={togglePublish}>
            {detail.isPublished ? 'Unpublish' : 'Publish'}
          </Button>
        )}
      </div>

      {error && <ErrorBanner message={error} />}

      <Card className="p-6">
        <form
          onSubmit={(e) => {
            e.preventDefault()
            saveEvent()
          }}
          className="space-y-5"
        >
          {saveError && <ErrorBanner message={saveError} />}

          <Field label="Title">
            <Input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              required
              disabled={!canEdit}
              placeholder="Summer Family Picnic"
            />
          </Field>

          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="Start date & time">
              <Input
                type="datetime-local"
                value={form.startDate}
                onChange={(e) => setForm({ ...form, startDate: e.target.value })}
                required
                disabled={!canEdit}
              />
            </Field>
            <Field label="End date & time">
              <Input
                type="datetime-local"
                value={form.endDate}
                onChange={(e) => setForm({ ...form, endDate: e.target.value })}
                required
                disabled={!canEdit}
              />
            </Field>
          </div>

          <Field label="Location">
            <Input
              value={form.location ?? ''}
              onChange={(e) => setForm({ ...form, location: e.target.value })}
              disabled={!canEdit}
              placeholder="Grace Community Church, Main Hall"
            />
          </Field>

          <Field label="Description" hint="Shown on the event page.">
            <Textarea
              rows={4}
              value={form.description ?? ''}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              disabled={!canEdit}
              placeholder="Tell families what to expect…"
            />
          </Field>

          {/* Ticket types */}
          <div className="rounded-xl border border-cream-deep bg-cream/40 p-4">
            <div className="mb-1 flex items-center justify-between">
              <h3 className="font-display text-lg text-heading">Ticket types</h3>
            </div>
            <p className="mb-3 text-xs text-ink/50">Set a price for each attendee category. Price 0 = free.</p>

            <div className="space-y-2">
              {form.ticketTypes.map((t, i) => (
                <div key={i} className="flex items-center gap-2">
                  <Input
                    className="flex-1"
                    value={t.name}
                    onChange={(e) => setRow(i, { name: e.target.value })}
                    placeholder="Ticket name"
                    disabled={!canEdit}
                    required
                  />
                  <div className="relative w-32">
                    <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink/40">£</span>
                    <Input
                      className="pl-7"
                      type="number"
                      min={0}
                      step="0.01"
                      value={t.price}
                      onChange={(e) => setRow(i, { price: Number(e.target.value) })}
                      disabled={!canEdit}
                      required
                    />
                  </div>
                  {canEdit && (
                    <button
                      type="button"
                      onClick={() => removeRow(i)}
                      disabled={form.ticketTypes.length === 1}
                      className="grid size-9 shrink-0 place-items-center rounded-lg border border-cream-deep text-ink/50 transition hover:bg-white hover:text-red-600 disabled:opacity-40"
                      aria-label="Remove ticket type"
                    >
                      ✕
                    </button>
                  )}
                </div>
              ))}
            </div>

            {canEdit && (
              <Button type="button" variant="ghost" onClick={addRow} className="mt-2">
                + Add ticket type
              </Button>
            )}
          </div>

          {/* T-shirt toggle */}
          <div className="flex items-center justify-between rounded-xl border border-cream-deep bg-cream/40 p-4">
            <div>
              <div className="font-medium text-ink">Add a T-shirt</div>
              <div className="text-xs text-ink/50">Offer a T-shirt add-on when people register.</div>
            </div>
            <button
              type="button"
              role="switch"
              aria-checked={form.enableTshirt}
              aria-label="Add a T-shirt"
              onClick={() => canEdit && setForm({ ...form, enableTshirt: !form.enableTshirt })}
              disabled={!canEdit}
              className={[
                'relative inline-flex h-6 w-11 shrink-0 items-center rounded-full transition disabled:opacity-50',
                form.enableTshirt ? 'bg-grove' : 'bg-cream-deep',
              ].join(' ')}
            >
              <span
                className={[
                  'inline-block size-5 transform rounded-full bg-white shadow transition',
                  form.enableTshirt ? 'translate-x-5' : 'translate-x-0.5',
                ].join(' ')}
              />
            </button>
          </div>

          {canEdit && (
            <div className="flex justify-end pt-1">
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving…' : isNew ? 'Create event' : 'Save changes'}
              </Button>
            </div>
          )}
        </form>
      </Card>

      {/* Shareable public registration link — only meaningful once published */}
      {!isNew && detail && (
        <Card className="p-6">
          <h2 className="font-display text-lg text-heading">Public registration link</h2>
          {detail.isPublished ? (
            <>
              <p className="mt-1 text-sm text-ink/55">Share this link so anyone can register — no login needed.</p>
              <div className="mt-3 flex items-center gap-2">
                <Input
                  readOnly
                  value={`${window.location.origin}/p/events/${detail.id}`}
                  onFocus={(e) => e.currentTarget.select()}
                />
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => navigator.clipboard?.writeText(`${window.location.origin}/p/events/${detail.id}`)}
                >
                  Copy
                </Button>
              </div>
            </>
          ) : (
            <p className="mt-1 text-sm text-ink/55">Publish this event to get a public registration link.</p>
          )}
        </Card>
      )}
    </div>
  )
}
