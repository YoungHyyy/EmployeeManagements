using EmployeeManagement.Application.Common;
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
        await EnsureUniqueAsync(request.Code, request.Name);

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
            ?? throw new KeyNotFoundException(ApiMessages.PositionNotFound);

        await EnsureUniqueAsync(request.Code, request.Name, excludeId: id);

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Level = request.Level;
        existing.IsActive = request.IsActive;

        await _repository.UpdateAsync(existing);
    }

    private async Task EnsureUniqueAsync(string code, string name, int? excludeId = null)
    {
        if (await _repository.ExistsByCodeAsync(code, excludeId))
        {
            throw new InvalidOperationException(ApiMessages.PositionCodeExists(code));
        }

        if (await _repository.ExistsByNameAsync(name, excludeId))
        {
            throw new InvalidOperationException(ApiMessages.PositionNameExists(name));
        }
    }

    public async Task DeleteAsync(int id)
    {
        _ = await _repository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException(ApiMessages.PositionNotFound);

        if (await _employeeRepository.ExistsByPositionIdAsync(id))
        {
            throw new InvalidOperationException(ApiMessages.PositionHasEmployees);
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
