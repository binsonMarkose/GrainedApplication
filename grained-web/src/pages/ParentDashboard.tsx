import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toast'
import { Avatar, AVATARS } from '../components/Avatar'
import { AnimatedLogo } from '../components/AnimatedLogo'
import { BadgeIcon } from '../components/BadgeIcon'
import { GrowthTree } from '../components/GrowthTree'
import { GrowthJourney } from '../components/GrowthJourney'
import { Button, EmptyState, ErrorBanner, Loading, Modal, Pill } from '../components/ui'
import type { ParentChild, ParentLesson, ParentLessonDetail, ParentLessonStatus, ParentWorkspace } from '../types'

function formatDate(iso: string | null) {
  if (!iso) return ''
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

// Remembered set of badge ids the parent has already seen, so only *newly* earned badges sparkle.
const SEEN_BADGES_KEY = 'grained.seenBadges'
function readSeenBadges(): Set<string> {
  try {
    return new Set(JSON.parse(localStorage.getItem(SEEN_BADGES_KEY) ?? '[]') as string[])
  } catch {
    return new Set()
  }
}

const STATUS_STYLE: Record<ParentLessonStatus, { border: string; badge: 'green' | 'gold' | 'gray'; label: string }> = {
  Completed: { border: 'border-l-leaf', badge: 'green', label: 'Completed' },
  Missed: { border: 'border-l-gold', badge: 'gold', label: 'Missed' },
  Upcoming: { border: 'border-l-cream-deep', badge: 'gray', label: 'Upcoming' },
}

// Clickable status chips (mirror the Pill tones) that both count and filter the lessons list.
const CHIP_TONE: Record<'green' | 'gold' | 'gray', string> = {
  green: 'bg-leaf-light/40 text-heading',
  gold: 'bg-gold-soft/25 text-gold-ink',
  gray: 'bg-cream-deep text-ink/50',
}
const CHIP_RING: Record<'green' | 'gold' | 'gray', string> = {
  green: 'ring-2 ring-leaf/70',
  gold: 'ring-2 ring-gold/70',
  gray: 'ring-2 ring-ink/25',
}

const LESSONS_PER_PAGE = 3

function LessonRow({ lesson, onOpen }: { lesson: ParentLesson; onOpen: () => void }) {
  const s = STATUS_STYLE[lesson.status]
  return (
    <button
      onClick={onOpen}
      className={`w-full rounded-xl border border-cream-deep border-l-4 ${s.border} bg-cream/40 p-4 text-left transition hover:bg-cream/70`}
    >
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div>
          <div className="font-display text-lg text-heading">{lesson.title}</div>
          <div className="mt-0.5 text-sm text-ink/60">{lesson.bibleReference}</div>
        </div>
        <div className="text-right">
          <Pill tone={s.badge}>{s.label}</Pill>
          {lesson.status === 'Completed' && lesson.completedAtUtc && (
            <div className="mt-1 text-xs text-ink/45">{formatDate(lesson.completedAtUtc)}</div>
          )}
        </div>
      </div>
      {lesson.memoryVerseText && (
        <div className="mt-3 rounded-lg bg-white/70 p-3">
          <div className="text-[0.7rem] font-semibold uppercase tracking-wide text-gold">
            Memory verse{lesson.memoryVerseReference ? ` · ${lesson.memoryVerseReference}` : ''}
          </div>
          <blockquote className="mt-1 font-display text-base italic leading-relaxed text-ink/80">
            &ldquo;{lesson.memoryVerseText}&rdquo;
          </blockquote>
        </div>
      )}
      <div className="mt-2 text-xs font-semibold text-accent/70">
        {lesson.status === 'Missed' ? 'Catch up at home →' : 'Read the lesson →'}
      </div>
    </button>
  )
}

export function ParentDashboard() {
  const { user } = useAuth()
  const toast = useToast()
  const [data, setData] = useState<ParentWorkspace | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Which child's detail card is on screen (parents with several kids switch between them instead
  // of scrolling one long stack). Defaults to the first child once data arrives.
  const [selectedChildId, setSelectedChildId] = useState<string | null>(null)

  const [avatarChild, setAvatarChild] = useState<ParentChild | null>(null)
  const [savingAvatar, setSavingAvatar] = useState(false)

  const [lesson, setLesson] = useState<ParentLessonDetail | null>(null)
  const [lessonLoading, setLessonLoading] = useState(false)

  // Badge ids to sparkle this session (newly earned since the parent last looked).
  const [sparkleIds, setSparkleIds] = useState<Set<string>>(new Set())

  const load = () => api<ParentWorkspace>('/parent/children').then(setData).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  // When data arrives, figure out which badges are new. On the very first visit we just record a
  // baseline (no sparkle for historical badges); after that, only freshly-earned badges sparkle.
  useEffect(() => {
    if (!data) return
    const firstVisit = localStorage.getItem(SEEN_BADGES_KEY) === null
    const seen = readSeenBadges()
    const allIds = data.children.flatMap((c) => c.badges.map((b) => b.badgeId))
    const fresh = firstVisit ? [] : allIds.filter((id) => !seen.has(id))
    setSparkleIds(new Set(fresh))
    localStorage.setItem(SEEN_BADGES_KEY, JSON.stringify([...new Set([...seen, ...allIds])]))
  }, [data])

  // Keep the selected child valid as data reloads (avatar change, etc.); default to the first child.
  useEffect(() => {
    if (!data || data.children.length === 0) return
    setSelectedChildId((cur) => (cur && data.children.some((c) => c.id === cur) ? cur : data.children[0].id))
  }, [data])

  const firstName = user?.fullName?.split(' ')[0] ?? ''
  const hasKids = !!data && data.children.length > 0
  const selectedChild = data?.children.find((c) => c.id === selectedChildId) ?? null
  const journeyKids =
    data?.children.map((c) => ({ id: c.id, name: c.firstName, avatarId: c.avatarId, stage: c.growth.stageIndex })) ?? []

  async function chooseAvatar(childId: string, avatarId: string) {
    setSavingAvatar(true)
    try {
      await api(`/parent/children/${childId}/avatar`, { method: 'POST', body: JSON.stringify({ avatarId }) })
      setAvatarChild(null)
      await load()
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setSavingAvatar(false)
    }
  }

  async function openLesson(lessonId: string) {
    setLessonLoading(true)
    setLesson(null)
    try {
      const detail = await api<ParentLessonDetail>(`/parent/lessons/${lessonId}`)
      setLesson(detail)
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setLessonLoading(false)
    }
  }

  return (
    <div className="mx-auto max-w-5xl">
      <div className="space-y-6">
      <section className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-grove to-leaf shadow-sm">
        <div className="pointer-events-none absolute -right-16 -top-24 size-72 rounded-full bg-gold-soft/10 blur-2xl" aria-hidden />
        <div className="relative grid gap-8 p-7 sm:p-9 lg:grid-cols-[1.1fr_1fr] lg:items-center">
          <div>
            <p className="text-[0.7rem] font-semibold uppercase tracking-[0.25em] text-gold-soft">Your family</p>
            <h1 className="mt-3 font-display text-4xl font-medium leading-tight text-oncream">
              Hello{firstName && `, ${firstName}`}
            </h1>
            <p className="mt-2 max-w-md text-oncream/75">
              Watch your children grow — lessons learned, memory verses to practise at home, and badges earned.
            </p>
          </div>

          <div className="flex items-center gap-5 rounded-2xl bg-cream/10 p-5 ring-1 ring-cream/15 backdrop-blur-sm">
            <div className="grid size-20 shrink-0 place-items-center rounded-2xl bg-cream shadow-inner">
              <AnimatedLogo className="w-14" />
            </div>
            <div>
              <blockquote className="font-display text-lg leading-relaxed text-oncream">
                &ldquo;Let the little children come to me.&rdquo;
              </blockquote>
              <cite className="mt-2 block text-xs font-semibold uppercase not-italic tracking-[0.2em] text-gold-soft">
                Matthew 19:14
              </cite>
            </div>
          </div>
        </div>
      </section>

      {hasKids && <GrowthJourney kids={journeyKids} />}

      {error && <ErrorBanner message={error} />}
      {!data && !error && <Loading label="Loading your children…" />}

      {data && data.children.length === 0 && (
        <EmptyState
          icon="👨‍👩‍👧"
          title="No children linked yet"
          hint="Ask your church to link your child to this account, then their progress will appear here."
        />
      )}

      {/* Child switcher — one clean card at a time; tap a face to jump between kids. */}
      {data && data.children.length > 1 && (
        <div className="-mx-1 flex gap-2 overflow-x-auto px-1 pb-1">
          {data.children.map((c) => {
            const active = c.id === selectedChildId
            return (
              <button
                key={c.id}
                onClick={() => setSelectedChildId(c.id)}
                aria-pressed={active}
                className={`flex shrink-0 items-center gap-2.5 rounded-full border px-3 py-1.5 transition ${
                  active
                    ? 'border-gold bg-gold-soft/15 shadow-sm'
                    : 'border-cream-deep bg-white hover:-translate-y-0.5 hover:bg-cream'
                }`}
              >
                <Avatar avatarId={c.avatarId} name={`${c.firstName} ${c.lastName}`} size={30} ring />
                <span className={`text-sm font-semibold ${active ? 'text-heading' : 'text-ink/70'}`}>{c.firstName}</span>
              </button>
            )
          })}
        </div>
      )}

      {selectedChild && (
        <ChildCard
          key={selectedChild.id}
          child={selectedChild}
          sparkleIds={sparkleIds}
          onChangeAvatar={() => setAvatarChild(selectedChild)}
          onOpenLesson={openLesson}
        />
      )}
      </div>

      {/* Avatar picker */}
      <Modal open={avatarChild !== null} onClose={() => setAvatarChild(null)} title="Choose an avatar" wide>
        {avatarChild && (
          <div className="space-y-4">
            <p className="text-sm text-ink/70">Pick a fun avatar for {avatarChild.firstName}.</p>
            <div className="grid grid-cols-4 gap-3 sm:grid-cols-6">
              {AVATARS.map((a) => {
                const selected = avatarChild.avatarId === a.id
                return (
                  <button
                    key={a.id}
                    disabled={savingAvatar}
                    onClick={() => chooseAvatar(avatarChild.id, a.id)}
                    className={`flex flex-col items-center gap-1 rounded-xl border p-2 transition hover:-translate-y-0.5 ${
                      selected ? 'border-gold bg-gold-soft/15' : 'border-cream-deep hover:bg-cream'
                    }`}
                    title={a.label}
                  >
                    <Avatar avatarId={a.id} name={a.label} size={44} />
                    <span className="text-[0.65rem] text-ink/55">{a.label}</span>
                  </button>
                )
              })}
            </div>
          </div>
        )}
      </Modal>

      {/* Lesson detail (revisit at home) */}
      <Modal open={lessonLoading || lesson !== null} onClose={() => setLesson(null)} title={lesson?.title ?? 'Lesson'} wide>
        {lessonLoading && <Loading label="Opening lesson…" />}
        {lesson && (
          <div className="space-y-4">
            <div className="flex flex-wrap gap-2">
              <Pill tone="green">{lesson.bibleReference}</Pill>
              {lesson.theme && <Pill tone="gold">{lesson.theme}</Pill>}
            </div>

            {lesson.memoryVerseText && (
              <div className="rounded-xl bg-cream/60 p-4">
                <div className="text-[0.7rem] font-semibold uppercase tracking-wide text-gold">
                  Memory verse{lesson.memoryVerseReference ? ` · ${lesson.memoryVerseReference}` : ''}
                </div>
                <blockquote className="mt-1 font-display text-lg italic leading-relaxed text-ink/85">
                  &ldquo;{lesson.memoryVerseText}&rdquo;
                </blockquote>
              </div>
            )}

            <Section title="The story">{lesson.storyContent}</Section>
            {lesson.learningObjective && <Section title="What we're learning">{lesson.learningObjective}</Section>}
            {lesson.activity && <Section title="Activity to try at home">{lesson.activity}</Section>}
            {lesson.prayer && <Section title="Prayer">{lesson.prayer}</Section>}

            <div className="flex justify-end pt-2">
              <Button onClick={() => setLesson(null)}>Done</Button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  )
}

function Section({ title, children }: { title: string; children: string }) {
  return (
    <div>
      <h4 className="mb-1 text-sm font-semibold uppercase tracking-wide text-ink/55">{title}</h4>
      <p className="whitespace-pre-wrap text-sm leading-relaxed text-ink/80">{children}</p>
    </div>
  )
}

function ChildCard({
  child,
  sparkleIds,
  onChangeAvatar,
  onOpenLesson,
}: {
  child: ParentChild
  sparkleIds: Set<string>
  onChangeAvatar: () => void
  onOpenLesson: (lessonId: string) => void
}) {
  const g = child.growth
  const span = g.nextStageAt != null ? g.nextStageAt - g.stageFloor : 1
  const into = g.nextStageAt != null ? g.growthPoints - g.stageFloor : 1
  const pct = g.nextStageAt != null ? Math.max(4, Math.min(100, Math.round((into / span) * 100))) : 100

  // Lessons: filter by status via the count chips, then page through 3 at a time so the card
  // never grows unbounded as the class racks up lessons.
  const [filter, setFilter] = useState<'All' | ParentLessonStatus>('All')
  const [page, setPage] = useState(0)
  const chips: { key: ParentLessonStatus; tone: 'green' | 'gold' | 'gray'; count: number }[] = [
    { key: 'Completed', tone: 'green', count: child.completedCount },
    { key: 'Missed', tone: 'gold', count: child.missedCount },
    { key: 'Upcoming', tone: 'gray', count: child.upcomingCount },
  ]
  const filtered = filter === 'All' ? child.lessons : child.lessons.filter((l) => l.status === filter)
  const pageCount = Math.max(1, Math.ceil(filtered.length / LESSONS_PER_PAGE))
  const curPage = Math.min(page, pageCount - 1)
  const visibleLessons = filtered.slice(curPage * LESSONS_PER_PAGE, curPage * LESSONS_PER_PAGE + LESSONS_PER_PAGE)
  const pickFilter = (key: ParentLessonStatus) => {
    setFilter((cur) => (cur === key ? 'All' : key))
    setPage(0)
  }

  return (
    <section className="rounded-2xl border border-cream-deep bg-white shadow-sm">
      <div className="border-b border-cream-deep p-5">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-4">
            <button onClick={onChangeAvatar} className="group relative grained-wiggle" title="Change avatar">
              <Avatar avatarId={child.avatarId} name={`${child.firstName} ${child.lastName}`} size={64} ring />
              <span className="absolute -bottom-1 -right-1 grid size-6 place-items-center rounded-full border border-cream-deep bg-white text-xs shadow-sm transition group-hover:bg-cream">
                ✏️
              </span>
            </button>
            <div>
              <h2 className="font-display text-2xl text-heading">
                {child.firstName} {child.lastName}
              </h2>
              <p className="text-sm text-ink/55">
                {child.classGroupName} · Age {child.age}
              </p>
            </div>
          </div>
          <div className="flex flex-wrap gap-2">
            {chips.map((c) => {
              const active = filter === c.key
              return (
                <button
                  key={c.key}
                  onClick={() => pickFilter(c.key)}
                  aria-pressed={active}
                  title={active ? 'Show all lessons' : `Show ${c.key.toLowerCase()} lessons`}
                  className={`rounded-full px-2.5 py-0.5 text-xs font-semibold transition hover:-translate-y-0.5 ${CHIP_TONE[c.tone]} ${
                    active ? CHIP_RING[c.tone] : ''
                  }`}
                >
                  {c.count} {c.key.toLowerCase()}
                </button>
              )
            })}
          </div>
        </div>
      </div>

      <div className="grid gap-6 p-5 lg:grid-cols-[minmax(0,300px)_1fr]">
        {/* Growth tree + badges */}
        <div>
          <div className="rounded-2xl bg-gradient-to-b from-[#EAF3E6] to-cream p-3">
            <GrowthTree stageIndex={g.stageIndex} badges={child.badges} uid={child.id} sparkleBadgeIds={sparkleIds} />
            <div className="mt-1 flex flex-col items-center gap-2 px-2 pb-1">
              <span className="inline-flex items-center gap-1.5 rounded-full bg-leaf-light/40 px-3 py-1 text-sm font-semibold text-heading">
                <span aria-hidden className="text-base">{g.stageEmoji}</span> {g.stageName}
                <span className="font-normal text-heading/60">· {g.seasonName}</span>
              </span>
              <div className="w-full">
                <div className="mb-1 flex items-center justify-between text-xs text-ink/50">
                  <span>{g.growthPoints} pts</span>
                  <span>{g.nextStageAt != null ? `${g.nextStageAt - g.growthPoints} to ${g.nextStageName}` : 'Fully grown 🎉'}</span>
                </div>
                <div className="h-2.5 overflow-hidden rounded-full bg-cream-deep">
                  <div className="h-full rounded-full bg-gradient-to-r from-leaf to-grove" style={{ width: `${pct}%` }} />
                </div>
              </div>
              <div className="flex flex-wrap justify-center gap-x-3 gap-y-1 text-[0.72rem] text-ink/60">
                <span>📖 {g.lessonsCompleted} lessons</span>
                <span>✅ {g.sundaysAttended} Sundays</span>
                <span>📜 {g.versesLearned} verses</span>
                <span>🏅 {g.badgeCount} badges</span>
                {g.achievementCount > 0 && <span>🏆 {g.achievementCount} achievements</span>}
              </div>
            </div>
          </div>

          {/* Forest cap — trees this child has collected; a new one lands here when a season ends. */}
          <div className="mt-4 flex items-center gap-3 rounded-2xl border border-leaf-light/50 bg-gradient-to-b from-[#EAF3E6] to-cream p-3 dark:border-oncream/5">
            <div className="flex -space-x-1.5 text-xl">
              {g.forest.length > 0 ? (
                Array.from({ length: Math.min(g.forest.length, 5) }).map((_, i) => <span key={i}>🌳</span>)
              ) : (
                <span className="text-2xl opacity-40">🌲</span>
              )}
            </div>
            <div className="text-sm leading-tight">
              <div className="font-semibold text-heading">{child.firstName}&rsquo;s forest</div>
              <div className="text-ink/55">
                {g.forest.length === 0
                  ? 'Finish this season to plant a tree'
                  : `${g.forest.length} tree${g.forest.length === 1 ? '' : 's'} collected`}
              </div>
            </div>
          </div>

          <h3 className="mb-2 mt-4 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink/55">
            <span aria-hidden>🏅</span> Badges earned
          </h3>
          {child.badges.length === 0 ? (
            <p className="rounded-xl border border-dashed border-cream-deep px-4 py-4 text-center text-sm text-ink/45">
              No badges yet — they&rsquo;re coming!
            </p>
          ) : (
            <div className="flex flex-wrap gap-3">
              {child.badges.map((b) => (
                <div key={b.badgeId} className="flex w-20 flex-col items-center gap-1 text-center" title={formatDate(b.awardedAtUtc)}>
                  <div className="relative">
                    <BadgeIcon icon={b.iconName} size={44} />
                    {b.count > 1 && (
                      <span className="absolute -right-1 -top-1 grid min-w-[1.15rem] place-items-center rounded-full bg-grove px-1 text-[0.65rem] font-bold text-oncream ring-2 ring-white">
                        ×{b.count}
                      </span>
                    )}
                  </div>
                  <span className="text-xs font-medium text-ink/70">{b.name}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Lessons */}
        <div>
          <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
            <h3 className="flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink/55">
              <span aria-hidden>📖</span> Lessons
              {filter !== 'All' && <span className="font-medium normal-case text-ink/45">· {filter.toLowerCase()}</span>}
            </h3>
            {filter !== 'All' && (
              <button
                onClick={() => {
                  setFilter('All')
                  setPage(0)
                }}
                className="text-xs font-semibold text-accent hover:underline"
              >
                Show all
              </button>
            )}
          </div>
          {child.lessons.length === 0 ? (
            <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
              No lessons in this class yet.
            </p>
          ) : filtered.length === 0 ? (
            <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
              No {filter.toLowerCase()} lessons.
            </p>
          ) : (
            <>
              <div className="space-y-3">
                {visibleLessons.map((l) => (
                  <LessonRow key={l.id} lesson={l} onOpen={() => onOpenLesson(l.id)} />
                ))}
              </div>
              {pageCount > 1 && (
                <div className="mt-4 flex items-center justify-between gap-2">
                  <button
                    onClick={() => setPage(curPage - 1)}
                    disabled={curPage === 0}
                    className="rounded-lg border border-cream-deep px-3 py-1.5 text-xs font-semibold text-ink/70 transition enabled:hover:bg-cream disabled:opacity-40"
                  >
                    ← Prev
                  </button>
                  <span className="text-xs text-ink/50">
                    Page {curPage + 1} of {pageCount} · {filtered.length} lesson{filtered.length === 1 ? '' : 's'}
                  </span>
                  <button
                    onClick={() => setPage(curPage + 1)}
                    disabled={curPage >= pageCount - 1}
                    className="rounded-lg border border-cream-deep px-3 py-1.5 text-xs font-semibold text-ink/70 transition enabled:hover:bg-cream disabled:opacity-40"
                  >
                    Next →
                  </button>
                </div>
              )}
            </>
          )}
        </div>
      </div>

      {/* Forest — trees from past seasons */}
      {g.forest.length > 0 && (
        <div className="border-t border-cream-deep px-5 py-4">
          <h3 className="mb-2 flex items-center gap-2 text-sm font-semibold uppercase tracking-wide text-ink/55">
            <span aria-hidden>🌲</span> Past seasons
          </h3>
          <div className="flex flex-wrap gap-2.5">
            {g.forest.map((f) => (
              <div key={f.seasonName} className="flex items-center gap-2.5 rounded-xl border border-cream-deep bg-cream/40 px-3 py-2">
                <span className="text-2xl" aria-hidden>{f.stageEmoji}</span>
                <div>
                  <div className="text-sm font-medium text-ink">{f.seasonName}</div>
                  <div className="text-xs text-ink/50">
                    {f.stageName} · {f.growthPoints} pts
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </section>
  )
}
