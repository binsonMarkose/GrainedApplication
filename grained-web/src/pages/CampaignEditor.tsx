import { useEffect, useRef, useState } from 'react'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { api } from '../lib/api'
import { useToast } from '../components/Toast'
import { useAuth } from '../auth/AuthContext'
import type { CampaignDetail, CampaignForm } from '../types'
import { Button, Card, ErrorBanner, Field, Input, Loading, Pill, Textarea } from '../components/ui'

const emptyForm: CampaignForm = { title: '', description: '', targetAmount: null }

export function CampaignEditor() {
  const { id } = useParams()
  const navigate = useNavigate()
  const { user } = useAuth()
  const toast = useToast()
  const isNew = !id || id === 'new'
  const canEdit = user?.roles.includes('ChurchAdmin') ?? false

  const [detail, setDetail] = useState<CampaignDetail | null>(null)
  const [form, setForm] = useState<CampaignForm>({ ...emptyForm })
  const [loading, setLoading] = useState(!isNew)
  const [error, setError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  function populate(d: CampaignDetail) {
    setDetail(d)
    setForm({ title: d.title, description: d.description ?? '', targetAmount: d.targetAmount })
  }

  async function loadDetail() {
    populate(await api<CampaignDetail>('/campaigns/' + id))
  }

  useEffect(() => {
    if (isNew) return
    let active = true
    setLoading(true)
    api<CampaignDetail>('/campaigns/' + id)
      .then((d) => active && populate(d))
      .catch((e) => active && setError((e as Error).message))
      .finally(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [id, isNew])

  async function saveCampaign() {
    setSaving(true)
    setSaveError(null)
    try {
      const body = JSON.stringify(form)
      if (isNew) {
        const created = await api<{ id: string }>('/campaigns', { method: 'POST', body })
        toast.success('Campaign created')
        navigate('/campaigns/' + created.id)
      } else {
        await api('/campaigns/' + id, { method: 'PUT', body })
        await loadDetail()
        toast.success('Campaign saved')
      }
    } catch (e) {
      setSaveError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setSaving(false)
    }
  }

  async function togglePublish() {
    if (!detail) return
    setError(null)
    const publishing = !detail.isPublished
    try {
      await api(`/campaigns/${id}/${publishing ? 'publish' : 'unpublish'}`, { method: 'POST' })
      await loadDetail()
      toast.success(publishing ? 'Campaign published' : 'Campaign unpublished')
    } catch (e) {
      setError((e as Error).message)
      toast.error((e as Error).message)
    }
  }

  // Logo upload is multipart, so it bypasses the JSON api() helper and posts FormData directly.
  async function uploadLogo(file: File) {
    setUploading(true)
    setError(null)
    try {
      const fd = new FormData()
      fd.append('file', file)
      const token = localStorage.getItem('grained.token')
      const res = await fetch(`/api/campaigns/${id}/logo`, {
        method: 'POST',
        body: fd,
        headers: token ? { Authorization: `Bearer ${token}` } : undefined,
      })
      if (!res.ok) {
        const j = await res.json().catch(() => ({}))
        throw new Error(j.message || 'Upload failed.')
      }
      await loadDetail()
      toast.success('Logo updated')
    } catch (e) {
      setError((e as Error).message)
      toast.error((e as Error).message)
    } finally {
      setUploading(false)
      if (fileRef.current) fileRef.current.value = ''
    }
  }

  if (loading) return <Loading />

  const publicLink = detail ? `${window.location.origin}/p/campaigns/${detail.id}` : ''

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      <div className="flex items-end justify-between gap-4">
        <div>
          <Link to="/campaigns" className="text-sm font-medium text-accent hover:text-heading">
            ← Fundraising
          </Link>
          <h1 className="mt-1 font-display text-3xl font-medium text-heading">
            {isNew ? 'New campaign' : detail?.title || 'Campaign'}
          </h1>
          {!isNew && detail && (
            <div className="mt-2">
              {detail.isPublished ? <Pill tone="green">Published</Pill> : <Pill tone="gray">Draft</Pill>}
            </div>
          )}
        </div>
        {!isNew && canEdit && detail && (
          <Button variant={detail.isPublished ? 'outline' : 'primary'} onClick={togglePublish}>
            {detail.isPublished ? 'Unpublish' : 'Publish'}
          </Button>
        )}
      </div>

      {error && <ErrorBanner message={error} />}

      <Card className="p-6">
        <form
          onSubmit={(e) => {
            e.preventDefault()
            saveCampaign()
          }}
          className="space-y-5"
        >
          {saveError && <ErrorBanner message={saveError} />}
          <Field label="Title">
            <Input
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
              required
              disabled={!canEdit}
              placeholder="New Sunday School Roof"
            />
          </Field>
          <Field label="Description" hint="Shown on the donation page.">
            <Textarea
              rows={5}
              value={form.description ?? ''}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
              disabled={!canEdit}
              placeholder="Tell supporters what you're raising for…"
            />
          </Field>
          <Field label="Fundraising goal (optional)">
            <div className="relative max-w-[12rem]">
              <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink/40">£</span>
              <Input
                className="pl-7"
                type="number"
                min={0}
                step="1"
                value={form.targetAmount ?? ''}
                onChange={(e) =>
                  setForm({ ...form, targetAmount: e.target.value === '' ? null : Number(e.target.value) })
                }
                disabled={!canEdit}
                placeholder="0"
              />
            </div>
          </Field>
          {canEdit && (
            <div className="flex justify-end pt-1">
              <Button type="submit" disabled={saving}>
                {saving ? 'Saving…' : isNew ? 'Create campaign' : 'Save changes'}
              </Button>
            </div>
          )}
        </form>
      </Card>

      {/* Logo — only after the campaign exists */}
      {!isNew && detail && (
        <Card className="p-6">
          <h2 className="font-display text-lg text-heading">Campaign logo</h2>
          <p className="mt-1 text-sm text-ink/55">PNG, JPEG, WebP, GIF or SVG · up to 2 MB.</p>
          <div className="mt-4 flex items-center gap-4">
            {detail.logoImageId ? (
              <img
                src={`/api/images/${detail.logoImageId}`}
                alt="Campaign logo"
                className="size-20 rounded-xl border border-cream-deep object-cover"
              />
            ) : (
              <span className="grid size-20 place-items-center rounded-xl border border-dashed border-cream-deep text-2xl text-ink/30">
                🎗️
              </span>
            )}
            {canEdit && (
              <div>
                <input
                  ref={fileRef}
                  type="file"
                  accept="image/png,image/jpeg,image/webp,image/gif,image/svg+xml"
                  className="hidden"
                  onChange={(e) => e.target.files?.[0] && uploadLogo(e.target.files[0])}
                />
                <Button type="button" variant="outline" disabled={uploading} onClick={() => fileRef.current?.click()}>
                  {uploading ? 'Uploading…' : detail.logoImageId ? 'Replace logo' : 'Upload logo'}
                </Button>
              </div>
            )}
          </div>
        </Card>
      )}

      {/* Public donation link */}
      {!isNew && detail && (
        <Card className="p-6">
          <h2 className="font-display text-lg text-heading">Public donation link</h2>
          {detail.isPublished ? (
            <>
              <p className="mt-1 text-sm text-ink/55">Share this link so anyone can donate — no login needed.</p>
              <div className="mt-3 flex items-center gap-2">
                <Input readOnly value={publicLink} onFocus={(e) => e.currentTarget.select()} />
                <Button type="button" variant="outline" onClick={() => navigator.clipboard?.writeText(publicLink)}>
                  Copy
                </Button>
              </div>
            </>
          ) : (
            <p className="mt-1 text-sm text-ink/55">Publish this campaign to get a public donation link.</p>
          )}
        </Card>
      )}
    </div>
  )
}
