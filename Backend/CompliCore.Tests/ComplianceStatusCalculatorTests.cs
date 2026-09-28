using CompliCore.Enums;
using CompliCore.Rules;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace CompliCore.Tests;

public class ComplianceStatusCalculatorTests
{
    [Fact]
    public void StatusBoundaries_AreCorrect()
    {
        var fakeTime = new FakeTimeProvider();
        fakeTime.SetUtcNow(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var today = ComplianceStatusCalculator.Today(fakeTime);

        Assert.Equal(ComplianceStatus.Expiring, ComplianceStatusCalculator.GetStatus(today, today));
        Assert.Equal(ComplianceStatus.Expiring, ComplianceStatusCalculator.GetStatus(today.AddDays(60), today));
        Assert.Equal(ComplianceStatus.Valid, ComplianceStatusCalculator.GetStatus(today.AddDays(61), today));
        Assert.Equal(ComplianceStatus.Expired, ComplianceStatusCalculator.GetStatus(today.AddDays(-1), today));
    }
}