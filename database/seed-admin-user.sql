-- Seed data to create ADMIN role and user
-- Database: EmployeeManagementDb

USE EmployeeManagementDb;

-- Default catalog for public register (Employee profile)
INSERT IGNORE INTO Departments (Code, Name, Description, IsActive, IsDeleted)
VALUES ('UNASSIGNED', 'Chưa phân bổ', 'Phòng ban mặc định khi đăng ký tài khoản', 1, 0);

INSERT IGNORE INTO Positions (Code, Name, Description, Level, IsActive, IsDeleted)
VALUES ('STAFF', 'Nhân viên', 'Chức vụ mặc định khi đăng ký tài khoản', 4, 1, 0);

-- 1. Insert ADMIN role if not exists
INSERT IGNORE INTO Roles (Code, Name, Description, IsActive, IsDeleted)
VALUES ('ADMIN', 'Administrator', 'Admin role with full access', 1, 0);

-- 2. Insert EMPLOYEE role if not exists
INSERT IGNORE INTO Roles (Code, Name, Description, IsActive, IsDeleted)
VALUES ('EMPLOYEE', 'Employee', 'Employee role with limited access', 1, 0);

-- 3. Insert admin user if not exists
-- Email: admin@example.com
-- Password: Admin123@ (bcrypt hashed)
INSERT IGNORE INTO Users (Email, PasswordHash, FullName, IsActive, IsDeleted)
VALUES (
  'admin@example.com',
  '$2a$11$HUOb4SsRKq8fU9ArecSNFOtM6rKTR2ePj7kbzviYpc9DEm6NaCpn2',
  'Administrator User',
  1,
  0
);

-- If user already existed with wrong hash, reset password to Admin123@
UPDATE Users
SET PasswordHash = '$2a$11$HUOb4SsRKq8fU9ArecSNFOtM6rKTR2ePj7kbzviYpc9DEm6NaCpn2',
    IsActive = 1,
    IsDeleted = 0
WHERE Email = 'admin@example.com';

-- 4. Assign ADMIN role to admin user
INSERT IGNORE INTO UserRoles (UserId, RoleId, IsDeleted)
SELECT u.Id, r.Id, 0
FROM Users u, Roles r
WHERE u.Email = 'admin@example.com' AND r.Code = 'ADMIN';

-- 5. Employee row linked to admin user (Profile GET requires Employee.UserId)
INSERT INTO Employees (
    UserId, DepartmentId, PositionId, EmployeeCode, FullName, Email, PhoneNumber,
    DateOfBirth, Gender, HireDate, Status, IsActive, IsDeleted, CreatedAt
)
SELECT
    u.Id,
    d.Id,
    p.Id,
    'EMPADMIN',
    u.FullName,
    u.Email,
    '0987654321',
    '1990-01-01',
    'Male',
    CURRENT_DATE,
    'Working',
    1,
    0,
    NOW()
FROM Users u
JOIN Departments d ON d.Code = 'BOD' AND d.IsDeleted = 0
JOIN Positions p ON p.Code = 'DIR' AND p.IsDeleted = 0
WHERE u.Email = 'admin@example.com'
  AND u.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM Employees e WHERE e.UserId = u.Id AND e.IsDeleted = 0
  );

-- Verify created data
SELECT 'ROLES' as 'Table';
SELECT * FROM Roles WHERE Code IN ('ADMIN', 'EMPLOYEE');

SELECT 'ADMIN USER' as 'Table';
SELECT Id, Email, FullName, IsActive FROM Users WHERE Email = 'admin@example.com';

SELECT 'USER ROLES' as 'Table';
SELECT ur.* FROM UserRoles ur
JOIN Users u ON ur.UserId = u.Id
WHERE u.Email = 'admin@example.com';
