import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { AnimatedLogo } from '../components/AnimatedLogo'
import type { DashboardSummary } from '../types'

function formatDate(iso: string) {
  const d = new Date(iso)
  return d.toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
}

function initials(name: string) {
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const last = parts.length > 1 ? parts[parts.length - 1][0] : ''
  return (first + last).toUpperCase() || '·'
}

function Avatar({ name }: { name: string }) {
  return (
    <span className="grid size-9 shrink-0 place-items-center rounded-full bg-leaf-light/40 text-xs font-semibold text-heading">
      {initials(name)}
    </span>
  )
}

type Accent = 'grove' | 'gold' | 'leaf' | 'ink'
const ACCENT_BAR: Record<Accent, string> = {
  grove: 'from-grove to-grove-deep',
  gold: 'from-gold to-gold-soft',
  leaf: 'from-leaf to-grove',
  ink: 'from-ink to-grove-deep',
}

function StatTile({
  label,
  value,
  icon,
  accent,
  to,
}: {
  label: string
  value: number
  icon: string
  accent: Accent
  to: string
}) {
  return (
    <Link
      to={to}
      className="group relative block overflow-hidden rounded-2xl border border-cream-deep bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md"
    >
      <div className={`absolute inset-x-0 top-0 h-1 bg-gradient-to-r ${ACCENT_BAR[accent]}`} />
      <div className="flex items-start justify-between">
        <div>
          <div className="font-display text-4xl font-medium leading-none text-heading">{value}</div>
          <div className="mt-2 text-sm font-medium text-ink/55">{label}</div>
        </div>
        <span className="grid size-11 place-items-center rounded-xl bg-cream text-xl">{icon}</span>
      </div>
      <div className="mt-3 flex items-center gap-1 text-xs font-semibold text-accent/0 transition group-hover:text-accent/70">
        View <span aria-hidden>→</span>
      </div>
    </Link>
  )
}

function ActivityPanel({
  title,
  icon,
  empty,
  emptyHint,
  children,
}: {
  title: string
  icon: string
  empty: boolean
  emptyHint: string
  children: React.ReactNode
}) {
  return (
    <section className="flex flex-col rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
      <div className="flex items-center gap-2">
        <span className="grid size-8 place-items-center rounded-lg bg-cream text-base">{icon}</span>
        <h2 className="font-display text-lg text-heading">{title}</h2>
      </div>
      {empty ? (
        <div className="mt-6 flex flex-1 flex-col items-center justify-center rounded-xl border border-dashed border-cream-deep py-8 text-center">
          <p className="text-sm text-ink/45">{emptyHint}</p>
        </div>
      ) : (
        <div className="mt-4 divide-y divide-cream-deep/70">{children}</div>
      )}
    </section>
  )
}

export function Dashboard() {
  const { user } = useAuth()
  const [data, setData] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<DashboardSummary>('/dashboard').then(setData).catch((e) => setError(e.message))
  }, [])

  const firstName = user?.fullName?.split(' ')[0] ?? ''
  const isAdmin = user?.roles?.includes('ChurchAdmin') ?? false

  const quickActions = isAdmin
    ? [
        { label: 'Take attendance', icon: '✅', to: '/attendance' },
        { label: 'New lesson', icon: '📖', to: '/lessons/new' },
        { label: 'Add children', icon: '👧', to: '/children' },
        { label: 'View reports', icon: '📊', to: '/reports' },
      ]
    : [
        { label: 'Take attendance', icon: '✅', to: '/attendance' },
        { label: 'Children', icon: '👧', to: '/children' },
        { label: 'Lessons', icon: '📖', to: '/lessons' },
      ]

  return (
    <div className="mx-auto max-w-6xl space-y-6">
      {/* Hero — greeting and the founding verse share one band; the mark draws itself in */}
      <section className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-grove to-leaf shadow-sm">
        <div
          className="pointer-events-none absolute -right-16 -top-24 size-72 rounded-full bg-gold-soft/10 blur-2xl"
          aria-hidden
        />
        <div className="relative grid gap-8 p-7 sm:p-9 lg:grid-cols-[1.1fr_1fr] lg:items-center">
          <div>
            <p className="text-[0.7rem] font-semibold uppercase tracking-[0.25em] text-gold-soft">
              Your ministry at a glance
            </p>
            <h1 className="mt-3 font-display text-4xl font-medium leading-tight text-oncream">
              Welcome back{firstName && `, ${firstName}`}
            </h1>
            <p className="mt-2 max-w-md text-oncream/75">
              Here&rsquo;s how the children in your care are growing this season.
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

      {/* Quick actions */}
      <div className="flex flex-wrap gap-2">
        {quickActions.map((a) => (
          <Link
            key={a.to}
            to={a.to}
            className="inline-flex items-center gap-2 rounded-xl border border-cream-deep bg-white px-3.5 py-2 text-sm font-semibold text-accent shadow-sm transition hover:-translate-y-0.5 hover:bg-cream hover:shadow-md"
          >
            <span aria-hidden>{a.icon}</span>
            {a.label}
          </Link>
        ))}
      </div>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      {!data && !error && (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-32 animate-pulse rounded-2xl border border-cream-deep bg-white/60" />
          ))}
        </div>
      )}

      {data && (
        <>
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatTile label="Children" value={data.totalChildren} icon="🧒" accent="grove" to="/children" />
            <StatTile label="Teachers" value={data.totalTeachers} icon="🍎" accent="gold" to="/teachers" />
            <StatTile label="Class groups" value={data.totalClasses} icon="👧" accent="leaf" to="/class-groups" />
            <StatTile label="Published lessons" value={data.publishedLessons} icon="📖" accent="ink" to="/lessons" />
          </div>

          <div className="grid gap-4 lg:grid-cols-2">
            <ActivityPanel
              title="Recent attendance"
              icon="✅"
              empty={data.recentAttendance.length === 0}
              emptyHint="No attendance recorded yet."
            >
              {data.recentAttendance.map((a, i) => (
                <div key={i} className="flex items-center gap-3 py-2.5 first:pt-0">
                  <Avatar name={a.childName} />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-sm font-medium text-ink">{a.childName}</div>
                    <div className="truncate text-xs text-ink/50">
                      {a.classGroupName} · {formatDate(a.attendanceDate)}
                    </div>
                  </div>
                  <span
                    className={[
                      'rounded-full px-2.5 py-0.5 text-xs font-semibold',
                      a.isPresent ? 'bg-leaf-light/40 text-heading' : 'bg-cream-deep text-ink/50',
                    ].join(' ')}
                  >
                    {a.isPresent ? 'Present' : 'Absent'}
                  </span>
                </div>
              ))}
            </ActivityPanel>

            <ActivityPanel
              title="Recent lesson completions"
              icon="📖"
              empty={data.recentLessonCompletions.length === 0}
              emptyHint="No lessons completed yet."
            >
              {data.recentLessonCompletions.map((c, i) => (
                <div key={i} className="flex items-center gap-3 py-2.5 first:pt-0">
                  <Avatar name={c.childName} />
                  <div className="min-w-0 flex-1">
                    <div className="truncate text-sm font-medium text-ink">{c.childName}</div>
                    <div className="truncate text-xs text-ink/50">{c.lessonTitle}</div>
                  </div>
                  <span className="shrink-0 text-xs text-ink/45">{formatDate(c.completedAtUtc)}</span>
                </div>
              ))}
            </ActivityPanel>
          </div>
        </>
      )}
    </div>
  )
}
