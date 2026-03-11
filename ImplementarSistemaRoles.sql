-- =============================================
-- Script: Implementar Sistema de Roles
-- Descripción: Agrega roles de Admin/User
-- =============================================

USE [ExtensionesShopDb]
GO

-- 1. Agregar columna Role a la tabla Users
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Users]') AND name = 'Role')
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [Role] NVARCHAR(20) NOT NULL DEFAULT 'User'
    
    PRINT '✅ Columna Role agregada a Users'
END
ELSE
BEGIN
    PRINT '⚠️ Columna Role ya existe'
END
GO

-- 2. Hacer admin al primer usuario (o al que quieras)
-- Cambia el email por el tuyo
UPDATE [dbo].[Users]
SET [Role] = 'Admin'
WHERE Email = 'juan.carlos.alonso.hernando@students.thepower.education'  -- ✅ Cambia esto por tu email

PRINT '✅ Usuario admin configurado'
GO

-- 3. Verificar que funcionó
SELECT Id, Email, FirstName, LastName, Role
FROM [dbo].[Users]
WHERE Role = 'Admin'
GO

PRINT ''
PRINT '========================================='
PRINT 'Sistema de roles implementado correctamente'
PRINT 'Usuarios Admin:'
SELECT Email, Role FROM [dbo].[Users] WHERE Role = 'Admin'
PRINT '========================================='
GO
