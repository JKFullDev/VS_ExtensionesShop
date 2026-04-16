-- =============================================
-- Script: Implementar Borrado Lógico (Soft Delete) en Products y ProductVariants
-- Fecha: Diciembre 2024
-- Propósito: Agregar capacidad de soft delete sin perder datos históricos
-- =============================================

-- PASO 1: Agregar columna IsActive a la tabla Products
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'IsActive'
)
BEGIN
    ALTER TABLE Products
    ADD IsActive BIT NOT NULL DEFAULT 1;
    
    PRINT '✅ Columna IsActive agregada a Products (todos activos por defecto)';
END
ELSE
BEGIN
    PRINT '⚠️ La columna IsActive ya existe en Products';
END

-- PASO 2: Agregar columna IsActive a la tabla ProductVariants
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'ProductVariants' AND COLUMN_NAME = 'IsActive'
)
BEGIN
    ALTER TABLE ProductVariants
    ADD IsActive BIT NOT NULL DEFAULT 1;
    
    PRINT '✅ Columna IsActive agregada a ProductVariants (todos activos por defecto)';
END
ELSE
BEGIN
    PRINT '⚠️ La columna IsActive ya existe en ProductVariants';
END

-- PASO 3: Crear índice para optimizar queries de productos activos
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Products_IsActive' AND object_id = OBJECT_ID('Products')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Products_IsActive
    ON Products(IsActive)
    INCLUDE (Id, Name, Price, CategoryId);
    
    PRINT '✅ Índice IX_Products_IsActive creado para optimización';
END

-- PASO 4: Crear índice para variantes activas
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_ProductVariants_IsActive' AND object_id = OBJECT_ID('ProductVariants')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_ProductVariants_IsActive
    ON ProductVariants(IsActive)
    INCLUDE (Id, ProductId, Price, Stock);
    
    PRINT '✅ Índice IX_ProductVariants_IsActive creado para optimización';
END

-- VERIFICACIÓN
PRINT '
=== VERIFICACIÓN ===
Productos activos:
';

SELECT 
    Id,
    Name,
    IsActive,
    COUNT(*) OVER() as TotalProductos
FROM Products
WHERE IsActive = 1
ORDER BY Id;

PRINT '
=== RESULTADO ===
✅ Soft Delete implementado exitosamente
✅ Todos los productos/variantes están activos por defecto
✅ Índices creados para optimizar queries
';
