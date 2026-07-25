import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { Logo } from '../components/Logo'
import { Wordmark } from '../components/Wordmark'
import type { InviteInfo, LoginResponse } from '../types'

type Phase = 'checking' | 'invalid' | 'ready' | 'submitting'

export function AcceptInvite() {
  const [params] = useSearchParams()
  const token = params.get('token') ?? ''
  const navigate = useNavigate()
  const { applySession } = useAuth()

  const [phase, setPhase] = useState<Phase>('checking')
  const [info, setInfo] = useState<InviteInfo | null>(null)
  const [error, setError] = useState<string | null>(null)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [address, setAddress] = useState('')
  const [phone, setPhone] = useState('')
  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')

  useEffect(() => {
    if (!token) {
      setPhase('invalid')
      return
    }
    api<InviteInfo>(`/invites?token=${encodeURIComponent(token)}`)
      .then((i) => {
        setInfo(i)
        setPhase('ready')
      })
      .catch(() => setPhase('invalid'))
  }, [token])

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (password !== confirm) {
      setError('Passwords do not match.')
      return
    }
    setPhase('submitting')
    try {
      const res = await api<LoginResponse>('/invites/accept', {
        method: 'POST',
        body: JSON.stringify({ token, firstName, lastName, address, phone, password }),
      })
      applySession(res.token, res.user)
      navigate('/', { replace: true })
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
      setPhase('ready')
    }
  }

  const shell = (children: React.ReactNode) => (
    <div className="grid min-h-screen place-items-center bg-gradient-to-br from-grove to-leaf p-6">
      <div className="w-full max-w-md rounded-3xl bg-cream p-8 shadow-2xl">
        <div className="mb-6 text-center">
          <Logo className="mx-auto w-16" />
          {/* SVG wordmark centered on the "i" so it lands directly under the icon's Bible spine */}
          <Wordmark className="mx-auto mt-3 w-40 text-heading" />
          <p className="mt-2 font-logo text-xs uppercase tracking-[0.25em] text-gold">where faith is ingrained</p>
        </div>
        {children}
      </div>
    </div>
  )

  if (phase === 'checking') {
    return shell(<p className="text-center text-sm text-ink/50">Checking your invite…</p>)
  }

  if (phase === 'invalid') {
    return shell(
      <div className="text-center">
        <div className="text-3xl">🌿</div>
        <h2 className="mt-2 font-display text-xl text-heading">This invite link isn't valid</h2>
        <p className="mt-2 text-sm text-ink/60">
          It may have expired or already been used. Ask your Grained administrator to send a fresh invite.
        </p>
      </div>,
    )
  }

  return shell(
    <>
      <div className="mb-5 rounded-xl bg-cream-deep/60 px-4 py-3 text-center">
        <div className="text-sm text-ink/60">You're setting up</div>
        <div className="font-display text-lg text-heading">{info?.churchName}</div>
        <div className="text-xs text-ink/50">{info?.email}</div>
      </div>

      {error && (
        <div className="mb-4 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      <form onSubmit={onSubmit} className="space-y-4">
        <div className="grid grid-cols-2 gap-3">
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-ink/70">First name</span>
            <input required value={firstName} onChange={(e) => setFirstName(e.target.value)} className={field} />
          </label>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-ink/70">Last name</span>
            <input required value={lastName} onChange={(e) => setLastName(e.target.value)} className={field} />
          </label>
        </div>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-ink/70">Church address <span className="text-ink/40">(optional)</span></span>
          <input value={address} onChange={(e) => setAddress(e.target.value)} className={field} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-ink/70">Church phone <span className="text-ink/40">(optional)</span></span>
          <input value={phone} onChange={(e) => setPhone(e.target.value)} className={field} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-ink/70">Create a password</span>
          <input type="password" required autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} className={field} />
        </label>
        <label className="block">
          <span className="mb-1 block text-sm font-medium text-ink/70">Confirm password</span>
          <input type="password" required autoComplete="new-password" value={confirm} onChange={(e) => setConfirm(e.target.value)} className={field} />
        </label>
        <button
          type="submit"
          disabled={phase === 'submitting'}
          className="w-full rounded-xl bg-grove py-2.5 font-semibold text-oncream transition hover:bg-grove-deep disabled:opacity-60"
        >
          {phase === 'submitting' ? 'Setting up…' : 'Activate my account'}
        </button>
      </form>
    </>,
  )
}

const field =
  'w-full rounded-xl border border-cream-deep bg-white px-3.5 py-2.5 text-ink outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30'
