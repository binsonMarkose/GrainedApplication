import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toast'
import { Avatar } from '../components/Avatar'
import { AnimatedLogo } from '../components/AnimatedLogo'
import { Button, EmptyState, ErrorBanner, Field, Loading, Modal, Pill, Select } from '../components/ui'
import { MarkLessonCompleteModal } from '../components/MarkLessonCompleteModal'
import { TeachMode } from '../components/TeachMode'
import type {
  TeacherBadge,
  TeacherWorkspace,
  TeacherWorkspaceChild,
  TeacherWorkspaceClass,
  TeacherWorkspaceLesson,
} from '../types'

type CompleteTarget = { lesson: TeacherWorkspaceLesson; cls: TeacherWorkspaceClass }
type AwardTarget = { child: TeacherWorkspaceChild; cls: TeacherWorkspaceClass }

export function TeacherDashboard() {
  const { user } = useAuth()
  const toast = useToast()
  const [data, setData] = useState<TeacherWorkspace | null>(null)
  const [badges, setBadges] = useState<TeacherBadge[]>([])
  const [error, setError] = useState<string | null>(null)

  // Mark-lesson-complete modal — the shared component handles the roster + verse ticks.
  const [completeTarget, setCompleteTarget] = useState<CompleteTarget | null>(null)
  // Full-screen teach mode for a lesson in a class.
  const [teachTarget, setTeachTarget] = useState<CompleteTarget | null>(null)

  // Award-badge modal
  const [awardTarget, setAwardTarget] = useState<AwardTarget | null>(null)
  const [awardBadgeId, setAwardBadgeId] = useState('')
  const [awarding, setAwarding] = useState(false)

  const load = () => api<TeacherWorkspace>('/teacher/workspace').then(setData).catch((e) => setError(e.message))

  useEffect(() => {
    load()
    api<TeacherBadge[]>('/teacher/badges').then(setBadges).catch(() => setBadges([]))
  }, [])

  const firstName = user?.fullName?.split(' ')[0] ?? ''

  function openAward(child: TeacherWorkspaceChild, cls: TeacherWorkspaceClass) {
    setAwardBadgeId(badges[0]?.id ?? '')
    setAwardTarget({ child, cls })
  }

  async function submitAward() {
    if (!awardTarget || !awardBadgeId) return
    setAwarding(true)
    try {
      await api(`/teacher/children/${awardTarget.child.id}/badges`, {
        method: 'POST',
        body: JSON.stringify({ badgeId: awardBadgeId }),
      })
      const badge = badges.find((b) => b.id === awardBadgeId)
      setAwardTarget(null)
      await load()
      toast.success(`Awarded “${badge?.name}” to ${awardTarget.child.firstName}`)
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setAwarding(false)
    }
  }

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      {/* Hero */}
      <section className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-grove to-leaf shadow-sm">
        <div
          className="pointer-events-none absolute -right-16 -top-24 size-72 rounded-full bg-gold-soft/10 blur-2xl"
          aria-hidden
        />
        <div className="relative grid gap-8 p-7 sm:p-9 lg:grid-cols-[1.1fr_1fr] lg:items-center">
          <div>
            <p className="text-[0.7rem] font-semibold uppercase tracking-[0.25em] text-gold-soft">Your classroom</p>
            <h1 className="mt-3 font-display text-4xl font-medium leading-tight text-oncream">
              Welcome{firstName && `, ${firstName}`}
            </h1>
            <p className="mt-2 max-w-md text-oncream/75">
              Mark lessons complete for the children present, and celebrate great work with badges.
            </p>
          </div>

          <div className="flex items-center gap-5 rounded-2xl bg-cream/10 p-5 ring-1 ring-cream/15 backdrop-blur-sm">
            <div className="grid size-20 shrink-0 place-items-center rounded-2xl bg-cream shadow-inner">
              <AnimatedLogo className="w-14" />
            </div>
            <div>
              <blockquote className="font-display text-lg leading-relaxed text-oncream">
                &ldquo;Train up a child in the way he should go.&rdquo;
              </blockquote>
              <cite className="mt-2 block text-xs font-semibold uppercase not-italic tracking-[0.2em] text-gold-soft">
                Proverbs 22:6
              </cite>
            </div>
          </div>
        </div>
      </section>

      {error && <ErrorBanner message={error} />}
      {!data && !error && <Loading label="Loading your classes…" />}

      {data && data.classes.length === 0 && (
        <EmptyState
          icon="🍎"
          title="No class assigned yet"
          hint="Your church admin hasn't assigned you to a class group. Once they do, it'll appear here."
        />
      )}

      {data &&
        data.classes.map((c) => (
          <section key={c.classGroupId} className="rounded-2xl border border-cream-deep bg-white shadow-sm">
            {/* Class header */}
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-cream-deep p-5">
              <div className="flex flex-wrap items-center gap-3">
                <h2 className="font-display text-2xl text-heading">{c.name}</h2>
                <Pill tone="gold">
                  Ages {c.minAge}–{c.maxAge}
                </Pill>
                <Pill tone="green">
                  {c.children.length} {c.children.length === 1 ? 'child' : 'children'}
                </Pill>
              </div>
              <Link
                to={`/attendance?classGroupId=${c.classGroupId}`}
                className="inline-flex items-center gap-2 rounded-xl bg-grove px-4 py-2 text-sm font-semibold text-oncream transition hover:bg-grove-deep"
              >
                ✅ Mark attendance
              </Link>
            </div>

            <div className="grid gap-6 p-5 lg:grid-cols-2">
              {/* Lessons */}
              <div>
                <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink/55">
                  <span aria-hidden>📖</span> Lessons
                </h3>
                {c.lessons.length === 0 ? (
                  <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
                    No published lessons assigned to this class yet.
                  </p>
                ) : (
                  <div className="space-y-3">
                    {c.lessons.map((l) => {
                      const started = l.completedCount > 0
                      const done = started && l.completedCount >= c.children.length
                      const shade = done
                        ? 'border-leaf/40 bg-leaf-light/15'
                        : started
                          ? 'border-gold-soft/50 bg-gold-soft/10'
                          : 'border-cream-deep bg-cream/40'
                      return (
                        <div key={l.id} className={`rounded-xl border p-4 ${shade}`}>
                          <div className="flex items-start justify-between gap-3">
                            <div>
                              <div className="font-display text-lg text-heading">{l.title}</div>
                              <div className="mt-0.5 text-sm text-ink/60">{l.bibleReference}</div>
                            </div>
                            <div className="flex shrink-0 flex-col gap-2">
                              <Button variant="gold" onClick={() => setTeachTarget({ lesson: l, cls: c })}>
                                ▶ Teach
                              </Button>
                              {done ? (
                                <Button variant="outline" disabled title="Every child has this lesson">
                                  ✓ Completed
                                </Button>
                              ) : (
                                <Button variant="outline" onClick={() => setCompleteTarget({ lesson: l, cls: c })}>
                                  Mark completed
                                </Button>
                              )}
                            </div>
                          </div>
                          <div className="mt-2 flex flex-wrap items-center gap-2">
                            {l.theme && <Pill tone="gray">{l.theme}</Pill>}
                            {l.memoryVerseReference && <Pill tone="green">Memory verse · {l.memoryVerseReference}</Pill>}
                            <Pill tone={done ? 'green' : started ? 'gold' : 'gray'}>
                              {l.completedCount}/{c.children.length} completed
                            </Pill>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                )}
              </div>

              {/* Children */}
              <div>
                <h3 className="mb-3 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink/55">
                  <span aria-hidden>🧒</span> Children
                </h3>
                {c.children.length === 0 ? (
                  <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
                    No children in this class yet.
                  </p>
                ) : (
                  <div className="space-y-2">
                    {c.children.map((child) => (
                      <div
                        key={child.id}
                        className="flex items-center gap-3 rounded-xl border border-cream-deep bg-cream/50 px-3 py-2"
                      >
                        <Avatar avatarId={child.avatarId} name={`${child.firstName} ${child.lastName}`} size={36} />
                        <div className="min-w-0 flex-1">
                          <div className="truncate text-sm font-medium text-ink">
                            {child.firstName} {child.lastName}
                            <span className="ml-1 font-normal text-ink/45">· Age {child.age}</span>
                          </div>
                          {child.badges.length > 0 && (
                            <div className="mt-1 flex flex-wrap gap-1">
                              {child.badges.map((b) => (
                                <span
                                  key={b.badgeId}
                                  className="inline-flex items-center gap-1 rounded-full bg-gold-soft/25 px-2 py-0.5 text-[0.7rem] font-semibold text-gold-ink"
                                  title={b.name}
                                >
                                  {b.iconName ? `${b.iconName} ` : '🏅 '}
                                  {b.name}
                                  {b.count > 1 && <span className="text-gold-ink/70">×{b.count}</span>}
                                </span>
                              ))}
                            </div>
                          )}
                        </div>
                        <Button variant="ghost" onClick={() => openAward(child, c)}>
                          🏅 Award
                        </Button>
                      </div>
                    ))}
                  </div>
                )}
              </div>
            </div>
          </section>
        ))}

      {/* Mark-lesson-complete modal (shared with the Lessons page) */}
      <MarkLessonCompleteModal
        open={completeTarget !== null}
        lesson={completeTarget ? { id: completeTarget.lesson.id, title: completeTarget.lesson.title } : null}
        classes={completeTarget ? [{ classGroupId: completeTarget.cls.classGroupId, name: completeTarget.cls.name }] : []}
        onClose={() => setCompleteTarget(null)}
        onCompleted={load}
      />

      {teachTarget && (
        <TeachMode
          lessonId={teachTarget.lesson.id}
          classes={[{ classGroupId: teachTarget.cls.classGroupId, name: teachTarget.cls.name }]}
          onClose={() => setTeachTarget(null)}
          onCompleted={load}
        />
      )}

      {/* Award-badge modal */}
      <Modal open={awardTarget !== null} onClose={() => setAwardTarget(null)} title="Award a badge">
        {awardTarget && (
          <div className="space-y-4">
            <p className="text-sm text-ink/70">
              Award a badge to <span className="font-medium">{awardTarget.child.firstName} {awardTarget.child.lastName}</span>.
            </p>
            {badges.length === 0 ? (
              <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
                No badges available yet. Ask your church admin to create some.
              </p>
            ) : (
              <Field label="Badge">
                <Select value={awardBadgeId} onChange={(e) => setAwardBadgeId(e.target.value)}>
                  {badges.map((b) => (
                    <option key={b.id} value={b.id}>
                      {b.iconName ? `${b.iconName} ` : ''}
                      {b.name}
                    </option>
                  ))}
                </Select>
              </Field>
            )}
            <div className="flex justify-end gap-2 pt-2">
              <Button variant="ghost" onClick={() => setAwardTarget(null)}>
                Cancel
              </Button>
              <Button onClick={submitAward} disabled={awarding || badges.length === 0 || !awardBadgeId}>
                {awarding ? 'Awarding…' : 'Award badge'}
              </Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}
