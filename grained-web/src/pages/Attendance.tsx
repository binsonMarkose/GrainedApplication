import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { api } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { toDateInput } from '../lib/format'
import type { AttendanceSave, ClassGroup, RosterEntry, TeacherWorkspace } from '../types'

// The class picker only needs id + name; both admins (/class-groups) and teachers
// (/teacher/workspace, scoped to their own classes) feed this shape.
interface ClassOption {
  id: string
  name: string
}
import {
  Button,
  Card,
  EmptyState,
  ErrorBanner,
  Field,
  Input,
  Loading,
  PageHeader,
  Pill,
  Select,
} from '../components/ui'

interface Row {
  childId: string
  firstName: string
  lastName: string
  isPresent: boolean
  notes: string
}

export function Attendance() {
  const { user } = useAuth()
  const [searchParams] = useSearchParams()
  const roles = user?.roles ?? []
  const isTeacherOnly =
    roles.includes('Teacher') && !roles.includes('ChurchAdmin') && !roles.includes('SuperAdmin')

  const [groups, setGroups] = useState<ClassOption[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [classGroupId, setClassGroupId] = useState('')
  const [date, setDate] = useState(toDateInput())

  const [roster, setRoster] = useState<Row[] | null>(null)
  const [loadingRoster, setLoadingRoster] = useState(false)
  const [rosterError, setRosterError] = useState<string | null>(null)

  const [saving, setSaving] = useState(false)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  useEffect(() => {
    const source = isTeacherOnly
      ? api<TeacherWorkspace>('/teacher/workspace').then((w) =>
          w.classes.map((c) => ({ id: c.classGroupId, name: c.name })),
        )
      : api<ClassGroup[]>('/class-groups').then((gs) => gs.map((g) => ({ id: g.id, name: g.name })))

    source
      .then((opts) => {
        setGroups(opts)
        // Preselect a class group deep-linked from the teacher dashboard, and auto-load its roster.
        const requested = searchParams.get('classGroupId')
        const preselect = opts.find((o) => o.id === requested)?.id ?? (opts.length > 0 ? opts[0].id : '')
        setClassGroupId(preselect)
        if (requested && preselect === requested) loadRoster(preselect)
      })
      .catch((e) => setError((e as Error).message))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isTeacherOnly])

  async function loadRoster(overrideClassGroupId?: string) {
    const cgId = overrideClassGroupId ?? classGroupId
    if (!cgId) return
    setLoadingRoster(true)
    setRosterError(null)
    setSaveError(null)
    setSaved(false)
    try {
      const entries = await api<RosterEntry[]>(
        `/attendance/roster?classGroupId=${cgId}&date=${date}`,
      )
      setRoster(
        entries.map((e) => ({
          childId: e.childId,
          firstName: e.firstName,
          lastName: e.lastName,
          isPresent: e.isPresent,
          notes: e.notes ?? '',
        })),
      )
    } catch (e) {
      setRoster(null)
      setRosterError((e as Error).message)
    } finally {
      setLoadingRoster(false)
    }
  }

  function toggle(childId: string) {
    setSaved(false)
    setRoster((rs) =>
      rs ? rs.map((r) => (r.childId === childId ? { ...r, isPresent: !r.isPresent } : r)) : rs,
    )
  }

  function setNotes(childId: string, notes: string) {
    setSaved(false)
    setRoster((rs) => (rs ? rs.map((r) => (r.childId === childId ? { ...r, notes } : r)) : rs))
  }

  function markAllPresent() {
    setSaved(false)
    setRoster((rs) => (rs ? rs.map((r) => ({ ...r, isPresent: true })) : rs))
  }

  async function save() {
    if (!roster) return
    setSaving(true)
    setSaveError(null)
    setSaved(false)
    try {
      const payload: AttendanceSave = {
        classGroupId,
        attendanceDate: date,
        entries: roster.map((r) => ({ childId: r.childId, isPresent: r.isPresent, notes: r.notes })),
      }
      await api('/attendance', { method: 'POST', body: JSON.stringify(payload) })
      setSaved(true)
    } catch (e) {
      setSaveError((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <PageHeader title="Attendance" subtitle="Record who's here, class by class." />

      {error && <ErrorBanner message={error} />}
      {!groups && !error && <Loading />}

      {groups && (
        <>
          <Card className="mb-6 p-5">
            <div className="grid gap-4 sm:grid-cols-[1fr_auto_auto] sm:items-end">
              <Field label="Class group">
                <Select value={classGroupId} onChange={(e) => setClassGroupId(e.target.value)}>
                  {groups.map((g) => (
                    <option key={g.id} value={g.id}>
                      {g.name}
                    </option>
                  ))}
                </Select>
              </Field>
              <Field label="Date">
                <Input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
              </Field>
              <Button onClick={() => loadRoster()} disabled={loadingRoster || !classGroupId}>
                {loadingRoster ? 'Loading…' : 'Load roster'}
              </Button>
            </div>
          </Card>

          {rosterError && <ErrorBanner message={rosterError} />}

          {roster && roster.length === 0 && (
            <EmptyState
              icon="✅"
              title="No children in this class"
              hint="Add children to this class group to take attendance."
            />
          )}

          {roster && roster.length > 0 && (
            <>
              <div className="mb-3 flex items-center justify-between">
                <span className="text-sm text-ink/55">
                  {roster.filter((r) => r.isPresent).length} of {roster.length} present
                </span>
                <Button variant="outline" onClick={markAllPresent}>
                  Mark all present
                </Button>
              </div>

              <div className="space-y-3">
                {roster.map((r) => (
                  <Card key={r.childId} className="p-4">
                    <div className="flex flex-wrap items-center gap-4">
                      <button
                        type="button"
                        onClick={() => toggle(r.childId)}
                        aria-pressed={r.isPresent}
                        className={`flex min-w-[7.5rem] items-center justify-center gap-2 rounded-xl px-5 py-4 text-sm font-semibold transition ${
                          r.isPresent
                            ? 'bg-leaf-light/40 text-heading hover:bg-leaf-light/60'
                            : 'bg-cream-deep text-ink/50 hover:bg-cream'
                        }`}
                      >
                        <span className="text-lg">{r.isPresent ? '✅' : '⬜'}</span>
                        {r.isPresent ? 'Present' : 'Absent'}
                      </button>

                      <div className="min-w-[8rem] flex-1">
                        <div className="font-medium text-ink">
                          {r.firstName} {r.lastName}
                        </div>
                        <div className="mt-0.5">
                          {r.isPresent ? (
                            <Pill tone="green">Present</Pill>
                          ) : (
                            <Pill tone="gray">Absent</Pill>
                          )}
                        </div>
                      </div>

                      <Input
                        className="sm:w-56"
                        placeholder="Notes (optional)"
                        value={r.notes}
                        onChange={(e) => setNotes(r.childId, e.target.value)}
                      />
                    </div>
                  </Card>
                ))}
              </div>

              {saveError && (
                <div className="mt-4">
                  <ErrorBanner message={saveError} />
                </div>
              )}
              {saved && (
                <div className="mt-4 rounded-xl border border-leaf-light bg-leaf-light/30 px-4 py-3 text-sm font-medium text-heading">
                  Attendance saved ✓
                </div>
              )}

              <div className="mt-6 flex justify-end">
                <Button onClick={save} disabled={saving}>
                  {saving ? 'Saving…' : 'Save attendance'}
                </Button>
              </div>
            </>
          )}
        </>
      )}
    </div>
  )
}
