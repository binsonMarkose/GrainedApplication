export function formatDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' })
}

export function formatShortDate(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return '—'
  return d.toLocaleDateString(undefined, { day: 'numeric', month: 'short' })
}

// yyyy-MM-dd for <input type="date"> and DateOnly APIs
export function toDateInput(d: Date = new Date()): string {
  return d.toISOString().slice(0, 10)
}

export function percent(n: number): string {
  return `${Math.round(n)}%`
}
