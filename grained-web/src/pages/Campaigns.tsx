import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import type { Campaign } from '../types'
import {
  Button,
  type Column,
  ConfirmDialog,
  DataTable,
  EmptyState,
  ErrorBanner,
  Loading,
  PageHeader,
  Pill,
} from '../components/ui'

function money(n: number) {
  return `£${n.toFixed(2)}`
}

export function Campaigns() {
  const navigate = useNavigate()
  const toast = useToast()
  const [items, setItems] = useState<Campaign[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [deleting, setDeleting] = useState<Campaign | null>(null)
  const [deletingBusy, setDeletingBusy] = useState(false)

  const load = () =>
    api<Campaign[]>('/campaigns?includeInactive=true').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  async function toggleActive(c: Campaign) {
    try {
      await api(`/campaigns/${c.id}/active`, { method: 'POST', body: JSON.stringify({ isActive: !c.isActive }) })
      await load()
      toast.success(c.isActive ? 'Campaign disabled' : 'Campaign enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  async function confirmDelete() {
    if (!deleting) return
    setDeletingBusy(true)
    try {
      await api(`/campaigns/${deleting.id}`, { method: 'DELETE' })
      setDeleting(null)
      await load()
      toast.success('Campaign deleted')
    } catch (e) {
      toast.error((e as Error).message)
    } finally {
      setDeletingBusy(false)
    }
  }

  const columns: Column<Campaign>[] = [
    {
      header: 'Campaign',
      primary: true,
      cell: (c) => (
        <div className="flex items-center gap-3">
          {c.logoImageId ? (
            <img src={`/api/images/${c.logoImageId}`} alt="" className="size-10 shrink-0 rounded-lg object-cover" />
          ) : (
            <span className="grid size-10 shrink-0 place-items-center rounded-lg bg-cream-deep text-ink/40">🎗️</span>
          )}
          <span className="font-medium text-ink">{c.title}</span>
        </div>
      ),
    },
    {
      header: 'Raised',
      cell: (c) => (
        <span>
          {money(c.raised)}
          {c.targetAmount != null && <span className="text-ink/45"> of {money(c.targetAmount)}</span>}
        </span>
      ),
    },
    { header: 'Donations', cell: (c) => c.donationCount },
    {
      header: 'Status',
      cell: (c) =>
        !c.isActive ? (
          <Pill tone="gray">Disabled</Pill>
        ) : c.isPublished ? (
          <Pill tone="green">Published</Pill>
        ) : (
          <Pill tone="gray">Draft</Pill>
        ),
    },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Fundraising"
        subtitle="Create campaigns and collect donations on a public page."
        action={<Button onClick={() => navigate('/campaigns/new')}>+ New campaign</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="🎗️" title="No campaigns yet" hint="Start a campaign to begin raising funds." />
      )}

      {items && items.length > 0 && (
        <DataTable
          rows={items}
          rowKey={(c) => c.id}
          dim={(c) => !c.isActive}
          columns={columns}
          onRowClick={(c) => navigate(`/campaigns/${c.id}`)}
          actions={(c) => (
            <>
              <Button variant="outline" onClick={() => navigate(`/campaigns/${c.id}`)}>
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
        title="Delete campaign?"
        busy={deletingBusy}
        message={
          <>
            Permanently delete <b>{deleting?.title}</b>? A campaign with donations can’t be deleted — disable it
            instead. This can’t be undone.
          </>
        }
        onConfirm={confirmDelete}
        onClose={() => setDeleting(null)}
      />
    </div>
  )
}
