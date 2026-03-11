-- =============================================
-- Script: Hacer ProductId nullable en OrderItems
-- Descripción: Permite guardar pedidos sin ProductId (histórico)
-- =============================================

USE [ExtensionesShopDB]
GO

-- 1. Eliminar la foreign key constraint existente
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Products')
BEGIN
    ALTER TABLE [dbo].[OrderItems]
    DROP CONSTRAINT [FK_OrderItems_Products]
    PRINT '✅ Foreign key constraint eliminada'
END
GO

-- 2. Hacer la columna ProductId nullable
ALTER TABLE [dbo].[OrderItems]
ALTER COLUMN [ProductId] INT NULL
GO

PRINT '✅ ProductId ahora es nullable'
GO

-- 3. Recrear la foreign key como opcional (ON DELETE SET NULL)
ALTER TABLE [dbo].[OrderItems]
ADD CONSTRAINT [FK_OrderItems_Products] 
FOREIGN KEY ([ProductId]) 
REFERENCES [dbo].[Products]([Id])
ON DELETE SET NULL  -- Si se elimina el producto, se pone NULL pero el pedido se mantiene
GO

PRINT '✅ Foreign key constraint recreada (nullable)'
GO

PRINT ''
PRINT '========================================='
PRINT 'Tabla OrderItems actualizada correctamente'
PRINT 'ProductId ahora es opcional'
PRINT '========================================='
GO
