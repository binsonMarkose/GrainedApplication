// ---- Auth / user ----
export interface ParentLookup {
  exists: boolean
  name: string | null
  isStaff: boolean
  alreadyParent: boolean
}

export interface User {
  id: string
  email: string
  fullName: string
  churchId: string | null
  roles: string[]
  mustChangePassword: boolean
}
export interface LoginResponse {
  token: string
  expiresAtUtc: string
  user: User
}

// ---- Dashboard ----
export interface RecentAttendance {
  childName: string
  classGroupName: string
  attendanceDate: string
  isPresent: boolean
}
export interface RecentCompletion {
  childName: string
  lessonTitle: string
  completedAtUtc: string
}
export interface DashboardSummary {
  totalChildren: number
  totalTeachers: number
  totalClasses: number
  publishedLessons: number
  recentAttendance: RecentAttendance[]
  recentLessonCompletions: RecentCompletion[]
}

// ---- Teacher workspace (scoped teacher dashboard) ----
export interface TeacherWorkspaceBadge {
  badgeId: string
  name: string
  iconName: string | null
  count: number
}
export interface TeacherWorkspaceChild {
  id: string
  firstName: string
  lastName: string
  age: number
  avatarId: string | null
  badges: TeacherWorkspaceBadge[]
}
export interface TeacherWorkspaceLesson {
  id: string
  title: string
  bibleReference: string
  theme: string | null
  memoryVerseReference: string | null
  completedCount: number
}
export interface TeacherWorkspaceClass {
  classGroupId: string
  name: string
  minAge: number
  maxAge: number
  description: string | null
  lessons: TeacherWorkspaceLesson[]
  children: TeacherWorkspaceChild[]
}
export interface TeacherWorkspace {
  teacherName: string
  classes: TeacherWorkspaceClass[]
}
export interface TeacherBadge {
  id: string
  name: string
  description: string | null
  iconName: string | null
}

// ---- Parent workspace (parent dashboard) ----
export type ParentLessonStatus = 'Completed' | 'Missed' | 'Upcoming'
export interface ParentLesson {
  id: string
  title: string
  bibleReference: string
  theme: string | null
  memoryVerseReference: string | null
  memoryVerseText: string | null
  status: ParentLessonStatus
  completedAtUtc: string | null
}
export interface ParentBadge {
  badgeId: string
  name: string
  description: string | null
  iconName: string | null
  awardedAtUtc: string
  count: number
}
export interface ParentLessonDetail {
  id: string
  title: string
  bibleReference: string
  theme: string | null
  storyContent: string
  learningObjective: string | null
  activity: string | null
  prayer: string | null
  memoryVerseReference: string | null
  memoryVerseText: string | null
}
export interface ParentChild {
  id: string
  firstName: string
  lastName: string
  age: number
  classGroupName: string
  avatarId: string | null
  growth: GrowthSummary
  completedCount: number
  missedCount: number
  upcomingCount: number
  totalLessons: number
  lessons: ParentLesson[]
  badges: ParentBadge[]
}
export interface ParentWorkspace {
  parentName: string
  children: ParentChild[]
}

// ---- Churches (SuperAdmin) ----
export interface Church {
  id: string
  name: string
  address: string | null
  email: string
  phone: string | null
  isActive: boolean
  status: string // "Pending" | "Active" | "Suspended"
  createdAtUtc: string
}
export interface ChurchForm {
  id?: string
  name: string
  address?: string | null
  email: string
  phone?: string | null
}
// Onboarding: SuperAdmin provisions with just name + admin email.
export interface CreateChurchResult {
  churchId: string
  status: string
  acceptUrl: string | null // dev-only convenience link
}
export interface InviteInfo {
  churchName: string
  email: string
}

// ---- Class groups ----
export interface ClassGroup {
  id: string
  churchId: string
  name: string
  minAge: number
  maxAge: number
  description: string | null
  isActive: boolean
  childCount: number
}
export interface ClassGroupForm {
  id?: string
  name: string
  minAge: number
  maxAge: number
  description?: string | null
}

// ---- Children ----
export interface Child {
  id: string
  churchId: string
  classGroupId: string
  classGroupName: string
  firstName: string
  lastName: string
  dateOfBirth: string
  age: number
  parentName: string
  parentEmail: string
  parentPhone: string | null
  isActive: boolean
}
export interface ChildForm {
  id?: string
  firstName: string
  lastName: string
  dateOfBirth: string
  classGroupId: string | null
  parentName: string
  parentEmail: string
  parentPhone?: string | null
}

// ---- Teachers ----
export interface Teacher {
  teacherProfileId: string
  applicationUserId: string
  fullName: string
  email: string
  isActive: boolean
  assignedClassGroupIds: string[]
  assignedClassGroupNames: string[]
}
export interface TeacherForm {
  teacherProfileId?: string
  fullName: string
  email: string
  assignedClassGroupIds: string[]
}
export interface TeacherCreatedResult {
  teacherProfileId: string
  temporaryPassword: string
}

// ---- Badges ----
// 0 = Standard (teacher), 1 = Achievement (admin milestone)
export type BadgeTier = 0 | 1
export interface Badge {
  id: string
  churchId: string
  name: string
  description: string | null
  iconName: string | null
  criteria: string | null
  tier: BadgeTier
  points: number
  isActive: boolean
  repeatable: boolean
}
export interface BadgeForm {
  id?: string
  name: string
  description?: string | null
  iconName?: string | null
  criteria?: string | null
  tier: BadgeTier
  points: number
  repeatable: boolean
}

// ---- Growth journey ----
export interface GrowthForestEntry {
  seasonName: string
  stageIndex: number
  stageName: string
  stageEmoji: string
  growthPoints: number
}
export interface GrowthSummary {
  seasonName: string
  stageIndex: number
  stageName: string
  stageEmoji: string
  growthPoints: number
  stageFloor: number
  nextStageAt: number | null
  nextStageName: string | null
  lessonsCompleted: number
  sundaysAttended: number
  versesLearned: number
  badgeCount: number
  achievementCount: number
  forest: GrowthForestEntry[]
}
export interface GrowthSeason {
  id: string
  name: string
  startsOnUtc: string
  endsOnUtc: string
  weeks: number
  harvestPoints: number
  isCurrent: boolean
}
export interface GrowthSeasonForm {
  name?: string | null
  startsOnUtc: string
  endsOnUtc: string
}
export interface ChildStage {
  childId: string
  stageIndex: number
  stageName: string
  stageEmoji: string
  growthPoints: number
}

// ---- Lessons ----
// 0 = Draft, 1 = InReview, 2 = Published
export type LessonStatus = 0 | 1 | 2
export interface LessonListItem {
  id: string
  churchId: string
  title: string
  bibleReference: string
  theme: string | null
  ageGroup: string
  isPublished: boolean
  status: LessonStatus
  authorUserId: string | null
  authorName: string | null
  createdAtUtc: string
  updatedAtUtc: string | null
  hasMemoryVerse: boolean
  questionCount: number
  assignedClassGroupNames: string[]
  assignedClassGroupIds: string[]
  lastCompletedAtUtc: string | null
}
export interface MemoryVerse {
  verseText: string
  bibleReference: string
  shortExplanation: string | null
}
export interface QuizOption {
  id: string
  optionText: string
  isCorrect: boolean
}
// QuestionType: 0 = SingleChoice, 1 = TrueFalse, 2 = FillInTheBlank
export type QuestionType = 0 | 1 | 2
export interface QuizQuestion {
  id: string
  questionText: string
  questionType: QuestionType
  points: number
  options: QuizOption[]
}
export interface Quiz {
  id: string
  title: string
  description: string | null
  questions: QuizQuestion[]
}
export interface LessonDetail {
  id: string
  churchId: string
  title: string
  bibleReference: string
  theme: string | null
  ageGroup: string
  storyContent: string
  learningObjective: string | null
  activity: string | null
  prayer: string | null
  isPublished: boolean
  status: LessonStatus
  authorUserId: string | null
  authorName: string | null
  reviewNote: string | null
  memoryVerse: MemoryVerse | null
  quiz: Quiz | null
  assignedClassGroupIds: string[]
  lastCompletedAtUtc: string | null
}
export interface LessonForm {
  id?: string
  title: string
  bibleReference: string
  theme?: string | null
  ageGroup: string
  storyContent: string
  learningObjective?: string | null
  activity?: string | null
  prayer?: string | null
  memoryVerse: {
    verseText?: string | null
    bibleReference?: string | null
    shortExplanation?: string | null
  }
}
export interface QuizQuestionForm {
  id?: string
  questionText: string
  questionType: QuestionType
  points: number
  options: { id?: string; optionText: string; isCorrect: boolean }[]
}

// ---- Events ----
export interface EventTicketType {
  id?: string
  name: string
  price: number
}
export interface EventListItem {
  id: string
  churchId: string
  title: string
  startDate: string
  endDate: string
  location: string | null
  enableTshirt: boolean
  isPublished: boolean
  isActive: boolean
  ticketTypeCount: number
}
export interface EventDetail {
  id: string
  churchId: string
  title: string
  startDate: string
  endDate: string
  location: string | null
  description: string | null
  enableTshirt: boolean
  isPublished: boolean
  isActive: boolean
  ticketTypes: EventTicketType[]
}
export interface EventForm {
  id?: string
  title: string
  startDate: string
  endDate: string
  location?: string | null
  description?: string | null
  enableTshirt: boolean
  ticketTypes: EventTicketType[]
}

// ---- Fundraising (ChurchAdmin) ----
export interface Campaign {
  id: string
  churchId: string
  title: string
  targetAmount: number | null
  raised: number
  logoImageId: string | null
  isPublished: boolean
  isActive: boolean
  donationCount: number
}
export interface CampaignDetail {
  id: string
  churchId: string
  title: string
  description: string | null
  targetAmount: number | null
  raised: number
  logoImageId: string | null
  isPublished: boolean
  isActive: boolean
}
export interface CampaignForm {
  id?: string
  title: string
  description?: string | null
  targetAmount?: number | null
}

// ---- Public storefront / registration ----
export interface PublicEventListItem {
  id: string
  title: string
  startDate: string
  endDate: string
  location: string | null
  fromPrice: number | null
}
export interface PublicCampaignListItem {
  id: string
  title: string
  targetAmount: number | null
  raised: number
  logoImageId: string | null
}
export interface PublicChurch {
  slug: string
  name: string
  events: PublicEventListItem[]
  campaigns: PublicCampaignListItem[]
}
export interface PublicCampaignDetail {
  id: string
  churchName: string
  title: string
  description: string | null
  targetAmount: number | null
  raised: number
  logoImageId: string | null
}
export interface DonationResult {
  donationId: string
  amount: number
  raised: number
  status: string
  reference: string
}
export interface PublicTicketType {
  id: string
  name: string
  price: number
}
export interface PublicEventDetail {
  id: string
  churchName: string
  title: string
  startDate: string
  endDate: string
  location: string | null
  description: string | null
  enableTshirt: boolean
  ticketTypes: PublicTicketType[]
}
export interface RegistrationResult {
  registrationId: string
  total: number
  currency: string
  status: string
  reference: string
}

// ---- Attendance ----
export interface RosterEntry {
  childId: string
  firstName: string
  lastName: string
  isPresent: boolean
  notes: string | null
}
export interface AttendanceSave {
  classGroupId: string
  attendanceDate: string
  lessonId?: string | null
  entries: { childId: string; isPresent: boolean; notes?: string | null }[]
}

// ---- Announcements ----
// 0 = Teachers, 1 = Parents, 2 = Everyone
export type AnnouncementAudience = 0 | 1 | 2
export interface Announcement {
  id: string
  title: string
  body: string
  audience: AnnouncementAudience
  audienceLabel: string
  createdByName: string
  createdAtUtc: string
  isActive: boolean
  readCount: number
}
export interface AnnouncementForm {
  title: string
  body: string
  audience: AnnouncementAudience
}
export interface InboxAnnouncement {
  id: string
  title: string
  body: string
  audienceLabel: string
  createdByName: string
  createdAtUtc: string
  isRead: boolean
}

// ---- Reports ----
export interface ChildProgressReportRow {
  childId: string
  childName: string
  avatarId: string | null
  classGroupName: string
  stageIndex: number
  stageName: string
  stageEmoji: string
  growthPoints: number
  lessonsCompleted: number
  versesLearned: number
  sundaysAttended: number
  badgeCount: number
  achievementCount: number
  averageQuizScore: number | null
}
export interface ChildBadgeReportRow {
  badgeId: string
  name: string
  description: string | null
  iconName: string | null
  tier: 0 | 1
  points: number
  awardedAtUtc: string
}
export interface ClassProgressReportRow {
  classGroupId: string
  classGroupName: string
  totalChildren: number
  totalLessonsCompleted: number
  averageCompletionRate: number
}
export interface AttendanceReportRow {
  classGroupId: string
  classGroupName: string
  totalSessions: number
  totalPresent: number
  totalAbsent: number
  attendanceRatePercent: number
}
export interface LessonCompletionReportRow {
  lessonId: string
  title: string
  isPublished: boolean
  completedCount: number
  averageQuizScore: number | null
}
