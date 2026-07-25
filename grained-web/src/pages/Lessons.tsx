import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'
import { formatDate } from '../lib/format'
import type { ClassGroup, LessonListItem, LessonStatus } from '../types'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toast'
import { LessonReorderList } from '../components/LessonReorderList'
import {
  Button,
  type Column,
  ConfirmDialog,
  DataTable,
  EmptyState,
  ErrorBanner,
  Field,
  Loading,
  Modal,
  PageHeader,
  Pill,
  Select,
  Textarea,
} from '../components/ui'

function statusPill(status: LessonStatus) {
  if (status === 2) return <Pill tone="green">Published</Pill>
  if (status === 1) return <Pill tone="gold">In review</Pill>
  return <Pill tone="gray">Draft</Pill>
}

export function Lessons() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const toast = useToast()
  const isAdmin = (user?.roles.includes('ChurchAdmin') || user?.roles.includes('SuperAdmin')) ?? false
  const canAuthor = (l: LessonListItem) => isAdmin || l.authorUserId === user?.id

  const [items, setItems] = useState<LessonListItem[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [filter, setFilter] = useState<'all' | 'review' | 'published' | 'draft'>('all')
  const [sendBackFor, setSendBackFor] = useState<LessonListItem | null>(null)
  const [note, setNote] = useState('')
  const [deleting, setDeleting] = useState<LessonListItem | null>(null)
  const [deletingBusy, setDeletingBusy] = useState(false)
  // Per-group view: pick a class group to see + reorder its curriculum ('' = all groups).
  const [groups, setGroups] = useState<ClassGroup[]>([])
  const [groupId, setGroupId] = useState('')

  const load = () =>
    api<LessonListItem[]>('/lessons' + (groupId ? `?classGroupId=${groupId}` : ''))
      .then(setItems)
      .catch((e) => setError((e as Error).message))

  useEffect(() => {
    api<ClassGroup[]>('/class-groups')
      .then((gs) => {
        setGroups(gs)
        // Teachers land straight on their class's lessons — no "all groups" picker to wade through.
        if (!isAdmin && gs.length > 0) setGroupId(gs[0].id)
      })
      .catch(() => setGroups([]))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    setItems(null)
    load()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [groupId])

  const pendingCount = useMemo(() => items?.filter((l) => l.status === 1).length ?? 0, [items])
  const view = useMemo(() => {
    if (!items) return []
    if (filter === 'review') return items.filter((l) => l.status === 1)
    if (filter === 'published') return items.filter((l) => l.status === 2)
    if (filter === 'draft') return items.filter((l) => l.status === 0)
    return items
  }, [items, filter])

  async function act(url: string, ok: string, body?: object) {
    setError(null)
    try {
      await api(url, { method: 'POST', ...(body ? { body: JSON.stringify(body) } : {}) })
      toast.success(ok)
      await load()
    } catch (e) {
      toast.error((e as Error).message)
      setError((e as Error).message)
    }
  }

  async function confirmSendBack() {
    if (!sendBackFor) return
    const l = sendBackFor
    setSendBackFor(null)
    await act(`/lessons/${l.id}/send-back`, 'Sent back to the author', { note: note.trim() || null })
    setNote('')
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeletingBusy(true)
    try {
      await api(`/lessons/${deleting.id}`, { method: 'DELETE' })
      setDeleting(null)
      await load()
      toast.success('Lesson deleted')
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setDeletingBusy(false)
    }
  }

  const columns: Column<LessonListItem>[] = [
    {
      header: 'Title',
      primary: true,
      cell: (l) => (
        <>
          <div className="font-medium text-ink">{l.title}</div>
          <div className="text-xs text-ink/50">{l.bibleReference}</div>
        </>
      ),
    },
    { header: 'Author', cell: (l) => <span className="text-ink/70">{l.authorName ?? '—'}</span> },
    {
      header: 'Details',
      cell: (l) => (
        <div className="flex flex-wrap items-center gap-2">
          {l.hasMemoryVerse && <Pill tone="green">Verse ✓</Pill>}
          <span className="text-xs text-ink/50">{l.questionCount} Qs</span>
        </div>
      ),
    },
    {
      header: 'Assigned',
      cell: (l) => (l.assignedClassGroupNames.length ? l.assignedClassGroupNames.join(', ') : '—'),
    },
    {
      header: 'Taught',
      cell: (l) =>
        l.lastCompletedAtUtc ? (
          <Pill tone="green">✅ {formatDate(l.lastCompletedAtUtc)}</Pill>
        ) : (
          <span className="text-ink/40">—</span>
        ),
    },
    { header: 'Status', cell: (l) => statusPill(l.status) },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Lessons"
        subtitle="Your Sunday School lesson library."
        action={<Button onClick={() => navigate('/lessons/new')}>+ New lesson</Button>}
      />

      {isAdmin && pendingCount > 0 && (
        <button
          onClick={() => setFilter('review')}
          className="mb-4 flex w-full items-center gap-3 rounded-xl border border-gold-soft/50 bg-gold-soft/10 px-4 py-3 text-left transition hover:bg-gold-soft/20"
        >
          <span className="text-lg" aria-hidden>📝</span>
          <span className="text-sm font-medium text-gold-ink">
            {pendingCount} lesson{pendingCount > 1 ? 's' : ''} awaiting your review
          </span>
          <span className="ml-auto text-xs font-semibold text-gold-ink">Review →</span>
        </button>
      )}

      {groups.length > 0 || (items && items.length > 0) ? (
        <div className="mb-4 flex flex-wrap items-center gap-2">
          {/* Admins pick any group (incl. "all"); teachers just see their class(es), no "all" option. */}
          {isAdmin && groups.length > 0 && (
            <Select value={groupId} onChange={(e) => setGroupId(e.target.value)} className="w-auto">
              <option value="">All class groups</option>
              {groups.map((g) => (
                <option key={g.id} value={g.id}>
                  {g.name}
                </option>
              ))}
            </Select>
          )}
          {!isAdmin && groups.length > 1 && (
            <Select value={groupId} onChange={(e) => setGroupId(e.target.value)} className="w-auto">
              {groups.map((g) => (
                <option key={g.id} value={g.id}>
                  {g.name}
                </option>
              ))}
            </Select>
          )}
          {!isAdmin && groups.length === 1 && (
            <span className="font-display text-lg text-heading">{groups[0].name}</span>
          )}
          {!groupId && items && items.length > 0 && (
            <>
              <span className="text-sm text-ink/55">Show</span>
              <Select value={filter} onChange={(e) => setFilter(e.target.value as typeof filter)} className="w-auto">
                <option value="all">All lessons</option>
                <option value="review">Pending review</option>
                <option value="published">Published</option>
                <option value="draft">Drafts</option>
              </Select>
            </>
          )}
          {groupId && (
            <span className="text-sm text-ink/55">
              <span aria-hidden>⠿</span> Drag lessons (or use the arrows) to set the teaching order for this group.
            </span>
          )}
        </div>
      ) : null}

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState
          icon="📖"
          title={groupId ? 'No lessons in this group yet' : 'No lessons yet'}
          hint={
            groupId
              ? 'Assign lessons to this class group (from a lesson’s page) and they’ll appear here to order.'
              : 'Create your first lesson to start building your library.'
          }
        />
      )}

      {/* Per-group view → drag-to-reorder the teaching order (admins + assigned teachers). */}
      {groupId && items && items.length > 0 && (
        <LessonReorderList
          lessons={items}
          classGroupId={groupId}
          onReordered={load}
          onOpen={(id) => navigate(`/lessons/${id}`)}
        />
      )}

      {!groupId && items && items.length > 0 && (
        <DataTable
          rows={view}
          rowKey={(l) => l.id}
          columns={columns}
          onRowClick={(l) => navigate(`/lessons/${l.id}`)}
          actions={(l) => (
            <>
              {canAuthor(l) ? (
                <Button variant="outline" onClick={() => navigate(`/lessons/${l.id}`)}>
                  Edit
                </Button>
              ) : (
                <Button variant="outline" onClick={() => navigate(`/lessons/${l.id}`)}>
                  View
                </Button>
              )}

              {/* Teacher: submit own draft for review */}
              {!isAdmin && canAuthor(l) && l.status === 0 && (
                <Button onClick={() => act(`/lessons/${l.id}/submit`, 'Submitted for review')}>Submit for review</Button>
              )}

              {/* Admin review actions */}
              {isAdmin && l.status === 1 && (
                <>
                  <Button onClick={() => act(`/lessons/${l.id}/publish`, 'Lesson published')}>Publish</Button>
                  <Button variant="danger" onClick={() => setSendBackFor(l)}>
                    Send back
                  </Button>
                </>
              )}
              {isAdmin && l.status === 0 && (
                <Button variant="ghost" onClick={() => act(`/lessons/${l.id}/publish`, 'Lesson published')}>
                  Publish
                </Button>
              )}
              {isAdmin && l.status === 2 && (
                <Button variant="ghost" onClick={() => act(`/lessons/${l.id}/unpublish`, 'Lesson unpublished')}>
                  Unpublish
                </Button>
              )}

              {/* Admin can delete a lesson that isn't in use yet (guarded server-side). */}
              {isAdmin && (
                <Button variant="danger" onClick={() => setDeleting(l)}>
                  Delete
                </Button>
              )}
            </>
          )}
        />
      )}

      <ConfirmDialog
        open={deleting !== null}
        title="Delete lesson?"
        busy={deletingBusy}
        message={
          <>
            Permanently delete <b>{deleting?.title}</b> and its memory verse, quiz and class assignments? A lesson that
            children have progress or attendance on can’t be deleted — unpublish it instead. This can’t be undone.
          </>
        }
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />

      <Modal open={sendBackFor !== null} onClose={() => setSendBackFor(null)} title="Send back to author">
        <div className="space-y-4">
          <p className="text-sm text-ink/70">
            Return <span className="font-medium">{sendBackFor?.title}</span> to {sendBackFor?.authorName ?? 'the author'} as a
            draft with a note on what to change.
          </p>
          <Field label="Note (optional)">
            <Textarea
              rows={4}
              value={note}
              placeholder="e.g. Please add an activity and a second quiz question."
              onChange={(e) => setNote(e.target.value)}
            />
          </Field>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setSendBackFor(null)}>
              Cancel
            </Button>
            <Button variant="danger" onClick={confirmSendBack}>
              Send back
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
