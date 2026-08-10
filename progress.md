# Progress - EmployeeManagement Backend

Date: 2026-07-28
Updated: 2026-07-28 00:00

## Tình trạng hiện tại

Đã có nền tảng backend cơ bản theo kiến trúc Clean Architecture và đã triển khai thêm phần quan trọng cho giai đoạn tiếp theo:
- Kết nối DB và DI đã hoạt động ổn định.
- Auth cơ bản đã có: đăng ký, đăng nhập, JWT.
- Đã thêm validator cho đăng ký bằng FluentValidation.
- Đã thêm middleware xử lý exception tập trung.
- Đã thêm logging bằng Serilog.
- Đã triển khai CRUD ban đầu cho Departments và Positions.

## Các phần đã hoàn thành gần đây
- Tạo validator cho RegisterRequest.
- Tạo test đầu tiên cho validator bằng xUnit.
- Thêm middleware exception và cấu hình logging toàn ứng dụng.
- Tạo repository và controller cho Departments.
- Tạo repository và controller cho Positions.
- Tạo module Employees với CRUD, soft delete/restore, search/filter và controller phân quyền.
- Tạo validator cho EmployeeDto và thêm test cho validator nhân viên.
- Thêm endpoint hồ sơ cá nhân và dashboard thống kê cho Admin.
- Xác nhận build solution thành công và unit test chạy thành công (5 test passed).
- Hoàn thiện chuyển đổi lưu Refresh Token từ memory sang MySQL Database qua Dapper (`RefreshTokenRepository`), áp dụng mã hóa SHA256 và cơ chế Token Rotation.
- Bổ sung Unit Tests cho `AuthService` và Refresh Token logic.
  - Thêm bảng RefreshTokens nếu chưa có đầy đủ logic.
  - Tạo endpoint refresh-token và logout.
  - Gắn refresh token vào response auth.
- Thêm đổi mật khẩu và quên mật khẩu (OTP giả lập).
  - Endpoint đổi mật khẩu cho user đã đăng nhập.
  - Endpoint quên mật khẩu trả về OTP giả lập.
  - Lưu OTP tạm thời và validate trước khi đổi mật khẩu.
- Áp dụng validation cho toàn bộ DTOs và controller.
  - Validator cho LoginRequest, ChangePasswordRequest, ForgotPasswordRequest.
  - Áp dụng validation tự động cho API input.
- Tạo module Employees với CRUD, soft delete, khôi phục.
- Tạo dashboard và search/filter/sort/pagination.
- Tạo Dockerfile và docker-compose.
- Viết thêm unit test để đạt tối thiểu 10 test.

## Kế hoạch ưu tiên tiếp theo
1. Hoàn thiện auth nâng cao: refresh token, logout, đổi mật khẩu.
2. Triển khai Employees CRUD và business rules.
3. Cải thiện response chuẩn và logging chi tiết.
4. Hoàn thiện Docker và test.
5. Thêm validation cho các DTO còn lại và expose các endpoint mới qua Swagger.

## Nhật ký phát triển
- [2026-07-28 00:00] Đã thực hiện: thiết lập validation đầu tiên cho RegisterRequest, thêm middleware exception, cấu hình Serilog, triển khai CRUD Departments và Positions, và mở rộng auth với refresh token, logout, đổi mật khẩu, quên mật khẩu.
- [2026-07-28 00:00] Đang thực hiện: chuẩn hóa response API và tiếp tục mở rộng auth nâng cao.
- [2026-07-28 00:00] Kế hoạch tiếp theo: triển khai Employees CRUD, business rules và unit test tiếp theo.
- [2026-07-28 00:10] Đã thực hiện: thêm cơ chế role claim vào JWT và repository role để chuẩn bị phân quyền Admin/Employee.
- [2026-07-28 00:10] Kiểm tra: build và test đã chạy thành công với 3 test passed.
- [2026-07-28 00:20] Đã thực hiện: thêm module Employees với CRUD, soft delete/restore, search/filter và validator riêng cho EmployeeDto.
- [2026-07-28 00:20] Kiểm tra: build và test đã chạy thành công với 5 test passed.

## Mẫu cập nhật tính năng mới
- [2026-07-28 00:10] Đã thực hiện: thêm role claim vào JWT và repository role cho phân quyền.
- [2026-07-28 00:10] Đang thực hiện: áp dụng authorization thực tế cho các endpoint tương ứng.
- [2026-07-28 00:10] Kế hoạch tiếp theo: triển khai Employees CRUD, soft delete/restore, và business rules.
- [2026-07-28 00:10] Kiểm tra: build và test đã chạy thành công với 3 test passed.
- [2026-07-28 00:20] Đã thực hiện: triển khai EmployeesController và IEmployeeRepository/EmployeeRepository cho CRUD và search/filter.
- [2026-07-28 00:20] Kiểm tra: build và test đã chạy thành công với 5 test passed.

## Kế hoạch xây dựng project theo backend.md
1. Hoàn thiện authentication nâng cao [Đã hoàn thành]
   - Refresh Token ✅
   - Logout ✅
   - Đổi mật khẩu (trích xuất userId tự động từ JWT Claim NameIdentifier/Email) ✅
   - Quên mật khẩu (OTP giả lập) ✅
   - Phân quyền Admin/Employee ✅

2. Hoàn thiện module nhân viên
   - CRUD Employees ✅
   - Soft delete / restore ✅
   - Tự sinh EmployeeCode ✅
   - Validation và business rules ⏳

3. Hoàn thiện module phòng ban và chức vụ
   - CRUD Departments
   - CRUD Positions
   - Ràng buộc không xóa khi còn nhân viên

4. Hoàn thiện API nâng cao
   - Search theo tên/email/số điện thoại
   - Filter theo phòng ban/chức vụ/trạng thái
   - Sort và pagination

5. Hoàn thiện hồ sơ cá nhân và upload 
   - Profile view/update
   - Upload avatar jpg/png tối đa 2MB

6. Hoàn thiện báo cáo/dashboard
   - Tổng nhân viên
   - Nhân viên theo phòng ban/chức vụ
   - Nhân viên đang làm/nghỉ việc
   - Nhân viên mới theo tháng

7. Hoàn thiện chất lượng dự án
   - Unit test tối thiểu 10 test
   - Dockerfile + docker-compose
   - Swagger và tài liệu API

## Ghi chú
- Không viết SQL trực tiếp trong Controller.
- Tất cả logic dữ liệu phải đi qua Service/Repository và Dapper.
- Ưu tiên làm đúng kiến trúc và business rule trước khi mở rộng tính năng.

## Tiến độ cập nhật

- Ngày: 2026-07-29
- Những việc đã thực hiện:
  - Bổ sung quy tắc nghiệp vụ: chặn xóa Phòng ban/Chức vụ nếu còn nhân viên liên kết.
  - Ngăn người dùng tự xóa tài khoản trong UsersController.
  - Thêm API reset-password (POST /api/auth/reset-password) kèm DTO và validator; triển khai xác thực OTP và cập nhật mật khẩu.
  - Chuyển các thông báo mới sang tiếng Việt (thông báo xóa, auth, reset password).
- Trạng thái build:
  - Xây dựng project API (src/Api) thành công.
  - Xây dựng toàn bộ solution gặp lỗi không liên quan (một dự án test thiếu tham chiếu UrlEncoder) — lỗi này không phải do các thay đổi hiện tại gây ra.
- Bước tiếp theo đề xuất:
  - Thêm unit tests cho các business rules và reset-password.
  - Cân nhắc tạo phương thức repository chuyên dụng để kiểm tra tồn tại nhân viên liên kết (ví dụ ExistsByDepartmentId/ExistsByPositionId) để tối ưu hiệu suất.

