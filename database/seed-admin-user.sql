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
-- Password: admin123 (bcrypt hashed)
INSERT IGNORE INTO Users (Email, PasswordHash, FullName, IsActive, IsDeleted)
VALUES (
  'admin@example.com',
  '$2a$11$dXJ3SW6G7P50eS3q6iHxO.5aaGBPSr1OTfQGmPwCJ2XhvJkADf7QC',
  'Administrator User',
  1,
  0
);

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
