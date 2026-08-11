namespace EmployeeManagement.Domain.Attributes;

/// <summary>Tên bảng MySQL — GenericRepository đọc attribute này (repo con không cần override TableName).</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class DbTableAttribute : Attribute
{
    public string Name { get; }

    public DbTableAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Table name is required.", nameof(name));
        }

        Name = name.Trim();
    }
}

/// <summary>Bỏ property khỏi INSERT và UPDATE.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbIgnoreAttribute : Attribute
{
}

/// <summary>Không đưa vào INSERT (vd: UserName rỗng — tránh UNIQUE).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbInsertIgnoreAttribute : Attribute
{
}

/// <summary>Không đưa vào UPDATE (vd: EmployeeCode, UserId lúc tạo).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class DbUpdateIgnoreAttribute : Attribute
{
}
