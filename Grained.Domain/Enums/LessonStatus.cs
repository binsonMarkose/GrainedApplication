namespace Grained.Domain.Enums;

// A lesson's authoring lifecycle. A teacher drafts and submits; a ChurchAdmin reviews and publishes.
// (IsPublished on Lesson is kept in lock-step with Status == Published for existing "is it live?" reads.)
public enum LessonStatus
{
    Draft = 0,
    InReview = 1,
    Published = 2,
}
