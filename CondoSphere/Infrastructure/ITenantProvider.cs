namespace CondoSphere.Infrastructure
{
    public interface ITenantProvider
    {
        int? CompanyId { get; }
        string? UserId { get; }
    }
}
