import { Navigate } from 'react-router-dom'
import { useAuth, workspaceOptions } from '../auth/AuthContext'
import { Dashboard } from './Dashboard'
import { TeacherDashboard } from './TeacherDashboard'
import { ParentDashboard } from './ParentDashboard'

// Index route: renders the dashboard for the active workspace. Parents see their family view;
// staff see the admin dashboard, or the scoped teacher dashboard for plain teachers.
export function Home() {
  const { user, workspace } = useAuth()
  const roles = user?.roles ?? []
  const active = workspace ?? workspaceOptions(roles)[0]

  if (active === 'parent') return <ParentDashboard />

  const isAdmin = roles.includes('ChurchAdmin') || roles.includes('SuperAdmin')
  if (!isAdmin && roles.includes('Teacher')) return <TeacherDashboard />

  // A platform SuperAdmin has no church context, so the church-scoped dashboard can't load. Their
  // home is the Churches list (their only nav item) — send them straight there.
  if (roles.includes('SuperAdmin') && !user?.churchId) return <Navigate to="/churches" replace />

  return <Dashboard />
}
