# Grained — marketing landing page

Static marketing site for **grained.org** (the apex domain). This is deliberately **separate**
from the Blazor app (`Grained.AdminWeb`, which lives at `app.grained.org`) — a copy tweak here
should never require an app redeploy, and this page must stay up even while the app is deploying.

## Files

| File | Purpose |
|---|---|
| `index.html` | The whole page — self-contained (inline CSS/JS, animated SVG logo). |
| `favicon.svg` | Browser-tab icon — the official Grained app-icon tile (scalable). |
| `favicon-32.png` | PNG favicon fallback for older browsers (32×32). |
| `apple-touch-icon.png` | iOS home-screen icon (180×180). |
| `icon-512.png` | Large / PWA icon (512×512). |
| `og-image.png` | Social share card (1200×630) for WhatsApp / Facebook / LinkedIn / X. |

The icon PNGs are rendered from the canonical `../brand/grained-icon-bible.svg`. Canonical brand
source art (icon + primary + stacked logos) lives in the repo-root `brand/` folder.

## Before it goes live — 1 thing

The signup forms post to Formspree with a **placeholder ID**. Until it's set, the form falls back
to opening the visitor's mail client to `binson.markose@grained.org` (no signup is lost).

To capture signups properly:
1. Create a free form at https://formspree.io (register with a grained.org address).
2. Replace **both** occurrences of `YOUR_FORM_ID` in `index.html` with your real form ID.

## Deploy (Cloudflare Pages — recommended)

1. Cloudflare dashboard → **Workers & Pages** → **Create** → **Pages** → **Upload assets**.
2. Drag this `landing/` folder in. (Or connect the repo and set the build output dir to `landing`.)
3. Add the custom domain `grained.org` (and `www.grained.org`) under the Pages project.

**Netlify Drop** is the 2-minute alternative: https://app.netlify.com/drop — drag the folder,
then point DNS.

## DNS reminder

- `grained.org` (apex) → this static site.
- `app.grained.org` → the Blazor app VPS.
- Church public sites later → `*.grained.org` wildcard to the app.

## Editing the share image / icons

`og-image.png` and `icon-512.png` were rendered from brand templates. To regenerate after a design
change, re-render a 1200×630 (share) / 512×512 (icon) grove-green canvas with the logo + wordmark.
When you change `og-image.png`, re-share the link through
https://developers.facebook.com/tools/debug/ to bust the scraper cache.
