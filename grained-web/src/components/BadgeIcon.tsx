// A curated set of fun badge icons the admin can pick from, plus a medallion renderer.
export const BADGE_ICONS = [
  '⭐', '🏆', '🥇', '🌟', '🎖️', '👑', '💎', '🔥',
  '📖', '🙏', '❤️', '🕊️', '🌈', '✝️', '🐑', '🌱',
  '🎯', '🎨', '🎵', '☀️', '😇', '🤝', '💪', '🧠',
]

// A circular gold medallion with the icon inside — reads as an award, not just an emoji.
export function BadgeIcon({ icon, size = 44 }: { icon?: string | null; size?: number }) {
  return (
    <span
      className="grid shrink-0 place-items-center rounded-full text-oncream shadow-sm ring-2 ring-white"
      style={{
        width: size,
        height: size,
        background: 'radial-gradient(circle at 32% 28%, #E9CE7E 0%, #D9B45C 45%, #C29A45 100%)',
        fontSize: size * 0.5,
      }}
      role="img"
      aria-label="badge"
    >
      {icon || '🏅'}
    </span>
  )
}
