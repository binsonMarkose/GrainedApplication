import { useEffect, useState, type FormEvent } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../../lib/api'
import type { PublicEventDetail, RegistrationResult } from '../../types'
import { PublicShell, formatEventWhen } from '../../components/PublicShell'
import { Button, ErrorBanner, Field, Input, Loading, Select } from '../../components/ui'

const TSHIRT_SIZES = ['XS', 'S', 'M', 'L', 'XL', 'XXL']

export function PublicEvent() {
  const { id } = useParams()
  const [ev, setEv] = useState<PublicEventDetail | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [loading, setLoading] = useState(true)

  const [qty, setQty] = useState<Record<string, number>>({})
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [tshirt, setTshirt] = useState('M')

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [done, setDone] = useState<RegistrationResult | null>(null)

  useEffect(() => {
    api<PublicEventDetail>('/public/events/' + id)
      .then((e) => {
        setEv(e)
        setQty(Object.fromEntries(e.ticketTypes.map((t) => [t.id, 0])))
      })
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [id])

  const total = ev ? ev.ticketTypes.reduce((sum, t) => sum + (qty[t.id] || 0) * t.price, 0) : 0
  const count = Object.values(qty).reduce((a, b) => a + b, 0)

  function setQ(ticketId: string, v: number) {
    setQty((prev) => ({ ...prev, [ticketId]: Math.min(1000, Math.max(0, v)) }))
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (count === 0) {
      setError('Please select at least one ticket.')
      return
    }
    setSubmitting(true)
    try {
      const selections = Object.entries(qty)
        .filter(([, q]) => q > 0)
        .map(([ticketTypeId, quantity]) => ({ ticketTypeId, quantity }))
      const res = await api<RegistrationResult>('/public/events/' + id + '/register', {
        method: 'POST',
        body: JSON.stringify({
          purchaserName: name,
          purchaserEmail: email,
          purchaserPhone: phone || null,
          tshirtSize: ev?.enableTshirt ? tshirt : null,
          selections,
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

  if (notFound || !ev) {
    return (
      <PublicShell>
        <div className="py-16 text-center">
          <div className="text-4xl">🌿</div>
          <h1 className="mt-3 font-display text-2xl text-heading">Event not available</h1>
          <p className="mt-2 text-sm text-ink/60">This event isn't open for registration.</p>
        </div>
      </PublicShell>
    )
  }

  if (done) {
    return (
      <PublicShell>
        <div className="rounded-3xl border border-cream-deep bg-white p-8 text-center shadow-sm">
          <div className="text-5xl">🎉</div>
          <h1 className="mt-3 font-display text-2xl text-heading">You're registered!</h1>
          <p className="mt-2 text-ink/60">
            Thanks {name.split(' ')[0]} — your spot at <span className="font-medium">{ev.title}</span> is booked.
          </p>
          <div className="mx-auto mt-5 max-w-xs rounded-xl bg-cream/60 px-4 py-3 text-sm">
            <div className="flex justify-between">
              <span className="text-ink/55">Total</span>
              <span className="font-semibold text-heading">£{done.total.toFixed(2)}</span>
            </div>
            <div className="mt-1 flex justify-between">
              <span className="text-ink/55">Reference</span>
              <span className="font-mono text-xs text-ink/70">{done.reference}</span>
            </div>
          </div>
          <p className="mt-5 text-xs text-ink/40">Please keep your reference for the day of the event.</p>
        </div>
      </PublicShell>
    )
  }

  return (
    <PublicShell>
      <div className="text-xs text-ink/45">{ev.churchName}</div>
      <h1 className="mt-1 font-display text-3xl text-heading">{ev.title}</h1>
      <p className="mt-2 text-ink/60">
        {formatEventWhen(ev.startDate)}
        {ev.location ? ` · ${ev.location}` : ''}
      </p>
      {ev.description && <p className="mt-4 whitespace-pre-line text-ink/75">{ev.description}</p>}

      <form onSubmit={submit} className="mt-8 space-y-6">
        {error && <ErrorBanner message={error} />}

        {/* Tickets */}
        <div className="rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
          <h2 className="font-display text-lg text-heading">Tickets</h2>
          <div className="mt-4 space-y-3">
            {ev.ticketTypes.map((t) => (
              <div key={t.id} className="flex items-center justify-between gap-3">
                <div>
                  <div className="font-medium text-ink">{t.name}</div>
                  <div className="text-sm text-ink/50">{t.price === 0 ? 'Free' : `£${t.price.toFixed(2)}`}</div>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => setQ(t.id, (qty[t.id] || 0) - 1)}
                    className="grid size-9 place-items-center rounded-lg border border-cream-deep text-lg text-accent transition hover:bg-cream disabled:opacity-40"
                    aria-label={`Fewer ${t.name}`}
                    disabled={(qty[t.id] || 0) === 0}
                  >
                    −
                  </button>
                  <input
                    type="number"
                    min={0}
                    value={qty[t.id] || 0}
                    onChange={(e) => setQ(t.id, Number(e.target.value))}
                    className="w-14 rounded-lg border border-cream-deep bg-white px-2 py-1.5 text-center text-ink outline-none focus:border-gold focus:ring-2 focus:ring-gold/30"
                    aria-label={`${t.name} quantity`}
                  />
                  <button
                    type="button"
                    onClick={() => setQ(t.id, (qty[t.id] || 0) + 1)}
                    className="grid size-9 place-items-center rounded-lg border border-cream-deep text-lg text-accent transition hover:bg-cream"
                    aria-label={`More ${t.name}`}
                  >
                    +
                  </button>
                </div>
              </div>
            ))}
          </div>

          <div className="mt-4 flex items-center justify-between border-t border-cream-deep pt-4">
            <span className="text-sm text-ink/55">{count} ticket{count === 1 ? '' : 's'}</span>
            <span className="font-display text-xl text-heading">£{total.toFixed(2)}</span>
          </div>
        </div>

        {/* T-shirt */}
        {ev.enableTshirt && (
          <div className="rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
            <h2 className="font-display text-lg text-heading">T-shirt</h2>
            <p className="mt-1 text-sm text-ink/55">Pick a size to include an event T-shirt.</p>
            <div className="mt-3 max-w-[10rem]">
              <Select value={tshirt} onChange={(e) => setTshirt(e.target.value)}>
                {TSHIRT_SIZES.map((s) => (
                  <option key={s} value={s}>
                    {s}
                  </option>
                ))}
              </Select>
            </div>
          </div>
        )}

        {/* Your details */}
        <div className="rounded-2xl border border-cream-deep bg-white p-5 shadow-sm">
          <h2 className="mb-4 font-display text-lg text-heading">Your details</h2>
          <div className="space-y-4">
            <Field label="Full name">
              <Input value={name} onChange={(e) => setName(e.target.value)} required />
            </Field>
            <div className="grid gap-4 sm:grid-cols-2">
              <Field label="Email">
                <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
              </Field>
              <Field label="Phone (optional)">
                <Input value={phone} onChange={(e) => setPhone(e.target.value)} />
              </Field>
            </div>
          </div>
        </div>

        <div className="flex items-center justify-between gap-4">
          <Link to="/p/events/back" onClick={(e) => { e.preventDefault(); history.back() }} className="text-sm text-ink/50 hover:text-ink">
            ← Back
          </Link>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Registering…' : total > 0 ? `Complete registration · £${total.toFixed(2)}` : 'Complete registration'}
          </Button>
        </div>
      </form>
    </PublicShell>
  )
}
