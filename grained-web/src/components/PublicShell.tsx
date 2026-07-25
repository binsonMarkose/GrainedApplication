import type { ReactNode } from 'react'
import { LogoTile } from './LogoTile'

// Branded, login-free layout for the public storefront + registration pages (no AppShell).
export function PublicShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col bg-cream text-ink">
      <header className="border-b border-cream-deep bg-white/70 backdrop-blur">
        <div className="mx-auto flex max-w-3xl items-center gap-2.5 px-4 py-3">
          <LogoTile className="size-8 rounded-lg" />
          <span className="font-logo text-lg text-accent">grained</span>
        </div>
      </header>
      <main className="mx-auto w-full max-w-3xl flex-1 px-4 py-8">{children}</main>
      <footer className="mx-auto w-full max-w-3xl px-4 py-8 text-center text-xs text-ink/40">
        Powered by grained · where faith is ingrained
      </footer>
    </div>
  )
}

export function formatEventWhen(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    timeZone: 'UTC',
  })
}
