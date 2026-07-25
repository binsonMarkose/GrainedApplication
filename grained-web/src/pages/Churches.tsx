import { useEffect, useState } from 'react'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import type { Church, ChurchForm, CreateChurchResult } from '../types'
import {
  Button,
  type Column,
  DataTable,
  EmptyState,
  ErrorBanner,
  Field,
  Input,
  Loading,
  Modal,
  PageHeader,
  Pill,
} from '../components/ui'

function statusPill(status: string) {
  if (status === 'Active') return <Pill tone="green">Active</Pill>
  if (status === 'Pending') return <Pill tone="gold">Pending invite</Pill>
  return <Pill tone="gray">{status}</Pill>
}

export function Churches() {
  const toast = useToast()
  const [items, setItems] = useState<Church[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  // Provision (name + admin email) modal
  const [provision, setProvision] = useState<{ name: string; adminEmail: string } | null>(null)
  const [busy, setBusy] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  // Edit existing church details modal
  const [editing, setEditing] = useState<ChurchForm | null>(null)

  // "Invite ready" modal — shows the accept link (dev) with a copy button
  const [inviteLink, setInviteLink] = useState<{ email: string; url: string | null } | null>(null)
  const [copied, setCopied] = useState(false)

  const load = () =>
    api<Church[]>('/churches?includeInactive=true').then(setItems).catch((e) => setError(e.message))

  useEffect(() => {
    load()
  }, [])

  async function provisionChurch() {
    if (!provision) return
    setBusy(true)
    setFormError(null)
    try {
      const res = await api<CreateChurchResult>('/churches', {
        method: 'POST',
        body: JSON.stringify({ name: provision.name, adminEmail: provision.adminEmail }),
      })
      setProvision(null)
      setInviteLink({ email: provision.adminEmail, url: res.acceptUrl })
      await load()
      toast.success('Invite sent')
    } catch (e) {
      setFormError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  async function saveEdit() {
    if (!editing) return
    setBusy(true)
    setFormError(null)
    try {
      await api(`/churches/${editing.id}`, { method: 'PUT', body: JSON.stringify(editing) })
      setEditing(null)
      await load()
      toast.success('Church updated')
    } catch (e) {
      setFormError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setBusy(false)
    }
  }

  async function resend(c: Church) {
    try {
      const res = await api<{ status: string; acceptUrl: string | null }>(`/churches/${c.id}/resend-invite`, {
        method: 'POST',
        body: JSON.stringify({}),
      })
      setInviteLink({ email: c.email, url: res.acceptUrl })
      toast.success('Invite resent')
    } catch (e) {
      setError((e as Error).message)
      toast.error((e as Error).message)
    }
  }

  async function toggleActive(c: Church) {
    try {
      await api(`/churches/${c.id}/active`, { method: 'POST', body: JSON.stringify({ isActive: !c.isActive }) })
      await load()
      toast.success(c.isActive ? 'Church disabled' : 'Church enabled')
    } catch (e) {
      toast.error((e as Error).message)
    }
  }

  function copyLink(url: string) {
    navigator.clipboard.writeText(url).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    })
  }

  const columns: Column<Church>[] = [
    {
      header: 'Name',
      primary: true,
      cell: (c) => (
        <>
          <div className="font-medium text-ink">{c.name}</div>
          {c.address && <div className="text-xs text-ink/50">{c.address}</div>}
        </>
      ),
    },
    { header: 'Admin email', cell: (c) => c.email },
    { header: 'Status', cell: (c) => statusPill(c.status) },
  ]

  return (
    <div className="mx-auto max-w-5xl">
      <PageHeader
        title="Churches"
        subtitle="Provision a church with an email — the admin sets it up themselves."
        action={<Button onClick={() => { setFormError(null); setProvision({ name: '', adminEmail: '' }) }}>+ Onboard a church</Button>}
      />

      {error && <ErrorBanner message={error} />}
      {!items && !error && <Loading />}
      {items && items.length === 0 && (
        <EmptyState icon="⛪" title="No churches yet" hint="Onboard your first church to get started." />
      )}

      {items && items.length > 0 && (
        <DataTable
          rows={items}
          rowKey={(c) => c.id}
          dim={(c) => !c.isActive}
          columns={columns}
          actions={(c) =>
            c.status === 'Pending' ? (
              <>
                <Button variant="outline" onClick={() => resend(c)}>
                  Resend invite
                </Button>
                <Button variant="ghost" onClick={() => toggleActive(c)}>
                  {c.isActive ? 'Disable' : 'Enable'}
                </Button>
              </>
            ) : (
              <>
                <Button
                  variant="outline"
                  onClick={() =>
                    setEditing({ id: c.id, name: c.name, email: c.email, address: c.address, phone: c.phone })
                  }
                >
                  Edit
                </Button>
                <Button variant="ghost" onClick={() => toggleActive(c)}>
                  {c.isActive ? 'Disable' : 'Enable'}
                </Button>
              </>
            )
          }
        />
      )}

      {/* Provision (name + admin email) */}
      <Modal open={provision !== null} onClose={() => setProvision(null)} title="Onboard a church">
        {provision && (
          <form onSubmit={(e) => { e.preventDefault(); provisionChurch() }} className="space-y-4">
            {formError && <ErrorBanner message={formError} />}
            <p className="text-sm text-ink/60">
              We'll email an invite. The admin sets their own name, church details, and password.
            </p>
            <Field label="Church name">
              <Input value={provision.name} onChange={(e) => setProvision({ ...provision, name: e.target.value })} required />
            </Field>
            <Field label="Admin email">
              <Input
                type="email"
                value={provision.adminEmail}
                onChange={(e) => setProvision({ ...provision, adminEmail: e.target.value })}
                required
                placeholder="pastor@church.org"
              />
            </Field>
            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="ghost" onClick={() => setProvision(null)}>
                Cancel
              </Button>
              <Button type="submit" disabled={busy}>
                {busy ? 'Sending…' : 'Send invite'}
              </Button>
            </div>
          </form>
        )}
      </Modal>

      {/* Invite ready — copy link (dev) */}
      <Modal open={inviteLink !== null} onClose={() => setInviteLink(null)} title="Invite sent 🌱">
        {inviteLink && (
          <div className="space-y-3">
            <p className="text-sm text-ink/70">
              An invite email was sent to <strong>{inviteLink.email}</strong>. It expires in 7 days.
            </p>
            {inviteLink.url ? (
              <>
                <p className="text-xs text-ink/50">Dev mode — no email configured yet, so share this link directly:</p>
                <div className="flex items-center gap-2">
                  <input
                    readOnly
                    value={inviteLink.url}
                    className="w-full truncate rounded-lg border border-cream-deep bg-cream-deep/50 px-3 py-2 font-mono text-xs text-ink"
                  />
                  <Button variant="outline" onClick={() => copyLink(inviteLink.url!)}>
                    {copied ? 'Copied' : 'Copy'}
                  </Button>
                </div>
              </>
            ) : (
              <p className="text-sm text-ink/50">The invite link was emailed to the admin.</p>
            )}
            <div className="flex justify-end pt-2">
              <Button onClick={() => setInviteLink(null)}>Done</Button>
            </div>
          </div>
        )}
      </Modal>

      {/* Edit existing church details */}
      <Modal open={editing !== null} onClose={() => setEditing(null)} title="Edit church">
        {editing && (
          <form onSubmit={(e) => { e.preventDefault(); saveEdit() }} className="space-y-4">
            {formError && <ErrorBanner message={formError} />}
            <Field label="Name">
              <Input value={editing.name} onChange={(e) => setEditing({ ...editing, name: e.target.value })} required />
            </Field>
            <Field label="Email">
              <Input type="email" value={editing.email} onChange={(e) => setEditing({ ...editing, email: e.target.value })} required />
            </Field>
            <Field label="Address">
              <Input value={editing.address ?? ''} onChange={(e) => setEditing({ ...editing, address: e.target.value })} />
            </Field>
            <Field label="Phone">
              <Input value={editing.phone ?? ''} onChange={(e) => setEditing({ ...editing, phone: e.target.value })} />
            </Field>
            <div className="flex justify-end gap-2 pt-2">
              <Button type="button" variant="ghost" onClick={() => setEditing(null)}>
                Cancel
              </Button>
              <Button type="submit" disabled={busy}>
                {busy ? 'Saving…' : 'Save'}
              </Button>
            </div>
          </form>
        )}
      </Modal>
    </div>
  )
}
