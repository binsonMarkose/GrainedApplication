import { useEffect, useState } from 'react'

// "Growing in Christ" tree — its form reflects the child's current growth STAGE (0 Seed … 6 Harvest),
// and each badge/achievement hangs on it as fruit. It grows in on mount; fresh badges sparkle.
// Pure inline SVG, sized responsively, motion-safe.

interface GrowthTreeBadge {
  iconName?: string | null
  badgeId?: string
}

const SLOTS: [number, number][] = [
  [130, 78],
  [96, 104],
  [164, 104],
  [110, 140],
  [150, 140],
  [130, 118],
]

// Canopy scale per stage index (0 Seed … 6 Harvest). 0 means "no canopy yet" (seed/roots/sprout).
const CANOPY_SCALE = [0, 0, 0, 0.45, 0.72, 0.95, 1.12]

const SPARKS = Array.from({ length: 9 }).map((_, k) => {
  const a = (k / 9) * Math.PI * 2
  const dist = 15 + (k % 3) * 5
  return { dx: Math.round(Math.cos(a) * dist), dy: Math.round(Math.sin(a) * dist), r: 1.3 + (k % 2), fill: k % 2 ? '#F6E7A6' : '#D9B45C', delay: (k % 4) * 55 }
})

function prefersReducedMotion() {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches
}
function easeOutBack(t: number) {
  const c1 = 1.70158
  const c3 = c1 + 1
  return 1 + c3 * Math.pow(t - 1, 3) + c1 * Math.pow(t - 1, 2)
}

export function GrowthTree({
  stageIndex,
  badges,
  uid,
  animate = true,
  sparkleBadgeIds,
}: {
  stageIndex: number
  badges: GrowthTreeBadge[]
  uid: string
  animate?: boolean
  sparkleBadgeIds?: Set<string>
}) {
  const stage = Math.max(0, Math.min(6, stageIndex))
  const targetCanopy = CANOPY_SCALE[stage]
  const hasCanopy = stage >= 3
  const shown = hasCanopy ? badges.slice(0, SLOTS.length) : []

  const [reduced] = useState(prefersReducedMotion)
  const [progress, setProgress] = useState(() => (animate && !reduced ? 0 : 1))

  useEffect(() => {
    if (!animate || reduced) {
      setProgress(1)
      return
    }
    let raf = 0
    let startTs = 0
    const duration = 1000
    const tick = (ts: number) => {
      if (!startTs) startTs = ts
      const t = Math.min(1, (ts - startTs) / duration)
      setProgress(t)
      if (t < 1) raf = requestAnimationFrame(tick)
    }
    raf = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(raf)
  }, [animate, reduced, stage, shown.length])

  const grow = easeOutBack(Math.max(0.0001, progress))
  const canopy = (targetCanopy * grow).toFixed(3)
  const sprout = grow.toFixed(3) // for seed/roots/sprout motifs

  const leafGrad = `leaf-${uid}`
  const groundGrad = `ground-${uid}`
  const fruitGrad = `fruit-${uid}`

  return (
    <svg viewBox="0 0 260 280" className="h-auto w-full max-w-[260px]" role="img" aria-label="Growth tree">
      <defs>
        <radialGradient id={leafGrad} cx="38%" cy="30%" r="75%">
          <stop offset="0%" stopColor="#A8CD9C" />
          <stop offset="55%" stopColor="#3E8E4E" />
          <stop offset="100%" stopColor="#1E4B2C" />
        </radialGradient>
        <linearGradient id={groundGrad} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#BADBAE" />
          <stop offset="100%" stopColor="#8FBE82" />
        </linearGradient>
        <radialGradient id={fruitGrad} cx="32%" cy="28%" r="70%">
          <stop offset="0%" stopColor="#E9CE7E" />
          <stop offset="55%" stopColor="#D9B45C" />
          <stop offset="100%" stopColor="#C29A45" />
        </radialGradient>
      </defs>

      {/* Sun */}
      <circle cx="226" cy="40" r="16" fill="#F3D27A" />
      <g stroke="#F3D27A" strokeWidth="2.5" strokeLinecap="round" opacity="0.8">
        {Array.from({ length: 8 }).map((_, i) => {
          const ang = (i * Math.PI) / 4
          return <line key={i} x1={226 + Math.cos(ang) * 21} y1={40 + Math.sin(ang) * 21} x2={226 + Math.cos(ang) * 27} y2={40 + Math.sin(ang) * 27} />
        })}
      </g>

      {/* Harvest glow */}
      {stage === 6 && <circle cx="130" cy="120" r="92" fill="#F3D27A" opacity={0.14 * Number(sprout)} />}

      {/* Ground */}
      <ellipse cx="130" cy="256" rx="104" ry="20" fill={`url(#${groundGrad})`} />
      <ellipse cx="130" cy="262" rx="70" ry="9" fill="#000" opacity="0.06" />

      {/* Seed (stage 0) */}
      {stage === 0 && (
        <g transform={`translate(130 244) scale(${sprout})`}>
          <ellipse cx="0" cy="0" rx="11" ry="8" fill="#8B5E3C" />
          <path d="M-4 -3 Q0 2 4 -3" stroke="#6B4A2E" strokeWidth="1.5" fill="none" />
        </g>
      )}

      {/* Roots (stage 1) — seed with roots + a first shoot */}
      {stage === 1 && (
        <g opacity={sprout}>
          <ellipse cx="130" cy="240" rx="10" ry="7" fill="#8B5E3C" />
          <path d="M130 246 C126 256 120 260 116 268 M130 246 C134 256 140 260 144 268 M130 247 L130 268" stroke="#8B5E3C" strokeWidth="2" fill="none" strokeLinecap="round" />
          <path d="M130 240 L130 222" stroke="#3E8E4E" strokeWidth="3" strokeLinecap="round" />
          <ellipse cx="126" cy="222" rx="6" ry="3.4" fill="#3E8E4E" transform="rotate(-30 126 222)" />
        </g>
      )}

      {/* Sprout (stage 2) — a little stem with two leaves */}
      {stage === 2 && (
        <g transform={`translate(130 250) scale(${sprout})`} style={{ transformOrigin: '130px 250px' }}>
          <path d="M0 0 C-2 -14 -2 -22 0 -34" stroke="#3E8E4E" strokeWidth="4" fill="none" strokeLinecap="round" />
          <ellipse cx="-11" cy="-30" rx="11" ry="6" fill="#3E8E4E" transform="rotate(-28 -11 -30)" />
          <ellipse cx="11" cy="-24" rx="11" ry="6" fill="#4E9E6B" transform="rotate(28 11 -24)" />
        </g>
      )}

      {/* Trunk + canopy (stages 3–6) */}
      {hasCanopy && (
        <>
          <path d="M122 256 C 120 214, 118 190, 126 150 L 134 150 C 142 190, 140 214, 138 256 Z" fill="#8B5E3C" />
          <path d="M129 220 C 118 208, 112 202, 106 198" stroke="#8B5E3C" strokeWidth="6" fill="none" strokeLinecap="round" />
          <path d="M131 205 C 142 196, 149 192, 156 190" stroke="#8B5E3C" strokeWidth="6" fill="none" strokeLinecap="round" />
          <g transform={`translate(130 120) scale(${canopy}) translate(-130 -120)`}>
            <circle cx="130" cy="116" r="60" fill={`url(#${leafGrad})`} />
            <circle cx="90" cy="132" r="42" fill={`url(#${leafGrad})`} />
            <circle cx="172" cy="130" r="44" fill={`url(#${leafGrad})`} />
            <circle cx="132" cy="82" r="40" fill={`url(#${leafGrad})`} />
            <circle cx="104" cy="98" r="15" fill="#BADBAE" opacity="0.55" />
            <circle cx="150" cy="150" r="12" fill="#BADBAE" opacity="0.4" />
          </g>
        </>
      )}

      {/* Badge fruit */}
      {shown.map((b, i) => {
        const [x, y] = SLOTS[i]
        const startAt = 0.55 + i * 0.08
        const fp = Math.max(0, Math.min(1, (progress - startAt) / 0.2))
        const pop = easeOutBack(fp || 0.0001)
        const sparkle = !reduced && progress >= 1 && b.badgeId != null && sparkleBadgeIds?.has(b.badgeId)
        return (
          <g key={i}>
            <g opacity={fp} transform={`translate(${x} ${y}) scale(${(pop * fp).toFixed(3)}) translate(${-x} ${-y})`}>
              <line x1="130" y1="118" x2={x} y2={y} stroke="#6B4A2E" strokeWidth="1.5" opacity="0.5" />
              <circle cx={x} cy={y} r="13" fill={`url(#${fruitGrad})`} stroke="#fff" strokeWidth="1.5" />
              <text x={x} y={y + 5} textAnchor="middle" fontSize="14">
                {b.iconName || '🏅'}
              </text>
            </g>
            {sparkle &&
              SPARKS.map((p, k) => (
                <circle
                  key={k}
                  cx={x}
                  cy={y}
                  r={p.r}
                  fill={p.fill}
                  style={{ animation: `grained-sparkle 900ms ease-out ${p.delay}ms both`, ['--sx' as string]: `${p.dx}px`, ['--sy' as string]: `${p.dy}px` }}
                />
              ))}
          </g>
        )
      })}
    </svg>
  )
}
