# Grained — Design System (for Figma redesign)

Everything below is what the current React app uses. **Keep the palette and type pairing;** redesign
layout/components freely. Values are the source of truth — a Figma-importable token file is at
`design/grained.tokens.json` (import with the *Tokens Studio for Figma* plugin).

---

## 1. Colour palette

Brand tokens (primitives). Hex is authoritative.

| Token | Hex | Role |
|---|---|---|
| **Grove** | `#1E4B2C` | Primary brand green — sidebar, primary buttons, headings on light |
| Grove deep | `#16371F` | Hover/darker green, gradient end |
| **Wheat gold** | `#C29A45` | Primary accent — logo rings, focus ring, links/accents |
| Gold soft | `#D9B45C` | Lighter gold — active-nav tint, badges, tagline on dark |
| Gold deep | `#8A6D1D` | Readable gold for text on light (e.g. "Pending" pill text) |
| **Leaf** | `#2E6B3E` | Secondary green — sprout, stat accents, links |
| Leaf light | `#A8CD9C` | Pale green — "Active/Present" pill fills, success tints |
| Leaf bright | `#3E8A50` | Logo second leaf only |
| **Cream** | `#F8F5EC` | App background, sidebar text, cards on green |
| Cream deep | `#F1EBDC` | Borders, table header fill, subtle fills |
| **Ink** | `#22301F` | Body text (near-black green) |

Opacity conventions (Tailwind `/NN` = alpha): text uses `ink/70` (labels), `ink/55–60` (muted),
`ink/45–50` (hints). On the green sidebar: `cream`, `cream/85` (nav), `cream/55` (subtle).

**Status colours** (Tailwind defaults, only for validation/errors — not brand):
error text `#B91C1C` (red-700) on `#FEF2F2` (red-50) with `#FECACA` (red-200) border.

### Semantic mapping (how the primitives are used)
- Primary action → **Grove** bg, Cream text, hover **Grove deep**.
- Accent / focus → **Gold** (2px ring at ~30% alpha).
- Links → **Grove** (underline on hover).
- Success/positive pill → **Leaf light @ 40%** fill + **Grove deep** text.
- Pending/warning pill → **Gold soft @ 25%** fill + **Gold deep** text.
- Neutral/disabled pill → **Cream deep** fill + **Ink @ 50%** text.
- Surfaces → white cards on **Cream** page; **Cream** cards on green auth screens.

---

## 2. Typography

Two Google fonts. **Fraunces** (a warm serif) for display; **Inter** for UI/body.

| Style | Font | Weight | Size (px/rem) | Colour |
|---|---|---|---|---|
| Wordmark "grained" | Fraunces | 500 | 24–38 / — | Grove deep, letter-spacing ~0.05em |
| Tagline | Fraunces italic | 400 | 14 / .875 | Gold |
| Page title (H1) | Fraunces | 500 | 30 / 1.875 | Grove deep |
| Section title (H2) | Fraunces | 500 | 18–20 / 1.125–1.25 | Grove deep |
| Stat number | Fraunces | 500 | 36 / 2.25 | Grove deep |
| Body / table | Inter | 400 | 14 / .875 | Ink |
| Label | Inter | 500 | 14 / .875 | Ink @ 70% |
| Muted / hint | Inter | 400/500 | 12 / .75 | Ink @ 45–55% |
| Button | Inter | 600 | 14 / .875 | per variant |

Line-height ~1.15 for display, ~1.5 for body. (The marketing landing page also uses Fraunces+Inter;
same pairing.)

---

## 3. Shape, spacing, elevation

- **Radii:** buttons/inputs `rounded-xl` = **12px**; cards/tables/modals `rounded-2xl` = **16px**;
  auth cards `rounded-3xl` = **24px**; pills/avatars = **full**; logo badge `rounded-xl`.
- **Spacing scale** (Tailwind, 4px base): page padding **24px** desktop / **16px** mobile; card
  padding **20px**; form field gap **16px**; inline gaps **8–12px**.
- **Borders:** 1px, colour **Cream deep** (`#F1EBDC`).
- **Shadows:** cards `shadow-sm` (0 1px 2px rgba(0,0,0,.05)); hover `shadow-md`; modals & auth cards
  `shadow-2xl` (deep, ~0 18px 50px rgba(22,55,31,.35) tint on the green auth screens).
- **Sidebar width:** 256px (16rem). **Content max-width:** ~64rem (list pages ~5xl, editor ~3xl).

---

## 4. Components (current specs)

**Button** — `rounded-xl`, `px-16 py-8` (16/8px), Inter 600, 14px.
- Primary: Grove bg / Cream text → hover Grove deep.
- Gold: Gold bg / white text.
- Outline: white bg, Cream-deep border, Grove text → hover Cream.
- Ghost: transparent, Grove text → hover Cream bg.
- Danger: white bg, red-200 border, red-600 text.

**Input / Field** — label (Inter 500, 14, ink/70) above control. Control: white bg, Cream-deep
border, `rounded-xl`, padding 14/10px, focus = Gold border + 2px Gold@30% ring. Error text red-600 12px.

**Card** — white bg, 1px Cream-deep border, `rounded-2xl`, `shadow-sm`, padding 20px.

**Pill (status)** — `rounded-full`, `px-10 py-2` (10/2px), Inter 600, 12px. Tones: green / gold /
gray / red per §1 semantic mapping.

**Table → DataTable** — desktop: header row filled Cream-deep, semibold ink/70; body rows separated
by Cream-deep top borders; the whole table in a `rounded-2xl` white card. **Mobile: each row becomes
a stacked card** (primary field as title, others as label→value pairs, actions as a button row) — no
horizontal scroll. Disabled rows at ~55% opacity.

**Modal** — full-screen backdrop Ink@40% + blur; centered panel: Cream bg, `rounded-2xl`, header row
with title (Fraunces) + ✕, divider (Cream-deep), body padding 20px. Widths: 28rem default / 42rem wide.

**Sidebar (AppShell)** — Grove→Grove-deep vertical gradient, 256px. Top: logo in a Cream `rounded-xl`
badge + "grained" wordmark (Cream). Nav items: Cream/85 text, hover white@10%; **active** = Gold-soft@20%
fill + 3px inset Gold-soft left border + Cream text. Bottom: user avatar (initials, Gold-soft@25%
circle) + role + "Log out" outline button. **Mobile:** off-canvas drawer + hamburger in the top bar.

**Top bar** — white@70% + blur, 1px Cream-deep bottom border; right-aligned "👋 {name}".

**Stat card (dashboard)** — white `rounded-2xl` card; a 4px top accent bar as a gradient
(grove / gold / leaf / ink variants); big Fraunces number (Grove deep) + label (ink/55) + emoji chip.

**Auth card (login / onboarding / forgot / reset)** — full-screen **Grove→Leaf** diagonal gradient;
centered Cream card `rounded-3xl` `shadow-2xl`, 32px padding; header = logo + "grained" (Fraunces) +
gold italic tagline; then the form. Max-width ~24rem.

**Empty state** — dashed Cream-deep border, white/50 bg, `rounded-2xl`, centered emoji + Fraunces
title + hint.

---

## 5. Logo & iconography

- Canonical logo art: `brand/grained-icon-bible.svg` (app-icon tile), `grained-logo-horizontal.svg`
  (horizontal), `grained-logo-stacked.svg` (stacked, light), `grained-logo-stacked-dark.svg` (stacked,
  dark ground). Mark = open Bible with a cross + gold tree-ring cross-section + gold seed + green
  sprout. On light surfaces the cream "halo" behind the stem is invisible by design.
- **Logo wordmark = Georgia, converted to vector outlines** in the SVGs (self-contained; renders
  identically with no font installed). This is deliberate — the *logo* keeps the Georgia look, while
  the *app/site UI* display font is **Fraunces**. Don't "fix" the logo to Fraunces without asking.
  (To re-outline in another font, the pipeline is `scratchpad/outliner/outline.mjs` via opentype.js.)
- In-app, place the mark on a **Cream** chip when it sits on the green sidebar.
- Nav/section icons are currently emoji (📊 🧒 🍎 👧 📖 ✅ 🏅 📈 ⛪) — a redesign could swap these
  for a line-icon set in Grove/Gold; keep them simple and single-weight.

---

## 6. Page templates to redesign

1. **Auth** — centered card on the Grove→Leaf gradient (login, /accept-invite onboarding,
   /forgot-password, /reset-password).
2. **App shell** — left Grove sidebar (role-aware nav) + top bar + Cream content area.
3. **List page** — PageHeader (title + subtitle + primary action) → DataTable → Modal create/edit.
4. **Editor page** — back link + Cards for grouped sections (e.g. lesson: details / class assignment /
   quiz questions) + Publish toggle.
5. **Dashboard** — greeting (Fraunces) → 4 stat cards → two panels (recent activity).

---

*Keep grove-green + wheat-gold + cream. That combination is the brand.*
