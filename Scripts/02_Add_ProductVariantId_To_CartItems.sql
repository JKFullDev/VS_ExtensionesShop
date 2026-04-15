-- =============================================
-- Script: Agregar columnas para variantes en CartItems
-- Fecha: Diciembre 2024
-- Propósito: Soportar selección de variantes en carrito con precio y detalles persistidos
-- =============================================

-- Verificar si la columna ProductVariantId ya existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'ProductVariantId'
)
BEGIN
    ALTER TABLE CartItems
    ADD ProductVariantId INT NULL;
    PRINT '✅ Columna ProductVariantId agregada';
END

-- Verificar si la columna UnitPrice ya existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'UnitPrice'
)
BEGIN
    ALTER TABLE CartItems
    ADD UnitPrice DECIMAL(18, 2) NOT NULL DEFAULT 0;
    PRINT '✅ Columna UnitPrice agregada';
END

-- Verificar si la columna VariantColor ya existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'VariantColor'
)
BEGIN
    ALTER TABLE CartItems
    ADD VariantColor NVARCHAR(MAX) NULL;
    PRINT '✅ Columna VariantColor agregada';
END

-- Verificar si la columna VariantCentimeters ya existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'CartItems' AND COLUMN_NAME = 'VariantCentimeters'
)
BEGIN
    ALTER TABLE CartItems
    ADD VariantCentimeters DECIMAL(18, 2) NULL;
    PRINT '✅ Columna VariantCentimeters agregada';
END

-- Verificar resultado
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'CartItems'
ORDER BY ORDINAL_POSITION;
