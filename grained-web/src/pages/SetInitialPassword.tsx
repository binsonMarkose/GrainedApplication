import { useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import type { LoginResponse } from '../types'
import { Logo } from '../components/Logo'
import { Wordmark } from '../components/Wordmark'

// First sign-in gate: the account was created with a temporary login code, so the user must set
// their own password before entering the app. No current password needed — they just signed in.
export function SetInitialPassword() {
  const { user, applySession, logout } = useAuth()
  const [pw, setPw] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const firstName = user?.fullName?.split(' ')[0] ?? ''

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (pw.length < 8) {
      setError('Please choose a password of at least 8 characters.')
      return
    }
    if (pw !== confirm) {
      setError('The passwords do not match.')
      return
    }
    setBusy(true)
    try {
      const res = await api<LoginResponse>('/auth/set-password', { method: 'POST', body: JSON.stringify({ newPassword: pw }) })
      applySession(res.token, res.user) // clears mustChangePassword → the gate lets them in
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="grid min-h-screen place-items-center bg-gradient-to-br from-grove to-leaf p-6">
      <div className="w-full max-w-sm rounded-3xl bg-cream p-8 shadow-2xl">
        <div className="mb-6 text-center">
          <Logo className="mx-auto w-16" />
          <Wordmark className="mx-auto mt-3 w-40 text-heading" />
        </div>

        <h1 className="text-center font-display text-xl text-heading">Welcome{firstName && `, ${firstName}`}!</h1>
        <p className="mb-5 mt-1 text-center text-sm text-ink/60">
          Set a password of your own to finish setting up your account. You&rsquo;ll use it to sign in from now on.
        </p>

        <form onSubmit={submit} className="space-y-4">
          {error && (
            <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{error}</div>
          )}
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-ink/70">New password</span>
            <input
              type="password"
              value={pw}
              onChange={(e) => setPw(e.target.value)}
              autoComplete="new-password"
              autoFocus
              required
              className="w-full rounded-xl border border-cream-deep bg-white px-3.5 py-2.5 text-ink outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </label>
          <label className="block">
            <span className="mb-1 block text-sm font-medium text-ink/70">Confirm password</span>
            <input
              type="password"
              value={confirm}
              onChange={(e) => setConfirm(e.target.value)}
              autoComplete="new-password"
              required
              className="w-full rounded-xl border border-cream-deep bg-white px-3.5 py-2.5 text-ink outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30"
            />
          </label>
          <button
            type="submit"
            disabled={busy}
            className="w-full rounded-xl bg-grove py-2.5 font-semibold text-oncream transition hover:bg-grove-deep disabled:opacity-60"
          >
            {busy ? 'Saving…' : 'Set password & continue'}
          </button>
        </form>

        <button onClick={logout} className="mt-4 w-full text-center text-sm text-ink/45 transition hover:text-ink">
          Sign out
        </button>
      </div>
    </div>
  )
}
