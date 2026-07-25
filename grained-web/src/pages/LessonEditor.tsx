import { useEffect, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { api } from '../lib/api'
import { formatDate } from '../lib/format'
import { useToast } from '../components/Toast'
import { useAuth } from '../auth/AuthContext'
import { MarkLessonCompleteModal } from '../components/MarkLessonCompleteModal'
import { TeachMode } from '../components/TeachMode'
import type {
  ClassGroup,
  LessonDetail,
  LessonForm,
  QuizQuestion,
  QuizQuestionForm,
} from '../types'
import {
  Button,
  Card,
  Checkbox,
  ErrorBanner,
  Field,
  Input,
  Loading,
  Modal,
  Pill,
  Textarea,
} from '../components/ui'
import type { LessonStatus } from '../types'

function statusPill(status: LessonStatus) {
  if (status === 2) return <Pill tone="green">Published</Pill>
  if (status === 1) return <Pill tone="gold">In review</Pill>
  return <Pill tone="gray">Draft</Pill>
}

const emptyForm: LessonForm = {
  title: '',
  bibleReference: '',
  theme: '',
  ageGroup: '',
  storyContent: '',
  learningObjective: '',
  activity: '',
  prayer: '',
  memoryVerse: { verseText: '', bibleReference: '', shortExplanation: '' },
}

interface OptionDraft {
  optionText: string
  isCorrect: boolean
}

export function LessonEditor() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const toast = useToast()
  const isNew = !id || id === 'new'
  const isAdmin = (user?.roles.includes('ChurchAdmin') || user?.roles.includes('SuperAdmin')) ?? false
  const isTeacher = user?.roles.includes('Teacher') ?? false

  const [classGroups, setClassGroups] = useState<ClassGroup[] | null>(null)
  const [detail, setDetail] = useState<LessonDetail | null>(null)
  const [form, setForm] = useState<LessonForm>({ ...emptyForm })
  const [assignedIds, setAssignedIds] = useState<string[]>([])
  const [questions, setQuestions] = useState<QuizQuestion[]>([])

  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [showComplete, setShowComplete] = useState(false)
  const [showTeach, setShowTeach] = useState(false)

  // Add-question sub-form state
  const [qText, setQText] = useState('')
  const [qPoints, setQPoints] = useState(1)
  const [qOptions, setQOptions] = useState<OptionDraft[]>([
    { optionText: '', isCorrect: false },
    { optionText: '', isCorrect: false },
  ])
  const [qError, setQError] = useState<string | null>(null)
  const [qSaving, setQSaving] = useState(false)

  function populate(d: LessonDetail) {
    setDetail(d)
    setForm({
      title: d.title,
      bibleReference: d.bibleReference,
      theme: d.theme ?? '',
      ageGroup: d.ageGroup,
      storyContent: d.storyContent,
      learningObjective: d.learningObjective ?? '',
      activity: d.activity ?? '',
      prayer: d.prayer ?? '',
      memoryVerse: {
        verseText: d.memoryVerse?.verseText ?? '',
        bibleReference: d.memoryVerse?.bibleReference ?? '',
        shortExplanation: d.memoryVerse?.shortExplanation ?? '',
      },
    })
    setAssignedIds(d.assignedClassGroupIds)
    setQuestions(d.quiz?.questions ?? [])
  }

  async function loadDetail() {
    const d = await api<LessonDetail>('/lessons/' + id)
    populate(d)
  }

  useEffect(() => {
    let active = true
    setLoading(true)
    setError(null)
    async function run() {
      try {
        const groups = await api<ClassGroup[]>('/class-groups')
        if (!active) return
        setClassGroups(groups)
        if (!isNew) {
          const d = await api<LessonDetail>('/lessons/' + id)
          if (!active) return
          populate(d)
        }
      } catch (e) {
        if (active) setError((e as Error).message)
      } finally {
        if (active) setLoading(false)
      }
    }
    run()
    return () => {
      active = false
    }
  }, [id, isNew])

  async function saveLesson() {
    setSaving(true)
    setSaveError(null)
    try {
      const body = JSON.stringify(form)
      if (isNew) {
        const created = await api<{ id: string }>('/lessons', { method: 'POST', body })
        toast.success('Lesson created')
        navigate('/lessons/' + created.id)
      } else {
        await api('/lessons/' + id, { method: 'PUT', body })
        await loadDetail()
        toast.success('Lesson saved')
      }
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  const [showSendBack, setShowSendBack] = useState(false)
  const [sendBackNote, setSendBackNote] = useState('')

  async function runAction(action: string, ok: string, body?: object) {
    setError(null)
    try {
      await api(`/lessons/${id}/${action}`, { method: 'POST', ...(body ? { body: JSON.stringify(body) } : {}) })
      await loadDetail()
      toast.success(ok)
    } catch (e) {
      setError((e as Error).message)
      toast.error((e as Error).message)
    }
  }

  async function confirmSendBack() {
    setShowSendBack(false)
    await runAction('send-back', 'Sent back to the author', { note: sendBackNote.trim() || null })
    setSendBackNote('')
  }

  async function toggleAssign(classGroupId: string, assigned: boolean) {
    setError(null)
    try {
      const action = assigned ? 'unassign-class' : 'assign-class'
      await api(`/lessons/${id}/${action}`, {
        method: 'POST',
        body: JSON.stringify({ classGroupId }),
      })
      setAssignedIds((prev) =>
        assigned ? prev.filter((x) => x !== classGroupId) : [...prev, classGroupId],
      )
    } catch (e) {
      setError((e as Error).message)
    }
  }

  async function removeQuestion(questionId: string) {
    setError(null)
    try {
      await api(`/lessons/${id}/questions/${questionId}`, { method: 'DELETE' })
      await loadDetail()
      toast.success('Question removed')
    } catch (e) {
      setError((e as Error).message)
      toast.error((e as Error).message)
    }
  }

  function setOption(index: number, patch: Partial<OptionDraft>) {
    setQOptions((prev) => prev.map((o, i) => (i === index ? { ...o, ...patch } : o)))
  }
  function markCorrect(index: number) {
    setQOptions((prev) => prev.map((o, i) => ({ ...o, isCorrect: i === index })))
  }
  function addOption() {
    setQOptions((prev) => [...prev, { optionText: '', isCorrect: false }])
  }

  async function addQuestion() {
    setQError(null)
    const options = qOptions
      .filter((o) => o.optionText.trim() !== '')
      .map((o) => ({ optionText: o.optionText, isCorrect: o.isCorrect }))
    if (options.length === 0) {
      setQError('Add at least one option.')
      return
    }
    if (!options.some((o) => o.isCorrect)) {
      setQError('Mark one option as correct.')
      return
    }
    setQSaving(true)
    try {
      const payload: QuizQuestionForm = {
        questionText: qText,
        questionType: 0,
        points: qPoints,
        options,
      }
      await api(`/lessons/${id}/questions`, { method: 'POST', body: JSON.stringify(payload) })
      setQText('')
      setQPoints(1)
      setQOptions([
        { optionText: '', isCorrect: false },
        { optionText: '', isCorrect: false },
      ])
      await loadDetail()
      toast.success('Question added')
    } catch (e) {
      setQError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setQSaving(false)
    }
  }

  // Permissions: an admin manages any lesson; a teacher may edit only lessons they authored.
  const isAuthor = !!detail && detail.authorUserId === user?.id
  const canEditContent = isNew || isAdmin || isAuthor
  const canManageClasses = isAdmin
  const status: LessonStatus = detail?.status ?? 0
  const canSubmit = !isAdmin && isAuthor && status === 0

  // A teacher can mark this lesson complete for any of their classes it's assigned to — right here
  // after teaching it. (classGroups is the teacher's assigned classes.)
  const completeClasses = (classGroups ?? [])
    .filter((c) => detail?.assignedClassGroupIds.includes(c.id))
    .map((c) => ({ classGroupId: c.id, name: c.name }))
  const canMarkComplete = !isNew && isTeacher && status === 2 && completeClasses.length > 0

  if (loading) return <Loading />

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {/* Top bar */}
      <div className="flex items-end justify-between gap-4">
        <div>
          <Link to="/lessons" className="text-sm font-medium text-accent hover:text-heading">
            ← Lessons
          </Link>
          <h1 className="mt-1 font-display text-3xl font-medium text-heading">
            {isNew ? 'New lesson' : detail?.title || 'Lesson'}
          </h1>
          {!isNew && detail && (
            <div className="mt-2 flex flex-wrap items-center gap-2">
              {statusPill(status)}
              {detail.lastCompletedAtUtc && <Pill tone="green">✅ Taught {formatDate(detail.lastCompletedAtUtc)}</Pill>}
              {detail.authorName && <span className="text-xs text-ink/50">by {detail.authorName}</span>}
            </div>
          )}
        </div>
        {!isNew && detail && (
          <div className="flex flex-wrap justify-end gap-2">
            {/* Full-screen presenter view to teach the lesson. */}
            <Button variant="gold" onClick={() => setShowTeach(true)}>
              ▶ Teach
            </Button>
            {/* Teacher: mark this lesson complete for their class after teaching it. */}
            {canMarkComplete && (
              <Button onClick={() => setShowComplete(true)}>✅ Mark as completed</Button>
            )}
            {/* Teacher: submit own draft for admin review */}
            {canSubmit && (
              <Button onClick={() => runAction('submit', 'Submitted for review')}>Submit for review</Button>
            )}
            {!isAdmin && isAuthor && status === 1 && (
              <span className="self-center text-sm text-ink/55">Awaiting admin review…</span>
            )}
            {/* Admin review actions */}
            {isAdmin && status === 1 && (
              <>
                <Button variant="outline" onClick={() => setShowSendBack(true)}>
                  Send back
                </Button>
                <Button onClick={() => runAction('publish', 'Lesson published')}>Publish</Button>
              </>
            )}
            {isAdmin && status === 0 && (
              <Button onClick={() => runAction('publish', 'Lesson published')}>Publish</Button>
            )}
            {isAdmin && status === 2 && (
              <Button variant="outline" onClick={() => runAction('unpublish', 'Lesson unpublished')}>
                Unpublish
              </Button>
            )}
          </div>
        )}
      </div>

      {/* Review note sent back by an admin — shown to the author. */}
      {!isNew && detail?.reviewNote && status === 0 && (
        <div className="rounded-xl border border-gold-soft/50 bg-gold-soft/10 px-4 py-3 text-sm text-gold-ink">
          <span className="font-semibold">Sent back for changes:</span> {detail.reviewNote}
        </div>
      )}

      {/* A teacher editing a published lesson re-triggers review. */}
      {!isNew && !isAdmin && isAuthor && status === 2 && (
        <div className="rounded-xl border border-cream-deep bg-cream/50 px-4 py-3 text-sm text-ink/70">
          This lesson is live. Saving any change will send it back for admin re-approval.
        </div>
      )}

      {error && <ErrorBanner message={error} />}

      {/* SECTION 1 — Lesson details */}
      <Card className="p-6">
        <h2 className="mb-4 font-display text-xl text-heading">Lesson details</h2>
        <form
          onSubmit={(e) => {
            e.preventDefault()
            saveLesson()
          }}
          className="space-y-4"
        >
          {saveError && <ErrorBanner message={saveError} />}
          <Field label="Title">
            <Input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              required
              disabled={!canEditContent}
            />
          </Field>
          <div className="grid grid-cols-2 gap-4">
            <Field label="Bible reference">
              <Input
                value={form.bibleReference}
                onChange={(e) => setForm({ ...form, bibleReference: e.target.value })}
                required
                disabled={!canEditContent}
              />
            </Field>
            <Field label="Age group">
              <Input
                value={form.ageGroup}
                onChange={(e) => setForm({ ...form, ageGroup: e.target.value })}
                required
                disabled={!canEditContent}
              />
            </Field>
          </div>
          <Field label="Theme">
            <Input
              value={form.theme ?? ''}
              onChange={(e) => setForm({ ...form, theme: e.target.value })}
              disabled={!canEditContent}
            />
          </Field>
          <Field label="Story content">
            <Textarea
              rows={5}
              value={form.storyContent}
              onChange={(e) => setForm({ ...form, storyContent: e.target.value })}
              required
              disabled={!canEditContent}
            />
          </Field>
          <Field label="Learning objective">
            <Textarea
              rows={2}
              value={form.learningObjective ?? ''}
              onChange={(e) => setForm({ ...form, learningObjective: e.target.value })}
              disabled={!canEditContent}
            />
          </Field>
          <Field label="Activity">
            <Textarea
              rows={2}
              value={form.activity ?? ''}
              onChange={(e) => setForm({ ...form, activity: e.target.value })}
              disabled={!canEditContent}
            />
          </Field>
          <Field label="Prayer">
            <Textarea
              rows={2}
              value={form.prayer ?? ''}
              onChange={(e) => setForm({ ...form, prayer: e.target.value })}
              disabled={!canEditContent}
            />
          </Field>

          <div className="rounded-xl border border-cream-deep bg-cream/40 p-4">
            <h3 className="mb-1 font-display text-lg text-heading">Memory verse</h3>
            <p className="mb-3 text-xs text-ink/50">
              Enter the verse text to add a memory verse — required before the lesson can be published.
            </p>
            <div className="space-y-4">
              <Field label="Verse text" hint="Type the verse here to save a memory verse for this lesson.">
                <Textarea
                  rows={2}
                  value={form.memoryVerse.verseText ?? ''}
                  onChange={(e) =>
                    setForm({ ...form, memoryVerse: { ...form.memoryVerse, verseText: e.target.value } })
                  }
                  disabled={!canEditContent}
                />
              </Field>
              <Field label="Bible reference" hint="Optional — defaults to the lesson's Bible reference above.">
                <Input
                  value={form.memoryVerse.bibleReference ?? ''}
                  placeholder={form.bibleReference || 'e.g. John 3:16'}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      memoryVerse: { ...form.memoryVerse, bibleReference: e.target.value },
                    })
                  }
                  disabled={!canEditContent}
                />
              </Field>
              <Field label="Short explanation">
                <Textarea
                  rows={2}
                  value={form.memoryVerse.shortExplanation ?? ''}
                  onChange={(e) =>
                    setForm({
                      ...form,
                      memoryVerse: { ...form.memoryVerse, shortExplanation: e.target.value },
                    })
                  }
                  disabled={!canEditContent}
                />
              </Field>
            </div>
          </div>

          {canEditContent && (
            <div className="flex justify-end pt-2">
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving…' : isNew ? 'Create lesson' : 'Save changes'}
              </Button>
            </div>
          )}
        </form>
      </Card>

      {/* SECTION 2 — Class group assignment */}
      {!isNew && (
        <Card className="p-6">
          <h2 className="mb-4 font-display text-xl text-heading">Class groups</h2>
          {classGroups && classGroups.length === 0 && (
            <p className="text-sm text-ink/50">No class groups available.</p>
          )}
          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
            {classGroups?.map((g) => {
              const assigned = assignedIds.includes(g.id)
              return (
                <label key={g.id} className="flex items-center gap-2 text-sm text-ink">
                  <Checkbox
                    checked={assigned}
                    disabled={!canManageClasses}
                    onChange={() => toggleAssign(g.id, assigned)}
                  />
                  <span>{g.name}</span>
                </label>
              )
            })}
          </div>
        </Card>
      )}

      {/* SECTION 3 — Quiz questions */}
      {!isNew && (
        <Card className="p-6">
          <h2 className="mb-4 font-display text-xl text-heading">Quiz questions</h2>

          {questions.length === 0 && (
            <p className="text-sm text-ink/50">No questions yet.</p>
          )}

          <div className="space-y-4">
            {questions.map((q) => (
              <div key={q.id} className="rounded-xl border border-cream-deep p-4">
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-2">
                    <span className="font-medium text-ink">{q.questionText}</span>
                    <Pill tone="gold">{q.points} pts</Pill>
                  </div>
                  {canEditContent && (
                    <Button variant="danger" onClick={() => removeQuestion(q.id)}>
                      Remove
                    </Button>
                  )}
                </div>
                <ul className="mt-3 space-y-1">
                  {q.options.map((o) => (
                    <li key={o.id} className="flex items-center gap-2 text-sm">
                      {o.isCorrect ? (
                        <span className="font-semibold text-accent">✓</span>
                      ) : (
                        <span className="text-ink/30">•</span>
                      )}
                      <span className={o.isCorrect ? 'text-heading' : 'text-ink/70'}>
                        {o.optionText}
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>

          {canEditContent && (
            <div className="mt-6 rounded-xl border border-cream-deep bg-cream/40 p-4">
              <h3 className="mb-3 font-display text-lg text-heading">Add question</h3>
              <form
                onSubmit={(e) => {
                  e.preventDefault()
                  addQuestion()
                }}
                className="space-y-4"
              >
                {qError && <ErrorBanner message={qError} />}
                <div className="grid grid-cols-3 gap-4">
                  <div className="col-span-2">
                    <Field label="Question text">
                      <Input value={qText} onChange={(e) => setQText(e.target.value)} required />
                    </Field>
                  </div>
                  <Field label="Points">
                    <Input
                      type="number"
                      min={1}
                      value={qPoints}
                      onChange={(e) => setQPoints(Number(e.target.value))}
                    />
                  </Field>
                </div>

                <div className="space-y-2">
                  <span className="block text-sm font-medium text-ink/70">Options</span>
                  {qOptions.map((o, i) => (
                    <div key={i} className="flex items-center gap-2">
                      <input
                        type="radio"
                        name="correct-option"
                        checked={o.isCorrect}
                        onChange={() => markCorrect(i)}
                        className="size-4 text-accent focus:ring-gold/40"
                        aria-label="Mark correct"
                      />
                      <Input
                        value={o.optionText}
                        onChange={(e) => setOption(i, { optionText: e.target.value })}
                        placeholder={`Option ${i + 1}`}
                      />
                    </div>
                  ))}
                  <Button type="button" variant="ghost" onClick={addOption}>
                    + Add option
                  </Button>
                </div>

                <div className="flex justify-end">
                  <Button type="submit" disabled={qSaving}>
                    {qSaving ? 'Adding…' : 'Add question'}
                  </Button>
                </div>
              </form>
            </div>
          )}
        </Card>
      )}

      <Modal open={showSendBack} onClose={() => setShowSendBack(false)} title="Send back to author">
        <div className="space-y-4">
          <p className="text-sm text-ink/70">
            Return this lesson to {detail?.authorName ?? 'the author'} as a draft with a note on what to change.
          </p>
          <Field label="Note (optional)">
            <Textarea
              rows={4}
              value={sendBackNote}
              placeholder="e.g. Please add an activity and a second quiz question."
              onChange={(e) => setSendBackNote(e.target.value)}
            />
          </Field>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setShowSendBack(false)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={confirmSendBack}>
              Send back
            </Button>
          </div>
        </div>
      </Modal>

      <MarkLessonCompleteModal
        open={showComplete}
        lesson={detail ? { id: detail.id, title: detail.title } : null}
        classes={completeClasses}
        onClose={() => setShowComplete(false)}
        onCompleted={loadDetail}
      />

      {showTeach && detail && (
        <TeachMode
          lessonId={detail.id}
          classes={canMarkComplete ? completeClasses : []}
          onClose={() => setShowTeach(false)}
          onCompleted={loadDetail}
        />
      )}
    </div>
  )
}
