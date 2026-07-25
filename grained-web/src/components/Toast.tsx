import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'

type ToastKind = 'success' | 'error'
interface ToastItem {
  id: number
  kind: ToastKind
  message: string
}
interface ToastApi {
  success: (message: string) => void
  error: (message: string) => void
}

const ToastContext = createContext<ToastApi | null>(null)

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastItem[]>([])
  const idRef = useRef(0)

  const remove = useCallback((id: number) => {
    setToasts((t) => t.filter((x) => x.id !== id))
  }, [])

  const push = useCallback(
    (kind: ToastKind, message: string) => {
      const id = ++idRef.current
      setToasts((t) => [...t, { id, kind, message }])
      // Errors linger a little longer than confirmations; both are dismissible.
      window.setTimeout(() => remove(id), kind === 'error' ? 6000 : 3500)
    },
    [remove],
  )

  const api = useMemo<ToastApi>(
    () => ({ success: (m) => push('success', m), error: (m) => push('error', m) }),
    [push],
  )

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="pointer-events-none fixed inset-x-0 top-4 z-[60] flex flex-col items-center gap-2 px-4 sm:items-end sm:pr-6">
        {toasts.map((t) => (
          <div
            key={t.id}
            role="status"
            style={{ animation: 'toast-in .2s ease-out' }}
            className={[
              'pointer-events-auto flex w-full max-w-sm items-start gap-3 rounded-xl border bg-white px-4 py-3 shadow-lg',
              t.kind === 'success' ? 'border-leaf-light' : 'border-red-200',
            ].join(' ')}
          >
            <span className="mt-0.5 text-lg leading-none">{t.kind === 'success' ? '✅' : '⚠️'}</span>
            <div className={['flex-1 text-sm', t.kind === 'success' ? 'text-heading' : 'text-red-700'].join(' ')}>
              {t.message}
            </div>
            <button
              onClick={() => remove(t.id)}
              className="text-ink/30 transition hover:text-ink"
              aria-label="Dismiss"
            >
              ✕
            </button>
          </div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}

export function useToast() {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast must be used within ToastProvider')
  return ctx
}
