// Light / dark theme, driven by a data-theme attribute on <html> and persisted in localStorage.
// The dark palette lives in index.css (:root[data-theme="dark"]); this just flips the attribute.
export type Theme = 'light' | 'dark'
const KEY = 'grained.theme'

export function getStoredTheme(): Theme {
  return localStorage.getItem(KEY) === 'dark' ? 'dark' : 'light'
}

export function applyTheme(theme: Theme) {
  document.documentElement.setAttribute('data-theme', theme)
  localStorage.setItem(KEY, theme)
}
