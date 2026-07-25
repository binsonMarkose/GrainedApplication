interface StatCardProps {
  label: string
  value: number
  icon: string
  accent: 'grove' | 'gold' | 'leaf' | 'ink'
}

const ACCENT: Record<StatCardProps['accent'], string> = {
  grove: 'from-grove to-grove-deep',
  gold: 'from-gold to-gold-soft',
  leaf: 'from-leaf to-grove',
  ink: 'from-ink to-grove-deep',
}

export function StatCard({ label, value, icon, accent }: StatCardProps) {
  return (
    <div className="group relative overflow-hidden rounded-2xl border border-cream-deep bg-white p-5 shadow-sm transition hover:-translate-y-0.5 hover:shadow-md">
      <div className={`absolute inset-x-0 top-0 h-1 bg-gradient-to-r ${ACCENT[accent]}`} />
      <div className="flex items-start justify-between">
        <div>
          <div className="font-display text-4xl font-medium text-heading">{value}</div>
          <div className="mt-1 text-sm font-medium text-ink/55">{label}</div>
        </div>
        <span className="grid size-11 place-items-center rounded-xl bg-cream text-xl">{icon}</span>
      </div>
    </div>
  )
}
