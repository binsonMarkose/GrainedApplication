import { useState, type FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { api, ApiError } from '../lib/api'
import { AuthCard } from '../components/AuthCard'
import { Button, ErrorBanner, Field, Input } from '../components/ui'

export function ResetPassword() {
  const [params] = useSearchParams()
  const email = params.get('email') ?? ''
  const token = params.get('token') ?? ''

  const [password, setPassword] = useState('')
  const [confirm, setConfirm] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [done, setDone] = useState(false)

  if (!email || !token) {
    return (
      <AuthCard>
        <div className="space-y-3 text-center">
          <div className="text-3xl">🌿</div>
          <h2 className="font-display text-xl text-heading">This reset link isn't valid</h2>
          <p className="text-sm text-ink/60">Request a new one from the sign-in page.</p>
          <Link to="/forgot-password" className="inline-block text-sm font-medium text-accent hover:underline">
            Request a new link
          </Link>
        </div>
      </AuthCard>
    )
  }

  if (done) {
    return (
      <AuthCard>
        <div className="space-y-4 text-center">
          <div className="text-3xl">✅</div>
          <p className="text-sm text-ink/70">Your password has been updated.</p>
          <Link
            to="/login"
            className="inline-block rounded-xl bg-grove px-5 py-2.5 text-sm font-semibold text-oncream transition hover:bg-grove-deep"
          >
            Sign in
          </Link>
        </div>
      </AuthCard>
    )
  }

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    if (password !== confirm) {
      setError('Passwords do not match.')
      return
    }
    setBusy(true)
    try {
      await api('/auth/reset-password', {
        method: 'POST',
        body: JSON.stringify({ email, token, password }),
      })
      setDone(true)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Please try again.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <AuthCard>
      <h2 className="mb-1 text-center font-display text-xl text-heading">Set a new password</h2>
      <p className="mb-5 text-center text-sm text-ink/55">for {email}</p>

      {error && <div className="mb-4"><ErrorBanner message={error} /></div>}

      <form onSubmit={onSubmit} className="space-y-4">
        <Field label="New password">
          <Input
            type="password"
            required
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
        </Field>
        <Field label="Confirm password">
          <Input
            type="password"
            required
            autoComplete="new-password"
            value={confirm}
            onChange={(e) => setConfirm(e.target.value)}
          />
        </Field>
        <Button type="submit" disabled={busy} className="w-full">
          {busy ? 'Updating…' : 'Update password'}
        </Button>
      </form>
    </AuthCard>
  )
}
