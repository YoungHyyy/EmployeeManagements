-- Seed data to create ADMIN role and user
-- Database: EmployeeManagementDb

USE EmployeeManagementDb;

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

-- Verify created data
SELECT 'ROLES' as 'Table';
SELECT * FROM Roles WHERE Code IN ('ADMIN', 'EMPLOYEE');

SELECT 'ADMIN USER' as 'Table';
SELECT Id, Email, FullName, IsActive FROM Users WHERE Email = 'admin@example.com';

SELECT 'USER ROLES' as 'Table';
SELECT ur.* FROM UserRoles ur
JOIN Users u ON ur.UserId = u.Id
WHERE u.Email = 'admin@example.com';
