using CompliCore.Data;
using CompliCore.DTOs;
using CompliCore.DTOs.EmployeeDtos;
using CompliCore.Exceptions;
using CompliCore.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace CompliCore.Services;

public class EmployeeService
{
    private readonly AppDbContext _db;

    public EmployeeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<EmployeeResponse>> GetPagedAsync(string? search, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _db.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.FullName.Contains(search) ||
                (e.IqamaNumber != null && e.IqamaNumber.Contains(search)));
        }

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(e => e.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EmployeeResponse(
                e.Id, e.FullName, e.Nationality, e.IqamaNumber, e.JobTitle, e.CreatedAt, e.UpdatedAt))
            .ToListAsync();

        return new PagedResult<EmployeeResponse>(employees, page, pageSize, totalCount);
    }

    public async Task<EmployeeResponse> GetByIdAsync(Guid id)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
        }

        return new EmployeeResponse(
            employee.Id, employee.FullName, employee.Nationality,
            employee.IqamaNumber, employee.JobTitle, employee.CreatedAt, employee.UpdatedAt);
    }

    public async Task<EmployeeResponse> CreateAsync(CreateEmployeeRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.IqamaNumber))
        {
            var duplicate = await _db.Employees
                .AnyAsync(e => e.IqamaNumber == request.IqamaNumber);

            if (!string.IsNullOrWhiteSpace(request.IqamaNumber) && !Regex.IsMatch(request.IqamaNumber, @"^2\d{9}$"))
            {
                throw new DomainException("Iqama number must be 10 digits starting with 2.");
            }
            if (duplicate)
            {
                throw new ConflictException("An employee with this iqama number already exists.");
            }
        }

        var employee = new Employee
        {
            FullName = request.FullName,
            Nationality = request.Nationality,
            IqamaNumber = request.IqamaNumber,
            JobTitle = request.JobTitle
        };

        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();

        return new EmployeeResponse(
            employee.Id, employee.FullName, employee.Nationality,
            employee.IqamaNumber, employee.JobTitle, employee.CreatedAt, employee.UpdatedAt);
    }

    public async Task<EmployeeResponse> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
        }
        if (!string.IsNullOrWhiteSpace(request.IqamaNumber) && !Regex.IsMatch(request.IqamaNumber, @"^2\d{9}$"))
        {
            throw new DomainException("Iqama number must be 10 digits starting with 2.");
        }
        if (!string.IsNullOrWhiteSpace(request.IqamaNumber))
        {
            var duplicate = await _db.Employees
                .AnyAsync(e => e.IqamaNumber == request.IqamaNumber && e.Id != id);

            if (duplicate)
            {
                throw new ConflictException("An employee with this iqama number already exists.");
            }
        }

        employee.FullName = request.FullName;
        employee.Nationality = request.Nationality;
        employee.IqamaNumber = request.IqamaNumber;
        employee.JobTitle = request.JobTitle;

        await _db.SaveChangesAsync();

        return new EmployeeResponse(
            employee.Id, employee.FullName, employee.Nationality,
            employee.IqamaNumber, employee.JobTitle, employee.CreatedAt, employee.UpdatedAt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null)
        {
            throw new NotFoundException("Employee not found.");
        }

        _db.Employees.Remove(employee);
        await _db.SaveChangesAsync();
    }
}