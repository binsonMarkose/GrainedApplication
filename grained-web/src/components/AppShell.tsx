import { useEffect, useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAuth, workspaceOptions, type Workspace } from '../auth/AuthContext'
import { Logo } from './Logo'
import { LogoTile } from './LogoTile'
import { AnnouncementPopup } from './AnnouncementPopup'
import { fetchInbox, onAnnouncementsChanged } from '../lib/announcements'
import { applyTheme, getStoredTheme, type Theme } from '../lib/theme'

type Role = 'SuperAdmin' | 'ChurchAdmin' | 'Teacher' | 'Parent'

interface NavItem {
  label: string
  to: string
  icon: string
  roles: Role[]
  workspace: Workspace
}

const NAV: NavItem[] = [
  { label: 'Dashboard', to: '/', icon: '📊', roles: ['ChurchAdmin', 'Teacher'], workspace: 'staff' },
  { label: 'My Children', to: '/', icon: '👨‍👩‍👧', roles: ['Parent'], workspace: 'parent' },
  { label: 'Churches', to: '/churches', icon: '⛪', roles: ['SuperAdmin'], workspace: 'staff' },
  { label: 'Class Groups', to: '/class-groups', icon: '🧒', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Teachers', to: '/teachers', icon: '🍎', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Children', to: '/children', icon: '👧', roles: ['ChurchAdmin', 'Teacher'], workspace: 'staff' },
  { label: 'Lessons', to: '/lessons', icon: '📖', roles: ['ChurchAdmin', 'Teacher'], workspace: 'staff' },
  { label: 'Events', to: '/events', icon: '🎟️', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Fundraising', to: '/campaigns', icon: '🎗️', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Attendance', to: '/attendance', icon: '✅', roles: ['ChurchAdmin', 'Teacher'], workspace: 'staff' },
  { label: 'Badges', to: '/badges', icon: '🏅', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Growth', to: '/growth', icon: '🌳', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Reports', to: '/reports', icon: '📈', roles: ['ChurchAdmin', 'Teacher'], workspace: 'staff' },
  { label: 'Messages', to: '/announcements', icon: '📣', roles: ['ChurchAdmin'], workspace: 'staff' },
  { label: 'Announcements', to: '/inbox', icon: '📣', roles: ['Teacher'], workspace: 'staff' },
  { label: 'Announcements', to: '/inbox', icon: '📣', roles: ['Parent'], workspace: 'parent' },
]

export function AppShell() {
  const { user, logout, workspace, setWorkspace } = useAuth()
  const navigate = useNavigate()
  const [menuOpen, setMenuOpen] = useState(false)
  const roles = (user?.roles ?? []) as Role[]
  const options = workspaceOptions(user?.roles ?? [])
  const active = workspace ?? options[0]
  const otherWorkspace = options.find((w) => w !== active)
  const items = NAV.filter((i) => i.workspace === active && i.roles.some((r) => roles.includes(r)))

  // Unread announcement count for the inbox nav badge. Only recipients (teachers / parents) have
  // an inbox; skip the fetch entirely for pure admins.
  const isRecipient = roles.includes('Teacher') || roles.includes('Parent')
  const [unread, setUnread] = useState(0)
  useEffect(() => {
    if (!isRecipient) return
    const load = () =>
      fetchInbox()
        .then((list) => setUnread(list.filter((i) => !i.isRead).length))
        .catch(() => {})
    load()
    return onAnnouncementsChanged(load)
  }, [isRecipient])

  const [theme, setThemeState] = useState<Theme>(getStoredTheme)
  function toggleTheme() {
    const next: Theme = theme === 'dark' ? 'light' : 'dark'
    setThemeState(next)
    applyTheme(next)
  }

  function switchTo(w: Workspace) {
    setWorkspace(w)
    setMenuOpen(false)
    navigate('/')
  }
  const initials = user?.fullName?.split(' ').map((p) => p[0]).slice(0, 2).join('') ?? '?'
  const close = () => setMenuOpen(false)

  return (
    // h-screen + overflow-hidden makes the shell a fixed-height frame: the sidebar and header stay
    // put and only <main> scrolls, so the nav is visible on every page no matter how far you scroll.
    <div className="flex h-screen overflow-hidden bg-cream">
      {/* Backdrop for the mobile drawer */}
      {menuOpen && <div className="fixed inset-0 z-40 bg-black/50 md:hidden" onClick={close} aria-hidden />}

      {/* Sidebar — off-canvas drawer on mobile, static column on md+ */}
      <aside
        className={[
          'sidebar-grain fixed inset-y-0 left-0 z-50 flex w-64 flex-col bg-grove text-oncream transition-transform duration-200',
          'dark:bg-[#131b0f] dark:border-r dark:border-oncream/5', // dark mode: a deep panel instead of the green
          'md:static md:translate-x-0',
          menuOpen ? 'translate-x-0' : '-translate-x-full',
        ].join(' ')}
      >
        {/* Brand — matches the content header height so their bottom borders line up. */}
        <div className="flex h-[68px] shrink-0 items-center gap-3 border-b border-oncream/12 px-4">
          <span className="grid size-11 shrink-0 place-items-center rounded-2xl bg-oncream p-1.5 shadow-md ring-1 ring-black/10">
            <Logo className="size-full" />
          </span>
          <div className="min-w-0 flex-1">
            <div className="font-logo text-2xl leading-none tracking-wide">grained</div>
            <div className="mt-1 whitespace-nowrap text-[0.58rem] font-semibold uppercase tracking-[0.1em] text-oncream/45">
              Where faith is ingrained
            </div>
          </div>
          <button
            onClick={close}
            className="text-oncream/70 transition hover:text-oncream md:hidden"
            aria-label="Close menu"
          >
            ✕
          </button>
        </div>

        <nav className="mt-3 flex-1 space-y-0.5 overflow-y-auto px-3 pb-4">
          <div className="px-3 pb-1.5 text-[0.62rem] font-bold uppercase tracking-[0.18em] text-oncream/40">Menu</div>
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              onClick={close}
              className={({ isActive }) =>
                [
                  'flex items-center gap-3 rounded-lg px-3 py-2.5 text-[0.925rem] font-medium transition',
                  isActive
                    ? 'bg-gold-soft/20 font-bold text-oncream shadow-[inset_3px_0_0_var(--color-gold-soft)] dark:bg-gold-soft/15 dark:text-gold-soft dark:shadow-[inset_3px_0_0_var(--color-gold-soft),0_0_18px_-3px_rgba(217,180,92,0.65)]'
                    : 'text-oncream/80 hover:bg-oncream/10 hover:text-oncream dark:text-[#7fb86a] dark:hover:text-[#a6dc90]',
                ].join(' ')
              }
            >
              <span className="w-5 shrink-0 text-center text-[1.05rem] leading-none">{item.icon}</span>
              <span>{item.label}</span>
              {item.to === '/inbox' && unread > 0 && (
                <span className="ml-auto grid min-w-5 place-items-center rounded-full bg-gold px-1.5 text-[0.7rem] font-bold text-oncream">
                  {unread}
                </span>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-oncream/10 p-4">
          {/* Account settings — click your name to edit profile, password and church details. */}
          <NavLink
            to="/settings"
            onClick={close}
            className={({ isActive }) =>
              [
                'mb-3 flex items-center gap-3 rounded-lg px-2 py-2 transition',
                isActive ? 'bg-oncream/10' : 'hover:bg-oncream/10',
              ].join(' ')
            }
            title="Account settings"
          >
            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-gold-soft/25 text-sm font-semibold text-oncream">
              {initials}
            </span>
            <div className="min-w-0 flex-1">
              <div className="truncate text-sm font-medium">{user?.fullName}</div>
              <div className="truncate text-xs text-oncream/55">{user?.roles.join(', ')}</div>
            </div>
            <span aria-hidden className="text-oncream/45">⚙</span>
          </NavLink>
          {otherWorkspace && (
            <button
              onClick={() => switchTo(otherWorkspace)}
              className="mb-2 w-full rounded-lg bg-gold-soft/20 py-2 text-sm font-medium text-oncream transition hover:bg-gold-soft/30"
            >
              {otherWorkspace === 'parent' ? '👨‍👩‍👧 Switch to Parent view' : '🌳 Switch to Team view'}
            </button>
          )}
          <button
            onClick={toggleTheme}
            className="mb-2 flex w-full items-center justify-center gap-2 rounded-lg border border-oncream/15 py-2 text-sm text-oncream/80 transition hover:bg-oncream/10 hover:text-oncream"
            aria-label={theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {theme === 'dark' ? '☀️ Light mode' : '🌙 Dark mode'}
          </button>
          <button
            onClick={logout}
            className="w-full rounded-lg border border-oncream/15 py-2 text-sm text-oncream/80 transition hover:bg-oncream/10 hover:text-oncream"
          >
            Log out
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        {/* Mobile-only top bar — just the hamburger + brand, so the nav drawer stays reachable on
            phones. On desktop there's no header at all; the page content starts at the very top. */}
        <header className="flex h-14 shrink-0 items-center gap-3 border-b border-cream-deep bg-white/70 px-4 backdrop-blur md:hidden">
          <button
            onClick={() => setMenuOpen(true)}
            className="grid size-9 place-items-center rounded-lg border border-cream-deep text-accent transition hover:bg-cream"
            aria-label="Open menu"
          >
            <span className="text-lg leading-none">☰</span>
          </button>
          <div className="flex items-center gap-2">
            <LogoTile className="size-8 rounded-lg" />
            <span className="font-logo text-lg text-accent">grained</span>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-4 md:p-6">
          <Outlet />
        </main>
      </div>

      {/* Unread announcements pop up here after login, for teachers and parents. */}
      {isRecipient && <AnnouncementPopup />}
    </div>
  )
}
