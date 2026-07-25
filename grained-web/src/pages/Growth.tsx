import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { toDateInput } from '../lib/format'
import { useToast } from '../components/Toast'
import { Button, Card, ErrorBanner, Field, Input, Loading, Modal, PageHeader, Pill } from '../components/ui'
import type { GrowthSeason } from '../types'

const STAGES = ['🌰 Seed', '🌱 Roots', '🌿 Sprout', '🪴 Sapling', '🌳 Tree', '🍎 Fruit', '🌾 Harvest']
const EMPTY_ID = '00000000-0000-0000-0000-000000000000'

function fmtDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

interface Draft {
  id?: string // undefined = create, else edit
  name: string
  start: string // yyyy-mm-dd
  end: string
}

export function Growth() {
  const toast = useToast()
  const [seasons, setSeasons] = useState<GrowthSeason[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [draft, setDraft] = useState<Draft | null>(null)
  const [formError, setFormError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const load = () => api<GrowthSeason[]>('/growth/seasons').then(setSeasons).catch((e) => setError(e.message))
  useEffect(() => {
    load()
  }, [])

  const current = seasons?.find((s) => s.isCurrent)
  const past = seasons?.filter((s) => !s.isCurrent) ?? []
  const isPlaceholder = current?.id === EMPTY_ID // no real season set up yet

  function openCreate() {
    const now = new Date()
    const yearEnd = new Date(now)
    yearEnd.setFullYear(yearEnd.getFullYear() + 1)
    setFormError(null)
    setDraft({ name: '', start: toDateInput(now), end: toDateInput(yearEnd) })
  }

  function openEdit(s: GrowthSeason) {
    setFormError(null)
    setDraft({ id: s.id, name: s.name, start: toDateInput(new Date(s.startsOnUtc)), end: toDateInput(new Date(s.endsOnUtc)) })
  }

  async function save() {
    if (!draft) return
    if (!draft.start || !draft.end) {
      setFormError('Please set both a start and an end date.')
      return
    }
    if (draft.end <= draft.start) {
      setFormError('The end date must be after the start date.')
      return
    }
    setSaving(true)
    setFormError(null)
    const body = JSON.stringify({ name: draft.name.trim() || null, startsOnUtc: draft.start, endsOnUtc: draft.end })
    try {
      if (draft.id) {
        await api(`/growth/seasons/${draft.id}`, { method: 'PUT', body })
        toast.success('Season updated')
      } else {
        await api('/growth/seasons', { method: 'POST', body })
        toast.success('Season saved')
      }
      setDraft(null)
      await load()
    } catch (e) {
      const msg = (e as Error).message
      setFormError(msg)
      toast.error(msg)
    } finally {
      setSaving(false)
    }
  }

  // Rough weeks preview for the modal (backend is the source of truth).
  const draftWeeks =
    draft && draft.start && draft.end && draft.end > draft.start
      ? Math.max(1, Math.round((new Date(draft.end).getTime() - new Date(draft.start).getTime()) / (7 * 864e5)))
      : 0

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader
        title="Growth seasons"
        subtitle="Each child grows a tree through the seven stages over your ministry year."
        action={<Button onClick={openCreate}>+ New season</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!seasons && !error && <Loading />}

      {seasons && current && (
        <>
          <Card className="mb-6 p-5">
            <div className="text-xs font-semibold uppercase tracking-wide text-ink/45">Current season</div>
            <div className="mt-1 flex flex-wrap items-center gap-3">
              <span className="font-display text-2xl text-heading">{current.name}</span>
              {isPlaceholder ? <Pill tone="gold">Not set up</Pill> : <Pill tone="green">In progress</Pill>}
              <span className="text-sm text-ink/60">
                {fmtDate(current.startsOnUtc)} – {fmtDate(current.endsOnUtc)} · {current.weeks} weeks
              </span>
              <Button variant="outline" className="ml-auto" onClick={() => (isPlaceholder ? openCreate() : openEdit(current))}>
                {isPlaceholder ? 'Set ministry year' : 'Edit dates'}
              </Button>
            </div>

            {isPlaceholder && (
              <p className="mt-3 rounded-xl border border-gold-soft/50 bg-gold-soft/10 px-4 py-2.5 text-sm text-gold-ink">
                Currently using a default 52-week calendar year. Set your church&rsquo;s actual ministry year so the
                growth path lands on Harvest at the right time.
              </p>
            )}

            <p className="mt-3 text-sm text-ink/60">
              A faithful Sunday is worth <span className="font-medium">12</span> points — came (4), learned the
              lesson (4) and learned the memory verse (4). Over your <span className="font-medium">{current.weeks}-week</span>{' '}
              season, a faithful child reaches <span className="font-medium">🌾 Harvest at {current.harvestPoints}</span>{' '}
              points. Badges (12) and achievements (36) add more.
            </p>
            <div className="mt-3 flex flex-wrap gap-2">
              {STAGES.map((s, i) => (
                <span key={s} className="rounded-full bg-cream-deep px-3 py-1 text-sm">
                  {i + 1}. {s}
                </span>
              ))}
            </div>
          </Card>

          {past.length > 0 && (
            <Card className="p-5">
              <div className="text-xs font-semibold uppercase tracking-wide text-ink/45">Past seasons</div>
              <ul className="mt-2 divide-y divide-cream-deep">
                {past
                  .slice()
                  .reverse()
                  .map((s) => (
                    <li key={s.id} className="flex items-center justify-between gap-3 py-2.5">
                      <span className="font-medium text-ink">{s.name}</span>
                      <span className="text-sm text-ink/50">
                        {fmtDate(s.startsOnUtc)} – {fmtDate(s.endsOnUtc)}
                      </span>
                      <button
                        onClick={() => openEdit(s)}
                        className="text-sm font-semibold text-accent transition hover:text-heading"
                      >
                        Edit
                      </button>
                    </li>
                  ))}
              </ul>
            </Card>
          )}
        </>
      )}

      <Modal open={draft !== null} onClose={() => setDraft(null)} title={draft?.id ? 'Edit season' : 'New ministry season'}>
        {draft && (
          <div className="space-y-4">
            {formError && <ErrorBanner message={formError} />}
            {!draft.id && (
              <p className="text-sm text-ink/70">
                Set your ministry-year window. When this becomes the current season, each child&rsquo;s running tree
                joins their <span className="font-medium">forest</span> of past years and everyone begins a fresh tree.
                Badges and achievements are kept.
              </p>
            )}
            <Field label="Season name" hint="e.g. 2027 or 2026–27 · leave blank to use the start year">
              <Input value={draft.name} onChange={(e) => setDraft({ ...draft, name: e.target.value })} placeholder="Auto-named" />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Ministry year starts">
                <Input type="date" value={draft.start} onChange={(e) => setDraft({ ...draft, start: e.target.value })} />
              </Field>
              <Field label="Ministry year ends">
                <Input type="date" value={draft.end} onChange={(e) => setDraft({ ...draft, end: e.target.value })} />
              </Field>
            </div>
            {draftWeeks > 0 && (
              <p className="text-sm text-ink/55">
                That&rsquo;s about <span className="font-medium">{draftWeeks} weeks</span> — a faithful child reaches
                🌾 Harvest at <span className="font-medium">{draftWeeks * 12}</span> points.
              </p>
            )}
            <div className="flex justify-end gap-2 pt-1">
              <Button variant="ghost" onClick={() => setDraft(null)}>
                Cancel
              </Button>
              <Button onClick={save} disabled={saving}>
                {saving ? 'Saving…' : draft.id ? 'Save changes' : 'Save season'}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
