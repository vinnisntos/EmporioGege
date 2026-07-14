namespace EmporioGege.Core.Interfaces
{
    public interface ITenantProvider
    {
        Guid? TenantId { get; }

        Guid RequireTenantId();
    }
}
