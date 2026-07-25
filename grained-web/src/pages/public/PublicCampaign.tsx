import { useEffect, useState, type FormEvent } from 'react'
import { useParams } from 'react-router-dom'
import { api } from '../../lib/api'
import type { PublicCampaignDetail, DonationResult } from '../../types'
import { PublicShell } from '../../components/PublicShell'
import { Button, Checkbox, ErrorBanner, Field, Input, Loading, Textarea } from '../../components/ui'

const PRESETS = [10, 25, 50, 100]

function money(n: number) {
  return `£${n.toFixed(2)}`
}

export function PublicCampaign() {
  const { id } = useParams()
  const [c, setC] = useState<PublicCampaignDetail | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [loading, setLoading] = useState(true)

  const [amount, setAmount] = useState<number>(25)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [message, setMessage] = useState('')
  const [namePublic, setNamePublic] = useState(true)

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState<DonationResult | null>(null)

  useEffect(() => {
    api<PublicCampaignDetail>('/public/campaigns/' + id)
      .then(setC)
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [id])

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (!amount || amount <= 0) {
      setError('Please choose a donation amount.')
      return
    }
    setSubmitting(true)
    try {
      const res = await api<DonationResult>('/public/campaigns/' + id + '/donate', {
        method: 'POST',
        body: JSON.stringify({
          amount,
          donorName: name,
          donorEmail: email,
          message: message || null,
          isNamePublic: namePublic,
        }),
      })
      setDone(res)
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setSubmitting(false)
    }
  }

  if (loading) {
    return (
      <PublicShell>
        <Loading />
      </PublicShell>
    )
  }

  if (notFound || !c) {
    return (
      <PublicShell>
        <div className="py-16 text-center">
          <div className="text-4xl">🌿</div>
          <h1 className="mt-3 font-display text-2xl text-heading">Campaign not available</h1>
          <p className="mt-2 text-sm text-ink/60">This campaign isn't accepting donations.</p>
        </div>
      </PublicShell>
    )
  }

  const raisedAfter = done ? done.raised : c.raised
  const pct = c.targetAmount ? Math.min(100, Math.round((raisedAfter / c.targetAmount) * 100)) : null

  if (done) {
    return (
      <PublicShell>
        <div className="rounded-3xl border border-cream-deep bg-white p-8 text-center shadow-sm">
          <div className="text-5xl">💚</div>
          <h1 className="mt-3 font-display text-2xl text-heading">Thank you, {name.split(' ')[0]}!</h1>
          <p className="mt-2 text-ink/60">
            Your gift of <span className="font-semibold text-heading">{money(done.amount)}</span> to{' '}
            <span className="font-medium">{c.title}</span> means a lot.
          </p>
          <div className="mx-auto mt-5 max-w-xs rounded-xl bg-cream/60 px-4 py-3 text-sm">
            <div className="flex justify-between">
              <span className="text-ink/55">Raised so far</span>
              <span className="font-semibold text-heading">{money(done.raised)}</span>
            </div>
            <div className="mt-1 flex justify-between">
              <span className="text-ink/55">Reference</span>
              <span className="font-mono text-xs text-ink/70">{done.reference}</span>
            </div>
          </div>
        </div>
      </PublicShell>
    )
  }

  return (
    <PublicShell>
      <div className="flex items-center gap-4">
        {c.logoImageId && (
          <img
            src={`/api/images/${c.logoImageId}`}
            alt=""
            className="size-16 shrink-0 rounded-xl border border-cream-deep object-cover"
          />
        )}
        <div>
          <div className="text-xs text-ink/45">{c.churchName}</div>
          <h1 className="font-display text-3xl text-heading">{c.title}</h1>
        </div>
      </div>

      {/* Progress */}
      <div className="mt-5 rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
        <div className="flex items-baseline justify-between">
          <span className="font-display text-2xl text-heading">{money(c.raised)}</span>
          {c.targetAmount != null && <span className="text-sm text-ink/55">of {money(c.targetAmount)} goal</span>}
        </div>
        {pct != null && (
          <div className="mt-3 h-2.5 w-full overflow-hidden rounded-full bg-cream-deep">
            <div className="h-full rounded-full bg-grove transition-all" style={{ width: `${pct}%` }} />
          </div>
        )}
      </div>

      {c.description && <p className="mt-5 whitespace-pre-line text-ink/75">{c.description}</p>}

      <form onSubmit={submit} className="mt-8 space-y-6">
        {error && <ErrorBanner message={error} />}

        <div className="rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
          <h2 className="font-display text-lg text-heading">Your donation</h2>
          <div className="mt-4 flex flex-wrap gap-2">
            {PRESETS.map((p) => (
              <button
                key={p}
                type="button"
                onClick={() => setAmount(p)}
                className={[
                  'rounded-xl border px-4 py-2 text-sm font-semibold transition',
                  amount === p
                    ? 'border-grove bg-grove text-oncream'
                    : 'border-cream-deep bg-white text-accent hover:bg-cream',
                ].join(' ')}
              >
                £{p}
              </button>
            ))}
          </div>
          <div className="mt-3 max-w-[12rem]">
            <div className="relative">
              <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-ink/40">£</span>
              <Input
                className="pl-7"
                type="number"
                min={1}
                step="1"
                value={amount}
                onChange={(e) => setAmount(Number(e.target.value))}
                aria-label="Donation amount"
              />
            </div>
          </div>
        </div>

        <div className="rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
          <h2 className="mb-4 font-display text-lg text-heading">Your details</h2>
          <div className="space-y-4">
            <Field label="Full name">
              <Input value={name} onChange={(e) => setName(e.target.value)} required />
            </Field>
            <Field label="Email">
              <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </Field>
            <Field label="Message (optional)">
              <Textarea rows={2} value={message} onChange={(e) => setMessage(e.target.value)} />
            </Field>
            <label className="flex items-center gap-2 text-sm text-ink">
              <Checkbox checked={namePublic} onChange={(e) => setNamePublic(e.target.checked)} />
              <span>Show my name on the campaign</span>
            </label>
          </div>
        </div>

        <div className="flex justify-end">
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Processing…' : `Donate ${money(amount || 0)}`}
          </Button>
        </div>
      </form>
    </PublicShell>
  )
}
