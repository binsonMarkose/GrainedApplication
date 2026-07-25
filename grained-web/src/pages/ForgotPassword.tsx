import { useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../lib/api'
import { AuthCard } from '../components/AuthCard'
import { Button, ErrorBanner, Field, Input } from '../components/ui'

interface ForgotResponse {
  message: string
  resetUrl: string | null
}

export function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [sent, setSent] = useState(false)
  const [devLink, setDevLink] = useState<string | null>(null)
  const [copied, setCopied] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setError(null)
    setBusy(true)
    try {
      const res = await api<ForgotResponse>('/auth/forgot-password', {
        method: 'POST',
        body: JSON.stringify({ email }),
      })
      setDevLink(res.resetUrl)
      setSent(true)
    } catch {
      setError('Something went wrong. Please try again.')
    } finally {
      setBusy(false)
    }
  }

  function copy(url: string) {
    navigator.clipboard.writeText(url).then(() => {
      setCopied(true)
      setTimeout(() => setCopied(false), 1500)
    })
  }

  if (sent) {
    return (
      <AuthCard>
        <div className="space-y-4 text-center">
          <div className="text-3xl">📬</div>
          <p className="text-sm text-ink/70">
            If <strong>{email}</strong> is registered, we've sent a link to reset your password. It expires shortly.
          </p>
          {devLink && (
            <div className="space-y-2 text-left">
              <p className="text-xs text-ink/50">Dev mode — no email configured, so use this link:</p>
              <div className="flex items-center gap-2">
                <input
                  readOnly
                  value={devLink}
                  className="w-full truncate rounded-lg border border-cream-deep bg-cream-deep/50 px-3 py-2 font-mono text-xs text-ink"
                />
                <Button variant="outline" onClick={() => copy(devLink)}>
                  {copied ? 'Copied' : 'Copy'}
                </Button>
              </div>
            </div>
          )}
          <Link to="/login" className="inline-block text-sm font-medium text-accent hover:underline">
            ← Back to sign in
          </Link>
        </div>
      </AuthCard>
    )
  }

  return (
    <AuthCard>
      <h2 className="mb-1 text-center font-display text-xl text-heading">Forgot your password?</h2>
      <p className="mb-5 text-center text-sm text-ink/55">Enter your email and we'll send you a reset link.</p>

      {error && <div className="mb-4"><ErrorBanner message={error} /></div>}

      <form onSubmit={onSubmit} className="space-y-4">
        <Field label="Email">
          <Input
            type="email"
            required
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="name@church.org"
          />
        </Field>
        <Button type="submit" disabled={busy} className="w-full">
          {busy ? 'Sending…' : 'Send reset link'}
        </Button>
      </form>

      <div className="mt-5 text-center">
        <Link to="/login" className="text-sm font-medium text-accent hover:underline">
          ← Back to sign in
        </Link>
      </div>
    </AuthCard>
  )
}
