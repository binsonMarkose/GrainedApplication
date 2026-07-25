// The Grained app-icon: the mark on its own dark-green rounded tile, with a GOLD Bible so it reads
// on the green ground (the plain <Logo> uses a dark-green Bible meant for light surfaces, which
// vanishes on a green tile). Use this anywhere the mark sits on a green/dark background.
export function LogoTile({ className }: { className?: string }) {
  return (
    <svg className={className} viewBox="0 0 512 512" role="img" aria-label="Grained">
      <rect width="512" height="512" rx="112" fill="#1E4B2C" />
      <g transform="translate(58,38) scale(1.8)">
        <g fill="none" stroke="#D9B45C">
          <ellipse cx="110" cy="128" rx="19" ry="18.1" strokeWidth="4.0" transform="rotate(-3 110 128)" />
          <ellipse cx="108.5" cy="129.5" rx="33" ry="31.6" strokeWidth="2.6" transform="rotate(2 108.5 129.5)" />
          <ellipse cx="111" cy="127" rx="47" ry="45.8" strokeWidth="4.4" transform="rotate(-2 111 127)" />
          <ellipse cx="109" cy="129" rx="61" ry="59.0" strokeWidth="2.4" transform="rotate(3 109 129)" />
          <ellipse cx="110.5" cy="127.5" rx="75" ry="72.8" strokeWidth="3.8" transform="rotate(-1.5 110.5 127.5)" />
          <ellipse cx="109.5" cy="128.5" rx="88" ry="85.6" strokeWidth="2.8" transform="rotate(1 109.5 128.5)" />
        </g>
        <path d="M 110 130 C 107.5 102, 112.5 72, 110 38" fill="none" stroke="#1E4B2C" strokeWidth="15" strokeLinecap="round" />
        <circle cx="110" cy="128" r="6" fill="#D9B45C" />
        <path d="M 110 123 C 107.5 100, 112.5 72, 110 40" fill="none" stroke="#A8CD9C" strokeWidth="5.2" strokeLinecap="round" />
        <path d="M 110 44 Q 90 40 83 17 Q 105 21 110 44 Z" fill="#A8CD9C" />
        <path d="M 110 36 Q 126 31 133 8 Q 113 13 110 36 Z" fill="#CDE3C2" />
        <g transform="translate(20,190)">
          <path d="M 90 8 C 72 -1, 48 -2, 20 4 L 20 28 C 48 22, 72 23, 90 32 C 108 23, 132 22, 160 28 L 160 4 C 132 -2, 108 -1, 90 8 Z" fill="#D9B45C" />
          <g stroke="#1E4B2C" strokeLinecap="round" fill="none" opacity="0.9">
            <path d="M 54 4 L 54 22" strokeWidth="2.8" />
            <path d="M 46.5 10 L 61.5 10" strokeWidth="2.8" />
            <path d="M 98 11 C 112 5, 130 5, 148 10" strokeWidth="1.8" />
            <path d="M 98 19 C 112 13, 130 13, 148 18" strokeWidth="1.8" />
          </g>
          <path d="M 90 9 L 90 31" stroke="#1E4B2C" strokeWidth="2.4" strokeLinecap="round" />
          <g fill="none" stroke="#D9B45C" strokeLinecap="round">
            <path d="M 26 34 C 50 28.5, 72 29.5, 90 38 C 108 29.5, 130 28.5, 154 34" strokeWidth="3.2" />
            <path d="M 33 40 C 54 35, 72 36, 90 44 C 108 36, 126 35, 147 40" strokeWidth="3.2" />
          </g>
        </g>
      </g>
    </svg>
  )
}
