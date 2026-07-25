import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter, Routes, Route } from 'react-router-dom'
import './index.css'
import { AuthProvider } from './auth/AuthContext'
import { ToastProvider } from './components/Toast'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { WorkspaceGate } from './components/WorkspaceGate'
import { Login } from './pages/Login'
import { ForgotPassword } from './pages/ForgotPassword'
import { ResetPassword } from './pages/ResetPassword'
import { AcceptInvite } from './pages/AcceptInvite'
import { Home } from './pages/Home'
import { Churches } from './pages/Churches'
import { ClassGroups } from './pages/ClassGroups'
import { Teachers } from './pages/Teachers'
import { Children } from './pages/Children'
import { Lessons } from './pages/Lessons'
import { LessonEditor } from './pages/LessonEditor'
import { Events } from './pages/Events'
import { EventEditor } from './pages/EventEditor'
import { Campaigns } from './pages/Campaigns'
import { CampaignEditor } from './pages/CampaignEditor'
import { Storefront } from './pages/public/Storefront'
import { PublicEvent } from './pages/public/PublicEvent'
import { PublicCampaign } from './pages/public/PublicCampaign'
import { Attendance } from './pages/Attendance'
import { Badges } from './pages/Badges'
import { Growth } from './pages/Growth'
import { Reports } from './pages/Reports'
import { Announcements } from './pages/Announcements'
import { Inbox } from './pages/Inbox'
import { Settings } from './pages/Settings'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <ToastProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="/accept-invite" element={<AcceptInvite />} />
          {/* Public, login-free storefront + event registration */}
          <Route path="/p/:slug" element={<Storefront />} />
          <Route path="/p/events/:id" element={<PublicEvent />} />
          <Route path="/p/campaigns/:id" element={<PublicCampaign />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <WorkspaceGate />
              </ProtectedRoute>
            }
          >
            <Route index element={<Home />} />
            <Route path="churches" element={<Churches />} />
            <Route path="class-groups" element={<ClassGroups />} />
            <Route path="teachers" element={<Teachers />} />
            <Route path="children" element={<Children />} />
            <Route path="lessons" element={<Lessons />} />
            <Route path="lessons/new" element={<LessonEditor />} />
            <Route path="lessons/:id" element={<LessonEditor />} />
            <Route path="events" element={<Events />} />
            <Route path="events/new" element={<EventEditor />} />
            <Route path="events/:id" element={<EventEditor />} />
            <Route path="campaigns" element={<Campaigns />} />
            <Route path="campaigns/new" element={<CampaignEditor />} />
            <Route path="campaigns/:id" element={<CampaignEditor />} />
            <Route path="attendance" element={<Attendance />} />
            <Route path="badges" element={<Badges />} />
            <Route path="growth" element={<Growth />} />
            <Route path="reports" element={<Reports />} />
            <Route path="announcements" element={<Announcements />} />
            <Route path="inbox" element={<Inbox />} />
            <Route path="settings" element={<Settings />} />
          </Route>
        </Routes>
      </BrowserRouter>
      </ToastProvider>
    </AuthProvider>
  </StrictMode>,
)
