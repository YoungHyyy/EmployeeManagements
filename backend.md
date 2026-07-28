# Đề bài Backend Fresher (1 tháng)

## Chủ đề: Hệ thống Quản lý Nhân viên (Employee Management API)

## 1. Mục tiêu

Xây dựng **RESTful API** cho hệ thống quản lý nhân viên bằng **.NET 8 +
MySQL**.

-   Thời gian: **01 tháng**
-   Chỉ xây dựng Backend API.
-   Không sử dụng Entity Framework.
-   Bắt buộc sử dụng **Dapper**.

------------------------------------------------------------------------

# 2. Công nghệ bắt buộc

-   .NET 8 Web API
-   MySQL
-   Dapper
-   JWT Authentication
-   BCrypt
-   Swagger
-   Serilog
-   Docker
-   xUnit
-   Git

------------------------------------------------------------------------

# 3. Kiến trúc

Áp dụng Clean Architecture.

``` text
EmployeeManagement.sln

src/
 ├── Api
 ├── Application
 ├── Domain
 └── Infrastructure

tests/
 └── EmployeeManagement.Tests
```

Không được viết SQL trong Controller.

------------------------------------------------------------------------

# 4. Chức năng bắt buộc

## 4.1 Đăng ký tài khoản

Thông tin:

-   Họ tên
-   Email
-   Số điện thoại
-   Mật khẩu
-   Xác nhận mật khẩu

Yêu cầu

-   Email không được trùng
-   Password hash bằng BCrypt
-   Validate dữ liệu

------------------------------------------------------------------------

## 4.2 Đăng nhập

-   JWT
-   Refresh Token
-   Đăng xuất
-   Đổi mật khẩu
-   Quên mật khẩu (chỉ thiết kế API, có thể sinh OTP giả lập)

------------------------------------------------------------------------

## 4.3 Phân quyền

Có 2 Role

-   Admin
-   Employee

Admin

-   Quản lý toàn bộ hệ thống

Employee

-   Chỉ xem và cập nhật thông tin của chính mình

------------------------------------------------------------------------

## 4.4 Quản lý nhân viên

Thông tin

-   Mã nhân viên
-   Họ tên
-   Email
-   Số điện thoại
-   Ngày sinh
-   Giới tính
-   Địa chỉ
-   Phòng ban
-   Chức vụ
-   Ngày vào làm
-   Trạng thái làm việc
-   Avatar

API

-   Thêm nhân viên
-   Cập nhật
-   Xóa mềm
-   Khôi phục
-   Xem chi tiết
-   Danh sách

------------------------------------------------------------------------

## 4.5 Phòng ban

CRUD

Mỗi nhân viên thuộc một phòng ban.

------------------------------------------------------------------------

## 4.6 Chức vụ

CRUD

Ví dụ

-   Developer
-   Tester
-   BA
-   PM
-   HR

------------------------------------------------------------------------

## 4.7 Hồ sơ cá nhân

Employee có thể

-   Xem profile
-   Cập nhật profile
-   Đổi mật khẩu
-   Upload avatar

------------------------------------------------------------------------

## 4.8 Dashboard

Thống kê

-   Tổng nhân viên
-   Nhân viên theo phòng ban
-   Nhân viên theo chức vụ
-   Nhân viên đang làm
-   Nhân viên nghỉ việc
-   Nhân viên mới theo tháng

------------------------------------------------------------------------

# 5. Tìm kiếm

Danh sách nhân viên hỗ trợ

-   Search tên
-   Search email
-   Search số điện thoại

Filter

-   Phòng ban
-   Chức vụ
-   Trạng thái

Sort

-   Họ tên
-   Ngày tạo
-   Ngày vào làm

Pagination

-   page
-   pageSize

------------------------------------------------------------------------

# 6. Upload

Cho phép upload

-   jpg
-   png

Giới hạn

-   tối đa 2MB

------------------------------------------------------------------------

# 7. Logging

Sử dụng Serilog

Log

-   Request
-   Response
-   Exception
-   Login

------------------------------------------------------------------------

# 8. Exception

Xử lý bằng Middleware.

Không try/catch trong Controller.

------------------------------------------------------------------------

# 9. Validation

Bắt buộc

Email

-   đúng định dạng
-   không trùng

Password

-   tối thiểu 8 ký tự
-   có chữ hoa
-   có chữ thường
-   có số

Phone

-   đúng định dạng Việt Nam

------------------------------------------------------------------------

# 10. Database

Thiết kế các bảng

-   Users
-   Roles
-   UserRoles
-   Employees
-   Departments
-   Positions
-   RefreshTokens
-   AuditLogs

Yêu cầu

-   PK
-   FK
-   Index
-   Unique
-   Soft Delete

------------------------------------------------------------------------

# 11. Business Rule

-   Không xóa phòng ban nếu còn nhân viên.
-   Không xóa chức vụ nếu còn nhân viên.
-   Email là duy nhất.
-   Mã nhân viên sinh tự động.
-   Chỉ Admin được tạo tài khoản nhân viên.
-   Employee không được sửa Role.
-   Không được tự xóa tài khoản của chính mình.

------------------------------------------------------------------------

# 12. API Response

``` json
{
  "success": true,
  "message": "",
  "data": {}
}
```

------------------------------------------------------------------------

# 13. Docker

Phải có

-   Dockerfile
-   docker-compose.yml

Chạy được bằng

``` bash
docker compose up
```

------------------------------------------------------------------------

# 14. Unit Test

Viết tối thiểu 10 Unit Test cho tầng Application.

------------------------------------------------------------------------

# 15. Tài liệu Fresher phải tự nghiên cứu

## .NET

-   Dependency Injection
-   Middleware
-   JWT Authentication
-   Authorization
-   BackgroundService
-   Configuration
-   FluentValidation

## Dapper

-   Query
-   Execute
-   Transaction
-   DynamicParameters
-   Multi Mapping

## MySQL

-   Index
-   Foreign Key
-   Transaction
-   Explain
-   Join
-   Constraint

## REST API

-   RESTful Design
-   HTTP Status Code
-   Pagination
-   Filtering
-   Sorting

## Security

-   JWT
-   Refresh Token
-   BCrypt
-   SQL Injection
-   CORS
-   Upload File Validation

## Docker

-   Dockerfile
-   Docker Compose
-   Volume
-   Network

## Git

-   Branch
-   Merge
-   Pull Request

## Testing

-   xUnit
-   Moq

------------------------------------------------------------------------

# 16. Tiêu chí đánh giá

  Hạng mục                Điểm
  --------------------- ------
  Kiến trúc                 15
  Database                  10
  API & Business            20
  Authentication            10
  Validation                10
  Logging & Exception       10
  Docker                     5
  Unit Test                 10
  Code Quality               5
  README & Git               5

------------------------------------------------------------------------

# 17. Yêu cầu nộp bài

-   Source code Git.
-   README hướng dẫn chạy.
-   Script SQL.
-   Dockerfile.
-   docker-compose.yml.
-   Bộ Postman Collection.
-   Tài liệu mô tả kiến trúc.
-   Danh sách API.
-   Hình ảnh Swagger.

## Khuyến khích

-   Redis Cache.//
-   Rate Limiting.//gioi han so lan request
-   Health Check.
-   Global Response Wrapper.
-   API Versioning.
-   CI/CD bằng GitHub Actions.
