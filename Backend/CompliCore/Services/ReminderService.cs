using CompliCore.Data;
using CompliCore.Models;
using CompliCore.Rules;
using Microsoft.EntityFrameworkCore;

namespace CompliCore.Services;

public class ReminderService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public ReminderService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<int> RunOnceAsync(CancellationToken ct = default)
    {
        var today = ComplianceStatusCalculator.Today(_time);
        var horizon = today.AddDays(60);

        var items = await _db.ComplianceItems
            .IgnoreQueryFilters() // worker scans ALL tenants, no logged-in user
            .Include(i => i.Employee)
            .Where(i => i.ExpiryDate <= horizon)
            .ToListAsync(ct);

        var created = 0;

        foreach (var item in items)
        {
            var days = item.ExpiryDate.DayNumber - today.DayNumber;
            var threshold = days < 0 ? 0 : days <= 7 ? 7 : days <= 30 ? 30 : 60;

            var exists = await _db.Notifications.IgnoreQueryFilters().AnyAsync(n =>
                n.ComplianceItemId == item.Id &&
                n.Threshold == threshold &&
                n.ExpiryDateSnapshot == item.ExpiryDate, ct);

            if (exists) continue;

            var subject = item.Employee != null ? $"{item.Employee.FullName} - {item.Title}" : item.Title;
            var message = threshold == 0
                ? $"{subject} expired on {item.ExpiryDate}"
                : $"{subject} expires in {days} days ({item.ExpiryDate})";

            _db.Notifications.Add(new Notification
            {
                TenantId = item.TenantId,
                ComplianceItemId = item.Id,
                Threshold = threshold,
                ExpiryDateSnapshot = item.ExpiryDate,
                Message = message,
                CreatedAt = DateTime.UtcNow
            });

            created++;
        }

        await _db.SaveChangesAsync(ct);
        return created;
    }
}