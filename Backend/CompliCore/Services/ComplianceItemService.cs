using CompliCore.Data;
using CompliCore.DTOs;
using CompliCore.DTOs.ComplianceItemDtos;
using CompliCore.Enums;
using CompliCore.Exceptions;
using CompliCore.Models;
using CompliCore.Rules;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace CompliCore.Services;

public class ComplianceItemService
{
    private readonly AppDbContext _db;
    private readonly TimeProvider _time;

    public ComplianceItemService(AppDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }


    public async Task<PagedResult<ComplianceItemResponse>> GetPagedAsync(
    string? status, string? type, Guid? employeeId, bool? companyOnly, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.ComplianceItems.AsQueryable();

        if (employeeId != null)
        {
            query = query.Where(i => i.EmployeeId == employeeId);
        }

        if (companyOnly == true)
        {
            query = query.Where(i => i.EmployeeId == null);
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ComplianceItemType>(type, true, out var parsedType))
        {
            query = query.Where(i => i.Type == parsedType);
        }

        var today = ComplianceStatusCalculator.Today(_time);
        var soon = today.AddDays(60);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status switch
            {
                "Expired" => query.Where(i => i.ExpiryDate < today),
                "Expiring" => query.Where(i => i.ExpiryDate >= today && i.ExpiryDate <= soon),
                "Valid" => query.Where(i => i.ExpiryDate > soon),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(i => i.ExpiryDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var responses = items.Select(MapToResponse).ToList();

        return new PagedResult<ComplianceItemResponse>(responses, page, pageSize, totalCount);
    }

    public async Task<ComplianceItemResponse> GetByIdAsync(Guid id)
    {
        var item = await _db.ComplianceItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) throw new NotFoundException("Compliance item not found.");
        return MapToResponse(item);
    }

    public async Task<ComplianceItemResponse> CreateAsync(CreateComplianceItemRequest request)
    {
        ValidateTypeSubject(request.Type, request.EmployeeId);

        var reference = await ValidateEmployeeAsync(request.Type, request.EmployeeId, request.ReferenceNumber);

        if (request.IssueDate != null && request.IssueDate > request.ExpiryDate)
        {
            throw new DomainException("Issue date must be before or equal to expiry date.");
        }

        var item = new ComplianceItem
        {
            EmployeeId = request.EmployeeId,
            Type = request.Type,
            Title = string.IsNullOrWhiteSpace(request.Title) ? ComplianceItemRules.DisplayName(request.Type) : request.Title,
            ReferenceNumber = reference,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            Notes = request.Notes
        };

        _db.ComplianceItems.Add(item);
        await _db.SaveChangesAsync();

        return MapToResponse(item);
    }

    public async Task<ComplianceItemResponse> UpdateAsync(Guid id, UpdateComplianceItemRequest request)
    {
        var item = await _db.ComplianceItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) throw new NotFoundException("Compliance item not found.");

        ValidateTypeSubject(request.Type, request.EmployeeId);

        var reference = await ValidateEmployeeAsync(request.Type, request.EmployeeId, request.ReferenceNumber);

        if (request.IssueDate != null && request.IssueDate > request.ExpiryDate)
        {
            throw new DomainException("Issue date must be before or equal to expiry date.");
        }

        item.EmployeeId = request.EmployeeId;
        item.Type = request.Type;
        item.Title = string.IsNullOrWhiteSpace(request.Title) ? ComplianceItemRules.DisplayName(request.Type) : request.Title;
        item.ReferenceNumber = reference;
        item.IssueDate = request.IssueDate;
        item.ExpiryDate = request.ExpiryDate;
        item.Notes = request.Notes;

        await _db.SaveChangesAsync();

        return MapToResponse(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        var item = await _db.ComplianceItems.FirstOrDefaultAsync(i => i.Id == id);
        if (item == null) throw new NotFoundException("Compliance item not found.");

        _db.ComplianceItems.Remove(item);
        await _db.SaveChangesAsync();
    }
    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var items = await _db.ComplianceItems.ToListAsync();
        var today = ComplianceStatusCalculator.Today(_time);

        var responses = items.Select(MapToResponse).ToList();

        var valid = responses.Count(r => r.Status == "Valid");
        var expiring = responses.Count(r => r.Status == "Expiring");
        var expired = responses.Count(r => r.Status == "Expired");

        var nextToExpire = responses
            .Where(r => r.ExpiryDate >= today)
            .OrderBy(r => r.ExpiryDate)
            .Take(10)
            .ToList();

        return new DashboardResponse(responses.Count, valid, expiring, expired, nextToExpire);
    }

    // Validates the employee link and returns the reference number to store.
    // The employee is looked up through the tenant-filtered set, so another
    // company's employee simply "does not exist" here.
    // An Iqama item requires the employee to actually have an iqama number,
    // and a blank reference number is filled in from it (one source of truth).
    private async Task<string?> ValidateEmployeeAsync(ComplianceItemType type, Guid? employeeId, string? referenceNumber)
    {
        if (employeeId == null) return referenceNumber;

        var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == employeeId);
        if (employee == null) throw new DomainException("Employee not found.");

        if (type == ComplianceItemType.Iqama)
        {
            if (string.IsNullOrWhiteSpace(employee.IqamaNumber))
                throw new DomainException($"{employee.FullName} has no iqama number.");

            return string.IsNullOrWhiteSpace(referenceNumber) ? employee.IqamaNumber : referenceNumber;
        }

        return referenceNumber;
    }

    private static void ValidateTypeSubject(CompliCore.Enums.ComplianceItemType type, Guid? employeeId)
    {
        if (ComplianceItemRules.EmployeeLevel.Contains(type) && employeeId == null)
            throw new DomainException($"{type} must be linked to an employee.");

        if (ComplianceItemRules.CompanyLevel.Contains(type) && employeeId != null)
            throw new DomainException($"{type} cannot be linked to an employee.");
    }

    private ComplianceItemResponse MapToResponse(ComplianceItem item)
    {
        var today = ComplianceStatusCalculator.Today(_time);
        var status = ComplianceStatusCalculator.GetStatus(item.ExpiryDate, today);
        var daysRemaining = ComplianceStatusCalculator.GetDaysRemaining(item.ExpiryDate, today);

        return new ComplianceItemResponse(
            item.Id, item.EmployeeId, item.Type.ToString(), item.Title, item.ReferenceNumber,
            item.IssueDate, item.ExpiryDate, item.Notes, status.ToString(), daysRemaining,
            item.CreatedAt, item.UpdatedAt);
    }
}