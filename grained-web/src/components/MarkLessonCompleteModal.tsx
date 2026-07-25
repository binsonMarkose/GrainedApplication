import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { toDateInput } from '../lib/format'
import { useToast } from './Toast'
import { Button, Checkbox, Field, Input, Loading, Modal, Select } from './ui'
import type { RosterEntry } from '../types'

export interface CompleteClass {
  classGroupId: string
  name: string
}

// Records a lesson as completed for the children present in a class on a chosen date, ticking who
// also learned the memory verse. Used from the teacher dashboard and the Lessons page so a teacher
// can mark right after teaching, without navigating away. When several classes are eligible (the
// lesson is assigned to more than one of the teacher's classes) a class picker is shown.
export function MarkLessonCompleteModal({
  open,
  lesson,
  classes,
  onClose,
  onCompleted,
}: {
  open: boolean
  lesson: { id: string; title: string } | null
  classes: CompleteClass[]
  onClose: () => void
  onCompleted?: () => void
}) {
  const toast = useToast()
  const [classId, setClassId] = useState('')
  const [date, setDate] = useState(toDateInput())
  const [rosterKids, setRosterKids] = useState<RosterEntry[] | null>(null)
  const [rosterLoading, setRosterLoading] = useState(false)
  const [verseSet, setVerseSet] = useState<Set<string>>(new Set())
  const [completing, setCompleting] = useState(false)

  // On open (or lesson change), default the class + date and load the present roster.
  useEffect(() => {
    if (!open || classes.length === 0) return
    const first = classes[0].classGroupId
    const today = toDateInput()
    setClassId(first)
    setDate(today)
    loadRoster(first, today)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, lesson?.id])

  async function loadRoster(cid: string, d: string) {
    setRosterLoading(true)
    setRosterKids(null)
    try {
      const roster = await api<RosterEntry[]>(`/attendance/roster?classGroupId=${cid}&date=${d}`)
      const present = roster.filter((r) => r.isPresent)
      setRosterKids(present)
      // Default: everyone present is assumed to have learned the verse; the teacher unticks any who didn't.
      setVerseSet(new Set(present.map((r) => r.childId)))
    } catch {
      setRosterKids([])
    } finally {
      setRosterLoading(false)
    }
  }

  function changeClass(cid: string) {
    setClassId(cid)
    loadRoster(cid, date)
  }
  function changeDate(d: string) {
    setDate(d)
    loadRoster(classId, d)
  }
  function toggleVerse(id: string) {
    setVerseSet((prev) => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  async function submit() {
    if (!lesson || !classId) return
    setCompleting(true)
    try {
      const res = await api<{ childrenCompleted: number }>(`/teacher/lessons/${lesson.id}/complete`, {
        method: 'POST',
        body: JSON.stringify({ classGroupId: classId, date, verseChildIds: [...verseSet] }),
      })
      toast.success(
        `Lesson marked complete for ${res.childrenCompleted} present ${res.childrenCompleted === 1 ? 'child' : 'children'}`,
      )
      onCompleted?.()
      onClose()
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setCompleting(false)
    }
  }

  const className = classes.find((c) => c.classGroupId === classId)?.name ?? ''

  return (
    <Modal open={open} onClose={onClose} title="Mark lesson completed">
      {lesson && (
        <div className="space-y-4">
          <p className="text-sm text-ink/70">
            This records <span className="font-medium">{lesson.title}</span> as completed for every child marked{' '}
            <span className="font-medium">present</span>
            {className && (
              <>
                {' '}
                in <span className="font-medium">{className}</span>
              </>
            )}{' '}
            on the date below.
          </p>

          {classes.length > 1 && (
            <Field label="Class">
              <Select value={classId} onChange={(e) => changeClass(e.target.value)}>
                {classes.map((c) => (
                  <option key={c.classGroupId} value={c.classGroupId}>
                    {c.name}
                  </option>
                ))}
              </Select>
            </Field>
          )}

          <Field label="Session date" hint="Take attendance for this date first.">
            <Input type="date" value={date} onChange={(e) => changeDate(e.target.value)} />
          </Field>

          {rosterLoading && <Loading label="Loading class…" />}
          {rosterKids && rosterKids.length === 0 && !rosterLoading && (
            <p className="rounded-xl border border-dashed border-cream-deep px-4 py-4 text-center text-sm text-ink/50">
              No children are marked present on this date. Take attendance first.
            </p>
          )}
          {rosterKids && rosterKids.length > 0 && (
            <div>
              <div className="mb-1 flex items-center justify-between">
                <span className="text-sm font-medium text-ink/70">📜 Who learned the memory verse?</span>
                <button
                  type="button"
                  className="text-xs font-semibold text-accent hover:underline"
                  onClick={() =>
                    setVerseSet((prev) =>
                      prev.size === rosterKids.length ? new Set() : new Set(rosterKids.map((r) => r.childId)),
                    )
                  }
                >
                  {verseSet.size === rosterKids.length ? 'Clear all' : 'Select all'}
                </button>
              </div>
              <div className="max-h-56 space-y-1 overflow-y-auto rounded-xl border border-cream-deep p-2">
                {rosterKids.map((r) => (
                  <label
                    key={r.childId}
                    className="flex cursor-pointer items-center gap-2 rounded-lg px-2 py-1.5 hover:bg-cream"
                  >
                    <Checkbox checked={verseSet.has(r.childId)} onChange={() => toggleVerse(r.childId)} />
                    <span className="text-sm text-ink">
                      {r.firstName} {r.lastName}
                    </span>
                  </label>
                ))}
              </div>
              <p className="mt-1 text-xs text-ink/45">
                All present children get the lesson; ticked children also get the memory verse.
              </p>
            </div>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="ghost" onClick={onClose}>
              Cancel
            </Button>
            <Button onClick={submit} disabled={completing || !rosterKids || rosterKids.length === 0}>
              {completing ? 'Saving…' : 'Mark completed'}
            </Button>
          </div>
        </div>
      )}
    </Modal>
  )
}
