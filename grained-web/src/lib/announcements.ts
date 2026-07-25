import { api } from './api'
import type { InboxAnnouncement } from '../types'

// Recipient inbox helpers, shared by the pop-up, the Announcements tab, and the nav unread badge.
// A tiny pub/sub lets one place mark something read and the others refresh without prop-drilling.

const listeners = new Set<() => void>()

export function onAnnouncementsChanged(cb: () => void) {
  listeners.add(cb)
  return () => {
    listeners.delete(cb)
  }
}
function notifyChanged() {
  listeners.forEach((l) => l())
}

export function fetchInbox() {
  return api<InboxAnnouncement[]>('/my/announcements')
}

export async function markAnnouncementRead(id: string) {
  await api(`/my/announcements/${id}/read`, { method: 'POST' })
  notifyChanged()
}

export async function markAllAnnouncementsRead() {
  await api('/my/announcements/read-all', { method: 'POST' })
  notifyChanged()
}
