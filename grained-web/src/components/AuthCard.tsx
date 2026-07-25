import type { ReactNode } from 'react'
import { Logo } from './Logo'
import { Wordmark } from './Wordmark'

// Shared full-screen grove gradient + cream card used by the public auth pages.
export function AuthCard({ children }: { children: ReactNode }) {
  return (
    <div className="grid min-h-screen place-items-center bg-gradient-to-br from-grove to-leaf p-6">
      <div className="w-full max-w-sm rounded-3xl bg-cream p-8 shadow-2xl">
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
}
