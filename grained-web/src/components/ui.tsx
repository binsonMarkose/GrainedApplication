import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
  TextareaHTMLAttributes,
} from 'react'

// ---- PageHeader ----
export function PageHeader({
  title,
  subtitle,
  action,
}: {
  title: string
  subtitle?: string
  action?: ReactNode
}) {
  return (
    <div className="mb-6 flex items-end justify-between gap-4">
      <div>
        <h1 className="font-display text-3xl font-medium text-heading">{title}</h1>
        {subtitle && <p className="mt-1 text-sm text-ink/55">{subtitle}</p>}
      </div>
      {action}
    </div>
  )
}

// ---- Button ----
type Variant = 'primary' | 'gold' | 'outline' | 'ghost' | 'danger'
const VARIANTS: Record<Variant, string> = {
  primary: 'bg-grove text-oncream hover:bg-grove-deep',
  gold: 'bg-gold text-oncream hover:bg-gold/90',
  outline: 'border border-cream-deep bg-white text-accent hover:bg-cream',
  ghost: 'text-accent hover:bg-cream',
  danger: 'border border-red-200 bg-white text-red-600 hover:bg-red-50',
}
export function Button({
  variant = 'primary',
  className = '',
  ...props
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: Variant }) {
  return (
    <button
      className={`inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-xl px-4 py-2 text-sm font-semibold transition disabled:opacity-50 ${VARIANTS[variant]} ${className}`}
      {...props}
    />
  )
}

// ---- Card ----
export function Card({ children, className = '' }: { children: ReactNode; className?: string }) {
  return (
    <div className={`rounded-2xl border border-cream-deep bg-white shadow-sm ${className}`}>{children}</div>
  )
}

// ---- Form controls ----
export function Field({
  label,
  error,
  hint,
  children,
}: {
  label: string
  error?: string | null
  hint?: string
  children: ReactNode
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-sm font-medium text-ink/70">{label}</span>
      {children}
      {hint && !error && <span className="mt-1 block text-xs text-ink/45">{hint}</span>}
      {error && <span className="mt-1 block text-xs text-red-600">{error}</span>}
    </label>
  )
}

const CONTROL =
  'w-full rounded-xl border border-cream-deep bg-white px-3.5 py-2.5 text-ink outline-none transition focus:border-gold focus:ring-2 focus:ring-gold/30'

export function Input({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={`${CONTROL} ${className}`} {...props} />
}
export function Textarea({ className = '', ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={`${CONTROL} ${className}`} {...props} />
}
export function Select({ className = '', ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return <select className={`${CONTROL} ${className}`} {...props} />
}

export function Checkbox({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <input
      type="checkbox"
      className={`size-4 rounded border-cream-deep text-accent focus:ring-gold/40 ${className}`}
      {...props}
    />
  )
}

// ---- Table ----
export function Table({ children }: { children: ReactNode }) {
  return (
    <div className="overflow-x-auto rounded-2xl border border-cream-deep bg-white shadow-sm">
      <table className="w-full text-left text-sm">{children}</table>
    </div>
  )
}
export function Th({ children, className = '' }: { children?: ReactNode; className?: string }) {
  return (
    <th className={`bg-cream-deep px-4 py-3 font-semibold text-ink/70 first:rounded-tl-2xl last:rounded-tr-2xl ${className}`}>
      {children}
    </th>
  )
}
export function Td({ children, className = '' }: { children?: ReactNode; className?: string }) {
  return <td className={`border-t border-cream-deep px-4 py-3 align-middle ${className}`}>{children}</td>
}

// ---- Pill ----
type Tone = 'green' | 'gold' | 'gray' | 'red'
const TONES: Record<Tone, string> = {
  green: 'bg-leaf-light/40 text-heading',
  gold: 'bg-gold-soft/25 text-gold-ink',
  gray: 'bg-cream-deep text-ink/50',
  red: 'bg-red-100 text-red-700',
}
export function Pill({ tone = 'gray', children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span className={`inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ${TONES[tone]}`}>
      {children}
    </span>
  )
}

// ---- Modal ----
export function Modal({
  open,
  onClose,
  title,
  children,
  wide,
}: {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  wide?: boolean
}) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/50 p-4 backdrop-blur-sm">
      <div className={`my-8 w-full ${wide ? 'max-w-2xl' : 'max-w-md'} rounded-2xl bg-cream shadow-2xl`}>
        <div className="flex items-center justify-between border-b border-cream-deep px-5 py-4">
          <h2 className="font-display text-xl text-heading">{title}</h2>
          <button onClick={onClose} className="text-ink/40 transition hover:text-ink" aria-label="Close">
            ✕
          </button>
        </div>
        <div className="p-5">{children}</div>
      </div>
    </div>
  )
}

// ---- ConfirmDialog: a small yes/no modal for destructive actions ----
export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Delete',
  busyLabel = 'Deleting…',
  busy = false,
  danger = true,
  onConfirm,
  onClose,
}: {
  open: boolean
  title: string
  message: ReactNode
  confirmLabel?: string
  busyLabel?: string
  busy?: boolean
  danger?: boolean
  onConfirm: () => void
  onClose: () => void
}) {
  return (
    <Modal open={open} onClose={onClose} title={title}>
      <div className="space-y-5">
        <div className="text-sm leading-relaxed text-ink/70">{message}</div>
        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={onClose} disabled={busy}>
            Cancel
          </Button>
          <Button type="button" variant={danger ? 'danger' : 'primary'} onClick={onConfirm} disabled={busy}>
            {busy ? busyLabel : confirmLabel}
          </Button>
        </div>
      </div>
    </Modal>
  )
}

// ---- States ----
export function EmptyState({ icon, title, hint }: { icon: string; title: string; hint?: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-cream-deep bg-white/50 p-12 text-center">
      <div className="text-4xl">{icon}</div>
      <div className="mt-3 font-display text-lg text-heading">{title}</div>
      {hint && <div className="mt-1 text-sm text-ink/50">{hint}</div>}
    </div>
  )
}

export function Loading({ label = 'Loading…' }: { label?: string }) {
  return <div className="py-16 text-center text-sm text-ink/45">{label}</div>
}

export function ErrorBanner({ message }: { message: string }) {
  return (
    <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">{message}</div>
  )
}

// ---- DataTable: responsive table (desktop) / stacked cards (mobile) ----
// Define columns once; renders a table on md+ and one card per row on small screens,
// so the actions column never scrolls off the side of a phone.
export interface Column<T> {
  header: string
  cell: (row: T) => ReactNode
  primary?: boolean // used as the card title on mobile (and not repeated as a label/value)
  className?: string // extra className for the desktop <td>/<th> (e.g. 'text-right')
}

export function DataTable<T,>({
  columns,
  rows,
  rowKey,
  actions,
  dim,
  onRowClick,
}: {
  columns: Column<T>[]
  rows: T[]
  rowKey: (row: T) => string
  actions?: (row: T) => ReactNode
  dim?: (row: T) => boolean
  onRowClick?: (row: T) => void
}) {
  const primary = columns.find((c) => c.primary)
  const secondary = columns.filter((c) => !c.primary)

  return (
    <>
      {/* Desktop: table */}
      <div className="hidden overflow-x-auto rounded-2xl border border-cream-deep bg-white shadow-sm md:block">
        <table className="w-full text-left text-sm">
          <thead>
            <tr>
              {columns.map((c) => (
                <Th key={c.header} className={c.className}>
                  {c.header}
                </Th>
              ))}
              {actions && <Th className="text-right">Actions</Th>}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr
                key={rowKey(row)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={[dim?.(row) ? 'opacity-55' : '', onRowClick ? 'cursor-pointer hover:bg-cream/50' : ''].join(
                  ' ',
                )}
              >
                {columns.map((c) => (
                  <Td key={c.header} className={c.className}>
                    {c.cell(row)}
                  </Td>
                ))}
                {actions && (
                  <Td className="text-right">
                    <div className="flex justify-end gap-2" onClick={(e) => e.stopPropagation()}>
                      {actions(row)}
                    </div>
                  </Td>
                )}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Mobile: cards */}
      <div className="space-y-3 md:hidden">
        {rows.map((row) => (
          <div
            key={rowKey(row)}
            onClick={onRowClick ? () => onRowClick(row) : undefined}
            className={`rounded-2xl border border-cream-deep bg-white p-4 shadow-sm ${dim?.(row) ? 'opacity-60' : ''}`}
          >
            {primary && <div className="mb-2 text-base">{primary.cell(row)}</div>}
            <dl className="space-y-1.5">
              {secondary.map((c) => (
                <div key={c.header} className="flex items-start justify-between gap-3 text-sm">
                  <dt className="shrink-0 text-ink/50">{c.header}</dt>
                  <dd className="text-right text-ink">{c.cell(row)}</dd>
                </div>
              ))}
            </dl>
            {actions && (
              <div
                className="mt-3 flex flex-wrap justify-end gap-2 border-t border-cream-deep pt-3"
                onClick={(e) => e.stopPropagation()}
              >
                {actions(row)}
              </div>
            )}
          </div>
        ))}
      </div>
    </>
  )
}
