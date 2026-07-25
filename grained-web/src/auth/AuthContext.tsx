import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { api, tokenStore } from '../lib/api'
import type { User, LoginResponse } from '../types'

// A "workspace" is which experience the user is currently in. A person can be both staff
// (admin/teacher) and a parent with one account; the workspace decides which UI they see.
export type Workspace = 'staff' | 'parent'
const WORKSPACE_KEY = 'grained.workspace'

export function workspaceOptions(roles: string[]): Workspace[] {
  const opts: Workspace[] = []
  if (roles.some((r) => r === 'SuperAdmin' || r === 'ChurchAdmin' || r === 'Teacher')) opts.push('staff')
  if (roles.includes('Parent')) opts.push('parent')
  return opts
}

interface AuthContextValue {
  user: User | null
  loading: boolean
  login: (email: string, password: string) => Promise<void>
  applySession: (token: string, user: User) => void
  logout: () => void
  // Workspace selection
  workspace: Workspace | null
  workspaceOptions: Workspace[]
  setWorkspace: (w: Workspace | null) => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within <AuthProvider>')
  return ctx
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null)
  const [loading, setLoading] = useState(true)
  const [workspace, setWorkspaceState] = useState<Workspace | null>(null)

  // Pick the workspace for a freshly-known user: a single option is auto-selected; with two,
  // reuse a previously stored choice or leave null so the chooser is shown.
  function resolveWorkspace(u: User) {
    const opts = workspaceOptions(u.roles)
    if (opts.length <= 1) {
      setWorkspaceState(opts[0] ?? null)
      return
    }
    const stored = localStorage.getItem(WORKSPACE_KEY) as Workspace | null
    setWorkspaceState(stored && opts.includes(stored) ? stored : null)
  }

  const setWorkspace = (w: Workspace | null) => {
    setWorkspaceState(w)
    if (w) localStorage.setItem(WORKSPACE_KEY, w)
    else localStorage.removeItem(WORKSPACE_KEY)
  }

  // On boot, if we have a token, re-hydrate the user from /auth/me.
  useEffect(() => {
    if (!tokenStore.get()) {
      setLoading(false)
      return
    }
    api<User>('/auth/me')
      .then((u) => {
        setUser(u)
        resolveWorkspace(u)
      })
      .catch(() => tokenStore.clear())
      .finally(() => setLoading(false))
  }, [])

  const login = async (email: string, password: string) => {
    const res = await api<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    })
    tokenStore.set(res.token)
    setUser(res.user)
    resolveWorkspace(res.user)
  }

  // Set the session directly from a token + user (used after accepting an invite).
  const applySession = (token: string, u: User) => {
    tokenStore.set(token)
    setUser(u)
    resolveWorkspace(u)
  }

  const logout = () => {
    tokenStore.clear()
    setUser(null)
    setWorkspace(null)
  }

  return (
    <AuthContext.Provider
      value={{
        user,
        loading,
        login,
        applySession,
        logout,
        workspace,
        workspaceOptions: user ? workspaceOptions(user.roles) : [],
        setWorkspace,
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}
