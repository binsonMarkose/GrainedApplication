// A fun, self-contained avatar set for kids — an emoji face on a colourful gradient.
// The `id` is what we persist on the child; everything visual lives here on the front end.
export interface AvatarDef {
  id: string
  emoji: string
  label: string
  from: string
  to: string
}

export const AVATARS: AvatarDef[] = [
  { id: 'fox', emoji: '🦊', label: 'Fox', from: '#F6A96B', to: '#E8792E' },
  { id: 'panda', emoji: '🐼', label: 'Panda', from: '#DCE6DD', to: '#A8CD9C' },
  { id: 'lion', emoji: '🦁', label: 'Lion', from: '#F2C879', to: '#D9A441' },
  { id: 'owl', emoji: '🦉', label: 'Owl', from: '#BCA9E4', to: '#8A6FC7' },
  { id: 'frog', emoji: '🐸', label: 'Frog', from: '#B7DCAC', to: '#3E8E4E' },
  { id: 'bunny', emoji: '🐰', label: 'Bunny', from: '#F7CAD4', to: '#E88BA0' },
  { id: 'bear', emoji: '🐻', label: 'Bear', from: '#DCB78E', to: '#B07E4E' },
  { id: 'cat', emoji: '🐱', label: 'Cat', from: '#F4B183', to: '#E08E45' },
  { id: 'penguin', emoji: '🐧', label: 'Penguin', from: '#A6CBE8', to: '#5A93C7' },
  { id: 'unicorn', emoji: '🦄', label: 'Unicorn', from: '#EAC9F1', to: '#C58FE0' },
  { id: 'chick', emoji: '🐥', label: 'Chick', from: '#F8E495', to: '#EFC948' },
  { id: 'dog', emoji: '🐶', label: 'Puppy', from: '#EACDA6', to: '#C89A5E' },
  { id: 'turtle', emoji: '🐢', label: 'Turtle', from: '#AFD8A4', to: '#4E9E6B' },
  { id: 'butterfly', emoji: '🦋', label: 'Butterfly', from: '#AEDCE4', to: '#6FBAC7' },
  { id: 'ladybug', emoji: '🐞', label: 'Ladybug', from: '#F2A6A6', to: '#D95C5C' },
  { id: 'sheep', emoji: '🐑', label: 'Lamb', from: '#E7ECEF', to: '#B7C3CA' },
]

export function avatarById(id?: string | null) {
  return AVATARS.find((a) => a.id === id)
}

function initials(name: string) {
  const parts = name.trim().split(/\s+/)
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase() || '·'
}

export function Avatar({
  avatarId,
  name,
  size = 40,
  ring = false,
  title,
}: {
  avatarId?: string | null
  name: string
  size?: number
  ring?: boolean
  title?: string // hover tooltip; defaults to the avatar label (or the name for the initials fallback)
}) {
  const a = avatarById(avatarId)
  const ringCls = ring ? 'ring-2 ring-white shadow-sm' : ''
  if (!a) {
    return (
      <span
        className={`grid shrink-0 place-items-center rounded-full bg-leaf-light/40 font-semibold text-heading ${ringCls}`}
        style={{ width: size, height: size, fontSize: size * 0.36 }}
        title={title ?? name}
      >
        {initials(name)}
      </span>
    )
  }
  return (
    <span
      className={`grid shrink-0 place-items-center rounded-full ${ringCls}`}
      style={{ width: size, height: size, background: `linear-gradient(135deg, ${a.from}, ${a.to})`, fontSize: size * 0.55 }}
      role="img"
      aria-label={title ?? a.label}
      title={title ?? a.label}
    >
      {a.emoji}
    </span>
  )
}
