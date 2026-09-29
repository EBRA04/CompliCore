using CompliCore.Enums;
using CompliCore.Interfaces;
using CompliCore.Models;
using CompliCore.Services;
using Microsoft.EntityFrameworkCore;

namespace CompliCore.Data;

// DbContext = EF Core's base class. Translates C# into SQL, owns the connection.
// This class is Day 1's version: no ICurrentUser, no query filters, no stamping yet.
// Those come on Day 4 and add zero schema changes (filters aren't in the migration).
public class AppDbContext : DbContext
{
    private readonly ICurrentUser _current;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser current) : base(options)
    {
        _current = current;
    }

    // Read on every query. MUST be a member of the context (not a captured
    // local var) so EF re-evaluates it fresh per query — the context itself
    // is created new per HTTP request, so this always reflects the CURRENT
    // request's tenant, not whichever tenant happened to build the query plan first.
    private Guid CurrentTenantId => _current.TenantId;
    // DbSet<T> = entry point to query/modify one table.
    // e.g. _db.Employees.Where(...) becomes a SQL query on the Employees table.
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ComplianceItem> ComplianceItems => Set<ComplianceItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    // OnModelCreating = the one hook for anything convention doesn't cover:
    // string lengths, enum storage, indexes, and FK delete behavior.
    protected override void OnModelCreating(ModelBuilder b)
    {
        // Tenant IS the tenant, not owned by one -> no TenantId, no query filter.
        // (A filter needs a TenantId column to compare against; Tenant has none.
        // Also: register/login must find/create a Tenant BEFORE any tenant
        // context exists, so a filter here would make that impossible.)
        b.Entity<Tenant>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
            // NOTE: CreatedAt is not ITimestamped -> nothing auto-fills it.
            // AuthService must set it manually at register time.
        });

        b.Entity<User>(e =>
        {
            e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.Property(x => x.PasswordHash).IsRequired();

            // HasConversion<string>() -> stores "Admin", not 0.
            // Without this, EF stores the enum's int, and reordering the enum
            // later would silently change what every existing row means.
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);

            // Email is GLOBALLY unique (not per tenant): login happens before
            // we know the tenant, so we look up by email alone, across all
            // tenants (via IgnoreQueryFilters() once filters exist).
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.TenantId);

            // WithMany() with no lambda = FK exists, but no navigation property
            // either side. We never need user.Tenant.Name — TenantId comes
            // straight from the JWT claim.
            // Restrict = Postgres blocks deleting a Tenant while Users exist.
            // Safety net only — there's no "delete tenant" feature in the spec.
            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Employee>(e =>
        {
            e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.Nationality).IsRequired().HasMaxLength(80);
            e.Property(x => x.IqamaNumber).HasMaxLength(10);
            e.Property(x => x.JobTitle).HasMaxLength(100);

            // COMPOSITE unique index: uniqueness applies to the (Tenant, Iqama)
            // PAIR, not either column alone. Two different tenants CAN share
            // the same iqama number; one tenant CANNOT have it twice.
            // Postgres treats every NULL as distinct in a unique index, so
            // employees with no iqama never collide with each other.
            e.HasIndex(x => new { x.TenantId, x.IqamaNumber }).IsUnique();

            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ComplianceItem>(e =>
        {
            e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            e.Property(x => x.Title).IsRequired().HasMaxLength(150);
            e.Property(x => x.ReferenceNumber).HasMaxLength(60);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);

            // Not unique — just a speed index. Every dashboard/list query
            // filters by tenant and sorts/filters by expiry date.
            e.HasIndex(x => new { x.TenantId, x.ExpiryDate });

            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

            // *** THE ONE THAT NEEDS EXPLICIT CASCADE ***
            // EmployeeId is nullable (Guid?) because company-level items have
            // no employee. EF's DEFAULT for a nullable FK is SetNull, not
            // Cascade — EF assumes "nullable" means "optional link," not
            // "owned by." Spec (7.8/BR-8) requires deleting an Employee to
            // delete their items. Without this explicit line, deleting an
            // employee would leave orphaned items with EmployeeId = NULL
            // instead of deleting them — a silent data-integrity bug.
            e.HasOne(x => x.Employee).WithMany(x => x.ComplianceItems)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Document>(e =>
        {
            e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            e.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.StoredFileName).IsRequired().HasMaxLength(80);
            e.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.TenantId);

            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

            // Cascade down to the item: delete a ComplianceItem -> its
            // Documents (rows) go too. (Files on disk are a separate step,
            // done in the service, not the database.)
            e.HasOne<ComplianceItem>().WithMany(i => i.Documents)
                .HasForeignKey(x => x.ComplianceItemId).OnDelete(DeleteBehavior.Cascade);

            // NOTE: UploadedAt is not ITimestamped -> DocumentService must
            // set it manually at upload time.
        });

        b.Entity<Notification>(e =>
        {
            e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            e.Property(x => x.Message).IsRequired().HasMaxLength(300);

            // Composite unique index = what makes the reminder worker
            // idempotent. Running it twice can't create the same
            // notification twice — the DATABASE rejects the duplicate
            // insert even if the C# logic has a bug.
            // ExpiryDateSnapshot is IN the key (not just ComplianceItemId +
            // Threshold) so that a RENEWAL — which changes the expiry date —
            // no longer matches the old key and the reminder cycle restarts
            // for the new date, instead of being blocked as a "duplicate."
            e.HasIndex(x => new { x.ComplianceItemId, x.Threshold, x.ExpiryDateSnapshot }).IsUnique();
            e.HasIndex(x => x.TenantId);

            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ComplianceItem>().WithMany()
                .HasForeignKey(x => x.ComplianceItemId).OnDelete(DeleteBehavior.Cascade);

            // NOTE: CreatedAt is not ITimestamped -> ReminderService must
            // set it manually when creating each notification.
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasQueryFilter(x => x.TenantId == CurrentTenantId);
            e.Property(x => x.UserEmail).HasMaxLength(200);
            e.Property(x => x.EntityName).IsRequired().HasMaxLength(60);
            e.Property(x => x.Action).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ChangesJson).IsRequired().HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.Timestamp });

            // Deliberately NO foreign keys — not even to Tenant, not to the
            // entity it describes (EntityId). History must outlive the rows
            // it logs: if there were an FK, deleting old data would either
            // be BLOCKED by audit rows referencing it, or (worse) CASCADE
            // and wipe the audit trail — the opposite of the point of
            // an audit log.
            // Timestamp is the one field the interceptor sets itself later
            // (Day 8) — unlike the CreatedAt fields above, nothing to
            // remember here.
        });
    }

    // The main problem this solves: protects every SELECT (via filters
    // above) AND every INSERT/UPDATE/DELETE (via the guard below) — plus
    // now, also logs Created/Updated/Deleted with field-level diffs for
    // the audited entity types.
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default)
    {
        ApplyConventions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, ct);
    }

    private void ApplyConventions()
    {
        // DetectChanges() forces EF to compare current vs original values
        // NOW, before we read them below — otherwise Modified entries might
        // not have their OriginalValue/CurrentValue diff computed yet.
        ChangeTracker.DetectChanges();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is ITenantEntity t && entry.State == EntityState.Added)
            {
                if (t.TenantId == Guid.Empty)
                {
                    t.TenantId = CurrentTenantId;
                }
                else if (CurrentTenantId != Guid.Empty && t.TenantId != CurrentTenantId)
                {
                    throw new InvalidOperationException("Cross-tenant write blocked.");
                }

                if (t.TenantId == Guid.Empty)
                {
                    throw new InvalidOperationException("Tenant-owned row without a tenant.");
                }
            }

            if (entry.Entity is AuditLog && entry.State is EntityState.Modified or EntityState.Deleted)
            {
                throw new InvalidOperationException("Audit log is append-only.");
            }
        }

        // Audit trail with field-level diffs (spec 11.2/11.3): for each
        // audited entity type that was Created/Updated/Deleted, build a
        // ChangesJson of { "Field": { "old": ..., "new": ... } }.
        var auditLogs = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is not (Employee or ComplianceItem or Document or User)) continue;
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var changes = BuildChanges(entry);
            // A Modified entry with no real changes (e.g. re-saved but
            // nothing actually differs) isn't worth logging.
            if (entry.State == EntityState.Modified && changes.Count == 0) continue;

            var tenantId = entry.Entity is ITenantEntity t2 ? t2.TenantId : CurrentTenantId;

            auditLogs.Add(new AuditLog
            {
                TenantId = tenantId == Guid.Empty ? CurrentTenantId : tenantId,
                UserId = _current.UserId,
                UserEmail = _current.Email,
                EntityName = entry.Entity.GetType().Name,
                EntityId = (Guid)entry.Property("Id").CurrentValue!,
                Action = entry.State switch
                {
                    EntityState.Added => AuditAction.Created,
                    EntityState.Modified => AuditAction.Updated,
                    _ => AuditAction.Deleted
                },
                ChangesJson = System.Text.Json.JsonSerializer.Serialize(changes),
                Timestamp = DateTime.UtcNow
            });
        }

        foreach (var log in auditLogs)
        {
            Entry(log).State = EntityState.Added;
        }
    }

    // Never log these: PasswordHash is a secret, CreatedAt/UpdatedAt are
    // timestamps stamped automatically and aren't meaningful "changes."
    private static readonly HashSet<string> AuditSkipProperties = new() { "PasswordHash", "CreatedAt", "UpdatedAt" };

    // Enums are stored as their names ("Viewer") instead of numbers (1) so the
    // audit log is readable without knowing the enum's order.
    private static object? Readable(object? value) => value is Enum e ? e.ToString() : value;

    private static Dictionary<string, object?> BuildChanges(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changes = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            var name = prop.Metadata.Name;
            if (AuditSkipProperties.Contains(name)) continue;

            if (entry.State == EntityState.Added)
            {
                changes[name] = new { @new = Readable(prop.CurrentValue) };
            }
            else if (entry.State == EntityState.Deleted)
            {
                changes[name] = new { old = Readable(prop.OriginalValue) };
            }
            else if (entry.State == EntityState.Modified && !Equals(prop.OriginalValue, prop.CurrentValue))
            {
                changes[name] = new { old = Readable(prop.OriginalValue), @new = Readable(prop.CurrentValue) };
            }
        }

        return changes;
    }
}