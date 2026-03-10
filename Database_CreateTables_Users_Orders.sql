-- ============================================================
-- EXTENSIONES SHOP - Tablas de Usuarios y Pedidos
-- Base de datos: ExtensionesShopDb
-- Versión: 1.0
-- Fecha: 2024
-- ============================================================

USE ExtensionesShopDb;
GO

-- ============================================================
-- TABLA: Users (Usuarios)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
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
        
        -- Constraints
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );

    PRINT 'Tabla Users creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla Users ya existe.';
END
GO

-- Crear índice en Email para búsquedas rápidas
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE INDEX IX_Users_Email ON Users(Email);
    PRINT 'Índice IX_Users_Email creado.';
END
GO

-- ============================================================
-- TABLA: Orders (Pedidos)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
BEGIN
    CREATE TABLE Orders (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL,
        
        -- Datos del cliente
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
        
        -- Estado y fechas
        [Status] INT NOT NULL DEFAULT 0, -- 0=Pending, 1=Confirmed, 2=Processing, 3=Shipped, 4=Delivered, 5=Cancelled
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ShippedAt DATETIME2 NULL,
        DeliveredAt DATETIME2 NULL,
        
        -- Notas
        Notes NVARCHAR(MAX) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_Orders_Users FOREIGN KEY (UserId) 
            REFERENCES Users(Id) ON DELETE SET NULL
    );

    PRINT 'Tabla Orders creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla Orders ya existe.';
END
GO

-- Crear índices para mejorar rendimiento
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_UserId' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE INDEX IX_Orders_UserId ON Orders(UserId);
    PRINT 'Índice IX_Orders_UserId creado.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_CreatedAt' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE INDEX IX_Orders_CreatedAt ON Orders(CreatedAt DESC);
    PRINT 'Índice IX_Orders_CreatedAt creado.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Orders_Status' AND object_id = OBJECT_ID('Orders'))
BEGIN
    CREATE INDEX IX_Orders_Status ON Orders([Status]);
    PRINT 'Índice IX_Orders_Status creado.';
END
GO

-- ============================================================
-- TABLA: OrderItems (Líneas de Pedido)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
BEGIN
    CREATE TABLE OrderItems (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        OrderId INT NOT NULL,
        ProductId INT NOT NULL,
        
        -- Datos del producto al momento de la compra
        ProductName NVARCHAR(200) NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        Quantity INT NOT NULL,
        SelectedColor NVARCHAR(50) NULL,
        SelectedCentimeters DECIMAL(5,2) NULL,
        
        -- Foreign Keys
        CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) 
            REFERENCES Orders(Id) ON DELETE CASCADE,
        
        CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) 
            REFERENCES Products(Id) ON DELETE NO ACTION
    );

    PRINT 'Tabla OrderItems creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla OrderItems ya existe.';
END
GO

-- Crear índices
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderItems_OrderId' AND object_id = OBJECT_ID('OrderItems'))
BEGIN
    CREATE INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
    PRINT 'Índice IX_OrderItems_OrderId creado.';
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OrderItems_ProductId' AND object_id = OBJECT_ID('OrderItems'))
BEGIN
    CREATE INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
    PRINT 'Índice IX_OrderItems_ProductId creado.';
END
GO

-- ============================================================
-- VISTA: Resumen de Pedidos (Opcional - para consultas rápidas)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.views WHERE name = 'vw_OrdersSummary')
BEGIN
    EXEC('
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
    GROUP BY o.Id, o.CustomerName, o.CustomerEmail, o.Total, o.Status, o.CreatedAt, u.Email
    ');
    PRINT 'Vista vw_OrdersSummary creada.';
END
ELSE
BEGIN
    PRINT 'La vista vw_OrdersSummary ya existe.';
END
GO

-- ============================================================
-- DATOS DE PRUEBA (Opcional - Comentar si no quieres datos de prueba)
-- ============================================================

-- Usuario de prueba
IF NOT EXISTS (SELECT * FROM Users WHERE Email = 'test@extensionesshop.com')
BEGIN
    INSERT INTO Users (Email, PasswordHash, FirstName, LastName, Phone, [Address], City, PostalCode)
    VALUES 
    (
        'test@extensionesshop.com',
        'AQAAAAEAACcQAAAAEJZK8X9Z5qYnHZb9v6XJ+w==', -- Esto es un hash de ejemplo, no es seguro
        'María',
        'García',
        '+34 600 123 456',
        'Calle Mayor 123, 3º B',
        'Madrid',
        '28001'
    );
    PRINT 'Usuario de prueba creado: test@extensionesshop.com';
END
GO

-- ============================================================
-- VERIFICACIÓN FINAL
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'RESUMEN DE TABLAS CREADAS:';
PRINT '============================================================';

SELECT 
    TABLE_NAME AS 'Tabla',
    (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = t.TABLE_NAME) AS 'Columnas'
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_NAME IN ('Users', 'Orders', 'OrderItems')
ORDER BY TABLE_NAME;

PRINT '';
PRINT '============================================================';
PRINT 'Script ejecutado exitosamente.';
PRINT 'Ahora puedes ejecutar tu aplicación Blazor.';
PRINT '============================================================';
GO
