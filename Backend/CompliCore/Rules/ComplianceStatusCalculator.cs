using CompliCore.Enums;

namespace CompliCore.Rules;

/// <summary>
/// here is where the days remmaining and status of compliance items are calculated based on the expiry date and the current date.
/// </summary>
public static class ComplianceStatusCalculator
{
    private const int ExpiringWindowDays = 60;

    public static DateOnly Today(TimeProvider time) =>
        DateOnly.FromDateTime(time.GetUtcNow().AddHours(3).DateTime); // Saudi Arabia = UTC+3, no DST

    public static ComplianceStatus GetStatus(DateOnly expiryDate, DateOnly today)
    {
        if (expiryDate < today) return ComplianceStatus.Expired;
        if (expiryDate <= today.AddDays(ExpiringWindowDays)) return ComplianceStatus.Expiring;
        return ComplianceStatus.Valid;
    }

    public static int GetDaysRemaining(DateOnly expiryDate, DateOnly today) =>
        expiryDate.DayNumber - today.DayNumber;
}