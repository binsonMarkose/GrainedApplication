namespace Grained.Application.Public;

public interface IPublicEventService
{
    Task<PublicChurchDto?> GetStorefrontAsync(string slug, CancellationToken ct = default);
    Task<PublicEventDto?> GetEventAsync(Guid eventId, CancellationToken ct = default);
    Task<RegistrationResultDto> RegisterAsync(Guid eventId, EventRegistrationModel model, CancellationToken ct = default);
}
