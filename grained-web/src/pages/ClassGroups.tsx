import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import type { ClassGroup, ClassGroupForm } from '../types'
import {
  Button,
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
  Textarea,
} from '../components/ui'

const empty: ClassGroupForm = { name: '', minAge: 0, maxAge: 4, description: '' }

export function ClassGroups() {
  const toast = useToast()
  const [items, setItems] = useState<ClassGroup[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [editing, setEditing] = useState<ClassGroupForm | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [deleting, setDeleting] = useState<ClassGroup | null>(null)
  const [deletingBusy, setDeletingBusy] = useState(false)

  const load = () =>
    api<ClassGroup[]>('/class-groups?includeInactive=true').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  function openNew() {
    setSaveError(null)
    setEditing({ ...empty })
  }
  function openEdit(c: ClassGroup) {
    setSaveError(null)
    setEditing({ id: c.id, name: c.name, minAge: c.minAge, maxAge: c.maxAge, description: c.description })
  }

  async function save() {
    if (!editing) return
    setSaving(true)
    setSaveError(null)
    const isUpdate = !!editing.id
    try {
      const body = JSON.stringify(editing)
      if (editing.id) await api(`/class-groups/${editing.id}`, { method: 'PUT', body })
      else await api('/class-groups', { method: 'POST', body })
      setEditing(null)
      await load()
      toast.success(isUpdate ? 'Class group updated' : 'Class group created')
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function toggleActive(c: ClassGroup) {
    try {
      await api(`/class-groups/${c.id}/active`, { method: 'POST', body: JSON.stringify({ isActive: !c.isActive }) })
      await load()
      toast.success(c.isActive ? 'Class group disabled' : 'Class group enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeletingBusy(true)
    try {
      await api(`/class-groups/${deleting.id}`, { method: 'DELETE' })
      setDeleting(null)
      await load()
      toast.success('Class group deleted')
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setDeletingBusy(false)
    }
  }

  const columns: Column<ClassGroup>[] = [
    {
      header: 'Name',
      primary: true,
      cell: (c) => (
        <>
          <div className="font-medium text-ink">{c.name}</div>
          {c.description && <div className="text-xs text-ink/50">{c.description}</div>}
        </>
      ),
    },
    { header: 'Ages', cell: (c) => `${c.minAge}–${c.maxAge}` },
    { header: 'Children', cell: (c) => c.childCount },
    {
      header: 'Status',
      cell: (c) => (c.isActive ? <Pill tone="green">Active</Pill> : <Pill tone="gray">Disabled</Pill>),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Class Groups"
        subtitle="Age-banded groups children are placed into."
        action={<Button onClick={openNew}>+ New class group</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="🧒" title="No class groups yet" hint="Create your first group to start adding children." />
      )}

      {items && items.length > 0 && (
        <DataTable
          rows={items}
          rowKey={(c) => c.id}
          dim={(c) => !c.isActive}
          columns={columns}
          actions={(c) => (
            <>
              <Button variant="outline" onClick={() => openEdit(c)}>
                Edit
              </Button>
              <Button variant="ghost" onClick={() => toggleActive(c)}>
                {c.isActive ? 'Disable' : 'Enable'}
              </Button>
              <Button variant="danger" onClick={() => setDeleting(c)}>
                Delete
              </Button>
            </>
          )}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title="Delete class group?"
        busy={deletingBusy}
        message={
          <>
            Permanently delete <b>{deleting?.name}</b>? This can’t be undone. A class with children or attendance
            history can’t be deleted — disable it instead.
          </>
        }
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />

      <Modal
        open={editing !== null}
        onClose={() => setEditing(null)}
        title={editing?.id ? 'Edit class group' : 'New class group'}
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
            <Field label="Name">
              <Input value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} required />
            </Field>
            <div className="grid grid-cols-2 gap-4">
              <Field label="Min age">
                <Input
                  type="number"
                  min={0}
                  max={120}
                  value={editing.minAge}
                  onChange={(e) => setEditing({ ...editing, minAge: Number(e.target.value) })}
                  required
                />
              </Field>
              <Field label="Max age">
                <Input
                  type="number"
                  min={0}
                  max={120}
                  value={editing.maxAge}
                  onChange={(e) => setEditing({ ...editing, maxAge: Number(e.target.value) })}
                  required
                />
              </Field>
            </div>
            <Field label="Description">
              <Textarea
                rows={2}
                value={editing.description ?? ''}
                onChange={(e) => setEditing({ ...editing, description: e.target.value })}
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
