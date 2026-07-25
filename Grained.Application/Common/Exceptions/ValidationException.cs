namespace Grained.Application.Common.Exceptions;

// Thrown by application services for business-rule violations that a Blazor page
// should display back to the user (e.g. "cannot publish lesson without a quiz").
public class ValidationException(string message) : Exception(message);
