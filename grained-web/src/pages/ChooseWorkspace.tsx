import { useAuth, type Workspace } from '../auth/AuthContext'
import { LogoTile } from '../components/LogoTile'

const CARD: Record<Workspace, { icon: string; title: string; blurb: string }> = {
  staff: {
    icon: '🌳',
    title: 'Ministry team',
    blurb: 'Manage classes, lessons, attendance and more as a teacher or admin.',
  },
  parent: {
    icon: '👨‍👩‍👧',
    title: 'Parent',
    blurb: "Follow your children's lessons, memory verses and badges.",
  },
}

export function ChooseWorkspace() {
  const { user, workspaceOptions, setWorkspace, logout } = useAuth()
  const firstName = user?.fullName?.split(' ')[0] ?? ''

  return (
    <div className="grid min-h-screen place-items-center bg-cream p-6">
      <div className="w-full max-w-lg">
        <div className="mb-8 flex flex-col items-center text-center">
          <LogoTile className="size-14 rounded-2xl shadow-sm" />
          <h1 className="mt-4 font-display text-3xl font-medium text-heading">
            Welcome{firstName && `, ${firstName}`}
          </h1>
          <p className="mt-1 text-ink/55">How would you like to continue today?</p>
        </div>

        <div className="grid gap-4 sm:grid-cols-2">
          {workspaceOptions.map((w) => (
            <button
              key={w}
              onClick={() => setWorkspace(w)}
              className="group rounded-2xl border border-cream-deep bg-white p-6 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-gold hover:shadow-md"
            >
              <div className="grid size-12 place-items-center rounded-xl bg-cream text-2xl">{CARD[w].icon}</div>
              <div className="mt-4 font-display text-xl text-heading">{CARD[w].title}</div>
              <p className="mt-1 text-sm text-ink/55">{CARD[w].blurb}</p>
              <span className="mt-3 inline-flex items-center gap-1 text-sm font-semibold text-accent/0 transition group-hover:text-accent">
                Continue <span aria-hidden>→</span>
              </span>
            </button>
          ))}
        </div>

        <div className="mt-6 text-center">
          <button onClick={logout} className="text-sm text-ink/45 underline-offset-2 transition hover:text-ink hover:underline">
            Log out
          </button>
        </div>
      </div>
    </div>
  )
}
