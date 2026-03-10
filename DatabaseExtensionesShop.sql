/*
=============================================================================
PROYECTO: Extensiones Shop
SISTEMA: Gestión de Inventario y Pedidos
TECNOLOGÍAS: SQL Server, .NET 9, Blazor
AUTOR: Juan Carlos
FECHA: 2026
=============================================================================
*/

-- 1. CONFIGURACIÓN INICIAL Y CREACIÓN DE LA BASE DE DATOS
USE master;
GO

IF DB_ID('ExtensionesShopDb') IS NOT NULL
BEGIN
    ALTER DATABASE ExtensionesShopDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE ExtensionesShopDb;
END
GO

CREATE DATABASE ExtensionesShopDb;
GO

USE ExtensionesShopDb;
GO

-- ============================================================
-- 2. TABLAS MAESTRAS (CATÁLOGOS)
-- ============================================================

-- CATEGORÍAS
CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL
);
GO

-- SUBCATEGORÍAS
CREATE TABLE Subcategories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    CategoryId INT NOT NULL,
    CONSTRAINT FK_Subcategories_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);
GO

-- USUARIOS
CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    Phone NVARCHAR(20) NULL,
    [Address] NVARCHAR(255) NULL,
    City NVARCHAR(100) NULL,
    PostalCode NVARCHAR(10) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
CREATE INDEX IX_Users_Email ON Users(Email);
GO

-- ============================================================
-- 3. TABLAS DE PRODUCTOS Y STOCK
-- ============================================================

CREATE TABLE Products (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(255) NULL,
    Stock INT NOT NULL DEFAULT 0,
    
    CategoryId INT NOT NULL,
    SubcategoryId INT NULL, 
    
    Color NVARCHAR(50) NULL,         
    Centimeters DECIMAL(5,2) NULL,        

    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id),
    CONSTRAINT FK_Products_Subcategories FOREIGN KEY (SubcategoryId) REFERENCES Subcategories(Id)
);
GO

-- ============================================================
-- 4. TABLAS DE PEDIDOS (VENTAS)
-- ============================================================

-- CABECERA DE PEDIDOS
CREATE TABLE Orders (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NULL,
    
    -- Datos de envío (Snapshot en el momento de la compra)
    CustomerEmail NVARCHAR(100) NOT NULL,
    CustomerName NVARCHAR(200) NOT NULL,
    CustomerPhone NVARCHAR(20) NOT NULL,
    ShippingAddress NVARCHAR(255) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    PostalCode NVARCHAR(10) NOT NULL,
    
    -- Totales
    Subtotal DECIMAL(18,2) NOT NULL,
    ShippingCost DECIMAL(18,2) NOT NULL,
    Total DECIMAL(18,2) NOT NULL,
    
    -- Estado: 0=Pending, 1=Confirmed, 2=Processing, 3=Shipped, 4=Delivered, 5=Cancelled
    [Status] INT NOT NULL DEFAULT 0, 
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    ShippedAt DATETIME2 NULL,
    DeliveredAt DATETIME2 NULL,
    Notes NVARCHAR(MAX) NULL,
    
    CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

CREATE INDEX IX_Orders_UserId ON Orders(UserId);
CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt DESC);
CREATE INDEX IX_Orders_Status ON Orders([Status]);
GO

-- DETALLE DE PEDIDOS (LÍNEAS)
CREATE TABLE OrderItems (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    
    -- Datos del producto persistidos para histórico de precios
    ProductName NVARCHAR(200) NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    Quantity INT NOT NULL,
    SelectedColor NVARCHAR(50) NULL,
    SelectedCentimeters DECIMAL(5,2) NULL,
    
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE NO ACTION
);

CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
GO

-- ============================================================
-- 5. VISTAS Y OBJETOS AUXILIARES
-- ============================================================

CREATE VIEW vw_OrdersSummary AS
SELECT 
    o.Id,
    o.CustomerName,
    o.CustomerEmail,
    o.Total,
    o.Status,
    o.CreatedAt,
    COUNT(oi.Id) AS TotalItems,
    SUM(oi.Quantity) AS TotalQuantity,
    u.Email AS UserEmail
FROM Orders o
LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
LEFT JOIN Users u ON o.UserId = u.Id
GROUP BY o.Id, o.CustomerName, o.CustomerEmail, o.Total, o.Status, o.CreatedAt, u.Email;
GO

-- ============================================================
-- 6. INSERCIÓN DE DATOS SEMILLA (MOCK DATA)
-- ============================================================

-- Categorías
INSERT INTO Categories (Name) VALUES 
('Extensiones de pelo natural'), ('Frontal, topper y Pelucas'), 
('Extensiones fibra sintética'), ('Coletas y moños postizos'), 
('Pelo Crochet'), ('Productos de peluquería'), 
('Productos de estética'), ('Herramientas para extensiones'), 
('Complementos'), ('Aparatos eléctricos');

-- Subcategorías (Pelo Natural)
INSERT INTO Subcategories (Name, CategoryId) VALUES 
('Cortina', 1), ('Adhesivas', 1), ('Queratina', 1), 
('Micro anillas', 1), ('Clips', 1), ('A Granel', 1);

-- Usuario de Prueba
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Phone, [Address], City, PostalCode)
VALUES ('test@extensionesshop.com', 'AQAAAAEAACcQAAAAEJZK8X9Z5qYnHZb9v6XJ+w==', 'María', 'García', '+34 600 123 456', 'Calle Mayor 123, 3º B', 'Madrid', '28001');

-- Productos (Muestra)
INSERT INTO Products (Name, Description, Price, ImageUrl, Stock, CategoryId, SubcategoryId, Color, Centimeters) VALUES 
('Extensiones Cortina Premium 60cm', 'Cortina de pelo 100% natural Remy liso.', 120.50, 'https://images.unsplash.com/photo-1519699047748-de8e457a634e?w=500', 15, 1, 1, 'Rubio Platino', 60.00),
('Adhesivas Invisibles 50cm', 'Pack de 20 tiras adhesivas reutilizables.', 85.99, 'https://images.unsplash.com/photo-1560014023-455b70d5c02b?w=500', 30, 1, 2, 'Negro', 50.00),
('Secador Iónico Salón 2000W', 'Secado ultra rápido sin frizz.', 115.00, 'https://images.unsplash.com/photo-1522337660859-02fbefca4702?w=500', 5, 10, NULL, NULL, NULL);

-- ============================================================
-- VERIFICACIÓN FINAL
-- ============================================================
SELECT 'Tablas Creadas' as Estatus, COUNT(*) as Total FROM INFORMATION_SCHEMA.TABLES;
SELECT * FROM Categories;
SELECT * FROM Products;