# Employee Management API

RESTful Backend API quản lý nhân viên, xây dựng theo đề bài `backend.md`.

| | |
|---|---|
| **Stack** | .NET 8 Web API, MySQL, Dapper (không dùng Entity Framework) |
| **Kiến trúc** | Clean Architecture (`Api` / `Application` / `Domain` / `Infrastructure`) |
| **Bảo mật** | JWT + Refresh Token, BCrypt, phân quyền Admin / Employee |
| **Khác** | FluentValidation, Serilog, Swagger, Docker, xUnit |

---

## 1. Yêu cầu môi trường

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- MySQL 8 (local) **hoặc** Docker + Docker Compose
- Git (tùy chọn) Postman

---

## 2. Cấu trúc dự án

```text
EmployeeManagement.sln
├── src/
│   ├── Api/                 # Controllers, Middleware, Filters, Swagger, DI host
│   ├── Application/         # Services, DTOs, Validators, Interfaces
│   ├── Domain/              # Entities
│   └── Infrastructure/      # Dapper Repositories, DbConnectionFactory
├── tests/
│   └── EmployeeManagement.Tests/   # xUnit + Moq (≥ 10 unit tests)
├── database/
│   ├── schema.sql           # Schema + seed mẫu
│   └── seed-admin-user.sql  # Role + tài khoản Admin
├── postman/                 # Postman Collection + Environment
├── Images/                  # Ảnh minh họa Swagger / Auth
├── Dockerfile
├── docker-compose.yml
├── API_LAYER.md             # Tài liệu kiến trúc tầng API
└── backend.md               # Đề bài / yêu cầu
```

### Luồng Clean Architecture

```text
HTTP Request
  → Api (Controller)          # chỉ HTTP, không SQL, không try/catch nghiệp vụ
    → Application (Service)   # business rules
      → Infrastructure (Repository + Dapper)
        → MySQL
  ← ExceptionMiddleware + ApiResponseFilter (chuẩn hóa lỗi & response)
```

**Quy ước:**

- Không viết SQL trong Controller.
- Controller chỉ gọi **Service** (Application), không gọi Repository trực tiếp.
- Exception xử lý tập trung bằng Middleware.

---

## 3. Cấu hình

File: `src/Api/appsettings.json`

| Key | Mô tả |
|-----|--------|
| `ConnectionStrings:DefaultConnection` | Chuỗi kết nối MySQL |
| `Jwt:Key` | Secret ký JWT (tối thiểu 32 ký tự) |
| `Jwt:Issuer` / `Jwt:Audience` | Issuer & Audience |
| `Jwt:ExpiresInMinutes` | Thời hạn Access Token (mặc định 60) |
| `Jwt:RefreshTokenDays` | Thời hạn Refresh Token (mặc định 7) |

**Mẫu connection (local):**

```text
Server=localhost;Database=EmployeeManagementDb;User Id=emsuser;Password=YourPassword123;
```

Production nên dùng biến môi trường, không commit secret thật.

---

## 4. Cơ sở dữ liệu

### 4.1. Tạo schema

```bash
# Từ thư mục gốc repo
mysql -u root -p < database/schema.sql
```

Hoặc user ứng dụng:

```bash
mysql -u emsuser -pYourPassword123 < database/schema.sql
```

### 4.2. Seed Admin (nếu chưa có)

```bash
mysql -u emsuser -pYourPassword123 EmployeeManagementDb < database/seed-admin-user.sql
```

### 4.3. Tài khoản Admin mặc định (seed)

| Trường | Giá trị |
|--------|---------|
| Email | `admin@example.com` |
| Password | `Admin123@` |
| Role | `ADMIN` |

> Nếu login fail: chạy lại `database/seed-admin-user.sql` (có UPDATE hash), hoặc tạo Admin qua `POST /api/Users` khi đã login bằng Admin khác.

### 4.4. Các bảng chính

`Users`, `Roles`, `UserRoles`, `Employees`, `Departments`, `Positions`, `RefreshTokens`, `AuditLogs`  
(+ PK, FK, Index, Unique, Soft Delete)

---

## 5. Chạy local

```bash
# 1. Đảm bảo MySQL đã chạy và đã import schema/seed
# 2. Cách nhanh (khuyến nghị — tránh treo NuGet restore):
./run-api.sh

# Hoặc thủ công:
cd src/Api
dotnet build --no-restore   # nếu đã restore trước đó
dotnet run --no-build --urls http://127.0.0.1:5269
```

- Swagger: **http://localhost:5269/swagger**
- Health: **http://localhost:5269/health**

Đổi cổng nếu cần:

```bash
./run-api.sh http://127.0.0.1:5270
# hoặc
dotnet run --no-build --urls http://127.0.0.1:5270
```

> **Lưu ý:** `dotnet run` / `dotnet build` có thể **treo** ở bước restore nếu mạng NuGet chậm, hoặc cổng 5269 đã có process. Dùng `./run-api.sh` hoặc `--no-restore` / `--no-build` như trên.

---

## 6. Chạy bằng Docker

Đã có `Dockerfile` + `docker-compose.yml`.

```bash
# Từ thư mục gốc repo
docker compose up --build
```

| Service | URL / Port |
|---------|------------|
| API + Swagger | http://localhost:8080/swagger |
| MySQL (host) | `localhost:3307` → container `3306` |
| Health | http://localhost:8080/health |

Dừng:

```bash
docker compose down
# Xóa volume MySQL:
docker compose down -v
```

Compose tự mount `database/schema.sql` và `seed-admin-user.sql` khi khởi tạo DB lần đầu.

---

## 7. Xác thực trên Swagger

### 7.1. Đăng nhập Admin

`POST /api/Auth/login`

```json
{
  "email": "admin@example.com",
  "password": "Admin123@"
}
```

### 7.2. Authorize

1. Copy `accessToken` từ response.
2. Bấm **Authorize** trên Swagger.
3. Dán: `Bearer <accessToken>` (có chữ `Bearer` và khoảng trắng).

### 7.3. Phân quyền

| Role | Quyền |
|------|--------|
| **ADMIN** | Quản lý Users, Employees, Departments, Positions, Dashboard |
| **EMPLOYEE** | Profile (xem/sửa), đổi mật khẩu, upload avatar; xem danh sách phòng ban (GET) |

### 7.4. Tạo tài khoản

| Cách | Endpoint | Ghi chú |
|------|----------|---------|
| Đăng ký public | `POST /api/Auth/register` | **Luôn** gán role `EMPLOYEE` (không nhận role từ client) |
| Admin tạo user | `POST /api/Users` | Cần JWT Admin; `roleCode`: `ADMIN` hoặc `EMPLOYEE` |

Ví dụ tạo Admin mới (đã Authorize bằng Admin):

```json
{
  "fullName": "Admin Mới",
  "email": "admin.new@company.com",
  "password": "Admin123@",
  "phoneNumber": "0912345678",
  "roleCode": "ADMIN"
}
```

---

## 8. Danh sách API chính

Base path: `/api`

### Auth — `/api/Auth`

| Method | Path | Auth | Mô tả |
|--------|------|------|--------|
| POST | `/register` | Public | Đăng ký (Employee) |
| POST | `/login` | Public | Đăng nhập → JWT + Refresh Token |
| POST | `/refresh-token` | Public | Làm mới token |
| POST | `/logout` | Public | Thu hồi refresh token |
| POST | `/change-password` | JWT | Đổi mật khẩu |
| POST | `/forgot-password` | Public | OTP giả lập |
| POST | `/reset-password` | Public | Đặt lại mật khẩu bằng OTP |

### Users — `/api/Users` (Admin)

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/` | Danh sách (page, pageSize) |
| GET | `/{id}` | Chi tiết |
| POST | `/` | Tạo user (+ role) |
| PUT | `/{id}` | Cập nhật |
| DELETE | `/{id}` | Soft delete (không được xóa chính mình) |

### Employees — `/api/Employees` (Admin)

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/` | Danh sách + search/filter/sort/pagination |
| GET | `/{id}` | Chi tiết |
| POST | `/` | Thêm (tự sinh `EmployeeCode`) |
| PUT | `/{id}` | Cập nhật |
| DELETE | `/{id}` | Soft delete |
| PATCH | `/{id}/restore` | Khôi phục |

**Query list (`GET /api/Employees`):**

| Tham số | Mô tả | Ví dụ |
|---------|--------|--------|
| `page` | Trang (mặc định 1) | `1` |
| `pageSize` | Số bản ghi/trang (max 100) | `20` |
| `search` | Tìm theo **tên HOẶC email HOẶC SĐT** | `nguyen` |
| `searchName` | Tìm theo họ tên | `Nguyen Van A` |
| `searchEmail` | Tìm theo email | `a@mail.com` |
| `searchPhone` | Tìm theo SĐT | `0912` |
| `departmentId` | Lọc phòng ban | `1` |
| `positionId` | Lọc chức vụ | `2` |
| `status` | Lọc trạng thái | `Working` |
| `sortBy` | `fullName` \| `createdAt` \| `hireDate` | `fullName` |
| `sortDir` | `asc` \| `desc` | `asc` |

```http
GET /api/Employees?search=nguyen&departmentId=1&status=Working&sortBy=fullName&sortDir=asc&page=1&pageSize=10
```

Response `data`: `{ items, page, pageSize, totalCount, totalPages }`

### Departments — `/api/Departments`

| Method | Path | Quyền |
|--------|------|--------|
| GET | `/`, `/{id}` | Admin hoặc Employee |
| POST, PUT, DELETE | | Admin only |

Không xóa phòng ban còn nhân viên.

### Positions — `/api/Positions` (Admin)

CRUD chức vụ. Không xóa chức vụ còn nhân viên.

### Profile — `/api/Profile` (Admin hoặc Employee)

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/` | Xem hồ sơ của mình |
| PUT | `/` | Cập nhật hồ sơ |
| POST | `/avatar` | Upload avatar (jpg/png, tối đa 2MB) |

### Dashboard — `/api/Dashboard` (Admin)

`GET /` — Tổng NV, theo phòng ban/chức vụ, đang làm, nghỉ việc, mới trong tháng.

### AuditLogs — `/api/AuditLogs` (Admin, chỉ đọc)

| Method | Path | Mô tả |
|--------|------|--------|
| GET | `/` | Danh sách audit log (phân trang + filter) |

**Query:** `page`, `pageSize`, `userId`, `action`, `module`, `from`, `to`

**Action thường gặp:** `LOGIN_SUCCESS`, `LOGIN_FAILED`, `LOGOUT`, `REGISTER`, `CHANGE_PASSWORD`, `RESET_PASSWORD`, `CREATE`, `UPDATE`, `SOFT_DELETE`, `RESTORE`

**Module:** `Auth`, `Employee`, `User`

Ghi log tự động khi: đăng ký/đăng nhập (thành công & thất bại), logout, đổi/reset MK, CRUD Employee, tạo/sửa/xóa User.

**Kiểm tra nhanh sau khi thao tác API:**

```sql
SELECT Id, UserId, Action, Module, EntityName, EntityId, CreatedAt
FROM AuditLogs
ORDER BY Id DESC
LIMIT 20;
```

### Khác

- `GET /health` — Health check  
- Swagger UI — `/swagger`

---

## 9. Response chuẩn

```json
{
  "success": true,
  "message": "Thao tác thành công",
  "data": {}
}
```

Auth endpoints dùng `AuthResponse` (có `accessToken`, `refreshToken`, `message` tiếng Việt).

---

## 10. Validation & Business rules (tóm tắt)

**Validation**

- Email đúng định dạng, không trùng  
- Password ≥ 8 ký tự, có chữ hoa, chữ thường, số  
- SĐT định dạng Việt Nam  

**Business**

- Không xóa Department/Position khi còn Employee  
- Email unique  
- Mã nhân viên tự sinh  
- Chỉ Admin tạo tài khoản (qua `/api/Users`); register public chỉ Employee  
- Employee không sửa Role  
- Không tự xóa tài khoản của mình  

---

## 11. Logging & Exception

- **Serilog**: Console + file `src/Api/Logs/log-YYYYMMDD.txt`  
- Log request / response / exception / login  
- **ExceptionMiddleware**: bắt lỗi toàn cục, message tiếng Việt, không try/catch trong Controller  

---

## 12. Unit test

```bash
# Từ thư mục gốc repo
dotnet test EmployeeManagement.sln
```

- Framework: **xUnit** + **Moq**  
- Hiện tại: **33** unit tests (vượt yêu cầu tối thiểu 10)  
- Thư mục: `tests/EmployeeManagement.Tests`

---

## 13. Postman & tài liệu kèm theo

| Tài liệu | Đường dẫn |
|----------|-----------|
| Postman Collection | `postman/EmployeeManagement.postman_collection.json` |
| Postman Environment | `postman/EmployeeManagement.postman_environment.json` |
| Kiến trúc tầng API | `API_LAYER.md` |
| Đề bài | `backend.md` |
| Ảnh Swagger / Auth | `Images/Authentication/` |

Import Postman: set `baseUrl` (vd. `http://localhost:5269`) và `accessToken` sau khi login.

---

## 14. Checklist theo `backend.md`

### Công nghệ & kiến trúc

- [x] .NET 8 Web API  
- [x] MySQL + Dapper (không EF)  
- [x] Clean Architecture  
- [x] JWT + Refresh Token + BCrypt  
- [x] FluentValidation  
- [x] Serilog  
- [x] Swagger  
- [x] Exception Middleware  
- [x] Docker (`Dockerfile` + `docker-compose.yml`)  
- [x] xUnit (≥ 10 tests)  

### Chức năng

- [x] Đăng ký / Đăng nhập / Logout / Refresh / Đổi MK / Quên MK (OTP giả)  
- [x] Phân quyền Admin / Employee  
- [x] CRUD Employees (soft delete + restore)  
- [x] CRUD Departments / Positions  
- [x] Profile + Upload avatar (jpg/png ≤ 2MB)  
- [x] Dashboard thống kê  
- [x] Search / Filter / Pagination nhân viên  
- [x] API response wrapper  
- [x] AuditLogs (ghi Auth/Employee/User + API Admin xem)  

### Nộp bài

- [x] Source code  
- [x] README hướng dẫn chạy  
- [x] Script SQL  
- [x] Dockerfile + docker-compose  
- [x] Postman Collection  
- [x] Tài liệu kiến trúc (`API_LAYER.md`)  
- [x] Danh sách API (mục 8 README)  
- [x] Hình ảnh Swagger (`Images/`)  

### Khuyến khích

- [x] Health Check (`/health`)  
- [x] Global Response Wrapper  
- [ ] Redis Cache  
- [ ] Rate Limiting  
- [ ] API Versioning  
- [ ] CI/CD GitHub Actions  

---

## 15. Khắc phục sự cố

**Cổng 5269 đang bị chiếm**

```bash
ss -ltnp | grep 5269
# hoặc
lsof -i :5269
kill <pid>
```

**`dotnet run` / build bị treo**

Nguyên nhân thường gặp:
1. **NuGet restore** chờ nuget.org / vulnerability check  
2. **Cổng 5269 đã có API** đang chạy  
3. Nhiều `dotnet`/MSBuild chạy song song (VS Code + terminal)

```bash
# Kiểm tra API đã chạy chưa
curl http://127.0.0.1:5269/health

# Nếu đã 200 → mở Swagger, không cần run lại:
# http://localhost:5269/swagger

# Build không restore (nhanh):
cd /home/tts/EmployeeManagement
dotnet build src/Api/EmployeeManagement.Api.csproj --no-restore

# Chạy không build/restore:
cd src/Api && dotnet run --no-build --no-restore --urls http://127.0.0.1:5269

# Hoặc script:
./run-api.sh
```

**Lỗi restore NuGet (mạng)**

```bash
dotnet restore --ignore-failed-sources
```

**Lỗi kết nối MySQL**

- Kiểm tra service MySQL đang chạy  
- Đúng user/password/database trong `appsettings.json`  
- Docker: host port là **3307**, không phải 3306  

**Swagger vẫn hiện code cũ**

- Dừng process API (`Ctrl+C`)  
- `dotnet run` lại  
- Hard refresh trình duyệt (`Ctrl+Shift+R`)  

---

## 16. Tác giả / Ghi chú

Dự án backend fresher theo đề **Hệ thống Quản lý Nhân viên** (`backend.md`).  
Chỉ backend API — không gồm frontend.
