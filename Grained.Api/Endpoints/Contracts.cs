namespace Grained.Api.Endpoints;

// Small request bodies shared across endpoints.
public record SetActiveRequest(bool IsActive);
public record ClassGroupRef(Guid ClassGroupId);
public record SendBackRequest(string? Note);
public record ReorderLessonsRequest(Guid ClassGroupId, List<Guid> LessonIds);
