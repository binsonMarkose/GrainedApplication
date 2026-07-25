import { useEffect, useState } from 'react'
import { api, ApiError } from '../lib/api'
import { useToast } from '../components/Toast'
import {
  PageHeader,
  Button,
  DataTable,
  EmptyState,
  ErrorBanner,
  Field,
  Input,
  Loading,
  Modal,
  Pill,
  Select,
  Textarea,
  type Column,
} from '../components/ui'
import type { Announcement, AnnouncementAudience, AnnouncementForm } from '../types'

const AUDIENCE_OPTIONS: { value: AnnouncementAudience; label: string; hint: string }[] = [
  { value: 2, label: 'Everyone', hint: 'All teachers and parents' },
  { value: 0, label: 'Teachers', hint: 'Teaching team only' },
  { value: 1, label: 'Parents', hint: 'Parents / guardians only' },
]

function audienceTone(label: string): 'green' | 'gold' | 'gray' {
  if (label === 'Teachers') return 'green'
  if (label === 'Parents') return 'gold'
  return 'gray'
}
function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

const EMPTY: AnnouncementForm = { title: '', body: '', audience: 2 }

export function Announcements() {
  const toast = useToast()
  const [items, setItems] = useState<Announcement[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [open, setOpen] = useState(false)
  const [form, setForm] = useState<AnnouncementForm>(EMPTY)
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const load = () => api<Announcement[]>('/announcements').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  function openNew() {
    setForm(EMPTY)
    setFormError(null)
    setOpen(true)
  }

  async function send() {
    if (!form.title.trim() || !form.body.trim()) {
      setFormError('A title and a message are both required.')
      return
    }
    setSaving(true)
    setFormError(null)
    try {
      await api('/announcements', { method: 'POST', body: JSON.stringify(form) })
      setOpen(false)
      toast.success('Announcement sent')
      await load()
    } catch (e) {
      const msg = e instanceof ApiError ? e.message : 'Could not send the announcement.'
      setFormError(msg)
      toast.error(msg)
    } finally {
      setSaving(false)
    }
  }

  async function setActive(a: Announcement, isActive: boolean) {
    try {
      await api(`/announcements/${a.id}/active`, { method: 'POST', body: JSON.stringify({ isActive }) })
      toast.success(isActive ? 'Announcement restored' : 'Announcement retracted')
      await load()
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  const columns: Column<Announcement>[] = [
    {
      header: 'Announcement',
      primary: true,
      cell: (a) => (
        <div className={a.isActive ? '' : 'opacity-60'}>
          <div className="font-medium text-heading">{a.title}</div>
          <div className="mt-0.5 line-clamp-2 max-w-md text-xs text-ink/55">{a.body}</div>
        </div>
      ),
    },
    { header: 'Audience', cell: (a) => <Pill tone={audienceTone(a.audienceLabel)}>{a.audienceLabel}</Pill> },
    { header: 'Sent', cell: (a) => <span className="text-ink/70">{formatDate(a.createdAtUtc)}</span> },
    {
      header: 'Seen by',
      className: 'text-right tabular-nums',
      cell: (a) => <span className="text-ink/70">{a.readCount}</span>,
    },
    {
      header: 'Status',
      cell: (a) => (a.isActive ? <Pill tone="green">Live</Pill> : <Pill tone="gray">Retracted</Pill>),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Messages"
        subtitle="Send announcements to your teachers and parents"
        action={<Button onClick={openNew}>✍️ New announcement</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading label="Loading announcements…" />}

      {items && items.length === 0 && (
        <EmptyState
          icon="📣"
          title="No announcements yet"
          hint="Write your first message — it pops up for teachers and parents when they next log in."
        />
      )}

      {items && items.length > 0 && (
        <DataTable
          columns={columns}
          rows={items}
          rowKey={(a) => a.id}
          dim={(a) => !a.isActive}
          actions={(a) =>
            a.isActive ? (
              <Button variant="danger" onClick={() => setActive(a, false)}>
                Retract
              </Button>
            ) : (
              <Button variant="outline" onClick={() => setActive(a, true)}>
                Restore
              </Button>
            )
          }
        />
      )}

      <Modal open={open} onClose={() => setOpen(false)} title="New announcement" wide>
        <div className="space-y-4">
          {formError && <ErrorBanner message={formError} />}
          <Field label="Send to">
            <Select
              value={form.audience}
              onChange={(e) => setForm({ ...form, audience: Number(e.target.value) as AnnouncementAudience })}
            >
              {AUDIENCE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label} — {o.hint}
                </option>
              ))}
            </Select>
          </Field>
          <Field label="Title">
            <Input
              value={form.title}
              maxLength={150}
              placeholder="e.g. No class this Sunday"
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </Field>
          <Field label="Message">
            <Textarea
              value={form.body}
              rows={6}
              maxLength={4000}
              placeholder="Write your message…"
              onChange={(e) => setForm({ ...form, body: e.target.value })}
            />
          </Field>
          <div className="flex justify-end gap-2 pt-1">
            <Button variant="outline" onClick={() => setOpen(false)} disabled={saving}>
              Cancel
            </Button>
            <Button onClick={send} disabled={saving}>
              {saving ? 'Sending…' : 'Send announcement'}
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
