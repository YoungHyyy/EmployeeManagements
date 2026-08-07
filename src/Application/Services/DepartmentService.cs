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
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return;

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.IsActive = request.IsActive;

        await _repository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(int id)
    {
        var linkedEmployees = await _employeeRepository.ListAsync(1, 1, null, id, null, null);
        if (linkedEmployees.Any())
        {
            throw new InvalidOperationException("Không thể xóa phòng ban khi vẫn còn nhân viên liên kết");
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
