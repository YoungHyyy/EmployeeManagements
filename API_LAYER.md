# Tài liệu Kiến trúc & Chi tiết Tầng API (EmployeeManagement.Api)

## 📌 1. Tổng quan về Tầng API (Presentation Layer)

Trong kiến trúc **Clean Architecture**, **Tầng API (`EmployeeManagement.Api`)** đóng vai trò là **Presentation Layer** (tầng giao tiếp ngoài cùng của ứng dụng Backend). 

Tầng này chịu trách nhiệm:
1. **Lắng nghe & Tiếp nhận:** Tiếp nhận các HTTP Request (`GET`, `POST`, `PUT`, `DELETE`, `PATCH`) từ Client (Web Frontend, Mobile App, Postman, Swagger UI).
2. **Xác thực & Phân quyền:** Kiểm tra danh tính người dùng (JWT Token) và phân quyền truy cập (`Admin` hay `Employee`).
3. **Định tuyến (Routing):** Điều hướng Request đến đúng Controller và Action Method tương ứng.
4. **Gọi tầng Application:** Chuyển giao dữ liệu đầu vào (DTO) xuống tầng `Application` để xử lý nghiệp vụ.
5. **Phản hồi (Response):** Đóng gói kết quả và trả về HTTP Response với các Status Code chuẩn (`200 OK`, `400 Bad Request`, `401 Unauthorized`, `404 Not Found`, `500 Internal Server Error`).
6. **Bắt lỗi toàn cục:** Bắt và xử lý tập trung các ngoại lệ phát sinh (Exceptions) để ứng dụng không bị sập.

---

## 🗂️ 2. Cấu trúc Thư mục & Tập tin Tầng API

```text
src/Api/
 ├── Authentication/
 │    ├── AuthorizationPolicies.cs
 │    └── BearerAuthenticationHandler.cs
 ├── Controllers/
 │    ├── AuthController.cs
 │    ├── DashboardController.cs
 │    ├── DepartmentsController.cs
 │    ├── EmployeesController.cs
 │    ├── PositionsController.cs
 │    ├── ProfileController.cs
 │    ├── UsersController.cs
 │    └── WeatherForecastController.cs
 ├── Extensions/
 │    ├── ValidationExtensions.cs
 │    └── ValidationServiceExtensions.cs
 ├── Middleware/
 │    └── ExceptionMiddleware.cs
 ├── Properties/
 │    └── launchSettings.json
 ├── appsettings.Development.json
 ├── appsettings.json
 ├── EmployeeManagement.Api.csproj
 ├── EmployeeManagement.Api.http
 ├── Program.cs
 └── WeatherForecast.cs
```

---

## 🚀 3. Chi tiết các File Cấu hình & Khởi chạy (Root)

### 3.1 `Program.cs`
* **Vai trò:** Trái tim khởi chạy của toàn bộ ứng dụng Web API.
* **Chức năng chính:**
  - Khởi tạo `WebApplicationBuilder`.
  - Khai báo và đăng ký các dịch vụ vào Dependency Injection (DI) Container (ví dụ: `IAuthService`, `IUserRepository`, `IEmployeeRepository`, `IDbConnectionFactory`).
  - Cấu hình Logging tập trung bằng **Serilog** (ghi log ra Console & File `Logs/log-.txt`).
  - Đăng ký Authentication, Authorization và Swagger Gen.
  - Cấu hình HTTP Request Pipeline (đăng ký `ExceptionMiddleware`, `app.UseAuthentication()`, `app.UseAuthorization()`, `app.MapControllers()`).

### 3.2 `appsettings.json`
* **Vai trò:** File chứa các thông số cấu hình môi trường tĩnh.
* **Nội dung:**
  - `ConnectionStrings:DefaultConnection`: Chuỗi kết nối đến MySQL Database.
  - `Jwt`: Khóa bí mật (`Key`), `Issuer`, `Audience`, thời gian hết hạn Token (`ExpiresInMinutes`).
  - `Logging`: Cấu hình mức độ ghi log mặc định.

### 3.3 `appsettings.Development.json`
* **Vai trò:** Chứa cấu hình ghi đè dành riêng cho môi trường phát triển (Development).

### 3.4 `EmployeeManagement.Api.csproj`
* **Vai trò:** File cấu hình dự án .NET.
* **Nội dung:** Khai báo `net8.0`, tham chiếu dự án tầng `Application` và `Infrastructure`, khai báo các thư viện NuGet (`Swashbuckle.AspNetCore`, `Serilog.AspNetCore`, `FluentValidation.AspNetCore`,...).

---

## 🎮 4. Chi tiết các Controllers (`src/Api/Controllers/`)

Tất cả các Controller đều kế thừa từ `ControllerBase` và được đánh dấu thuộc tính `[ApiController]` cùng `[Route("api/[controller]")]`.

### 4.1 `AuthController.cs`
* **Đường dẫn gốc:** `/api/auth`
* **Nhiệm vụ:** Xử lý xác thực người dùng và cấp phát Token.
* **Danh sách Endpoints:**
  - `POST /api/auth/register`: Đăng ký tài khoản người dùng mới.
  - `POST /api/auth/login`: Đăng nhập, xác thực BCrypt và trả về JWT Access Token & Refresh Token.
  - `POST /api/auth/refresh-token`: Cấp Access Token mới từ Refresh Token.
  - `POST /api/auth/logout`: Đăng xuất tài khoản.
  - `POST /api/auth/change-password`: Đổi mật khẩu cho người dùng hiện tại.
  - `POST /api/auth/forgot-password`: Yêu cầu gửi mã OTP quên mật khẩu.

### 4.2 `EmployeesController.cs`
* **Đường dẫn gốc:** `/api/employees`
* **Nhiệm vụ:** Quản lý danh sách và thông tin nhân viên.
* **Danh sách Endpoints:**
  - `GET /api/employees`: Lấy danh sách nhân viên (hỗ trợ phân trang, tìm kiếm theo tên/email/phone, lọc theo phòng ban/chức vụ/trạng thái).
  - `GET /api/employees/{id}`: Xem chi tiết thông tin 1 nhân viên.
  - `POST /api/employees`: Thêm mới nhân viên (tự động sinh mã `EMP...`, chỉ Admin).
  - `PUT /api/employees/{id}`: Cập nhật thông tin nhân viên (chỉ Admin).
  - `DELETE /api/employees/{id}`: Xóa mềm nhân viên (`IsDeleted = 1`, chỉ Admin).
  - `PATCH /api/employees/{id}/restore`: Khôi phục nhân viên đã xóa mềm (chỉ Admin).

### 4.3 `DepartmentsController.cs`
* **Đường dẫn gốc:** `/api/departments`
* **Nhiệm vụ:** Quản lý danh mục Phòng ban.
* **Quyền truy cập:** `EmployeeOrAdmin` cho `GET`, `AdminOnly` cho `POST`, `PUT`, `DELETE`.
* **Danh sách Endpoints:**
  - `GET /api/departments`: Lấy danh sách phòng ban (phân trang).
  - `GET /api/departments/{id}`: Lấy chi tiết phòng ban theo ID.
  - `POST /api/departments`: Tạo phòng ban mới.
  - `PUT /api/departments/{id}`: Cập nhật phòng ban.
  - `DELETE /api/departments/{id}`: Xóa mềm phòng ban nếu không có nhân viên liên kết.

### 4.4 `PositionsController.cs`
* **Đường dẫn gốc:** `/api/positions`
* **Nhiệm vụ:** Quản lý danh mục Chức vụ.
* **Quyền truy cập:** `AdminOnly` cho tất cả các phương thức.
* **Danh sách Endpoints:**
  - `GET /api/positions`: Lấy danh sách chức vụ (phân trang).
  - `GET /api/positions/{id}`: Lấy chi tiết chức vụ theo ID.
  - `POST /api/positions`: Tạo chức vụ mới.
  - `PUT /api/positions/{id}`: Cập nhật chức vụ.
  - `DELETE /api/positions/{id}`: Xóa mềm chức vụ nếu không có nhân viên liên kết.

### 4.5 `ProfileController.cs`
* **Đường dẫn gốc:** `/api/profile`
* **Nhiệm vụ:** Quản lý hồ sơ người dùng hiện tại.
* **Quyền truy cập:** `EmployeeOrAdmin`.
* **Danh sách Endpoints:**
  - `GET /api/profile`: Lấy thông tin profile của người dùng đang đăng nhập.
  - `PUT /api/profile`: Cập nhật thông tin cá nhân của người dùng đang đăng nhập.
  - `POST /api/profile/avatar`: Tải ảnh đại diện lên cho người dùng đang đăng nhập.

### 4.6 `DashboardController.cs`
* **Đường dẫn gốc:** `/api/dashboard`
* **Nhiệm vụ:** Trả về thống kê tổng quan của hệ thống.
* **Quyền truy cập:** `AdminOnly`.
* **Danh sách Endpoints:**
  - `GET /api/dashboard`: Lấy dữ liệu thống kê dashboard.

### 4.7 `UsersController.cs`
* **Đường dẫn gốc:** `/api/users`
* **Nhiệm vụ:** Quản lý tài khoản hệ thống.
* **Quyền truy cập:** `AdminOnly`.
* **Danh sách Endpoints:**
  - `GET /api/users`: Lấy danh sách người dùng hệ thống (phân trang).
  - `GET /api/users/{id}`: Lấy thông tin người dùng theo ID.
  - `POST /api/users`: Tạo tài khoản người dùng mới (Admin không được tạo tài khoản Admin khác bằng `RoleCode=ADMIN`).
  - `PUT /api/users/{id}`: Cập nhật thông tin người dùng.
  - `DELETE /api/users/{id}`: Xóa tài khoản người dùng (không cho xóa chính mình).

### 4.8 `WeatherForecastController.cs`
* **Đường dẫn gốc:** `/WeatherForecast`
* **Nhiệm vụ:** Endpoint mẫu mặc định từ dự án ASP.NET Core template.
* **Quyền truy cập:** Mở.
* **Danh sách Endpoints:**
  - `GET /WeatherForecast`: Lấy dữ liệu mẫu dự báo thời tiết.

---

## 💡 5. Cơ chế Routing (Định tuyến API) trong Controller

ASP.NET Core sử dụng **Attribute Routing** để tự động ghép nối đường dẫn:

```csharp
[ApiController]
[Route("api/[controller]")] // [controller] tự động lấy tên Class bỏ đuôi "Controller" => "auth"
public class AuthController : ControllerBase
{
    [HttpPost("register")] // Ghép với /api/auth => /api/auth/register
    public async Task<IActionResult> Register(...) { ... }
}
```

---

## 🛡️ 6. Các Thành phần Xác thực, Phân quyền & Middleware

### 6.1 `BearerAuthenticationHandler.cs` (`src/Api/Authentication/`)
* Bộ xử lý tùy biến (Custom Authentication Handler) kiểm tra Header `Authorization: Bearer <token>` từ HTTP Request, giải mã Token và gán đối tượng danh tính `ClaimsPrincipal` vào `HttpContext.User`.

### 6.2 `AuthorizationPolicies.cs` (`src/Api/Authentication/`)
* Định nghĩa các quy tắc phân quyền:
  - `AdminOnly`: Yêu cầu Role là `ADMIN`.
  - `EmployeeOrAdmin`: Yêu cầu Role là `ADMIN` hoặc `EMPLOYEE`.

### 6.3 `ExceptionMiddleware.cs` (`src/Api/Middleware/`)
* Middleware xử lý ngoại lệ tập trung (Global Exception Handler).
* Khi ứng dụng gặp sự cố/crash bất kỳ đâu, Middleware này sẽ chặn lại, ghi log lỗi ra Serilog và trả về kết quả JSON HTTP 500 chuẩn cho client mà không bị crash server.

### 6.4 `ValidationServiceExtensions.cs` (`src/Api/Extensions/`)
* Đăng ký tự động kiểm tra dữ liệu đầu vào (FluentValidation) trước khi Request đi vào trong Controller.

---

## 🔄 7. Luồng Xử lý một HTTP Request (Request Flow)

```mermaid
sequenceDiagram
    autonumber
    actor Client
    participant MW as ExceptionMiddleware
    participant Auth as Auth & Policy Handler
    participant Val as FluentValidation
    participant Ctrl as Controller
    participant Svc as Application Service
    participant Repo as Infrastructure Repository
    participant DB as MySQL Database

    Client->>MW: Gửi HTTP Request
    MW->>Auth: Chuyển Request qua Auth Pipeline
    Auth->>Val: Kiểm tra JWT Token & Rights OK
    Val->>Ctrl: Kiểm tra dữ liệu DTO đầu vào OK
    Ctrl->>Svc: Gọi hàm xử lý nghiệp vụ
    Svc->>Repo: Gọi Repository thực thi SQL qua Dapper
    Repo->>DB: Thực thi truy vấn MySQL
    DB-->>Repo: Trả về dữ liệu
    Repo-->>Svc: Mapping kết quả sang Entity/DTO
    Svc-->>Ctrl: Trả về kết quả nghiệp vụ
    Ctrl-->>Client: Trả về HTTP Response (200 OK / 400 Bad Request)
```
