using System.Reflection;
using CompliCore.Data;
using CompliCore.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Xunit;

namespace CompliCore.Tests;

public class QueryFilterTests
{
    [Fact]
    public void EveryTenantEntity_HasAQueryFilter()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("filter-check")
            .Options;

        using var db = new AppDbContext(options, new FakeCurrentUser());
        var model = db.Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (!typeof(ITenantEntity).IsAssignableFrom(clrType)) continue;

            Assert.True(entityType.GetDeclaredQueryFilters().Any(),
            $"{clrType.Name} implements ITenantEntity but has no query filter configured.");
        }
    }

    private class FakeCurrentUser : Services.ICurrentUser
    {
        public Guid TenantId => Guid.Empty;
        public Guid? UserId => null;
        public string? Email => null;
        public string? Role => null;
    }
}