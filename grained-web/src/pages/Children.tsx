import { useEffect, useMemo, useState } from 'react'
import { api } from '../lib/api'
import { formatDate } from '../lib/format'
import { useToast } from '../components/Toast'
import { useAuth } from '../auth/AuthContext'
import { BadgeIcon } from '../components/BadgeIcon'
import type { Badge, Child, ChildForm, ChildStage, ClassGroup, ParentLookup } from '../types'
import {
  Button,
  Card,
  type Column,
  ConfirmDialog,
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
} from '../components/ui'

function toForm(c: Child): ChildForm {
  return {
    id: c.id,
    firstName: c.firstName,
    lastName: c.lastName,
    dateOfBirth: c.dateOfBirth,
    classGroupId: c.classGroupId,
    parentName: c.parentName,
    parentEmail: c.parentEmail,
    parentPhone: c.parentPhone,
  }
}

export function Children() {
  const { user } = useAuth()
  const toast = useToast()
  const canEdit = user?.roles.includes('ChurchAdmin') ?? false

  const [groups, setGroups] = useState<ClassGroup[]>([])
  const [items, setItems] = useState<Child[] | null>(null)
  const [filterClassGroupId, setFilterClassGroupId] = useState('')
  const [groupByFamily, setGroupByFamily] = useState(true)
  const [search, setSearch] = useState('')
  const [stages, setStages] = useState<Record<string, ChildStage>>({})
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState<ChildForm | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  // Set when a save would link the child to an existing account (e.g. a teacher) — the admin confirms.
  const [linkPrompt, setLinkPrompt] = useState<{ name: string | null; isStaff: boolean; email: string } | null>(null)
  const [deleting, setDeleting] = useState<Child | null>(null)
  const [deletingBusy, setDeletingBusy] = useState(false)
  // Award badge/achievement (admin)
  const [badges, setBadges] = useState<Badge[]>([])
  const [awardChild, setAwardChild] = useState<Child | null>(null)
  const [awardBadgeId, setAwardBadgeId] = useState('')
  const [awarding, setAwarding] = useState(false)

  // Parent login code (shown once after generate/reset). `code` is null when the parent already
  // has an account (e.g. they're also staff) — then they keep their existing login.
  const [parentCode, setParentCode] = useState<{
    code: string | null
    email: string
    linked: boolean
    siblingCount: number
  } | null>(null)
  const [copied, setCopied] = useState(false)

  const load = () =>
    api<Child[]>('/children' + (filterClassGroupId ? `?classGroupId=${filterClassGroupId}` : ''))
      .then(setItems)
      .catch((e) => setError(e.message))

  useEffect(() => {
    api<ClassGroup[]>('/class-groups')
      .then(setGroups)
      .catch((e) => setError(e.message))
    if (canEdit) api<Badge[]>('/badges').then(setBadges).catch(() => setBadges([]))
    api<ChildStage[]>('/growth/children')
      .then((list) => setStages(Object.fromEntries(list.map((s) => [s.childId, s]))))
      .catch(() => setStages({}))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    setItems(null)
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterClassGroupId])

  function openNew() {
    setSaveError(null)
    setEditing({
      firstName: '',
      lastName: '',
      dateOfBirth: '2019-01-01',
      classGroupId: groups[0]?.id ?? '',
      parentName: '',
      parentEmail: '',
      parentPhone: '',
    })
  }
  function openEdit(c: Child) {
    setSaveError(null)
    setEditing(toForm(c))
  }

  async function save(confirmedLink = false) {
    if (!editing) return
    const email = editing.parentEmail.trim()
    // Warn before a save links this child to an existing account that isn't a parent yet (e.g. a teacher).
    if (!confirmedLink && email) {
      try {
        const lk = await api<ParentLookup>(`/children/parent-lookup?email=${encodeURIComponent(email)}`)
        if (lk.exists && !lk.alreadyParent) {
          setLinkPrompt({ name: lk.name, isStaff: lk.isStaff, email })
          return
        }
      } catch {
        /* lookup is best-effort — fall through and save */
      }
    }
    setSaving(true)
    setSaveError(null)
    const isUpdate = !!editing.id
    try {
      const body = JSON.stringify(editing)
      if (editing.id) await api(`/children/${editing.id}`, { method: 'PUT', body })
      else await api('/children', { method: 'POST', body })
      setLinkPrompt(null)
      setEditing(null)
      await load()
      toast.success(isUpdate ? 'Child updated' : 'Child added')
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function toggleActive(c: Child) {
    if (!canEdit) return
    try {
      await api(`/children/${c.id}/active`, { method: 'POST', body: JSON.stringify({ isActive: !c.isActive }) })
      await load()
      toast.success(c.isActive ? 'Child disabled' : 'Child enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeletingBusy(true)
    try {
      await api(`/children/${deleting.id}`, { method: 'DELETE' })
      setDeleting(null)
      await load()
      toast.success('Child deleted')
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setDeletingBusy(false)
    }
  }

  async function generateParentCode(c: Child) {
    try {
      const res = await api<{ temporaryPassword: string | null; linkedExistingAccount: boolean; parentEmail: string }>(
        `/children/${c.id}/parent-code`,
        { method: 'POST' },
      )
      setCopied(false)
      const siblingCount = (items ?? []).filter(
        (x) => x.parentEmail.toLowerCase() === c.parentEmail.toLowerCase(),
      ).length
      setParentCode({
        code: res.temporaryPassword,
        email: res.parentEmail,
        linked: res.linkedExistingAccount,
        siblingCount,
      })
      toast.success(res.linkedExistingAccount ? 'Parent linked to existing account' : 'Parent login code generated')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  function openAward(c: Child) {
    setAwardBadgeId(badges[0]?.id ?? '')
    setAwardChild(c)
  }
  async function award() {
    if (!awardChild || !awardBadgeId) return
    setAwarding(true)
    try {
      await api(`/children/${awardChild.id}/badges`, { method: 'POST', body: JSON.stringify({ badgeId: awardBadgeId }) })
      const b = badges.find((x) => x.id === awardBadgeId)
      setAwardChild(null)
      toast.success(`Awarded “${b?.name}” to ${awardChild.firstName}`)
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setAwarding(false)
    }
  }

  async function copyParentCode() {
    if (!parentCode?.code) return
    try {
      await navigator.clipboard.writeText(parentCode.code)
      setCopied(true)
    } catch {
      toast.error('Could not copy — select the code and copy it manually')
    }
  }

  // Search kicks in at 3+ characters, matching child or parent name.
  const searchActive = search.trim().length >= 3
  const filtered = useMemo(() => {
    if (!items) return []
    if (!searchActive) return items
    const q = search.trim().toLowerCase()
    return items.filter(
      (c) =>
        `${c.firstName} ${c.lastName}`.toLowerCase().includes(q) ||
        c.parentName.toLowerCase().includes(q) ||
        c.parentEmail.toLowerCase().includes(q),
    )
  }, [items, search, searchActive])

  // Group children by the parent email so siblings sit together as a family.
  const families = useMemo(() => {
    const map = new Map<string, { email: string; parentName: string; children: Child[] }>()
    for (const c of filtered) {
      const key = c.parentEmail.toLowerCase()
      const fam = map.get(key) ?? { email: c.parentEmail, parentName: c.parentName, children: [] }
      fam.children.push(c)
      map.set(key, fam)
    }
    const list = [...map.values()]
    list.forEach((f) =>
      f.children.sort((a, b) => `${a.firstName} ${a.lastName}`.localeCompare(`${b.firstName} ${b.lastName}`)),
    )
    return list.sort((a, b) => a.parentName.localeCompare(b.parentName))
  }, [filtered])

  // A compact "2 at Sprout · 1 Seed" summary for a family, ordered by stage.
  function familyGrowth(children: Child[]) {
    const counts = new Map<number, { emoji: string; name: string; n: number }>()
    for (const c of children) {
      const s = stages[c.id]
      if (!s) continue
      const e = counts.get(s.stageIndex) ?? { emoji: s.stageEmoji, name: s.stageName, n: 0 }
      e.n++
      counts.set(s.stageIndex, e)
    }
    return [...counts.entries()].sort((a, b) => b[0] - a[0]).map(([, v]) => v)
  }

  const columns: Column<Child>[] = [
    {
      header: 'Name',
      primary: true,
      cell: (c) => (
        <>
          <div className="font-medium text-ink">
            {c.firstName} {c.lastName}
          </div>
          <div className="text-xs text-ink/50">Born {formatDate(c.dateOfBirth)}</div>
        </>
      ),
    },
    { header: 'Class group', cell: (c) => c.classGroupName },
    { header: 'Age', cell: (c) => c.age },
    {
      header: 'Parent',
      cell: (c) => (
        <>
          <div>{c.parentName}</div>
          <div className="text-xs text-ink/50">{c.parentEmail}</div>
        </>
      ),
    },
    {
      header: 'Stage',
      cell: (c) => (stages[c.id] ? `${stages[c.id].stageEmoji} ${stages[c.id].stageName}` : '—'),
    },
    {
      header: 'Status',
      cell: (c) => (c.isActive ? <Pill tone="green">Active</Pill> : <Pill tone="gray">Disabled</Pill>),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Children"
        subtitle="Children enrolled in your ministry."
        action={canEdit ? <Button onClick={openNew}>+ New child</Button> : undefined}
      />

      {error && <ErrorBanner message={error} />}

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="w-full sm:w-64">
          <Input
            type="search"
            placeholder="Search by child or parent name…"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        <div className="w-full max-w-xs sm:w-52">
          <Select value={filterClassGroupId} onChange={(e) => setFilterClassGroupId(e.target.value)}>
            <option value="">All class groups</option>
            {groups.map((g) => (
              <option key={g.id} value={g.id}>
                {g.name}
              </option>
            ))}
          </Select>
        </div>
        <div className="inline-flex rounded-xl border border-cream-deep bg-white p-1 text-sm sm:ml-auto">
          {(
            [
              ['family', '👨‍👩‍👧 By family'],
              ['list', 'All children'],
            ] as const
          ).map(([key, label]) => {
            const active = (key === 'family') === groupByFamily
            return (
              <button
                key={key}
                onClick={() => setGroupByFamily(key === 'family')}
                className={`rounded-lg px-3 py-1.5 transition ${
                  active ? 'bg-grove font-semibold text-oncream' : 'text-ink/60 hover:text-ink'
                }`}
              >
                {label}
              </button>
            )
          })}
        </div>
      </div>

      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="👧" title="No children yet" hint="Add a child to start tracking their journey." />
      )}
      {items && items.length > 0 && filtered.length === 0 && (
        <EmptyState icon="🔍" title="No matches" hint={`No children match “${search.trim()}”.`} />
      )}

      {/* Family view — siblings grouped under their parent */}
      {items && filtered.length > 0 && groupByFamily && (
        <div className="space-y-4">
          {families.map((fam) => (
            <Card key={fam.email}>
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-cream-deep p-4">
                <div className="flex items-center gap-3">
                  <span className="grid size-10 place-items-center rounded-xl bg-cream text-lg">👨‍👩‍👧</span>
                  <div>
                    <div className="font-medium text-ink">{fam.parentName || 'Parent'}</div>
                    <div className="text-xs text-ink/50">{fam.email}</div>
                  </div>
                  <Pill tone="green">
                    {fam.children.length} {fam.children.length === 1 ? 'child' : 'children'}
                  </Pill>
                  {familyGrowth(fam.children).map((g) => (
                    <span
                      key={g.name}
                      className="inline-flex items-center gap-1 rounded-full bg-leaf-light/40 px-2.5 py-0.5 text-xs font-semibold text-heading"
                    >
                      {g.emoji} {g.name}
                      {g.n > 1 && <span className="text-heading/60">×{g.n}</span>}
                    </span>
                  ))}
                </div>
                {canEdit && (
                  <Button variant="outline" onClick={() => generateParentCode(fam.children[0])}>
                    Parent code
                  </Button>
                )}
              </div>
              <div className="divide-y divide-cream-deep">
                {fam.children.map((c) => (
                  <div key={c.id} className={`flex flex-wrap items-center gap-3 p-4 ${c.isActive ? '' : 'opacity-55'}`}>
                    <div className="min-w-[9rem] flex-1">
                      <div className="font-medium text-ink">
                        {c.firstName} {c.lastName}
                      </div>
                      <div className="text-xs text-ink/50">
                        {c.classGroupName} · Age {c.age}
                      </div>
                    </div>
                    {stages[c.id] && (
                      <span className="text-xs text-ink/60" title={`${stages[c.id].growthPoints} points`}>
                        {stages[c.id].stageEmoji} {stages[c.id].stageName}
                      </span>
                    )}
                    {c.isActive ? <Pill tone="green">Active</Pill> : <Pill tone="gray">Disabled</Pill>}
                    {canEdit && (
                      <div className="flex flex-wrap gap-2">
                        <Button variant="outline" onClick={() => openEdit(c)}>
                          Edit
                        </Button>
                        <Button variant="outline" onClick={() => openAward(c)}>
                          🏅 Award
                        </Button>
                        <Button variant="ghost" onClick={() => toggleActive(c)}>
                          {c.isActive ? 'Disable' : 'Enable'}
                        </Button>
                        <Button variant="danger" onClick={() => setDeleting(c)}>
                          Delete
                        </Button>
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </Card>
          ))}
        </div>
      )}

      {/* Flat list */}
      {items && filtered.length > 0 && !groupByFamily && (
        <DataTable
          rows={filtered}
          rowKey={(c) => c.id}
          dim={(c) => !c.isActive}
          columns={columns}
          actions={
            canEdit
              ? (c) => (
                  <>
                    <Button variant="outline" onClick={() => openEdit(c)}>
                      Edit
                    </Button>
                    <Button variant="outline" onClick={() => openAward(c)}>
                      🏅 Award
                    </Button>
                    <Button variant="outline" onClick={() => generateParentCode(c)}>
                      Parent code
                    </Button>
                    <Button variant="ghost" onClick={() => toggleActive(c)}>
                      {c.isActive ? 'Disable' : 'Enable'}
                    </Button>
                    <Button variant="danger" onClick={() => setDeleting(c)}>
                      Delete
                    </Button>
                  </>
                )
              : undefined
          }
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title="Delete child?"
        busy={deletingBusy}
        message={
          <>
            Permanently delete <b>{deleting?.firstName} {deleting?.lastName}</b> and their badges, lesson progress and
            attendance? The parent account is kept. This can’t be undone.
          </>
        }
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />

      <ConfirmDialog
        open={linkPrompt !== null}
        danger={false}
        title="Link to an existing account?"
        confirmLabel="Link & save"
        busyLabel="Saving…"
        busy={saving}
        message={
          <>
            <b>{linkPrompt?.email}</b> already belongs to <b>{linkPrompt?.name ?? 'an existing account'}</b>
            {linkPrompt?.isStaff ? ', who signs in as a teacher/admin' : ''}. Saving will link{' '}
            <b>{editing?.firstName || 'this child'}</b> to that account and give it <b>Parent</b> access, so they can
            switch to the Parent view to follow this child. Continue?
          </>
        }
        onConfirm={() => save(true)}
        onClose={() => setLinkPrompt(null)}
      />

      <Modal open={editing !== null} onClose={() => setEditing(null)} title={editing?.id ? 'Edit child' : 'New child'}>
        {editing && (
          <form
            onSubmit={(e) => {
              e.preventDefault()
              save()
            }}
            className="space-y-4"
          >
            {saveError && <ErrorBanner message={saveError} />}
            <div className="grid grid-cols-2 gap-4">
              <Field label="First name">
                <Input
                  value={editing.firstName}
                  onChange={(e) => setEditing({ ...editing, firstName: e.target.value })}
                  required
                />
              </Field>
              <Field label="Last name">
                <Input
                  value={editing.lastName}
                  onChange={(e) => setEditing({ ...editing, lastName: e.target.value })}
                  required
                />
              </Field>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Date of birth">
                <Input
                  type="date"
                  value={editing.dateOfBirth}
                  onChange={(e) => setEditing({ ...editing, dateOfBirth: e.target.value })}
                  required
                />
              </Field>
              <Field label="Class group">
                <Select
                  value={editing.classGroupId ?? ''}
                  onChange={(e) => setEditing({ ...editing, classGroupId: e.target.value })}
                  required
                >
                  <option value="" disabled>
                    Select a group
                  </option>
                  {groups.map((g) => (
                    <option key={g.id} value={g.id}>
                      {g.name}
                    </option>
                  ))}
                </Select>
              </Field>
            </div>
            <Field label="Parent name">
              <Input
                value={editing.parentName}
                onChange={(e) => setEditing({ ...editing, parentName: e.target.value })}
                required
              />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Parent email">
                <Input
                  type="email"
                  value={editing.parentEmail}
                  onChange={(e) => setEditing({ ...editing, parentEmail: e.target.value })}
                  required
                />
              </Field>
              <Field label="Parent phone">
                <Input
                  value={editing.parentPhone ?? ''}
                  onChange={(e) => setEditing({ ...editing, parentPhone: e.target.value })}
                />
              </Field>
            </div>
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

      <Modal open={awardChild !== null} onClose={() => setAwardChild(null)} title="Award a badge or achievement">
        {awardChild && (
          <div className="space-y-4">
            <p className="text-sm text-ink/70">
              Award to <span className="font-medium">{awardChild.firstName} {awardChild.lastName}</span>. Achievements are
              worth more growth points than everyday badges.
            </p>
            {badges.length === 0 ? (
              <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
                No badges yet — create some on the Badges page first.
              </p>
            ) : (
              <div className="max-h-72 space-y-2 overflow-y-auto">
                {badges.map((b) => (
                  <label
                    key={b.id}
                    className={`flex cursor-pointer items-center gap-3 rounded-xl border p-3 transition ${
                      awardBadgeId === b.id ? 'border-gold bg-gold-soft/15' : 'border-cream-deep hover:bg-cream'
                    }`}
                  >
                    <input
                      type="radio"
                      name="award-badge"
                      className="sr-only"
                      checked={awardBadgeId === b.id}
                      onChange={() => setAwardBadgeId(b.id)}
                    />
                    <BadgeIcon icon={b.iconName} size={36} />
                    <div className="min-w-0 flex-1">
                      <div className="text-sm font-medium text-ink">{b.name}</div>
                      <div className="text-xs text-ink/50">
                        {b.tier === 1 ? 'Achievement' : 'Badge'} · {b.points} pts
                      </div>
                    </div>
                  </label>
                ))}
              </div>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="ghost" onClick={() => setAwardChild(null)}>
                Cancel
              </Button>
              <Button onClick={award} disabled={awarding || !awardBadgeId}>
                {awarding ? 'Awarding…' : 'Award'}
              </Button>
            </div>
          </div>
        )}
      </Modal>

      <Modal
        open={parentCode !== null}
        onClose={() => setParentCode(null)}
        title={parentCode?.linked ? 'Parent linked' : 'Parent login code'}
      >
        {parentCode && (
          <div className="space-y-4">
            {parentCode.linked ? (
              <>
                <p className="text-sm text-ink/70">
                  <span className="font-medium">{parentCode.email}</span> already has a Grained account (for example,
                  they&rsquo;re also a teacher). All{' '}
                  <span className="font-medium">
                    {parentCode.siblingCount} child{parentCode.siblingCount === 1 ? '' : 'ren'}
                  </span>{' '}
                  with this email are now linked to it — so <span className="font-medium">no new code is needed</span>.
                </p>
                <div className="rounded-xl border border-cream-deep bg-cream/60 p-4 text-sm text-ink/70">
                  They sign in with their <span className="font-medium">existing password</span>, then choose{' '}
                  <span className="font-medium">Parent</span> after logging in (or use “Switch view”) to see their
                  child&rsquo;s progress.
                </div>
              </>
            ) : (
              <>
                <p className="text-sm text-ink/70">
                  The parent signs in with their email and this code to follow their children&rsquo;s progress. This
                  login covers all{' '}
                  <span className="font-medium">
                    {parentCode.siblingCount} child{parentCode.siblingCount === 1 ? '' : 'ren'}
                  </span>{' '}
                  sharing this email.
                </p>
                <div className="rounded-xl border border-cream-deep bg-cream/60 p-4">
                  <div className="text-xs font-medium uppercase tracking-wide text-ink/45">Email</div>
                  <div className="text-sm font-medium text-ink">{parentCode.email}</div>
                  <div className="mt-3 text-xs font-medium uppercase tracking-wide text-ink/45">Login code</div>
                  <div className="mt-1 flex items-center gap-2">
                    <code className="flex-1 rounded-lg bg-white px-3 py-2 font-mono text-base tracking-wide text-heading">
                      {parentCode.code}
                    </code>
                    <Button type="button" variant="outline" onClick={copyParentCode}>
                      {copied ? 'Copied ✓' : 'Copy'}
                    </Button>
                  </div>
                </div>
                <p className="text-xs text-ink/50">
                  This code is shown once and can&rsquo;t be retrieved later. Generating it again replaces the previous
                  code.
                </p>
              </>
            )}
            <div className="flex justify-end pt-2">
              <Button type="button" onClick={() => setParentCode(null)}>
                Done
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
