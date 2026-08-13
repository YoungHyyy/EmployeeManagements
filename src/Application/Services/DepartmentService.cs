using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public DepartmentService(IDepartmentRepository repository, IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync(int page, int pageSize)
    {
        var items = await _repository.ListAsync(page, pageSize);
        return items.Select(Map);
    }

    public async Task<DepartmentDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : Map(item);
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentDto request)
    {
        await EnsureUniqueAsync(request.Code, request.Name);

        var entity = new Department
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;
        return Map(entity);
    }

    public async Task UpdateAsync(int id, DepartmentDto request)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException(ApiMessages.DepartmentNotFound);

        await EnsureUniqueAsync(request.Code, request.Name, excludeId: id);

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.IsActive = request.IsActive;

        await _repository.UpdateAsync(existing);
    }

    private async Task EnsureUniqueAsync(string code, string name, int? excludeId = null)
    {
        if (await _repository.ExistsByCodeAsync(code, excludeId))
        {
            throw new InvalidOperationException(ApiMessages.DepartmentCodeExists(code));
        }

        if (await _repository.ExistsByNameAsync(name, excludeId))
        {
            throw new InvalidOperationException(ApiMessages.DepartmentNameExists(name));
        }
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException(ApiMessages.DepartmentNotFound);

        var (linkedEmployees, total) = await _employeeRepository.ListAsync(new EmployeeListQuery
        {
            Page = 1,
            PageSize = 1,
            DepartmentId = id
        });
        if (total > 0 || linkedEmployees.Any())
        {
            throw new InvalidOperationException(ApiMessages.DepartmentHasEmployees);
        }

        await _repository.SoftDeleteAsync(id);
    }

    private static DepartmentDto Map(Department department) => new()
    {
        Id = department.Id,
        Code = department.Code,
        Name = department.Name,
        Description = department.Description,
        IsActive = department.IsActive
    };
}
