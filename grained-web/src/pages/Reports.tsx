import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import type {
  AttendanceReportRow,
  ChildBadgeReportRow,
  ChildProgressReportRow,
  ClassProgressReportRow,
  LessonCompletionReportRow,
} from '../types'
import { percent, toDateInput } from '../lib/format'
import { Avatar } from '../components/Avatar'
import { BadgeIcon } from '../components/BadgeIcon'
import {
  Button,
  EmptyState,
  ErrorBanner,
  Input,
  Loading,
  Modal,
  PageHeader,
  Pill,
  Select,
  Table,
  Td,
  Th,
} from '../components/ui'

function reportDate(iso: string) {
  return new Date(iso).toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

type SortKey = 'name' | 'class' | 'stage' | 'points' | 'lessons' | 'verses' | 'sundays' | 'badges' | 'achv'
const NUMERIC_SORT = new Set<SortKey>(['stage', 'points', 'lessons', 'verses', 'sundays', 'badges', 'achv'])
function sortValue(r: ChildProgressReportRow, key: SortKey): number | string {
  switch (key) {
    case 'name':
      return r.childName
    case 'class':
      return r.classGroupName
    case 'stage':
      return r.stageIndex
    case 'points':
      return r.growthPoints
    case 'lessons':
      return r.lessonsCompleted
    case 'verses':
      return r.versesLearned
    case 'sundays':
      return r.sundaysAttended
    case 'badges':
      return r.badgeCount
    case 'achv':
      return r.achievementCount
  }
}

function downloadChildProgressCsv(rows: ChildProgressReportRow[]) {
  const cell = (v: string | number) => `"${String(v).replace(/"/g, '""')}"`
  const header = ['Child', 'Class', 'Stage', 'Points', 'Lessons', 'Verses', 'Sundays', 'Badges', 'Achievements']
  const lines = [header.join(',')]
  for (const r of rows) {
    lines.push(
      [r.childName, r.classGroupName, r.stageName, r.growthPoints, r.lessonsCompleted, r.versesLearned, r.sundaysAttended, r.badgeCount, r.achievementCount]
        .map(cell)
        .join(','),
    )
  }
  const blob = new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = `child-progress-${new Date().toISOString().slice(0, 10)}.csv`
  a.click()
  URL.revokeObjectURL(url)
}

function StatTile({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-cream-deep bg-white p-4 shadow-sm">
      <div className="font-display text-3xl font-medium text-heading">{value}</div>
      <div className="mt-0.5 text-sm text-ink/55">{label}</div>
    </div>
  )
}

type TabKey = 'child' | 'class' | 'attendance' | 'lesson'

const TABS: { key: TabKey; label: string }[] = [
  { key: 'child', label: 'Child progress' },
  { key: 'class', label: 'Class progress' },
  { key: 'attendance', label: 'Attendance' },
  { key: 'lesson', label: 'Lesson completion' },
]

function thirtyDaysAgo(): string {
  const d = new Date()
  d.setDate(d.getDate() - 30)
  return toDateInput(d)
}

export function Reports() {
  const [tab, setTab] = useState<TabKey>('child')

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader title="Reports" subtitle="Growth and engagement across your ministry." />

      <div className="mb-6 flex flex-wrap gap-2 border-b border-cream-deep">
        {TABS.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`-mb-px border-b-2 px-4 py-2.5 text-sm font-semibold transition ${
              tab === t.key
                ? 'border-gold bg-gold-soft/20 text-accent'
                : 'border-transparent text-ink/50 hover:text-ink'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {tab === 'child' && <ChildProgress />}
      {tab === 'class' && <ClassProgress />}
      {tab === 'attendance' && <Attendance />}
      {tab === 'lesson' && <LessonCompletion />}
    </div>
  )
}

function ChildProgress() {
  const [rows, setRows] = useState<ChildProgressReportRow[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [detail, setDetail] = useState<ChildProgressReportRow | null>(null)
  const [detailBadges, setDetailBadges] = useState<ChildBadgeReportRow[] | null>(null)
  const [classFilter, setClassFilter] = useState('')
  const [sortKey, setSortKey] = useState<SortKey>('points')
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc')

  useEffect(() => {
    api<ChildProgressReportRow[]>('/reports/child-progress').then(setRows).catch((e) => setError(e.message))
  }, [])

  function setSort(key: SortKey) {
    if (key === sortKey) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    else {
      setSortKey(key)
      setSortDir(NUMERIC_SORT.has(key) ? 'desc' : 'asc')
    }
  }

  async function openDetail(r: ChildProgressReportRow) {
    setDetail(r)
    setDetailBadges(null)
    try {
      setDetailBadges(await api<ChildBadgeReportRow[]>(`/reports/child/${r.childId}/badges`))
    } catch {
      setDetailBadges([])
    }
  }

  if (error) return <ErrorBanner message={error} />
  if (!rows) return <Loading />
  if (rows.length === 0)
    return <EmptyState icon="📈" title="No data yet" hint="Progress appears as children complete lessons." />

  const sum = (fn: (r: ChildProgressReportRow) => number) => rows.reduce((s, r) => s + fn(r), 0)
  const classOptions = [...new Set(rows.map((r) => r.classGroupName))].sort()
  const filtered = classFilter ? rows.filter((r) => r.classGroupName === classFilter) : rows
  const view = [...filtered].sort((a, b) => {
    const va = sortValue(a, sortKey)
    const vb = sortValue(b, sortKey)
    const cmp = typeof va === 'number' && typeof vb === 'number' ? va - vb : String(va).localeCompare(String(vb))
    return sortDir === 'asc' ? cmp : -cmp
  })

  const sortTh = (key: SortKey, label: string, right = false) => (
    <Th className={right ? 'text-right' : ''}>
      <button
        onClick={() => setSort(key)}
        className={`inline-flex items-center gap-1 transition hover:text-ink ${right ? 'flex-row-reverse' : ''}`}
      >
        {label}
        <span className="text-[0.6rem] text-gold">{sortKey === key ? (sortDir === 'asc' ? '▲' : '▼') : ''}</span>
      </button>
    </Th>
  )

  return (
    <div>
      <div className="mb-5 grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <StatTile label="Children" value={rows.length} />
        <StatTile label="Growth points earned" value={sum((r) => r.growthPoints)} />
        <StatTile label="Badges awarded" value={sum((r) => r.badgeCount)} />
        <StatTile label="Achievements" value={sum((r) => r.achievementCount)} />
      </div>

      <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
        <div className="w-full max-w-xs">
          <Select value={classFilter} onChange={(e) => setClassFilter(e.target.value)}>
            <option value="">All classes</option>
            {classOptions.map((c) => (
              <option key={c} value={c}>
                {c}
              </option>
            ))}
          </Select>
        </div>
        <Button variant="outline" onClick={() => downloadChildProgressCsv(view)}>
          ⬇ Export CSV
        </Button>
      </div>

      <p className="mb-2 text-xs text-ink/45">Tap a child to see their badges &amp; achievements · tap a column to sort.</p>
      <Table>
        <thead>
          <tr>
            {sortTh('name', 'Child')}
            {sortTh('class', 'Class')}
            {sortTh('stage', 'Stage')}
            {sortTh('points', 'Points', true)}
            {sortTh('lessons', 'Lessons', true)}
            {sortTh('verses', 'Verses', true)}
            {sortTh('sundays', 'Sundays', true)}
            {sortTh('badges', 'Badges', true)}
            {sortTh('achv', 'Achv.', true)}
          </tr>
        </thead>
        <tbody>
          {view.map((r) => (
            <tr key={r.childId} onClick={() => openDetail(r)} className="cursor-pointer hover:bg-cream/50">
              <Td>
                <div className="flex items-center gap-2">
                  <Avatar avatarId={r.avatarId} name={r.childName} size={30} />
                  <span className="font-medium text-ink">{r.childName}</span>
                </div>
              </Td>
              <Td>{r.classGroupName}</Td>
              <Td>
                <span className="whitespace-nowrap">
                  {r.stageEmoji} {r.stageName}
                </span>
              </Td>
              <Td className="text-right font-semibold text-heading">{r.growthPoints}</Td>
              <Td className="text-right">{r.lessonsCompleted}</Td>
              <Td className="text-right">{r.versesLearned}</Td>
              <Td className="text-right">{r.sundaysAttended}</Td>
              <Td className="text-right">{r.badgeCount}</Td>
              <Td className="text-right">
                {r.achievementCount > 0 ? <Pill tone="gold">{r.achievementCount}</Pill> : '—'}
              </Td>
            </tr>
          ))}
        </tbody>
      </Table>

      <Modal
        open={detail !== null}
        onClose={() => setDetail(null)}
        title={detail ? `${detail.childName} — badges & achievements` : ''}
        wide
      >
        {detail && (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-2">
              <Avatar avatarId={detail.avatarId} name={detail.childName} size={40} />
              <Pill tone="green">
                {detail.stageEmoji} {detail.stageName}
              </Pill>
              <Pill tone="gray">{detail.growthPoints} points</Pill>
              <span className="text-xs text-ink/50">
                {detail.lessonsCompleted} lessons · {detail.versesLearned} verses · {detail.sundaysAttended} Sundays
              </span>
            </div>

            {!detailBadges ? (
              <Loading label="Loading awards…" />
            ) : detailBadges.length === 0 ? (
              <p className="rounded-xl border border-dashed border-cream-deep px-4 py-6 text-center text-sm text-ink/45">
                No badges or achievements yet.
              </p>
            ) : (
              <div className="space-y-2">
                {detailBadges.map((b) => (
                  <div
                    key={`${b.badgeId}-${b.awardedAtUtc}`}
                    className="flex items-center gap-3 rounded-xl border border-cream-deep p-3"
                  >
                    <BadgeIcon icon={b.iconName} size={40} />
                    <div className="min-w-0 flex-1">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-ink">{b.name}</span>
                        {b.tier === 1 && <Pill tone="gold">Achievement</Pill>}
                      </div>
                      <div className="text-xs text-ink/50">
                        {b.points} pts · awarded {reportDate(b.awardedAtUtc)}
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </Modal>
    </div>
  )
}

function ClassProgress() {
  const [rows, setRows] = useState<ClassProgressReportRow[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<ClassProgressReportRow[]>('/reports/class-progress').then(setRows).catch((e) => setError(e.message))
  }, [])

  if (error) return <ErrorBanner message={error} />
  if (!rows) return <Loading />
  if (rows.length === 0) return <EmptyState icon="📈" title="No data yet" hint="Class stats appear once lessons are completed." />

  return (
    <Table>
      <thead>
        <tr>
          <Th>Class</Th>
          <Th>Children</Th>
          <Th>Lessons completed</Th>
          <Th>Avg completion</Th>
        </tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={r.classGroupId}>
            <Td>
              <span className="font-medium text-ink">{r.classGroupName}</span>
            </Td>
            <Td>{r.totalChildren}</Td>
            <Td>{r.totalLessonsCompleted}</Td>
            <Td>{percent(r.averageCompletionRate)}</Td>
          </tr>
        ))}
      </tbody>
    </Table>
  )
}

function Attendance() {
  const [rows, setRows] = useState<AttendanceReportRow[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [from, setFrom] = useState(thirtyDaysAgo)
  const [to, setTo] = useState(() => toDateInput())

  function load() {
    setRows(null)
    setError(null)
    api<AttendanceReportRow[]>(`/reports/attendance?from=${from}&to=${to}`)
      .then(setRows)
      .catch((e) => setError(e.message))
  }

  useEffect(() => {
    load()
  }, [])

  return (
    <div>
      <div className="mb-4 flex flex-wrap items-end gap-3">
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-ink/70">From</span>
          <Input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-ink/70">To</span>
          <Input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <Button onClick={load}>Apply</Button>
      </div>

      {error && <ErrorBanner message={error} />}
      {!rows && !error && <Loading />}
      {rows && rows.length === 0 && (
        <EmptyState icon="📈" title="No attendance in this range" hint="Adjust the dates and try again." />
      )}

      {rows && rows.length > 0 && (
        <Table>
          <thead>
            <tr>
              <Th>Class</Th>
              <Th>Sessions</Th>
              <Th>Present</Th>
              <Th>Absent</Th>
              <Th>Rate</Th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.classGroupId}>
                <Td>
                  <span className="font-medium text-ink">{r.classGroupName}</span>
                </Td>
                <Td>{r.totalSessions}</Td>
                <Td>{r.totalPresent}</Td>
                <Td>{r.totalAbsent}</Td>
                <Td>{percent(r.attendanceRatePercent)}</Td>
              </tr>
            ))}
          </tbody>
        </Table>
      )}
    </div>
  )
}

function LessonCompletion() {
  const [rows, setRows] = useState<LessonCompletionReportRow[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    api<LessonCompletionReportRow[]>('/reports/lesson-completion').then(setRows).catch((e) => setError(e.message))
  }, [])

  if (error) return <ErrorBanner message={error} />
  if (!rows) return <Loading />
  if (rows.length === 0) return <EmptyState icon="📈" title="No data yet" hint="Completion stats appear as lessons are finished." />

  return (
    <Table>
      <thead>
        <tr>
          <Th>Lesson</Th>
          <Th>Status</Th>
          <Th>Completed</Th>
          <Th>Avg quiz score</Th>
        </tr>
      </thead>
      <tbody>
        {rows.map((r) => (
          <tr key={r.lessonId}>
            <Td>
              <span className="font-medium text-ink">{r.title}</span>
            </Td>
            <Td>{r.isPublished ? <Pill tone="green">Published</Pill> : <Pill tone="gray">Draft</Pill>}</Td>
            <Td>{r.completedCount}</Td>
            <Td>{r.averageQuizScore == null ? '—' : `${Math.round(r.averageQuizScore)}%`}</Td>
          </tr>
        ))}
      </tbody>
    </Table>
  )
}
