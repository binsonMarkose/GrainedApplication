import { useEffect, useMemo, useState } from 'react'
import { api } from '../lib/api'
import { MarkLessonCompleteModal, type CompleteClass } from './MarkLessonCompleteModal'
import { Loading } from './ui'
import type { LessonDetail } from '../types'

// Full-screen "Teach mode": a distraction-free presenter view a teacher runs the class from. The
// lesson is broken into steps — Story → Memory verse → each Quiz question → Activity → Prayer — with
// big readable type (A−/A+), tap-to-reveal quiz answers, screen-keep-awake, and Mark-completed at the
// end. No schema change: it reads the existing lesson detail.

type Step =
  | { kind: 'story' }
  | { kind: 'verse' }
  | { kind: 'question'; index: number }
  | { kind: 'activity' }
  | { kind: 'prayer' }

const STEP_LABEL: Record<Step['kind'], string> = {
  story: 'The story',
  verse: 'Memory verse',
  question: 'Ask the class',
  activity: 'Activity',
  prayer: 'Prayer',
}

export function TeachMode({
  lessonId,
  classes = [],
  onClose,
  onCompleted,
}: {
  lessonId: string
  classes?: CompleteClass[]
  onClose: () => void
  onCompleted?: () => void
}) {
  const [lesson, setLesson] = useState<LessonDetail | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [stepIdx, setStepIdx] = useState(0)
  const [fontPx, setFontPx] = useState(22)
  const [revealed, setRevealed] = useState(false)
  const [showComplete, setShowComplete] = useState(false)

  useEffect(() => {
    api<LessonDetail>('/lessons/' + lessonId)
      .then(setLesson)
      .catch((e) => setError((e as Error).message))
  }, [lessonId])

  const steps = useMemo<Step[]>(() => {
    if (!lesson) return []
    const s: Step[] = []
    if (lesson.storyContent?.trim()) s.push({ kind: 'story' })
    if (lesson.memoryVerse?.verseText?.trim()) s.push({ kind: 'verse' })
    lesson.quiz?.questions.forEach((_, i) => s.push({ kind: 'question', index: i }))
    if (lesson.activity?.trim()) s.push({ kind: 'activity' })
    if (lesson.prayer?.trim()) s.push({ kind: 'prayer' })
    return s.length ? s : [{ kind: 'story' }]
  }, [lesson])

  const idx = Math.min(stepIdx, steps.length - 1)
  const step = steps[idx]
  const isLast = idx >= steps.length - 1

  useEffect(() => setRevealed(false), [idx])

  const go = (delta: number) => setStepIdx((i) => Math.max(0, Math.min(steps.length - 1, i + delta)))

  // Arrows navigate; Esc closes.
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'ArrowRight') go(1)
      else if (e.key === 'ArrowLeft') go(-1)
      else if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [steps.length])

  // Keep the screen awake while teaching (best-effort — not all browsers support the API).
  useEffect(() => {
    let lock: { release?: () => Promise<void> } | undefined
    const request = async () => {
      try {
        lock = await (navigator as unknown as { wakeLock?: { request: (t: string) => Promise<typeof lock> } }).wakeLock?.request('screen')
      } catch {
        /* unsupported / denied */
      }
    }
    request()
    const onVis = () => document.visibilityState === 'visible' && request()
    document.addEventListener('visibilitychange', onVis)
    return () => {
      document.removeEventListener('visibilitychange', onVis)
      void lock?.release?.()
    }
  }, [])

  const iconBtn =
    'grid size-9 place-items-center rounded-lg border border-cream-deep text-ink/70 transition hover:bg-cream'
  const completeBtn =
    'inline-flex items-center gap-1.5 rounded-xl bg-grove px-4 py-2 text-sm font-semibold text-oncream transition hover:bg-grove-deep'

  return (
    <div className="fixed inset-0 z-[60] flex flex-col bg-cream text-ink">
      {/* Top bar */}
      <div className="flex items-center gap-3 border-b border-cream-deep px-4 py-3">
        <button onClick={onClose} className={iconBtn} aria-label="Close teach mode">
          ✕
        </button>
        <div className="min-w-0 flex-1">
          <div className="truncate font-display text-lg text-heading">{lesson?.title ?? 'Loading…'}</div>
          {lesson && <div className="truncate text-xs text-ink/50">{lesson.bibleReference}</div>}
        </div>
        <div className="flex items-center gap-1">
          <button onClick={() => setFontPx((p) => Math.max(16, p - 2))} className={iconBtn} aria-label="Smaller text">
            A−
          </button>
          <button onClick={() => setFontPx((p) => Math.min(40, p + 2))} className={iconBtn} aria-label="Bigger text">
            A+
          </button>
        </div>
        {classes.length > 0 && (
          <button onClick={() => setShowComplete(true)} className={`${completeBtn} hidden sm:inline-flex`}>
            ✓ Mark completed
          </button>
        )}
      </div>

      {error && (
        <div className="flex flex-1 items-center justify-center p-8 text-center text-sm text-red-600">{error}</div>
      )}
      {!lesson && !error && <Loading label="Opening lesson…" />}

      {lesson && step && (
        <>
          <div className="px-4 pt-5 text-center text-xs font-semibold uppercase tracking-[0.2em] text-gold">
            {STEP_LABEL[step.kind]}
            {steps.length > 1 && ` · ${idx + 1} / ${steps.length}`}
          </div>

          <div className="flex-1 overflow-y-auto px-5 py-6">
            <div className="mx-auto max-w-3xl" style={{ fontSize: fontPx, lineHeight: 1.6 }}>
              {renderStep(lesson, step, revealed, () => setRevealed(true))}
            </div>
          </div>

          {/* Bottom nav */}
          <div className="flex items-center justify-between gap-3 border-t border-cream-deep px-4 py-3">
            <button
              onClick={() => go(-1)}
              disabled={idx === 0}
              className="rounded-xl border border-cream-deep px-4 py-2 text-sm font-semibold text-ink/70 transition enabled:hover:bg-cream disabled:opacity-30"
            >
              ← Back
            </button>
            <div className="flex flex-wrap justify-center gap-1.5">
              {steps.map((_, i) => (
                <button
                  key={i}
                  onClick={() => setStepIdx(i)}
                  aria-label={`Go to step ${i + 1}`}
                  className={`size-2.5 rounded-full transition ${i === idx ? 'bg-grove' : 'bg-cream-deep hover:bg-leaf-light'}`}
                />
              ))}
            </div>
            {isLast ? (
              classes.length > 0 ? (
                <button onClick={() => setShowComplete(true)} className={completeBtn}>
                  ✓ Mark completed
                </button>
              ) : (
                <button
                  onClick={onClose}
                  className="rounded-xl bg-grove px-4 py-2 text-sm font-semibold text-oncream transition hover:bg-grove-deep"
                >
                  Done
                </button>
              )
            ) : (
              <button
                onClick={() => go(1)}
                className="rounded-xl bg-grove px-5 py-2 text-sm font-semibold text-oncream transition hover:bg-grove-deep"
              >
                Next →
              </button>
            )}
          </div>
        </>
      )}

      {lesson && (
        <MarkLessonCompleteModal
          open={showComplete}
          lesson={{ id: lesson.id, title: lesson.title }}
          classes={classes}
          onClose={() => setShowComplete(false)}
          onCompleted={() => {
            onCompleted?.()
            onClose()
          }}
        />
      )}
    </div>
  )
}

function renderStep(lesson: LessonDetail, step: Step, revealed: boolean, reveal: () => void) {
  switch (step.kind) {
    case 'story':
      return (
        <div className="space-y-6">
          <p className="whitespace-pre-wrap text-ink/90">{lesson.storyContent}</p>
          {lesson.learningObjective && (
            <div className="rounded-2xl border border-leaf-light/60 bg-leaf-light/15 p-5">
              <div className="mb-1 text-[0.6em] font-bold uppercase tracking-[0.15em] text-gold">The big idea</div>
              <p className="font-display text-heading">{lesson.learningObjective}</p>
            </div>
          )}
        </div>
      )
    case 'verse':
      return (
        <div className="py-8 text-center">
          <blockquote className="font-display text-heading" style={{ fontSize: '1.45em', lineHeight: 1.5 }}>
            &ldquo;{lesson.memoryVerse!.verseText}&rdquo;
          </blockquote>
          <cite className="mt-5 block text-[0.7em] font-semibold uppercase not-italic tracking-[0.2em] text-gold">
            {lesson.memoryVerse!.bibleReference}
          </cite>
          {lesson.memoryVerse!.shortExplanation && (
            <p className="mt-4 text-[0.75em] text-ink/60">{lesson.memoryVerse!.shortExplanation}</p>
          )}
        </div>
      )
    case 'question': {
      const q = lesson.quiz!.questions[step.index]
      return (
        <div className="space-y-6">
          <p className="font-display text-heading" style={{ fontSize: '1.15em', lineHeight: 1.4 }}>
            {q.questionText}
          </p>
          <div className="space-y-2.5">
            {q.options.map((o) => (
              <div
                key={o.id}
                className={`flex items-center gap-2 rounded-xl border p-4 transition ${
                  revealed && o.isCorrect
                    ? 'border-leaf bg-leaf-light/25 font-semibold text-heading'
                    : 'border-cream-deep text-ink/80'
                }`}
              >
                {revealed && o.isCorrect && <span aria-hidden>✓</span>}
                <span>{o.optionText}</span>
              </div>
            ))}
          </div>
          {!revealed && (
            <button
              onClick={reveal}
              className="w-full rounded-xl border-2 border-dashed border-gold-soft/60 bg-gold-soft/10 py-3 text-[0.85em] font-semibold text-gold-ink transition hover:bg-gold-soft/20"
            >
              👀 Tap to reveal the answer
            </button>
          )}
        </div>
      )
    }
    case 'activity':
      return (
        <div className="rounded-2xl border border-cream-deep bg-white p-6">
          <div className="mb-2 text-[0.6em] font-bold uppercase tracking-[0.15em] text-gold">Try this together</div>
          <p className="whitespace-pre-wrap text-ink/90">{lesson.activity}</p>
        </div>
      )
    case 'prayer':
      return (
        <div className="rounded-2xl bg-gradient-to-b from-[#EAF3E6] to-cream p-8 text-center">
          <div className="mb-3 text-4xl" aria-hidden>
            🙏
          </div>
          <p className="whitespace-pre-wrap font-display text-heading">{lesson.prayer}</p>
        </div>
      )
  }
}
