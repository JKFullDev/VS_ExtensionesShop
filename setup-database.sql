-- ============================================================
-- EXTENSIONES SHOP - SQL SERVER DATABASE SETUP
-- Ejecutar en SQL Server Management Studio (SSMS)
-- O usar: sqlcmd -S (localdb)\MSSQLLocalDB -i setup-database.sql
-- ============================================================

USE master;
GO

-- Eliminar BD si existe
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'ExtensionesShopDb')
BEGIN
    ALTER DATABASE ExtensionesShopDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ExtensionesShopDb;
END
GO

-- Crear nueva BD
CREATE DATABASE ExtensionesShopDb;
GO

USE ExtensionesShopDb;
GO

-- ============================================================
-- CREAR TABLAS
-- ============================================================

CREATE TABLE Categories (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(500) NULL,
    SortOrder INT NOT NULL DEFAULT 0
);
GO

CREATE TABLE Products (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(200) NOT NULL,
    Slug NVARCHAR(200) NOT NULL UNIQUE,
    Description NVARCHAR(4000) NULL,
    Price DECIMAL(18,2) NOT NULL,
    OriginalPrice DECIMAL(18,2) NULL,
    ImageUrl NVARCHAR(500) NULL,
    CategoryId INT NOT NULL,
    IsNew BIT NOT NULL DEFAULT 0,
    IsFeatured BIT NOT NULL DEFAULT 0,
    Stock INT NOT NULL DEFAULT 0,
    HairType NVARCHAR(100) NULL,
    Length NVARCHAR(50) NULL,
    Weight NVARCHAR(50) NULL,
    Color NVARCHAR(100) NULL,
    ApplicationMethod NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);
GO

CREATE INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IX_Products_IsFeatured ON Products(IsFeatured);
CREATE INDEX IX_Products_IsNew ON Products(IsNew);
GO

-- ============================================================
-- INSERTAR CATEGORÍAS
-- ============================================================

SET IDENTITY_INSERT Categories ON;
GO

INSERT INTO Categories (Id, Name, Slug, SortOrder, Description) 
VALUES 
(1, N'Clip-In', N'clip-in', 1, N'Sin químicos ni calor. Perfectas para uso diario.'),
(2, N'Tape-In', N'tape-in', 2, N'Resultado de salón, duración de semanas.'),
(3, N'Keratina', N'keratin', 3, N'Fusión perfecta y natural.');
GO

SET IDENTITY_INSERT Categories OFF;
GO

-- ============================================================
-- INSERTAR PRODUCTOS
-- ============================================================

SET IDENTITY_INSERT Products ON;
GO

INSERT INTO Products (Id, Name, Slug, Description, Price, OriginalPrice, ImageUrl, CategoryId, IsNew, IsFeatured, Stock, HairType, Length, Weight, Color, ApplicationMethod, CreatedAt)
VALUES 
(1, N'Extensiones Clip-In Balayage Rubio', N'clip-in-balayage-rubio', N'Extensiones de cabello natural 100% Remy con efecto balayage. 7 piezas incluidas para volumen completo.', 129.90, 169.90, N'https://images.unsplash.com/photo-1522338140262-f46f5913618a?w=800&q=80', 1, 1, 1, 45, N'Remy', N'50cm', N'120g', N'Balayage Rubio #18/22', N'Clip-In', GETUTCDATE()),
(2, N'Extensiones Clip-In Castaño Chocolate', N'clip-in-castano-chocolate', N'Cabello 100% Remy en tono castaño chocolate natural. Perfectas para añadir volumen y largo al instante.', 119.90, NULL, N'https://images.unsplash.com/photo-1519699047748-de8e457a634e?w=800&q=80', 1, 0, 1, 32, N'Remy', N'60cm', N'140g', N'Castaño Chocolate #4', N'Clip-In', GETUTCDATE()),
(3, N'Extensiones Clip-In Rubio Platino', N'clip-in-rubio-platino', N'Extensiones rubio platino premium. Ideales para cabellos claros y looks de impacto.', 139.90, NULL, N'https://images.unsplash.com/photo-1562322140-8baeececf3df?w=800&q=80', 1, 0, 0, 18, N'Remy', N'55cm', N'120g', N'Rubio Platino #60', N'Clip-In', GETUTCDATE()),
(4, N'Extensiones Clip-In Negro Natural', N'clip-in-negro-natural', N'Extensiones en negro natural intenso. Calidad superior para un acabado profesional.', 109.90, NULL, N'https://images.unsplash.com/photo-1487412947147-5cebf100ffc2?w=800&q=80', 1, 0, 0, 50, N'Remy', N'50cm', N'120g', N'Negro Natural #1B', N'Clip-In', GETUTCDATE()),
(5, N'Extensiones Clip-In Pelirrojo Cobrizo', N'clip-in-pelirrojo-cobrizo', N'Tono pelirrojo vibrante y natural. Perfecto para looks únicos y llamativos.', 124.90, NULL, N'https://images.unsplash.com/photo-1512496015851-a90fb38ba796?w=800&q=80', 1, 0, 0, 14, N'Remy', N'50cm', N'120g', N'Pelirrojo Cobrizo #350', N'Clip-In', GETUTCDATE()),
(6, N'Extensiones Tape-In Rubio Miel', N'tape-in-rubio-miel', N'Extensiones adhesivas de larga duración. Aplicación profesional recomendada. Resultado ultranatural.', 189.90, 229.90, N'https://images.unsplash.com/photo-1516975080664-ed2fc6a32937?w=800&q=80', 2, 0, 1, 25, N'Remy', N'50cm', N'40 piezas / 80g', N'Rubio Miel #14', N'Tape-In', GETUTCDATE()),
(7, N'Extensiones Tape-In Castaño Ceniza', N'tape-in-castano-ceniza', N'Tono ceniza moderno y elegante. Textura sedosa y brillo natural garantizado.', 179.90, NULL, N'https://images.unsplash.com/photo-1522337360788-8b13dee7a37e?w=800&q=80', 2, 1, 1, 30, N'Remy', N'55cm', N'40 piezas / 80g', N'Castaño Ceniza #6A', N'Tape-In', GETUTCDATE()),
(8, N'Extensiones Tape-In Balayage Caramelo', N'tape-in-balayage-caramelo', N'Efecto balayage caramelo tendencia. Transición suave de tonos para un look de salón.', 199.90, NULL, N'https://images.unsplash.com/photo-1527799820374-dcf8d9d4a388?w=800&q=80', 2, 0, 0, 15, N'Remy', N'60cm', N'40 piezas / 100g', N'Balayage Caramelo #6/18', N'Tape-In', GETUTCDATE()),
(9, N'Extensiones Keratina Castaño Medio', N'keratina-castano-medio', N'Extensiones con punta de keratina para fusión perfecta. Duración de 3-6 meses.', 249.90, NULL, N'https://images.unsplash.com/photo-1519415510236-718bdfcd89c8?w=800&q=80', 3, 0, 1, 20, N'Virgin', N'50cm', N'100 mechas / 1g cada', N'Castaño Medio #6', N'Keratin Fusion', GETUTCDATE()),
(10, N'Extensiones Keratina Rubio Dorado', N'keratina-rubio-dorado', N'Keratina premium en rubio dorado. Aplicación profesional para resultados duraderos.', 259.90, 299.90, N'https://images.unsplash.com/photo-1492106087820-71f1a00d2b11?w=800&q=80', 3, 0, 0, 12, N'Virgin', N'55cm', N'100 mechas / 1g cada', N'Rubio Dorado #16', N'Keratin Fusion', GETUTCDATE()),
(11, N'Extensiones Keratina Negro Azabache', N'keratina-negro-azabache', N'Extensiones de keratina en negro intenso. Máxima calidad y duración.', 239.90, NULL, N'https://images.unsplash.com/photo-1515688594390-b649af70d282?w=800&q=80', 3, 0, 0, 22, N'Virgin', N'60cm', N'100 mechas / 1g cada', N'Negro Azabache #1', N'Keratin Fusion', GETUTCDATE()),
(12, N'Extensiones Keratina Ombré Rosa', N'keratina-ombre-rosa', N'Efecto ombré de fantasía. De castaño a rosa pastel para un look único.', 269.90, NULL, N'https://images.unsplash.com/photo-1519699483106-c7b088c1c2e3?w=800&q=80', 3, 1, 0, 8, N'Remy', N'50cm', N'100 mechas / 1g cada', N'Ombré Castaño/Rosa', N'Keratin Fusion', GETUTCDATE());
GO

SET IDENTITY_INSERT Products OFF;
GO

-- ============================================================
-- VERIFICAR DATOS
-- ============================================================

PRINT '=== CATEGORÍAS ===';
SELECT * FROM Categories ORDER BY SortOrder;

PRINT '';
PRINT '=== PRODUCTOS ===';
SELECT p.Id, p.Name, c.Name AS Category, p.Price, p.Stock, p.IsFeatured, p.IsNew
FROM Products p
INNER JOIN Categories c ON p.CategoryId = c.Id
ORDER BY c.SortOrder, p.IsFeatured DESC, p.Id;

PRINT '';
PRINT '=== ESTADÍSTICAS ===';
SELECT 
    c.Name AS Category,
    COUNT(p.Id) AS TotalProducts,
    SUM(CASE WHEN p.IsFeatured = 1 THEN 1 ELSE 0 END) AS Featured,
    SUM(CASE WHEN p.IsNew = 1 THEN 1 ELSE 0 END) AS [New],
    SUM(p.Stock) AS TotalStock
FROM Categories c
LEFT JOIN Products p ON c.Id = p.CategoryId
GROUP BY c.Name, c.SortOrder
ORDER BY c.SortOrder;

PRINT '';
PRINT '✅ Database setup completed successfully!';
PRINT 'Total Categories: 3';
PRINT 'Total Products: 12';
GO
