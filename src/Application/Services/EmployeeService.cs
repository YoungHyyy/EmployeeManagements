using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;
    private readonly IAuditLogService _auditLogService;

    public EmployeeService(IEmployeeRepository repository, IAuditLogService? auditLogService = null)
    {
        _repository = repository;
        _auditLogService = auditLogService ?? new NoOpAuditLogService();
    }

    public async Task<EmployeeListResult> GetAllAsync(EmployeeListQuery query)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, total) = await _repository.ListAsync(query);

        return new EmployeeListResult
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : Map(item);
    }

    public async Task<EmployeeDto> CreateAsync(EmployeeDto request, int? actorUserId = null)
    {
        var entity = new Employee
        {
            EmployeeCode = GenerateEmployeeCode(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            HireDate = request.HireDate,
            Status = request.Status ?? "Working",
            AvatarUrl = request.AvatarUrl,
            IsActive = request.IsActive
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;

        await _auditLogService.WriteAsync(new AuditLogWriteRequest
        {
            UserId = actorUserId,
            Action = AuditActions.Create,
            Module = AuditModules.Employee,
            EntityName = "Employee",
            EntityId = id.ToString(),
            RequestPath = "/api/Employees",
            Details = $"EmployeeCode={entity.EmployeeCode}; Email={entity.Email}"
        });

        return Map(entity);
    }

    public async Task UpdateAsync(int id, EmployeeDto request, int? actorUserId = null)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy nhân viên");

        // Defense-in-depth: email unique excluding current employee
        if (await _repository.ExistsByEmailAsync(request.Email, excludeEmployeeId: id))
        {
            throw new InvalidOperationException("Email đã tồn tại trong hệ thống");
        }

        existing.FullName = request.FullName;
        existing.Email = request.Email;
        existing.PhoneNumber = request.PhoneNumber;
        existing.DateOfBirth = request.DateOfBirth;
        existing.Gender = request.Gender;
        existing.Address = request.Address;
        existing.DepartmentId = request.DepartmentId;
        existing.PositionId = request.PositionId;
        existing.HireDate = request.HireDate;
        existing.Status = request.Status ?? existing.Status;
        existing.AvatarUrl = request.AvatarUrl;
        existing.IsActive = request.IsActive;

        await _repository.UpdateAsync(existing);

        await _auditLogService.WriteAsync(new AuditLogWriteRequest
        {
            UserId = actorUserId,
            Action = AuditActions.Update,
            Module = AuditModules.Employee,
            EntityName = "Employee",
            EntityId = id.ToString(),
            RequestPath = $"/api/Employees/{id}"
        });
    }

    public async Task DeleteAsync(int id, int? actorUserId = null)
    {
        _ = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy nhân viên");

        await _repository.SoftDeleteAsync(id);

        await _auditLogService.WriteAsync(new AuditLogWriteRequest
        {
            UserId = actorUserId,
            Action = AuditActions.SoftDelete,
            Module = AuditModules.Employee,
            EntityName = "Employee",
            EntityId = id.ToString(),
            RequestPath = $"/api/Employees/{id}"
        });
    }

    public async Task RestoreAsync(int id, int? actorUserId = null)
    {
        // Restore: bản ghi có thể đang soft-deleted → GetById (IsDeleted=0) sẽ null
        // SoftDeleteRepository.RestoreAsync vẫn chạy; kiểm tra tồn tại qua repo raw nếu cần
        await _repository.RestoreAsync(id);

        await _auditLogService.WriteAsync(new AuditLogWriteRequest
        {
            UserId = actorUserId,
            Action = AuditActions.Restore,
            Module = AuditModules.Employee,
            EntityName = "Employee",
            EntityId = id.ToString(),
            RequestPath = $"/api/Employees/{id}/restore"
        });
    }

    private static EmployeeDto Map(Employee employee) => new()
    {
        Id = employee.Id,
        EmployeeCode = employee.EmployeeCode,
        FullName = employee.FullName,
        Email = employee.Email,
        PhoneNumber = employee.PhoneNumber,
        DateOfBirth = employee.DateOfBirth,
        Gender = employee.Gender,
        Address = employee.Address,
        DepartmentId = employee.DepartmentId,
        PositionId = employee.PositionId,
        HireDate = employee.HireDate,
        Status = employee.Status,
        AvatarUrl = employee.AvatarUrl,
        IsActive = employee.IsActive
    };

    private static string GenerateEmployeeCode()
    {
        var date = DateTime.UtcNow.ToString("yyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"EMP{date}{random}";
    }

    private sealed class NoOpAuditLogService : IAuditLogService
    {
        public Task WriteAsync(AuditLogWriteRequest request) => Task.CompletedTask;
        public Task<AuditLogListResult> GetListAsync(AuditLogQuery query)
            => Task.FromResult(new AuditLogListResult());
    }
}
