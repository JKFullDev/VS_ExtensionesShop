-- =============================================
-- Script: Crear tablas Favorites y CartItems
-- Descripción: Implementación híbrida para favoritos y carrito
-- Autor: Sistema
-- Fecha: 2024
-- =============================================

USE [master]
GO

-- Cambiar al contexto de la base de datos ExtensionesShopDB
USE [ExtensionesShopDB]
GO

-- =============================================
-- TABLA: Favorites
-- Descripción: Almacena productos favoritos de usuarios
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favorites')
BEGIN
    CREATE TABLE [dbo].[Favorites] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_Favorites] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Favorites_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Favorites_Products] FOREIGN KEY ([ProductId]) 
            REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_Favorites_UserProduct] UNIQUE ([UserId], [ProductId])
    )
    
    PRINT '✅ Tabla Favorites creada correctamente'
END
ELSE
BEGIN
    PRINT '⚠️ La tabla Favorites ya existe'
END
GO

-- Crear índice para mejorar performance en consultas por UserId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Favorites_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Favorites_UserId] 
    ON [dbo].[Favorites] ([UserId])
    INCLUDE ([ProductId], [CreatedAt])
    
    PRINT '✅ Índice IX_Favorites_UserId creado'
END
GO

-- =============================================
-- TABLA: CartItems
-- Descripción: Almacena items del carrito de usuarios logueados
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
BEGIN
    CREATE TABLE [dbo].[CartItems] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [ProductId] INT NOT NULL,
        [Quantity] INT NOT NULL DEFAULT 1,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        
        CONSTRAINT [PK_CartItems] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_CartItems_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CartItems_Products] FOREIGN KEY ([ProductId]) 
            REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE,
        CONSTRAINT [UQ_CartItems_UserProduct] UNIQUE ([UserId], [ProductId]),
        CONSTRAINT [CK_CartItems_Quantity] CHECK ([Quantity] > 0)
    )
    
    PRINT '✅ Tabla CartItems creada correctamente'
END
ELSE
BEGIN
    PRINT '⚠️ La tabla CartItems ya existe'
END
GO

-- Crear índice para mejorar performance en consultas por UserId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_CartItems_UserId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_CartItems_UserId] 
    ON [dbo].[CartItems] ([UserId])
    INCLUDE ([ProductId], [Quantity], [UpdatedAt])
    
    PRINT '✅ Índice IX_CartItems_UserId creado'
END
GO

-- =============================================
-- TRIGGER: Actualizar UpdatedAt en CartItems
-- =============================================
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_CartItems_UpdatedAt')
    DROP TRIGGER [dbo].[TR_CartItems_UpdatedAt]
GO

CREATE TRIGGER [dbo].[TR_CartItems_UpdatedAt]
ON [dbo].[CartItems]
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [dbo].[CartItems]
    SET [UpdatedAt] = GETDATE()
    FROM [dbo].[CartItems] ci
    INNER JOIN inserted i ON ci.Id = i.Id
END
GO

PRINT '✅ Trigger TR_CartItems_UpdatedAt creado'
GO

-- =============================================
-- Verificación final
-- =============================================
PRINT ''
PRINT '========================================='
PRINT 'VERIFICACIÓN DE TABLAS CREADAS'
PRINT '========================================='

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Favorites')
    PRINT '✅ Favorites existe'
ELSE
    PRINT '❌ Favorites NO existe'

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
    PRINT '✅ CartItems existe'
ELSE
    PRINT '❌ CartItems NO existe'

PRINT ''
PRINT 'Script completado exitosamente'
PRINT '========================================='
GO
