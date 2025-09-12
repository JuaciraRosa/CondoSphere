namespace CondoSphere.Services
{
    public record OnlineMeetingResult(
      string Provider,
      string ExternalId,
      string JoinUrl,
      string? StartUrl
  );
}
