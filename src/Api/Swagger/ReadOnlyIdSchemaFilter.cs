using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EmployeeManagement.Api.Swagger;

/// <summary>
/// Đánh dấu property <c>id</c> là readOnly trên OpenAPI schema
/// → Swagger UI không hiện <c>id</c> trong Request body (POST/PUT),
/// vẫn hiện trong Response (GET).
/// </summary>
public sealed class ReadOnlyIdSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
        {
            return;
        }

        // camelCase (ASP.NET default) và PascalCase
        foreach (var key in new[] { "id", "Id" })
        {
            if (schema.Properties.TryGetValue(key, out var idProp))
            {
                idProp.ReadOnly = true;
            }
        }

        // EmployeeCode do server sinh — không nhập khi Create
        foreach (var key in new[] { "employeeCode", "EmployeeCode" })
        {
            if (schema.Properties.TryGetValue(key, out var codeProp)
                && context.Type.Name.Contains("Employee", StringComparison.OrdinalIgnoreCase))
            {
                codeProp.ReadOnly = true;
            }
        }
    }
}
