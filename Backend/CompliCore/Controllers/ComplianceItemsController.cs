using CompliCore.DTOs.ComplianceItemDtos;
using CompliCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompliCore.Controllers;

[ApiController]
[Route("api/compliance-items")]
[Authorize]
public class ComplianceItemsController : ControllerBase
{
    private readonly ComplianceItemService _service;

    public ComplianceItemsController(ComplianceItemService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
    [FromQuery] string? status, [FromQuery] string? type, [FromQuery] Guid? employeeId,
    [FromQuery] bool? companyOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetPagedAsync(status, type, employeeId, companyOnly, page, pageSize);
        return Ok(result);
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateComplianceItemRequest request)
    {
        var result = await _service.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateComplianceItemRequest request)
    {
        var result = await _service.UpdateAsync(id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpGet("/api/dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _service.GetDashboardAsync();
        return Ok(result);
    }
}