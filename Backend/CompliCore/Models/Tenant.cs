namespace CompliCore.Models
{


    /// <summary>
    /// Tenant is one customer company.
    /// It has no TenantId and no filter because it is the tenant table. Never delete it (there's no "delete company" feature).
    /// </summary>
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
