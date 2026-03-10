-- ============================================================
-- DIAGNÓSTICO: Verificar que los productos existen
-- ============================================================

USE ExtensionesShopDb;
GO

PRINT '============================================================';
PRINT 'DIAGNÓSTICO DE BASE DE DATOS';
PRINT '============================================================';
PRINT '';

-- 1. Verificar que existen productos
PRINT '1. TOTAL DE PRODUCTOS EN LA BASE DE DATOS:';
SELECT COUNT(*) AS 'Total Productos' FROM Products;
PRINT '';

-- 2. Mostrar todos los productos
PRINT '2. LISTADO DE PRODUCTOS:';
SELECT 
    Id,
    Name,
    Price,
    Stock,
    CategoryId,
    SubcategoryId,
    Color,
    Centimeters
FROM Products
ORDER BY Id;
PRINT '';

-- 3. Verificar categorías
PRINT '3. CATEGORÍAS:';
SELECT Id, Name FROM Categories ORDER BY Id;
PRINT '';

-- 4. Verificar subcategorías
PRINT '4. SUBCATEGORÍAS:';
SELECT Id, Name, CategoryId FROM Subcategories ORDER BY Id;
PRINT '';

-- 5. Productos con sus relaciones
PRINT '5. PRODUCTOS CON CATEGORÍAS (JOIN):';
SELECT 
    p.Id,
    p.Name AS ProductName,
    c.Name AS CategoryName,
    s.Name AS SubcategoryName,
    p.Price,
    p.Stock
FROM Products p
LEFT JOIN Categories c ON p.CategoryId = c.Id
LEFT JOIN Subcategories s ON p.SubcategoryId = s.Id
ORDER BY p.Id;
PRINT '';

PRINT '============================================================';
PRINT 'FIN DEL DIAGNÓSTICO';
PRINT '============================================================';
