-- =============================================
-- Script: Agregar columna StockValue a Products
-- Fecha: Diciembre 2024
-- Propósito: Permitir stock manual para productos sin variantes
-- =============================================

-- Verificar si la columna StockValue ya existe
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Products' AND COLUMN_NAME = 'StockValue'
)
BEGIN
    ALTER TABLE Products
    ADD StockValue INT NOT NULL DEFAULT 0;
    PRINT '✅ Columna StockValue agregada a Products';
END
ELSE
BEGIN
    PRINT '⚠️ La columna StockValue ya existe en Products';
END

-- Verificar resultado
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Products' AND COLUMN_NAME IN ('Id', 'Name', 'Stock', 'StockValue')
ORDER BY ORDINAL_POSITION;
