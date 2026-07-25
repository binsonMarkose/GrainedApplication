import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import { BADGE_ICONS, BadgeIcon } from '../components/BadgeIcon'
import type { Badge, BadgeForm } from '../types'
import {
  Button,
  Checkbox,
  type Column,
  DataTable,
  EmptyState,
  ErrorBanner,
  Field,
  Input,
  Loading,
  Modal,
  PageHeader,
  Pill,
  Select,
  Textarea,
} from '../components/ui'

const empty: BadgeForm = { name: '', description: '', iconName: '', criteria: '', tier: 0, points: 0, repeatable: true }
const DEFAULT_POINTS = { 0: 12, 1: 36 } as const

export function Badges() {
  const toast = useToast()
  const [items, setItems] = useState<Badge[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState<BadgeForm | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)

  const load = () =>
    api<Badge[]>('/badges?includeInactive=true').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  function openNew() {
    setSaveError(null)
    setEditing({ ...empty })
  }
  function openEdit(b: Badge) {
    setSaveError(null)
    setEditing({
      id: b.id,
      name: b.name,
      description: b.description,
      iconName: b.iconName,
      criteria: b.criteria,
      tier: b.tier,
      points: b.points,
      repeatable: b.repeatable,
    })
  }

  async function save() {
    if (!editing) return
    setSaving(true)
    setSaveError(null)
    const isUpdate = !!editing.id
    try {
      const body = JSON.stringify(editing)
      if (editing.id) await api(`/badges/${editing.id}`, { method: 'PUT', body })
      else await api('/badges', { method: 'POST', body })
      setEditing(null)
      await load()
      toast.success(isUpdate ? 'Badge updated' : 'Badge created')
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function toggleActive(b: Badge) {
    try {
      await api(`/badges/${b.id}/active`, { method: 'POST', body: JSON.stringify({ isActive: !b.isActive }) })
      await load()
      toast.success(b.isActive ? 'Badge disabled' : 'Badge enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  const columns: Column<Badge>[] = [
    {
      header: 'Name',
      primary: true,
      cell: (b) => (
        <div className="flex items-center gap-3">
          <BadgeIcon icon={b.iconName} size={36} />
          <div>
            <div className="font-medium text-ink">{b.name}</div>
            <div className="mt-0.5 flex flex-wrap items-center gap-1.5">
              {b.tier === 1 ? (
                <Pill tone="gold">🏆 Achievement · {b.points} pts</Pill>
              ) : (
                <Pill tone="green">Badge · {b.points} pts</Pill>
              )}
              <span className="text-[0.7rem] text-ink/45">{b.repeatable ? '· repeatable' : '· one-time'}</span>
            </div>
          </div>
        </div>
      ),
    },
    { header: 'Description', cell: (b) => b.description || '—' },
    { header: 'Criteria', cell: (b) => b.criteria || '—' },
    {
      header: 'Status',
      cell: (b) => (b.isActive ? <Pill tone="green">Active</Pill> : <Pill tone="gray">Disabled</Pill>),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Badges"
        subtitle="Achievements children can earn."
        action={<Button onClick={openNew}>+ New badge</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="🏅" title="No badges yet" hint="Create your first badge to start rewarding achievements." />
      )}

      {items && items.length > 0 && (
        <DataTable
          rows={items}
          rowKey={(b) => b.id}
          dim={(b) => !b.isActive}
          columns={columns}
          actions={(b) => (
            <>
              <Button variant="outline" onClick={() => openEdit(b)}>
                Edit
              </Button>
              <Button variant="ghost" onClick={() => toggleActive(b)}>
                {b.isActive ? 'Disable' : 'Enable'}
              </Button>
            </>
          )}
        />
      )}

      <Modal open={editing !== null} onClose={() => setEditing(null)} title={editing?.id ? 'Edit badge' : 'New badge'}>
        {editing && (
          <form
            onSubmit={(e) => {
              e.preventDefault()
              save()
            }}
            className="space-y-4"
          >
            {saveError && <ErrorBanner message={saveError} />}
            <Field label="Name">
              <Input value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} required />
            </Field>
            <Field label="Description">
              <Textarea
                rows={2}
                value={editing.description ?? ''}
                onChange={(e) => setEditing({ ...editing, description: e.target.value })}
              />
            </Field>
            <Field label="Icon" hint="Pick a fun icon kids will see on their tree.">
              <div className="flex flex-wrap gap-2">
                {BADGE_ICONS.map((ic) => {
                  const selected = editing.iconName === ic
                  return (
                    <button
                      key={ic}
                      type="button"
                      onClick={() => setEditing({ ...editing, iconName: ic })}
                      className={`grid size-11 place-items-center rounded-xl border text-xl transition hover:-translate-y-0.5 ${
                        selected ? 'border-gold bg-gold-soft/20' : 'border-cream-deep hover:bg-cream'
                      }`}
                    >
                      {ic}
                    </button>
                  )
                })}
              </div>
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Type" hint="Achievements are admin-awarded milestones.">
                <Select
                  value={editing.tier}
                  onChange={(e) => {
                    const tier = Number(e.target.value) as 0 | 1
                    // Default: effort/character badges repeat, milestones don't (admin can override below).
                    setEditing({ ...editing, tier, points: DEFAULT_POINTS[tier], repeatable: tier === 0 })
                  }}
                >
                  <option value={0}>🏅 Badge (teacher)</option>
                  <option value={1}>🏆 Achievement (admin)</option>
                </Select>
              </Field>
              <Field label="Growth points" hint="How much this adds to the tree.">
                <Input
                  type="number"
                  min={0}
                  value={editing.points}
                  onChange={(e) => setEditing({ ...editing, points: Number(e.target.value) })}
                />
              </Field>
            </div>
            <Field
              label="Awarding"
              hint="Effort/character badges can be earned again and again; milestones only once."
            >
              <label className="flex items-center gap-2 text-sm text-ink/80">
                <Checkbox
                  checked={editing.repeatable}
                  onChange={(e) => setEditing({ ...editing, repeatable: e.target.checked })}
                />
                Can be earned multiple times
              </label>
            </Field>
            <Field label="Criteria" hint="How is this badge earned?">
              <Textarea
                rows={2}
                value={editing.criteria ?? ''}
                onChange={(e) => setEditing({ ...editing, criteria: e.target.value })}
              />
            </Field>
            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="ghost" onClick={() => setEditing(null)}>
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving…' : 'Save'}
              </Button>
            </div>
          </form>
        )}
      </Modal>
    </div>
  )
}
