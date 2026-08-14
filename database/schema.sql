-- ==============================================================================
-- DATABASE: EmployeeManagementDb
-- RDBMS: MySQL
-- PURPOSE: Real-world backend schema for HR / Employee Management System
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `EmployeeManagementDb`
CHARACTER SET utf8mb4
COLLATE utf8mb4_unicode_ci;

USE `EmployeeManagementDb`;

SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `AuditLogs`;
DROP TABLE IF EXISTS `LeaveRequests`;
DROP TABLE IF EXISTS `Attendances`;
DROP TABLE IF EXISTS `RefreshTokens`;
DROP TABLE IF EXISTS `UserRoles`;
DROP TABLE IF EXISTS `RolePermissions`;
DROP TABLE IF EXISTS `Permissions`;
DROP TABLE IF EXISTS `Employees`;
DROP TABLE IF EXISTS `Users`;
DROP TABLE IF EXISTS `Roles`;
DROP TABLE IF EXISTS `Departments`;
DROP TABLE IF EXISTS `Positions`;
SET FOREIGN_KEY_CHECKS = 1;

-- ==============================================================================
-- 1. ROLES
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Roles` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Code` VARCHAR(50) NOT NULL,
    `Name` VARCHAR(100) NOT NULL,
    `Description` VARCHAR(255) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Roles_Code` UNIQUE (`Code`),
    CONSTRAINT `UQ_Roles_Name` UNIQUE (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 2. PERMISSIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Permissions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Code` VARCHAR(100) NOT NULL,
    `Module` VARCHAR(100) NOT NULL,
    `Action` VARCHAR(100) NOT NULL,
    `Description` VARCHAR(255) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Permissions_Code` UNIQUE (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 3. ROLE_PERMISSIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `RolePermissions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `RoleId` INT NOT NULL,
    `PermissionId` INT NOT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_RolePermissions_Role_Permission` UNIQUE (`RoleId`, `PermissionId`),
    CONSTRAINT `FK_RolePermissions_Roles` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_RolePermissions_Permissions` FOREIGN KEY (`PermissionId`) REFERENCES `Permissions` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 4. USERS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Users` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserName` VARCHAR(100) NULL,
    `Email` VARCHAR(255) NOT NULL,
    `PasswordHash` VARCHAR(512) NOT NULL,
    `FullName` VARCHAR(200) NOT NULL,
    `PhoneNumber` VARCHAR(20) NULL,
    `AvatarUrl` VARCHAR(500) NULL,
    `OtpCode` VARCHAR(10) NULL,
    `OtpExpiration` DATETIME NULL,
    `LastLoginAt` DATETIME NULL,
    `EmailVerifiedAt` DATETIME NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Users_Email` UNIQUE (`Email`),
    CONSTRAINT `UQ_Users_UserName` UNIQUE (`UserName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 5. USER_ROLES
-- =============================================================================
CREATE TABLE IF NOT EXISTS `UserRoles` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `RoleId` INT NOT NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_UserRoles_User_Role` UNIQUE (`UserId`, `RoleId`),
    CONSTRAINT `FK_UserRoles_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_UserRoles_Roles` FOREIGN KEY (`RoleId`) REFERENCES `Roles` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 6. DEPARTMENTS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Departments` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Code` VARCHAR(20) NOT NULL,
    `Name` VARCHAR(150) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Departments_Code` UNIQUE (`Code`),
    CONSTRAINT `UQ_Departments_Name` UNIQUE (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 7. POSITIONS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Positions` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `Code` VARCHAR(20) NOT NULL,
    `Name` VARCHAR(150) NOT NULL,
    `Description` VARCHAR(500) NULL,
    `Level` INT NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Positions_Code` UNIQUE (`Code`),
    CONSTRAINT `UQ_Positions_Name` UNIQUE (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 8. EMPLOYEES
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Employees` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NULL,
    `DepartmentId` INT NOT NULL,
    `PositionId` INT NOT NULL,
    `EmployeeCode` VARCHAR(50) NOT NULL,
    `FullName` VARCHAR(200) NOT NULL,
    `Email` VARCHAR(255) NOT NULL,
    `PhoneNumber` VARCHAR(20) NULL,
    `DateOfBirth` DATE NULL,
    `Gender` ENUM('Male', 'Female', 'Other') NOT NULL DEFAULT 'Other',
    `Address` VARCHAR(500) NULL,
    `HireDate` DATE NOT NULL,
    `Status` ENUM('Working', 'Probation', 'Resigned', 'OnLeave') NOT NULL DEFAULT 'Working',
    `Salary` DECIMAL(12,2) NULL,
    `ContractStartDate` DATE NULL,
    `ContractEndDate` DATE NULL,
    `AvatarUrl` VARCHAR(500) NULL,
    `IsActive` TINYINT(1) NOT NULL DEFAULT 1,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Employees_UserId` UNIQUE (`UserId`),
    CONSTRAINT `UQ_Employees_EmployeeCode` UNIQUE (`EmployeeCode`),
    CONSTRAINT `UQ_Employees_Email` UNIQUE (`Email`),
    CONSTRAINT `FK_Employees_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL,
    CONSTRAINT `FK_Employees_Departments` FOREIGN KEY (`DepartmentId`) REFERENCES `Departments` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Employees_Positions` FOREIGN KEY (`PositionId`) REFERENCES `Positions` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 9. REFRESH TOKENS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `RefreshTokens` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `TokenHash` VARCHAR(512) NOT NULL,
    `JwtId` VARCHAR(128) NULL,
    `ExpiresAt` DATETIME NOT NULL,
    `RevokedAt` DATETIME NULL,
    `ReplacedByToken` VARCHAR(512) NULL,
    `IsUsed` TINYINT(1) NOT NULL DEFAULT 0,
    `IsRevoked` TINYINT(1) NOT NULL DEFAULT 0,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_RefreshTokens_TokenHash` UNIQUE (`TokenHash`),
    CONSTRAINT `FK_RefreshTokens_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 10. LEAVE REQUESTS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `LeaveRequests` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `EmployeeId` INT NOT NULL,
    `LeaveType` ENUM('Annual', 'Sick', 'Personal', 'Unpaid') NOT NULL,
    `StartDate` DATE NOT NULL,
    `EndDate` DATE NOT NULL,
    `Reason` VARCHAR(500) NULL,
    `Status` ENUM('Pending', 'Approved', 'Rejected', 'Cancelled') NOT NULL DEFAULT 'Pending',
    `ApprovedBy` INT NULL,
    `ApprovedAt` DATETIME NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `FK_LeaveRequests_Employees` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_LeaveRequests_Approver` FOREIGN KEY (`ApprovedBy`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 11. ATTENDANCES
-- =============================================================================
CREATE TABLE IF NOT EXISTS `Attendances` (
    `Id` INT AUTO_INCREMENT PRIMARY KEY,
    `EmployeeId` INT NOT NULL,
    `WorkDate` DATE NOT NULL,
    `CheckInTime` DATETIME NULL,
    `CheckOutTime` DATETIME NULL,
    `Status` ENUM('Present', 'Absent', 'Late', 'Holiday', 'Remote') NOT NULL DEFAULT 'Present',
    `Note` VARCHAR(500) NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` DATETIME NULL ON UPDATE CURRENT_TIMESTAMP,
    `DeletedAt` DATETIME NULL,
    CONSTRAINT `UQ_Attendances_EmployeeDate` UNIQUE (`EmployeeId`, `WorkDate`),
    CONSTRAINT `FK_Attendances_Employees` FOREIGN KEY (`EmployeeId`) REFERENCES `Employees` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- 12. AUDIT LOGS
-- =============================================================================
CREATE TABLE IF NOT EXISTS `AuditLogs` (
    `Id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NULL,
    `Action` VARCHAR(100) NOT NULL,
    `Module` VARCHAR(100) NULL,
    `EntityName` VARCHAR(150) NULL,
    `EntityId` VARCHAR(100) NULL,
    `IpAddress` VARCHAR(45) NULL,
    `RequestPath` VARCHAR(255) NULL,
    `Details` TEXT NULL,
    `CreatedAt` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT `FK_AuditLogs_Users` FOREIGN KEY (`UserId`) REFERENCES `Users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ==============================================================================
-- INDEXES
-- =============================================================================
CREATE INDEX `IX_Users_Email` ON `Users` (`Email`);
CREATE INDEX `IX_Users_IsDeleted` ON `Users` (`IsDeleted`, `IsActive`);
CREATE INDEX `IX_UserRoles_UserId` ON `UserRoles` (`UserId`);
CREATE INDEX `IX_UserRoles_RoleId` ON `UserRoles` (`RoleId`);
CREATE INDEX `IX_Employees_Search` ON `Employees` (`FullName`, `Email`, `PhoneNumber`);
CREATE INDEX `IX_Employees_Filter` ON `Employees` (`DepartmentId`, `PositionId`, `Status`, `IsDeleted`);
CREATE INDEX `IX_Employees_HireDate` ON `Employees` (`HireDate`, `CreatedAt`);
CREATE INDEX `IX_LeaveRequests_Employee` ON `LeaveRequests` (`EmployeeId`, `Status`);
CREATE INDEX `IX_Attendances_EmployeeDate` ON `Attendances` (`EmployeeId`, `WorkDate`);
CREATE INDEX `IX_AuditLogs_UserId` ON `AuditLogs` (`UserId`, `CreatedAt`);
CREATE INDEX `IX_RefreshTokens_UserId` ON `RefreshTokens` (`UserId`, `IsRevoked`, `IsDeleted`);

-- ==============================================================================
-- SEED DATA
-- =============================================================================
INSERT INTO `Roles` (`Code`, `Name`, `Description`) VALUES
('ADMIN', 'Admin', 'System administrator'),
('MANAGER', 'Manager', 'Department manager'),
('EMPLOYEE', 'Employee', 'Regular employee');

INSERT INTO `Permissions` (`Code`, `Module`, `Action`, `Description`) VALUES
('USER_READ', 'User', 'Read', 'View users'),
('USER_WRITE', 'User', 'Write', 'Create or update users'),
('EMPLOYEE_READ', 'Employee', 'Read', 'View employees'),
('EMPLOYEE_WRITE', 'Employee', 'Write', 'Create or update employees'),
('DEPARTMENT_READ', 'Department', 'Read', 'View departments'),
('DEPARTMENT_WRITE', 'Department', 'Write', 'Create or update departments'),
('LEAVE_READ', 'Leave', 'Read', 'View leave requests'),
('LEAVE_WRITE', 'Leave', 'Write', 'Create or update leave requests'),
('ATTENDANCE_READ', 'Attendance', 'Read', 'View attendance records'),
('ATTENDANCE_WRITE', 'Attendance', 'Write', 'Create or update attendance');

INSERT INTO `RolePermissions` (`RoleId`, `PermissionId`)
SELECT r.Id, p.Id
FROM `Roles` r
JOIN `Permissions` p ON p.Code IN ('USER_READ','USER_WRITE','EMPLOYEE_READ','EMPLOYEE_WRITE','DEPARTMENT_READ','DEPARTMENT_WRITE','LEAVE_READ','LEAVE_WRITE','ATTENDANCE_READ','ATTENDANCE_WRITE')
WHERE r.Code = 'ADMIN';

INSERT INTO `Departments` (`Code`, `Name`, `Description`) VALUES
('BOD', 'Ban Giám Đốc', 'Executive board'),
('HR', 'Phòng Nhân Sự', 'Human resources'),
('IT', 'Phòng Công Nghệ', 'Information technology'),
('UNASSIGNED', 'Chưa phân bổ', 'Phòng ban mặc định khi đăng ký tài khoản');

INSERT INTO `Positions` (`Code`, `Name`, `Description`, `Level`) VALUES
('DIR', 'Director', 'Director', 1),
('MGR', 'Manager', 'Manager', 2),
('DEV', 'Developer', 'Developer', 3),
('HR', 'HR Executive', 'HR executive', 3),
('STAFF', 'Nhân viên', 'Chức vụ mặc định khi đăng ký tài khoản', 4);

-- Email: admin@example.com | Password: Admin123@
INSERT INTO `Users` (`UserName`, `Email`, `PasswordHash`, `FullName`, `PhoneNumber`) VALUES
('admin', 'admin@example.com', '$2a$11$HUOb4SsRKq8fU9ArecSNFOtM6rKTR2ePj7kbzviYpc9DEm6NaCpn2', 'System Administrator', '0987654321');

INSERT INTO `UserRoles` (`UserId`, `RoleId`)
SELECT u.Id, r.Id
FROM `Users` u
JOIN `Roles` r ON r.Code = 'ADMIN'
WHERE u.Email = 'admin@example.com';

INSERT INTO `Employees` (
    `UserId`, `DepartmentId`, `PositionId`, `EmployeeCode`, `FullName`, `Email`, `PhoneNumber`, `DateOfBirth`, `Gender`, `HireDate`, `Status`, `Salary`
) VALUES (
    (SELECT Id FROM `Users` WHERE Email = 'admin@example.com'),
    (SELECT Id FROM `Departments` WHERE Code = 'BOD'),
    (SELECT Id FROM `Positions` WHERE Code = 'DIR'),
    'EMP00001',
    'System Administrator',
    'admin@example.com',
    '0987654321',
    '1990-01-01',
    'Male',
    CURRENT_DATE,
    'Working',
    50000000
);