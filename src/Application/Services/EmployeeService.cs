using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _repository;

    public EmployeeService(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllAsync(int page, int pageSize, string? search, int? departmentId, int? positionId, string? status)
    {
        var items = await _repository.ListAsync(page, pageSize, search, departmentId, positionId, status);
        return items.Select(Map);
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : Map(item);
    }

    public async Task<EmployeeDto> CreateAsync(EmployeeDto request)
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
        return Map(entity);
    }

    public async Task UpdateAsync(int id, EmployeeDto request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return;

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
    }

    public async Task DeleteAsync(int id)
    {
        await _repository.SoftDeleteAsync(id);
    }

    public async Task RestoreAsync(int id)
    {
        await _repository.RestoreAsync(id);
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
}
