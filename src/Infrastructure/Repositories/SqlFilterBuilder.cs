using Dapper;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// WHERE động an toàn — whitelist cột (property entity). Dapper DynamicParameters.
/// </summary>
public sealed class SqlFilterBuilder
{
    private readonly HashSet<string> _allowed;
    private readonly List<string> _clauses = new();
    private readonly DynamicParameters _parameters = new();
    private int _paramIndex;

    public SqlFilterBuilder(IEnumerable<string> allowedColumns, bool softDeleteOnly = true)
    {
        _allowed = new HashSet<string>(allowedColumns, StringComparer.OrdinalIgnoreCase);
        _allowed.Add("Id");
        _allowed.Add("IsDeleted");

        if (softDeleteOnly)
        {
            _clauses.Add("`IsDeleted` = 0");
        }
    }

    public DynamicParameters Parameters => _parameters;

    public string WhereSql =>
        _clauses.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", _clauses);

    public SqlFilterBuilder Equal(string column, object? value)
    {
        if (value is null)
        {
            return this;
        }

        if (value is string s && string.IsNullOrWhiteSpace(s))
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"`{column}` = @{p}");
        _parameters.Add(p, value is string str ? str.Trim() : value);
        return this;
    }

    /// <summary>UPPER(col) = UPPER(@value) — filter Action chuẩn hóa.</summary>
    public SqlFilterBuilder EqualUpper(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"UPPER(`{column}`) = @{p}");
        _parameters.Add(p, value.Trim().ToUpperInvariant());
        return this;
    }

    public SqlFilterBuilder EqualIgnoreCase(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"LOWER(`{column}`) = LOWER(@{p})");
        _parameters.Add(p, value.Trim());
        return this;
    }

    public SqlFilterBuilder Like(string column, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"`{column}` LIKE @{p}");
        _parameters.Add(p, $"%{value.Trim()}%");
        return this;
    }

    public SqlFilterBuilder LikeAny(IReadOnlyList<string> columns, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || columns.Count == 0)
        {
            return this;
        }

        foreach (var c in columns)
        {
            EnsureColumn(c);
        }

        var p = NextParam("search");
        var or = string.Join(" OR ", columns.Select(c => $"`{c}` LIKE @{p}"));
        _clauses.Add($"({or})");
        _parameters.Add(p, $"%{value.Trim()}%");
        return this;
    }

    public SqlFilterBuilder NotEqual(string column, object? value)
    {
        if (value is null)
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"`{column}` <> @{p}");
        _parameters.Add(p, value);
        return this;
    }

    public SqlFilterBuilder GreaterOrEqual(string column, object? value)
    {
        if (value is null)
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"`{column}` >= @{p}");
        _parameters.Add(p, value);
        return this;
    }

    public SqlFilterBuilder LessOrEqual(string column, object? value)
    {
        if (value is null)
        {
            return this;
        }

        EnsureColumn(column);
        var p = NextParam(column);
        _clauses.Add($"`{column}` <= @{p}");
        _parameters.Add(p, value);
        return this;
    }

    public SqlFilterBuilder AddPaging(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;
        _parameters.Add("limit", pageSize);
        _parameters.Add("offset", (page - 1) * pageSize);
        return this;
    }

    public static string ResolveOrderBy(
        string? sortBy,
        string? sortDir,
        IReadOnlyDictionary<string, string> sortMap,
        string defaultColumn = "CreatedAt")
    {
        var sqlColumn = defaultColumn;
        var key = (sortBy ?? string.Empty).Trim();
        if (key.Length > 0)
        {
            foreach (var kv in sortMap)
            {
                if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    sqlColumn = kv.Value;
                    break;
                }
            }
        }

        if (!EntityColumnMapper.IsSafeIdentifier(sqlColumn))
        {
            sqlColumn = defaultColumn;
        }

        var dir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        return $"`{sqlColumn}` {dir}, `Id` {dir}";
    }

    private void EnsureColumn(string column)
    {
        if (!EntityColumnMapper.IsSafeIdentifier(column) || !_allowed.Contains(column))
        {
            throw new InvalidOperationException($"Column not allowed in filter: '{column}'");
        }
    }

    private string NextParam(string hint)
    {
        _paramIndex++;
        var safe = new string(hint.Where(char.IsLetterOrDigit).Take(24).ToArray());
        if (string.IsNullOrEmpty(safe))
        {
            safe = "p";
        }

        return $"{safe}_{_paramIndex}";
    }
}
