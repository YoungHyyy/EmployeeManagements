using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class PositionService : IPositionService
{
    private readonly IPositionRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public PositionService(IPositionRepository repository, IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    public async Task<IEnumerable<PositionDto>> GetAllAsync(int page, int pageSize)
    {
        var items = await _repository.ListAsync(page, pageSize);
        return items.Select(Map);
    }

    public async Task<PositionDto?> GetByIdAsync(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? null : Map(item);
    }

    public async Task<PositionDto> CreateAsync(PositionDto request)
    {
        var entity = new Position
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Level = request.Level,
            IsActive = request.IsActive
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;
        return Map(entity);
    }

    public async Task UpdateAsync(int id, PositionDto request)
    {
        var existing = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy chức vụ");

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Level = request.Level;
        existing.IsActive = request.IsActive;

        await _repository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy chức vụ");

        var (linkedEmployees, total) = await _employeeRepository.ListAsync(new EmployeeListQuery
        {
            Page = 1,
            PageSize = 1,
            PositionId = id
        });
        if (total > 0 || linkedEmployees.Any())
        {
            throw new InvalidOperationException("Không thể xóa chức vụ khi vẫn còn nhân viên liên kết");
        }

        await _repository.SoftDeleteAsync(id);
    }

    private static PositionDto Map(Position position) => new()
    {
        Id = position.Id,
        Code = position.Code,
        Name = position.Name,
        Description = position.Description,
        Level = position.Level,
        IsActive = position.IsActive
    };
}
