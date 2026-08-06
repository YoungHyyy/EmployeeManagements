# EmployeeManagement API

Kho lưu trữ này chứa phần backend được xây dựng theo kiến ​​trúc Clean Architecture trên nền tảng .NET 8 cho hệ thống Quản lý Nhân viên (sử dụng MySQL và Dapper). Tài liệu README này hướng dẫn cách chạy, kiểm thử, nạp dữ liệu mẫu (seed) và triển khai API, đồng thời tuân thủ các yêu cầu dự án được nêu trong tệp `backend.md`.


## Quick summary
- Framework: .NET 8
- Database: MySQL
- ORM: Dapper (no EF)
 # EmployeeManagement API

 Đây là repository chứa backend theo kiến trúc Clean Architecture sử dụng .NET 8 cho hệ thống quản lý nhân viên (MySQL + Dapper). README này mô tả cách cài đặt, chạy, seed dữ liệu, test và deploy API. Nội dung tuân theo yêu cầu trong `backend.md`.

 ## Tóm tắt nhanh
 - Framework: .NET 8
 - Cơ sở dữ liệu: MySQL
 - Data access: Dapper (không dùng EF)
 - Xác thực: JWT + Refresh Token, BCrypt
 - Validation: FluentValidation
 - Logging: Serilog
 - Tests: xUnit
 - Tài liệu API: Swagger (OpenAPI)

 ## Yêu cầu trước khi chạy
 - .NET 8 SDK
 - MySQL (local hoặc container)
 - Docker & docker-compose (tùy chọn, để chạy container)
 - Git

 ## Cấu trúc repository
 - `src/Api` — ASP.NET Core Web API (Program, Controllers, Middleware)
 - `src/Application` — Services, DTOs, Validators
 - `src/Domain` — Entities
 - `src/Infrastructure` — Repositories, DB access (Dapper)
 - `tests` — xUnit tests
 - `database` — file `schema.sql` và `seed-admin-user.sql`

 ## Cấu hình
 File `src/Api/appsettings.json` chứa cấu hình mẫu (connection string, JWT). Thay đổi các giá trị theo môi trường hoặc sử dụng biến môi trường.

 Các khóa quan trọng:
 - `ConnectionStrings:DefaultConnection` — chuỗi kết nối MySQL
 - `Jwt:Key` — secret để ký token (nên >= 32 ký tự)
 - `Jwt:Issuer` / `Jwt:Audience`

 ## Thiết lập cơ sở dữ liệu
 1. Tạo database và các bảng bằng `database/schema.sql`:

 ```bash
 # Nếu dùng root
 mysql -u root -p < database/schema.sql

 # Hoặc dùng user theo appsettings
 mysql -u emsuser -pYourPassword123 EmployeeManagementDb < database/schema.sql
 ```

 2. (Tùy chọn) Seed dữ liệu admin và role:

 ```bash
 mysql -u emsuser -pYourPassword123 EmployeeManagementDb < database/seed-admin-user.sql
 ```

 `seed-admin-user.sql` sẽ tạo các role `ADMIN` và `EMPLOYEE` và một user admin.

 ## Chạy API (chạy cục bộ)
 Từ thư mục gốc repository:

 ```bash
 cd src/Api
 dotnet run
 ```

 Nếu cổng `5269` đã được dùng, chạy trên cổng khác:

 ```bash
 dotnet run --urls http://127.0.0.1:5270
 ```

 Mở Swagger UI:

 ```
 http://localhost:5269/swagger
 # hoặc http://localhost:5270/swagger
 ```

 ## Chạy bằng Docker (khuyến nghị để deploy)
Repository đã có `Dockerfile` và `docker-compose.yml` ở root để chạy API cùng MySQL.

 Chạy toàn bộ stack:

 ```bash
 docker compose up --build
 ```

 Sau khi khởi động:
 - API: http://localhost:8080/swagger
 - MySQL: localhost:3306

 Dừng stack:

 ```bash
 docker compose down
 ```

 Nếu muốn xoá dữ liệu MySQL volume:

 ```bash
 docker compose down -v

 ## Xác thực & test bằng Swagger
 1. Tạo user (hoặc dùng admin đã seed): POST `/api/Auth/register` (nếu endpoint cho phép tạo public user)
 2. Đăng nhập: POST `/api/Auth/login` với body:

 ```json
 {
   "email": "admin.test@example.com",
   "password": "Admin123@"
 }
 ```

 3. Sao chép `accessToken` từ response. Trên Swagger bấm **Authorize** và dán:

 ```
 Bearer <accessToken>
 ```

 4. Gọi các endpoint cần quyền:
 - Employees, Departments, Positions, Profile, Dashboard

 Quyền:
 - `Admin`: toàn quyền
 - `Employee`: xem/cập nhật profile, hiển thị danh sách nơi được phép

 ## Upload avatar
 - Endpoint: `POST /api/Profile/avatar`
 - Chấp nhận: `jpg`, `jpeg`, `png`
 - Kích thước tối đa: 2 MB

 Ví dụ curl:

 ```bash
 curl -X POST "http://localhost:5269/api/Profile/avatar" -H "Authorization: Bearer <token>" -F "file=@./avatar.jpg"
 ```

 ## Tests
 Chạy unit tests:

 ```bash
 cd /home/tts/EmployeeManagement
 dotnet test EmployeeManagement.sln
 ```

 Unit tests nằm ở `tests/EmployeeManagement.Tests`.

 ## Health check
 Endpoint kiểm tra sức khỏe:

 ```
 GET /health
 ```

 ## Logging
 Serilog ghi log vào `src/Api/Logs/log-YYYYMMDD.txt` và console. Kiểm tra file này để xem request/response và exception.

 ## Kiểm tra bảo mật & thực hành tốt
 - Đảm bảo `Jwt:Key` đủ dài (>= 32 ký tự).
 - Không commit secrets; dùng biến môi trường cho production.
 - Xem xét thêm rate limiting, CORS cho môi trường thực tế.

 ## Checklist theo `backend.md`
 - [x] .NET 8 + Clean Architecture
 - [x] MySQL + Dapper
 - [x] JWT + Refresh Token + BCrypt
 - [x] Validation (FluentValidation)
 - [x] Logging (Serilog)
 - [x] Swagger / OpenAPI
 - [x] Exception middleware
 - [x] Unit tests (>= 10)
 - [ ] Dockerfile + docker-compose (chưa có)
 - [ ] Postman collection, ảnh Swagger (chưa có)

 ## Khắc phục sự cố (Troubleshooting)
 - Cổng đang dùng: đổi `--urls` hoặc tìm process chiếm cổng:

 ```bash
 lsof -i :5269
 ss -ltnp | grep 5269
 kill <pid>
 ```

 - Lỗi restore NuGet do mạng: dùng cache local hoặc:

 ```bash
 dotnet restore --ignore-failed-sources
 ```

 - Lỗi kết nối DB: kiểm tra `ConnectionStrings:DefaultConnection`, user/host/port, và quyền truy cập MySQL.

