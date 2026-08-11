namespace EmployeeManagement.Application.DTOs;

/// <summary>
/// Query danh sách nhân viên: search / filter / sort / pagination (backend.md §5).
/// </summary>
public class EmployeeListQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Tìm theo họ tên HOẶC email HOẶC số điện thoại.</summary>
    public string? Search { get; set; }

    /// <summary>Tìm riêng theo họ tên (ưu tiên hơn Search nếu có).</summary>
    public string? SearchName { get; set; }

    /// <summary>Tìm riêng theo email.</summary>
    public string? SearchEmail { get; set; }

    /// <summary>Tìm riêng theo số điện thoại.</summary>
    public string? SearchPhone { get; set; }

    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public string? Status { get; set; }

    /// <summary>fullName | createdAt | hireDate (mặc định createdAt).</summary>
    public string? SortBy { get; set; }

    /// <summary>asc | desc (mặc định desc).</summary>
    public string? SortDir { get; set; }
}

public class EmployeeListResult
{
    public IReadOnlyList<EmployeeDto> Items { get; set; } = Array.Empty<EmployeeDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
