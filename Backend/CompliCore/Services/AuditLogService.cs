using CompliCore.Data;
using CompliCore.DTOs;
using CompliCore.DTOs.AuditLogDtos;
using Microsoft.EntityFrameworkCore;

namespace CompliCore.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogResponse>> GetPagedAsync(string? entityName, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogResponse(a.Id, a.UserId, a.UserEmail, a.EntityName, a.EntityId, a.Action.ToString(), a.ChangesJson, a.Timestamp))
            .ToListAsync();

        return new PagedResult<AuditLogResponse>(logs, page, pageSize, totalCount);
    }
}