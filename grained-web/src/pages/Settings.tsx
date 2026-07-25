import { useEffect, useState, type FormEvent } from 'react'
import { api } from '../lib/api'
import { useAuth } from '../auth/AuthContext'
import { useToast } from '../components/Toast'
import { Button, Card, ErrorBanner, Field, Input, PageHeader, Textarea } from '../components/ui'
import type { Church, LoginResponse } from '../types'

export function Settings() {
  const { user, applySession } = useAuth()
  const toast = useToast()
  const isChurchAdmin = user?.roles.includes('ChurchAdmin') ?? false

  // ---- Profile ----
  const [fullName, setFullName] = useState(user?.fullName ?? '')
  const [email, setEmail] = useState(user?.email ?? '')
  const [profileErr, setProfileErr] = useState<string | null>(null)
  const [savingProfile, setSavingProfile] = useState(false)

  async function saveProfile(e: FormEvent) {
    e.preventDefault()
    setProfileErr(null)
    setSavingProfile(true)
    try {
      const res = await api<LoginResponse>('/auth/me', {
        method: 'PUT',
        body: JSON.stringify({ fullName: fullName.trim(), email: email.trim() }),
      })
      applySession(res.token, res.user) // refresh token + user so the new name/email stick everywhere
      toast.success('Profile updated')
    } catch (err) {
      setProfileErr((err as Error).message)
      toast.error((err as Error).message)
    } finally {
      setSavingProfile(false)
    }
  }

  // ---- Password ----
  const [currentPw, setCurrentPw] = useState('')
  const [newPw, setNewPw] = useState('')
  const [confirmPw, setConfirmPw] = useState('')
  const [pwErr, setPwErr] = useState<string | null>(null)
  const [savingPw, setSavingPw] = useState(false)

  async function savePassword(e: FormEvent) {
    e.preventDefault()
    setPwErr(null)
    if (newPw.length < 8) {
      setPwErr('Your new password must be at least 8 characters.')
      return
    }
    if (newPw !== confirmPw) {
      setPwErr('The new passwords do not match.')
      return
    }
    setSavingPw(true)
    try {
      await api('/auth/change-password', {
        method: 'POST',
        body: JSON.stringify({ currentPassword: currentPw, newPassword: newPw }),
      })
      setCurrentPw('')
      setNewPw('')
      setConfirmPw('')
      toast.success('Password updated')
    } catch (err) {
      setPwErr((err as Error).message)
      toast.error((err as Error).message)
    } finally {
      setSavingPw(false)
    }
  }

  // ---- Church (ChurchAdmin only) ----
  const [church, setChurch] = useState<Church | null>(null)
  const [churchErr, setChurchErr] = useState<string | null>(null)
  const [savingChurch, setSavingChurch] = useState(false)

  useEffect(() => {
    if (!isChurchAdmin) return
    api<Church>('/churches/mine')
      .then(setChurch)
      .catch((e) => setChurchErr((e as Error).message))
  }, [isChurchAdmin])

  async function saveChurch(e: FormEvent) {
    e.preventDefault()
    if (!church) return
    setChurchErr(null)
    setSavingChurch(true)
    try {
      await api('/churches/mine', {
        method: 'PUT',
        body: JSON.stringify({
          name: church.name,
          address: church.address || null,
          email: church.email,
          phone: church.phone || null,
        }),
      })
      toast.success('Church details updated')
    } catch (err) {
      setChurchErr((err as Error).message)
      toast.error((err as Error).message)
    } finally {
      setSavingChurch(false)
    }
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <PageHeader title="Account settings" subtitle="Manage your profile, password and church details." />

      {/* Profile */}
      <Card className="p-6">
        <h2 className="mb-1 font-display text-xl text-heading">Your profile</h2>
        <p className="mb-4 text-sm text-ink/55">Your name and the email you sign in with.</p>
        <form onSubmit={saveProfile} className="space-y-4">
          {profileErr && <ErrorBanner message={profileErr} />}
          <Field label="Full name">
            <Input value={fullName} onChange={(e) => setFullName(e.target.value)} required />
          </Field>
          <Field label="Email" hint="You'll use this to sign in.">
            <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </Field>
          <div className="flex justify-end">
            <Button type="submit" disabled={savingProfile}>
              {savingProfile ? 'Saving…' : 'Save profile'}
            </Button>
          </div>
        </form>
      </Card>

      {/* Password */}
      <Card className="p-6">
        <h2 className="mb-1 font-display text-xl text-heading">Password</h2>
        <p className="mb-4 text-sm text-ink/55">Choose a strong password of at least 8 characters.</p>
        <form onSubmit={savePassword} className="space-y-4">
          {pwErr && <ErrorBanner message={pwErr} />}
          <Field label="Current password">
            <Input type="password" value={currentPw} onChange={(e) => setCurrentPw(e.target.value)} autoComplete="current-password" required />
          </Field>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label="New password">
              <Input type="password" value={newPw} onChange={(e) => setNewPw(e.target.value)} autoComplete="new-password" required />
            </Field>
            <Field label="Confirm new password">
              <Input type="password" value={confirmPw} onChange={(e) => setConfirmPw(e.target.value)} autoComplete="new-password" required />
            </Field>
          </div>
          <div className="flex justify-end">
            <Button type="submit" disabled={savingPw}>
              {savingPw ? 'Updating…' : 'Change password'}
            </Button>
          </div>
        </form>
      </Card>

      {/* Church (admins) */}
      {isChurchAdmin && (
        <Card className="p-6">
          <h2 className="mb-1 font-display text-xl text-heading">Church details</h2>
          <p className="mb-4 text-sm text-ink/55">How your church appears across Grained.</p>
          {churchErr && <ErrorBanner message={churchErr} />}
          {church && (
            <form onSubmit={saveChurch} className="space-y-4">
              <Field label="Church name">
                <Input value={church.name} onChange={(e) => setChurch({ ...church, name: e.target.value })} required />
              </Field>
              <div className="grid gap-4 sm:grid-cols-2">
                <Field label="Contact email">
                  <Input type="email" value={church.email} onChange={(e) => setChurch({ ...church, email: e.target.value })} required />
                </Field>
                <Field label="Phone">
                  <Input value={church.phone ?? ''} onChange={(e) => setChurch({ ...church, phone: e.target.value })} />
                </Field>
              </div>
              <Field label="Address">
                <Textarea rows={2} value={church.address ?? ''} onChange={(e) => setChurch({ ...church, address: e.target.value })} />
              </Field>
              <div className="flex justify-end">
                <Button type="submit" disabled={savingChurch}>
                  {savingChurch ? 'Saving…' : 'Save church details'}
                </Button>
              </div>
            </form>
          )}
        </Card>
      )}
    </div>
  )
}
