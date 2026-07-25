import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import type { ClassGroup, Teacher, TeacherCreatedResult, TeacherForm } from '../types'
import {
  Button,
  Checkbox,
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
} from '../components/ui'

const empty: TeacherForm = { fullName: '', email: '', assignedClassGroupIds: [] }

export function Teachers() {
  const toast = useToast()
  const [items, setItems] = useState<Teacher[] | null>(null)
  const [groups, setGroups] = useState<ClassGroup[]>([])
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState<TeacherForm | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  // The one-time-visible login code, shown after create or reset. `isNew` tweaks the copy.
  const [codeInfo, setCodeInfo] = useState<{ code: string; teacher: string; email: string; isNew: boolean } | null>(
    null,
  )
  const [copied, setCopied] = useState(false)
  const [deleting, setDeleting] = useState<Teacher | null>(null)
  const [deletingBusy, setDeletingBusy] = useState(false)

  const load = () =>
    api<Teacher[]>('/teachers?includeInactive=true').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    api<ClassGroup[]>('/class-groups')
      .then(setGroups)
      .catch((e) => setError(e.message))
    load()
  }, [])

  function openNew() {
    setSaveError(null)
    setEditing({ ...empty, assignedClassGroupIds: [] })
  }
  function openEdit(t: Teacher) {
    setSaveError(null)
    setEditing({
      teacherProfileId: t.teacherProfileId,
      fullName: t.fullName,
      email: t.email,
      assignedClassGroupIds: [...t.assignedClassGroupIds],
    })
  }

  function toggleGroup(id: string) {
    if (!editing) return
    const has = editing.assignedClassGroupIds.includes(id)
    setEditing({
      ...editing,
      assignedClassGroupIds: has
        ? editing.assignedClassGroupIds.filter((g) => g !== id)
        : [...editing.assignedClassGroupIds, id],
    })
  }

  async function save() {
    if (!editing) return
    setSaving(true)
    setSaveError(null)
    const isUpdate = !!editing.teacherProfileId
    try {
      const body = JSON.stringify(editing)
      if (editing.teacherProfileId) {
        await api(`/teachers/${editing.teacherProfileId}`, { method: 'PUT', body })
        setEditing(null)
      } else {
        const result = await api<TeacherCreatedResult>('/teachers', { method: 'POST', body })
        setEditing(null)
        setCopied(false)
        setCodeInfo({ code: result.temporaryPassword, teacher: editing.fullName, email: editing.email, isNew: true })
      }
      await load()
      toast.success(isUpdate ? 'Teacher updated' : 'Teacher created')
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function toggleActive(t: Teacher) {
    try {
      await api(`/teachers/${t.teacherProfileId}/active`, {
        method: 'POST',
        body: JSON.stringify({ isActive: !t.isActive }),
      })
      await load()
      toast.success(t.isActive ? 'Teacher disabled' : 'Teacher enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeletingBusy(true)
    try {
      await api(`/teachers/${deleting.teacherProfileId}`, { method: 'DELETE' })
      setDeleting(null)
      await load()
      toast.success('Teacher deleted')
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setDeletingBusy(false)
    }
  }

  async function resetCode(t: Teacher) {
    try {
      const result = await api<TeacherCreatedResult>(`/teachers/${t.teacherProfileId}/reset-code`, { method: 'POST' })
      setCopied(false)
      setCodeInfo({ code: result.temporaryPassword, teacher: t.fullName, email: t.email, isNew: false })
      toast.success('New login code generated')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  async function copyCode() {
    if (!codeInfo) return
    try {
      await navigator.clipboard.writeText(codeInfo.code)
      setCopied(true)
    } catch {
      // clipboard may be unavailable (e.g. non-secure context) — the code is still visible to copy manually
      toast.error('Could not copy — select the code and copy it manually')
    }
  }

  const columns: Column<Teacher>[] = [
    {
      header: 'Name',
      primary: true,
      cell: (t) => <div className="font-medium text-ink">{t.fullName}</div>,
    },
    { header: 'Email', cell: (t) => t.email },
    {
      header: 'Assigned classes',
      cell: (t) => (t.assignedClassGroupNames.length ? t.assignedClassGroupNames.join(', ') : '—'),
    },
    {
      header: 'Status',
      cell: (t) => (t.isActive ? <Pill tone="green">Active</Pill> : <Pill tone="gray">Disabled</Pill>),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Teachers"
        subtitle="Teacher accounts and their class assignments."
        action={<Button onClick={openNew}>+ New teacher</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="🍎" title="No teachers yet" hint="Create a teacher account to assign them to classes." />
      )}

      {items && items.length > 0 && (
        <DataTable
          rows={items}
          rowKey={(t) => t.teacherProfileId}
          dim={(t) => !t.isActive}
          columns={columns}
          actions={(t) => (
            <>
              <Button variant="outline" onClick={() => openEdit(t)}>
                Edit
              </Button>
              <Button variant="outline" onClick={() => resetCode(t)}>
                Reset code
              </Button>
              <Button variant="ghost" onClick={() => toggleActive(t)}>
                {t.isActive ? 'Disable' : 'Enable'}
              </Button>
              <Button variant="danger" onClick={() => setDeleting(t)}>
                Delete
              </Button>
            </>
          )}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title="Delete teacher?"
        busy={deletingBusy}
        message={
          <>
            Permanently remove <b>{deleting?.fullName}</b> and their sign-in? Their class assignments are cleared and
            any lessons they authored keep their name. This can’t be undone.
          </>
        }
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />

      <Modal
        open={editing !== null}
        onClose={() => setEditing(null)}
        title={editing?.teacherProfileId ? 'Edit teacher' : 'New teacher'}
      >
        {editing && (
          <form
            onSubmit={(e) => {
              e.preventDefault()
              save()
            }}
            className="space-y-4"
          >
            {saveError && <ErrorBanner message={saveError} />}
            <Field label="Full name">
              <Input
                value={editing.fullName}
                onChange={(e) => setEditing({ ...editing, fullName: e.target.value })}
                required
              />
            </Field>
            <Field label="Email">
              <Input
                type="email"
                value={editing.email}
                onChange={(e) => setEditing({ ...editing, email: e.target.value })}
                required
              />
            </Field>
            <Field label="Assigned classes">
              {groups.length === 0 ? (
                <p className="text-sm text-ink/50">No class groups available.</p>
              ) : (
                <div className="space-y-2">
                  {groups.map((g) => (
                    <label key={g.id} className="flex items-center gap-2 text-sm text-ink/80">
                      <Checkbox
                        checked={editing.assignedClassGroupIds.includes(g.id)}
                        onChange={() => toggleGroup(g.id)}
                      />
                      {g.name}
                    </label>
                  ))}
                </div>
              )}
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

      <Modal
        open={codeInfo !== null}
        onClose={() => setCodeInfo(null)}
        title={codeInfo?.isNew ? 'Teacher account created' : 'New login code'}
      >
        {codeInfo && (
          <div className="space-y-4">
            <p className="text-sm text-ink/70">
              {codeInfo.isNew
                ? `${codeInfo.teacher} can now sign in with their email and this login code:`
                : `A new login code has been generated for ${codeInfo.teacher}. Their old code no longer works.`}
            </p>

            <div className="rounded-xl border border-cream-deep bg-cream/60 p-4">
              <div className="text-xs font-medium uppercase tracking-wide text-ink/45">Email</div>
              <div className="text-sm font-medium text-ink">{codeInfo.email}</div>
              <div className="mt-3 text-xs font-medium uppercase tracking-wide text-ink/45">Login code</div>
              <div className="mt-1 flex items-center gap-2">
                <code className="flex-1 rounded-lg bg-white px-3 py-2 font-mono text-base tracking-wide text-heading">
                  {codeInfo.code}
                </code>
                <Button type="button" variant="outline" onClick={copyCode}>
                  {copied ? 'Copied ✓' : 'Copy'}
                </Button>
              </div>
            </div>

            <p className="text-xs text-ink/50">
              This code is shown once and can&rsquo;t be retrieved later — copy it now and share it with the teacher.
              You can generate a fresh one anytime with <span className="font-medium">Reset code</span>.
            </p>

            <div className="flex justify-end pt-2">
              <Button type="button" onClick={() => setCodeInfo(null)}>
                Done
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
