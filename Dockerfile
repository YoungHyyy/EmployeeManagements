FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Api/EmployeeManagement.Api.csproj", "src/Api/"]
COPY ["src/Application/EmployeeManagement.Application.csproj", "src/Application/"]
COPY ["src/Domain/EmployeeManagement.Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/EmployeeManagement.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["EmployeeManagement.sln", "./"]
RUN dotnet restore "EmployeeManagement.sln"
COPY . .
WORKDIR /src/src/Api
RUN dotnet publish "EmployeeManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmployeeManagement.Api.dll"]
