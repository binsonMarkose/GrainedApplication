import { useEffect, useRef } from 'react'

// The Grained mark, animated like the landing (index.html) hero but as a play-once welcome:
// the Bible rises into place → a seed falls from the sky and bounces to rest at its heart →
// growth rings ripple outward one season at a time → the stem grows up → the leaves unfurl →
// then the mark rests (no perpetual motion — this sits on a working screen). Click to replay.
// Honors prefers-reduced-motion (snaps to the finished mark). Sits on a light/cream surface
// so the cream halo behind the stem reads invisibly.
export function AnimatedLogo({ className }: { className?: string }) {
  const svgRef = useRef<SVGSVGElement>(null)

  useEffect(() => {
    const svg = svgRef.current
    if (!svg) return
    const $ = (id: string) => svg.querySelector<SVGElement>(`#${id}`)!

    const seed = $('seed')
    const stem = $('stem') as unknown as SVGPathElement
    const halo = $('halo') as unknown as SVGPathElement
    const leafL = $('leafL')
    const leafR = $('leafR')
    const bible = $('bible')
    const rings = Array.from(svg.querySelectorAll<SVGEllipseElement>('.ring'))
    // Full ring sizes, kept as constants — NOT read from the live DOM, whose rx/ry the
    // animation shrinks to 0.01 (in StrictMode the effect runs twice and would otherwise
    // capture the already-shrunk values as its growth targets, making the rings invisible).
    const ringTargets = [
      { rx: 19, ry: 18.1 },
      { rx: 33, ry: 31.6 },
      { rx: 47, ry: 45.8 },
      { rx: 61, ry: 59.0 },
      { rx: 75, ry: 72.8 },
      { rx: 88, ry: 85.6 },
    ]
    const stemLen = stem.getTotalLength()
    const haloLen = halo.getTotalLength()
    const reduced = window.matchMedia('(prefers-reduced-motion: reduce)').matches

    const easeOutCubic = (t: number) => 1 - Math.pow(1 - t, 3)
    const easeOutBack = (t: number) => {
      const c = 1.70158
      return 1 + (c + 1) * Math.pow(t - 1, 3) + c * Math.pow(t - 1, 2)
    }
    // fall with two small bounces at the end
    const bounce = (t: number) => {
      const n = 7.5625,
        d = 2.75
      if (t < 1 / d) return n * t * t
      if (t < 2 / d) return n * (t -= 1.5 / d) * t + 0.75
      if (t < 2.5 / d) return n * (t -= 2.25 / d) * t + 0.9375
      return n * (t -= 2.625 / d) * t + 0.984375
    }

    let rafId = 0

    function finishInstant() {
      bible.style.opacity = '1'
      bible.setAttribute('transform', 'translate(0,0)')
      seed.style.opacity = '1'
      seed.setAttribute('cy', '128')
      rings.forEach((r, i) => {
        r.style.opacity = '1'
        r.setAttribute('rx', String(ringTargets[i].rx))
        r.setAttribute('ry', String(ringTargets[i].ry))
      })
      stem.style.strokeDashoffset = '0'
      halo.style.strokeDashoffset = '0'
      leafL.setAttribute('transform', '')
      leafR.setAttribute('transform', '')
    }

    function play() {
      cancelAnimationFrame(rafId)

      // reset
      bible.style.opacity = '0'
      bible.setAttribute('transform', 'translate(0,14)')
      seed.setAttribute('cy', '-14')
      seed.style.opacity = '0'
      rings.forEach((r) => {
        r.setAttribute('rx', '0.01')
        r.setAttribute('ry', '0.01')
        r.style.opacity = '0'
      })
      stem.style.strokeDasharray = String(stemLen)
      stem.style.strokeDashoffset = String(stemLen)
      halo.style.strokeDasharray = String(haloLen)
      halo.style.strokeDashoffset = String(haloLen)
      leafL.setAttribute('transform', 'translate(110,44) scale(0) translate(-110,-44)')
      leafR.setAttribute('transform', 'translate(110,36) scale(0) translate(-110,-36)')

      if (reduced) {
        finishInstant()
        return
      }

      const steps = [
        // 1. Bible rises into place
        { at: 0.15, dur: 0.8, tick: (p: number) => { const e = easeOutCubic(p); bible.style.opacity = String(e); bible.setAttribute('transform', 'translate(0,' + 14 * (1 - e) + ')') } },
        // 2. seed falls from the sky into the Bible, bouncing to rest at the heart
        { at: 1.0, dur: 1.15, tick: (p: number) => { seed.style.opacity = '1'; const y = -14 + (128 - -14) * bounce(p); seed.setAttribute('cy', String(y)) } },
        // 3. growth rings ripple outward, one season at a time
        ...rings.map((r, i) => ({ at: 2.3 + i * 0.32, dur: 0.9, tick: (p: number) => { const e = easeOutCubic(p); r.style.opacity = String(e); r.setAttribute('rx', String(Math.max(0.01, ringTargets[i].rx * e))); r.setAttribute('ry', String(Math.max(0.01, ringTargets[i].ry * e))) } })),
        // 4. the stem grows up through the rings
        { at: 5.0, dur: 1.0, tick: (p: number) => { const e = easeOutCubic(p); halo.style.strokeDashoffset = String(haloLen * (1 - e)); stem.style.strokeDashoffset = String(stemLen * (1 - e)) } },
        // 5. leaves unfurl — then the mark rests (plays once, no perpetual motion)
        { at: 5.9, dur: 0.6, tick: (p: number) => { const e = easeOutBack(p); leafL.setAttribute('transform', 'translate(110,44) scale(' + e + ') translate(-110,-44)') } },
        { at: 6.1, dur: 0.6, tick: (p: number) => { const e = easeOutBack(p); leafR.setAttribute('transform', 'translate(110,36) scale(' + e + ') translate(-110,-36)') } },
      ]
      const total = 6.9

      const start = performance.now()
      const frame = (now: number) => {
        const t = (now - start) / 1000
        for (const s of steps) if (t >= s.at) s.tick(Math.min(1, (t - s.at) / s.dur))
        if (t < total) rafId = requestAnimationFrame(frame)
      }
      rafId = requestAnimationFrame(frame)
    }

    svg.addEventListener('click', play)
    play()
    return () => {
      cancelAnimationFrame(rafId)
      svg.removeEventListener('click', play)
    }
  }, [])

  return (
    <svg
      ref={svgRef}
      className={className}
      viewBox="0 0 220 244"
      role="img"
      aria-label="Grained mark: a seed falls into an open Bible and grows into a ringed sprout"
      style={{ cursor: 'pointer' }}
    >
      <g id="growGroup">
        <g fill="none" stroke="#C29A45">
          <ellipse className="ring" cx="110" cy="128" rx="19" ry="18.1" strokeWidth="4.0" transform="rotate(-3 110 128)" />
          <ellipse className="ring" cx="108.5" cy="129.5" rx="33" ry="31.6" strokeWidth="2.6" transform="rotate(2 108.5 129.5)" />
          <ellipse className="ring" cx="111" cy="127" rx="47" ry="45.8" strokeWidth="4.4" transform="rotate(-2 111 127)" />
          <ellipse className="ring" cx="109" cy="129" rx="61" ry="59.0" strokeWidth="2.4" transform="rotate(3 109 129)" />
          <ellipse className="ring" cx="110.5" cy="127.5" rx="75" ry="72.8" strokeWidth="3.8" transform="rotate(-1.5 110.5 127.5)" />
          <ellipse className="ring" cx="109.5" cy="128.5" rx="88" ry="85.6" strokeWidth="2.8" transform="rotate(1 109.5 128.5)" />
        </g>
        <path id="halo" d="M 110 130 C 107.5 102, 112.5 72, 110 38" fill="none" stroke="#F8F5EC" strokeWidth="15" strokeLinecap="round" />
        <path id="stem" d="M 110 123 C 107.5 100, 112.5 72, 110 40" fill="none" stroke="#2E6B3E" strokeWidth="5.2" strokeLinecap="round" />
        <path id="leafL" d="M 110 44 Q 90 40 83 17 Q 105 21 110 44 Z" fill="#2E6B3E" />
        <path id="leafR" d="M 110 36 Q 126 31 133 8 Q 113 13 110 36 Z" fill="#97C25B" />
        <circle id="seed" cx="110" cy="128" r="6" fill="#C29A45" />
      </g>
      <g id="bible">
        <g transform="translate(20,190)">
          <path d="M 90 8 C 72 -1, 48 -2, 20 4 L 20 28 C 48 22, 72 23, 90 32 C 108 23, 132 22, 160 28 L 160 4 C 132 -2, 108 -1, 90 8 Z" fill="#1E4B2C" />
          <g stroke="#F8F5EC" strokeLinecap="round" fill="none" opacity="0.9">
            <path d="M 54 4 L 54 22" strokeWidth="2.8" />
            <path d="M 46.5 10 L 61.5 10" strokeWidth="2.8" />
            <path d="M 98 11 C 112 5, 130 5, 148 10" strokeWidth="1.8" />
            <path d="M 98 19 C 112 13, 130 13, 148 18" strokeWidth="1.8" />
          </g>
          <path d="M 90 9 L 90 31" stroke="#F8F5EC" strokeWidth="2.4" strokeLinecap="round" />
          <g fill="none" stroke="#1E4B2C" strokeLinecap="round">
            <path d="M 26 34 C 50 28.5, 72 29.5, 90 38 C 108 29.5, 130 28.5, 154 34" strokeWidth="3.2" />
            <path d="M 33 40 C 54 35, 72 36, 90 44 C 108 36, 126 35, 147 40" strokeWidth="3.2" />
          </g>
        </g>
      </g>
    </svg>
  )
}
