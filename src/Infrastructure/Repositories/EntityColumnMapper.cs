using System.Collections.Concurrent;
using System.Reflection;
using EmployeeManagement.Domain.Attributes;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// Reflection map property entity → cột INSERT/UPDATE (cache theo Type).
/// Dùng chung GenericRepository, AuditLogRepository (Reflection map cột — không EF).
/// </summary>
public static class EntityColumnMapper
{
    private static readonly ConcurrentDictionary<string, ColumnMap> Cache = new();

    /// <summary>Cột hệ thống soft-delete CRUD (base tự gán).</summary>
    public static readonly HashSet<string> SoftDeleteSystemColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt"
    };

    /// <summary>Chỉ bỏ Id (auto increment) — dùng AuditLog append-only.</summary>
    public static readonly HashSet<string> IdentityOnlySystemColumns = new(StringComparer.OrdinalIgnoreCase)
{
        "Id"
    };

    public static ColumnMap GetMap(Type entityType, ISet<string> systemColumns)
    {
        var key = entityType.FullName + "|" + string.Join(",", systemColumns.OrderBy(x => x));
        return Cache.GetOrAdd(key, _ => Build(entityType, systemColumns));
    }

    public static void EnsureSafeIdentifiers(IReadOnlyList<string> columns)
    {
        foreach (var col in columns)
        {
            if (string.IsNullOrWhiteSpace(col)
                || !col.All(ch => char.IsLetterOrDigit(ch) || ch == '_'))
            {
                throw new InvalidOperationException($"Tên cột không hợp lệ: '{col}'");
            }
        }
    }

    public static bool IsSafeIdentifier(string name)
        => !string.IsNullOrWhiteSpace(name)
           && name.All(ch => char.IsLetterOrDigit(ch) || ch == '_');

    private static ColumnMap Build(Type type, ISet<string> systemColumns)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Where(p => IsSimpleDbType(p.PropertyType))
            .ToList();

        var all = new List<string>();
        var insert = new List<string>();
        var update = new List<string>();

        foreach (var prop in props)
        {
            var name = prop.Name;
            all.Add(name);

            if (systemColumns.Contains(name))
            {
                continue;
            }

            if (prop.GetCustomAttribute<DbIgnoreAttribute>() != null)
            {
                continue;
            }

            if (prop.GetCustomAttribute<DbInsertIgnoreAttribute>() == null)
            {
                insert.Add(name);
            }

            if (prop.GetCustomAttribute<DbUpdateIgnoreAttribute>() == null)
            {
                update.Add(name);
            }
        }

        return new ColumnMap(insert, update, all);
    }

    private static bool IsSimpleDbType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type.IsEnum
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(Guid)
               || type == typeof(byte[])
               || type == typeof(long);
    }

    public sealed record ColumnMap(
        IReadOnlyList<string> InsertColumns,
        IReadOnlyList<string> UpdateColumns,
        IReadOnlyList<string> AllColumns);
}
