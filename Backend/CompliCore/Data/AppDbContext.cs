using CompliCore.Models;
using Microsoft.EntityFrameworkCore;

namespace CompliCore.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ComplianceItem> ComplianceItems => Set<ComplianceItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>(e =>
        {
            e.Property(x => x.Name).IsRequired().HasMaxLength(150);
        });

        b.Entity<User>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.Email).IsRequired().HasMaxLength(200);
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Employee>(e =>
        {
            e.Property(x => x.FullName).IsRequired().HasMaxLength(120);
            e.Property(x => x.Nationality).IsRequired().HasMaxLength(80);
            e.Property(x => x.IqamaNumber).HasMaxLength(10);
            e.Property(x => x.JobTitle).HasMaxLength(100);
            e.HasIndex(x => new { x.TenantId, x.IqamaNumber }).IsUnique();
            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<ComplianceItem>(e =>
        {
            e.Property(x => x.Title).IsRequired().HasMaxLength(150);
            e.Property(x => x.ReferenceNumber).HasMaxLength(60);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.Property(x => x.Type).HasConversion<string>().HasMaxLength(40);
            e.HasIndex(x => new { x.TenantId, x.ExpiryDate });
            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Employee).WithMany(x => x.ComplianceItems)
                .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Document>(e =>
        {
            e.Property(x => x.OriginalFileName).IsRequired().HasMaxLength(255);
            e.Property(x => x.StoredFileName).IsRequired().HasMaxLength(80);
            e.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ComplianceItem>().WithMany(i => i.Documents)
                .HasForeignKey(x => x.ComplianceItemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Notification>(e =>
        {
            e.Property(x => x.Message).IsRequired().HasMaxLength(300);
            e.HasIndex(x => new { x.ComplianceItemId, x.Threshold, x.ExpiryDateSnapshot }).IsUnique();
            e.HasIndex(x => x.TenantId);
            e.HasOne<Tenant>().WithMany()
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ComplianceItem>().WithMany()
                .HasForeignKey(x => x.ComplianceItemId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<AuditLog>(e =>
        {
            e.Property(x => x.UserEmail).HasMaxLength(200);
            e.Property(x => x.EntityName).IsRequired().HasMaxLength(60);
            e.Property(x => x.Action).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ChangesJson).IsRequired().HasColumnType("jsonb");
            e.HasIndex(x => new { x.TenantId, x.Timestamp });
            // No foreign keys: history must outlive the rows it describes.
        });
    }
}