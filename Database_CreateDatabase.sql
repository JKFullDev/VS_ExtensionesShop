-- ============================================================
-- CREAR BASE DE DATOS ExtensionesShopDb
-- ============================================================

-- Verificar si existe y eliminarla (CUIDADO: Esto borra todo)
USE master;
GO

IF DB_ID('ExtensionesShopDb') IS NOT NULL
BEGIN
    PRINT 'La base de datos ExtensionesShopDb ya existe.';
    PRINT '¿Quieres recrearla? Comenta las siguientes 3 líneas si NO quieres borrarla.';
    
    -- ALTER DATABASE ExtensionesShopDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    -- DROP DATABASE ExtensionesShopDb;
    -- PRINT 'Base de datos eliminada.';
END
GO

-- Crear la base de datos
CREATE DATABASE ExtensionesShopDb;
GO

PRINT '✅ Base de datos ExtensionesShopDb creada exitosamente.';
PRINT '';
PRINT 'Ahora ejecuta los siguientes scripts en orden:';
PRINT '1. Script de creación de tablas (Products, Categories, Subcategories)';
PRINT '2. Database_CreateTables_Users_Orders.sql';
PRINT '3. Script de inserts de productos';
GO

-- Usar la base de datos
USE ExtensionesShopDb;
GO

PRINT '';
PRINT '============================================================';
PRINT 'CONEXIÓN CORRECTA A: ExtensionesShopDb';
PRINT '============================================================';
PRINT 'Ahora puedes ejecutar tus scripts de creación de tablas.';
GO
