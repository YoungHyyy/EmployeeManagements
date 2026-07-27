using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var defaultConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(defaultConn));
builder.Services.AddScoped<EmployeeManagement.Application.Interfaces.IAuthService, EmployeeManagement.Application.Services.AuthService>();
builder.Services.AddScoped<EmployeeManagement.Application.Interfaces.IUserRepository, EmployeeManagement.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<EmployeeManagement.Application.Interfaces.IUserService, EmployeeManagement.Application.Services.UserService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
