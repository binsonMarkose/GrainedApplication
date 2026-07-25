import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { api } from '../../lib/api'
import type { PublicChurch } from '../../types'
import { PublicShell, formatEventWhen } from '../../components/PublicShell'
import { Loading } from '../../components/ui'

export function Storefront() {
  const { slug } = useParams()
  const [data, setData] = useState<PublicChurch | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    api<PublicChurch>('/public/churches/' + slug)
      .then(setData)
      .catch(() => setNotFound(true))
      .finally(() => setLoading(false))
  }, [slug])

  if (loading) {
    return (
      <PublicShell>
        <Loading />
      </PublicShell>
    )
  }

  if (notFound || !data) {
    return (
      <PublicShell>
        <div className="py-16 text-center">
          <div className="text-4xl">🌿</div>
          <h1 className="mt-3 font-display text-2xl text-heading">Page not found</h1>
          <p className="mt-2 text-sm text-ink/60">We couldn't find that church.</p>
        </div>
      </PublicShell>
    )
  }

  const nothing = data.events.length === 0 && data.campaigns.length === 0

  return (
    <PublicShell>
      <h1 className="font-display text-3xl text-heading">{data.name}</h1>

      {nothing && (
        <div className="mt-6 rounded-2xl border border-dashed border-cream-deep bg-white/50 p-10 text-center text-ink/50">
          Nothing on right now — check back soon.
        </div>
      )}

      {data.events.length > 0 && (
        <section className="mt-8">
          <h2 className="text-sm font-semibold uppercase tracking-[0.18em] text-gold">Events</h2>
          <div className="mt-4 space-y-4">
            {data.events.map((e) => (
              <Link
                key={e.id}
                to={'/p/events/' + e.id}
                className="block rounded-2xl border border-cream-deep bg-white p-5 shadow-sm transition hover:shadow-md"
              >
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <h3 className="font-display text-xl text-heading">{e.title}</h3>
                    <p className="mt-1 text-sm text-ink/60">
                      {formatEventWhen(e.startDate)}
                      {e.location ? ` · ${e.location}` : ''}
                    </p>
                  </div>
                  {e.fromPrice != null && (
                    <div className="shrink-0 text-right">
                      <div className="text-xs text-ink/45">from</div>
                      <div className="font-display text-lg text-heading">
                        {e.fromPrice === 0 ? 'Free' : `£${e.fromPrice.toFixed(2)}`}
                      </div>
                    </div>
                  )}
                </div>
                <div className="mt-3 text-sm font-semibold text-accent">Register →</div>
              </Link>
            ))}
          </div>
        </section>
      )}

      {data.campaigns.length > 0 && (
        <section className="mt-8">
          <h2 className="text-sm font-semibold uppercase tracking-[0.18em] text-gold">Fundraising</h2>
          <div className="mt-4 space-y-4">
            {data.campaigns.map((c) => {
              const pct = c.targetAmount ? Math.min(100, Math.round((c.raised / c.targetAmount) * 100)) : null
              return (
                <Link
                  key={c.id}
                  to={'/p/campaigns/' + c.id}
                  className="block rounded-2xl border border-cream-deep bg-white p-5 shadow-sm transition hover:shadow-md"
                >
                  <div className="flex items-start gap-4">
                    {c.logoImageId ? (
                      <img src={`/api/images/${c.logoImageId}`} alt="" className="size-12 shrink-0 rounded-lg object-cover" />
                    ) : (
                      <span className="grid size-12 shrink-0 place-items-center rounded-lg bg-cream-deep text-xl">🎗️</span>
                    )}
                    <div className="min-w-0 flex-1">
                      <h3 className="font-display text-xl text-heading">{c.title}</h3>
                      <div className="mt-1 text-sm text-ink/60">
                        £{c.raised.toFixed(2)} raised
                        {c.targetAmount != null && <span className="text-ink/45"> of £{c.targetAmount.toFixed(2)}</span>}
                      </div>
                      {pct != null && (
                        <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-cream-deep">
                          <div className="h-full rounded-full bg-grove" style={{ width: `${pct}%` }} />
                        </div>
                      )}
                    </div>
                  </div>
                  <div className="mt-3 text-sm font-semibold text-accent">Donate →</div>
                </Link>
              )
            })}
          </div>
        </section>
      )}
    </PublicShell>
  )
}
