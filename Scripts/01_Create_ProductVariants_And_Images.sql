-- ============================================================
-- CREACIÓN DE TABLAS PARA VARIANTES DE PRODUCTOS E IMÁGENES
-- ============================================================
-- Script para gestionar variantes (tallas, colores, stock) e imágenes
-- asociadas a productos de forma relacional y normalizada.
-- 
-- NO USAR Entity Framework Migrations.
-- Ejecutar este script manualmente en SQL Server Management Studio.
-- ============================================================

USE ExtensionesShopDb;
GO

-- ============================================================
-- TABLA: ProductVariants
-- Almacena las variantes de un producto (color, talla/longitud, stock, precio)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductVariants')
BEGIN
    CREATE TABLE dbo.ProductVariants
    (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ProductId INT NOT NULL,

        -- Atributos de la variante
        Color NVARCHAR(50) NULL,                    -- Ej: "Castaño", "Rubio", "Negro"
        Centimeters DECIMAL(5,2) NULL,             -- Ej: 60, 65, 70 (longitud en cm)

        -- Inventario
        Stock INT NOT NULL DEFAULT 0,              -- Cantidad disponible

        -- Precio puede variar por variante (opcional, si es diferente al base)
        Price DECIMAL(18,2) NOT NULL,              -- Precio de la variante

        -- Metadata
        SKU NVARCHAR(50) NULL,                     -- SKU único para la variante
        DisplayOrder INT NOT NULL DEFAULT 0,       -- Orden de visualización
        IsActive BIT NOT NULL DEFAULT 1,           -- Controla si la variante está activa
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Foreign Key
        CONSTRAINT FK_ProductVariants_Products 
            FOREIGN KEY (ProductId) 
            REFERENCES dbo.Products(Id) 
            ON DELETE CASCADE
    )
    
    PRINT 'Tabla ProductVariants creada correctamente.'
END
ELSE
BEGIN
    PRINT 'Tabla ProductVariants ya existe.'
END
GO

-- ============================================================
-- TABLA: ProductImages
-- Almacena imágenes asociadas a un producto o a una variante específica
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProductImages')
BEGIN
    CREATE TABLE dbo.ProductImages
    (
        Id INT PRIMARY KEY IDENTITY(1,1),
        ProductId INT NOT NULL,
        ProductVariantId INT NULL,                 -- NULL si es imagen general del producto
        
        -- Información de la imagen
        ImageUrl NVARCHAR(MAX) NOT NULL,          -- URL o ruta de la imagen
        AltText NVARCHAR(255) NULL,               -- Texto alternativo (SEO)
        DisplayOrder INT NOT NULL DEFAULT 0,      -- Orden de visualización
        
        -- Metadata
        IsActive BIT NOT NULL DEFAULT 1,          -- Controla si la imagen está visible
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        -- Foreign Keys
        CONSTRAINT FK_ProductImages_Products 
            FOREIGN KEY (ProductId) 
            REFERENCES dbo.Products(Id) 
            ON DELETE CASCADE,
        
        CONSTRAINT FK_ProductImages_ProductVariants 
            FOREIGN KEY (ProductVariantId) 
            REFERENCES dbo.ProductVariants(Id) 
            ON DELETE CASCADE
    )
    
    PRINT 'Tabla ProductImages creada correctamente.'
END
ELSE
BEGIN
    PRINT 'Tabla ProductImages ya existe.'
END
GO

-- ============================================================
-- ÍNDICES PARA MEJORAR PERFORMANCE
-- ============================================================

-- Índice para búsquedas rápidas de variantes por producto
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductVariants_ProductId' AND object_id = OBJECT_ID('dbo.ProductVariants'))
BEGIN
    CREATE INDEX IX_ProductVariants_ProductId 
        ON dbo.ProductVariants(ProductId) 
        INCLUDE (Color, Centimeters, Stock, Price)
    PRINT 'Índice IX_ProductVariants_ProductId creado.'
END
GO

-- Índice para búsquedas por SKU
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductVariants_SKU' AND object_id = OBJECT_ID('dbo.ProductVariants'))
BEGIN
    CREATE UNIQUE INDEX IX_ProductVariants_SKU 
        ON dbo.ProductVariants(SKU) 
        WHERE SKU IS NOT NULL
    PRINT 'Índice IX_ProductVariants_SKU creado.'
END
GO

-- Índice para búsquedas de imágenes por producto
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductImages_ProductId' AND object_id = OBJECT_ID('dbo.ProductImages'))
BEGIN
    CREATE INDEX IX_ProductImages_ProductId 
        ON dbo.ProductImages(ProductId, DisplayOrder)
    PRINT 'Índice IX_ProductImages_ProductId creado.'
END
GO

-- Índice para búsquedas de imágenes por variante
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductImages_ProductVariantId' AND object_id = OBJECT_ID('dbo.ProductImages'))
BEGIN
    CREATE INDEX IX_ProductImages_ProductVariantId 
        ON dbo.ProductImages(ProductVariantId)
    PRINT 'Índice IX_ProductImages_ProductVariantId creado.'
END
GO

-- ============================================================
-- VERIFICACIÓN: Mostrar estructura de las nuevas tablas
-- ============================================================
PRINT '✓ Estructura de ProductVariants:'
EXEC sp_columns 'ProductVariants'
GO

PRINT '✓ Estructura de ProductImages:'
EXEC sp_columns 'ProductImages'
GO

-- ============================================================
-- NOTAS DE MIGRACIÓN:
-- ============================================================
-- 1. Las columnas Color y Centimeters se pueden MOVER a ProductVariants
--    desde la tabla Products si queremos normalizarla aún más.
--
-- 2. La columna ImageUrl de Products puede mantenerse como imagen principal
--    o migrar a ProductImages con IsMainImage = 1.
--
-- 3. ProductVariants.Price permite precios diferentes por variante.
--    Si todos los precios son iguales, puedes ignorar esta columna.
--
-- 4. Para consultas frecuentes, considera agregar vistas:
--    - vw_ProductsWithVariants
--    - vw_ProductVariantsWithImages
--
-- ============================================================
