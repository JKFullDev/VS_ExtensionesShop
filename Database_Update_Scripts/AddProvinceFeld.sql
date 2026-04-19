-- ============================================================================
-- Script de Actualización de Base de Datos - Extensiones Shop
-- Propósito: Agregar campo Province a las tablas Users y Orders
-- Fecha: 2024
-- ============================================================================

-- Verificar la versión de SQL Server
PRINT '========================================';
PRINT 'Iniciando actualización de BD...';
PRINT 'Versión SQL Server: ' + @@VERSION;
PRINT '========================================';
GO

-- ============================================================================
-- 1. AGREGAR CAMPO Province A LA TABLA Users
-- ============================================================================
PRINT '';
PRINT '--- Agregando campo Province a tabla Users ---';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Province'
)
BEGIN
    ALTER TABLE [Users]
    ADD [Province] NVARCHAR(100) NULL;
    
    PRINT '✓ Campo Province agregado a tabla Users';
END
ELSE
BEGIN
    PRINT '⚠ Campo Province ya existe en tabla Users - omitiendo...';
END
GO

-- ============================================================================
-- 2. AGREGAR CAMPO Province A LA TABLA Orders
-- ============================================================================
PRINT '';
PRINT '--- Agregando campo Province a tabla Orders ---';
GO

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'Province'
)
BEGIN
    ALTER TABLE [Orders]
    ADD [Province] NVARCHAR(100) NULL DEFAULT '';
    
    PRINT '✓ Campo Province agregado a tabla Orders';
END
ELSE
BEGIN
    PRINT '⚠ Campo Province ya existe en tabla Orders - omitiendo...';
END
GO

-- ============================================================================
-- 3. ACTUALIZAR CONFIGURACIÓN DE MODELO (OnModelCreating)
-- ============================================================================
PRINT '';
PRINT '--- Validando estructura de columnas ---';
GO

-- Mostrar información sobre los cambios realizados
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (TABLE_NAME IN ('Users', 'Orders'))
AND COLUMN_NAME IN ('Province', 'City', 'PostalCode', 'Address')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

GO

PRINT '';
PRINT '========================================';
PRINT '✓ Actualización completada exitosamente';
PRINT '========================================';
PRINT '';
PRINT 'Cambios aplicados:';
PRINT '  1. Campo Province (nvarchar(100), nullable) agregado a Users';
PRINT '  2. Campo Province (nvarchar(100), nullable) agregado a Orders';
PRINT '';
PRINT 'Notas:';
PRINT '  • El campo es nullable para compatibilidad con registros existentes';
PRINT '  • En Orders, tiene un valor por defecto vacío';
PRINT '  • La aplicación cargará el valor desde el perfil del usuario';
PRINT '';
GO
