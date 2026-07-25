import { Avatar } from './Avatar'

// The full journey rail, laid out horizontally: Seed (left) → Harvest (right). Each child's avatar
// sits above their current stage, fruit/trophies mark the higher reached stages, and a Forest cap
// counts trees collected across seasons. Grows in on load (motion-safe), left to right.

const STAGES = [
  { emoji: '🌰', name: 'Seed' },
  { emoji: '🌱', name: 'Roots' },
  { emoji: '🌿', name: 'Sprout' },
  { emoji: '🪴', name: 'Sapling' },
  { emoji: '🌳', name: 'Tree' },
  { emoji: '🍎', name: 'Fruit' },
  { emoji: '🌾', name: 'Harvest' },
]

export interface JourneyKid {
  id: string
  name: string
  avatarId: string | null
  stage: number
}

// A little fruit/trophy accent that appears on the higher reached stages.
const STAGE_FRUIT: Record<number, string> = { 3: '🍎', 4: '🍎', 5: '🏆', 6: '🏆' }

export function GrowthJourney({ kids }: { kids: JourneyKid[] }) {
  const maxStage = kids.reduce((m, k) => Math.max(m, k.stage), 0)

  return (
    <div className="rounded-2xl border border-leaf-light/60 bg-white p-4 shadow-[0_14px_36px_-14px_rgba(95,166,48,0.35)] sm:p-5 dark:border-[#7fb86a]/30 dark:shadow-[0_0_30px_-6px_rgba(127,184,106,0.4)]">
      <div className="mb-4">
        <div className="text-[0.7rem] font-semibold uppercase tracking-[0.18em] text-gold">Growth path</div>
        <div className="font-display text-lg text-heading">Seed → Harvest</div>
      </div>

      {/* The path runs left → right. Each stage is a fixed column so the rail passes cleanly through
          the node centres; children at a stage stack as overlapping avatars above it. Scrolls
          horizontally on narrow screens. */}
      <div className="overflow-x-auto pb-1">
        <div className="relative flex w-full min-w-max gap-2 sm:gap-4">
          {/* the rail through the node centres (avatar row 1.75rem + 0.5rem gap + half of 2.5rem node) */}
          <div
            className="absolute left-[7%] right-[7%] top-[3.5rem] h-0.5 -translate-y-1/2 rounded-full bg-leaf-light/60"
            aria-hidden
          />
          {STAGES.map((s, i) => {
            const reached = i <= maxStage
            const here = kids.filter((k) => k.stage === i)
            return (
              <div
                key={i}
                className="grained-rise relative flex flex-1 flex-col items-center"
                style={{ animationDelay: `${i * 0.1}s` }}
              >
                {/* children sitting at this stage */}
                <div className="mb-2 flex h-7 items-end -space-x-1.5">
                  {here.slice(0, 3).map((k) => (
                    <Avatar key={k.id} avatarId={k.avatarId} name={k.name} size={26} ring title={k.name} />
                  ))}
                  {here.length > 3 && (
                    <span className="grid size-[26px] place-items-center rounded-full bg-cream-deep text-[0.6rem] font-bold text-ink/60 ring-2 ring-white">
                      +{here.length - 3}
                    </span>
                  )}
                </div>

                <span
                  className={[
                    'relative z-10 grid size-10 shrink-0 place-items-center rounded-full text-lg ring-2 ring-white transition',
                    reached ? 'bg-leaf-light/70' : 'bg-cream-deep opacity-60',
                    here.length ? 'growth-node-here' : '',
                  ].join(' ')}
                >
                  {s.emoji}
                  {reached && STAGE_FRUIT[i] && (
                    <span className="absolute -right-1 -top-1 text-[0.7rem]">{STAGE_FRUIT[i]}</span>
                  )}
                </span>

                <span
                  className={`mt-2 whitespace-nowrap text-xs font-semibold ${reached ? 'text-heading' : 'text-ink/40'}`}
                >
                  {s.name}
                </span>
              </div>
            )
          })}
        </div>
      </div>

      <p className="mt-3 text-[0.72rem] leading-relaxed text-ink/45">
        Complete lessons, come each Sunday and earn badges to climb the path. Reach Harvest to add a tree to your
        forest!
      </p>
    </div>
  )
}
