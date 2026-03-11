-- =============================================
-- Script: Eliminar trigger y ajustar tabla CartItems
-- Descripción: Solución para SqlException con OUTPUT clause
-- =============================================

USE [ExtensionesShopDB]
GO

-- Eliminar el trigger problemático
IF EXISTS (SELECT * FROM sys.triggers WHERE name = 'TR_CartItems_UpdatedAt')
BEGIN
    DROP TRIGGER [dbo].[TR_CartItems_UpdatedAt]
    PRINT '✅ Trigger TR_CartItems_UpdatedAt eliminado correctamente'
END
ELSE
BEGIN
    PRINT '⚠️ El trigger TR_CartItems_UpdatedAt no existe'
END
GO

PRINT ''
PRINT '========================================='
PRINT 'Trigger eliminado exitosamente'
PRINT 'Entity Framework manejará UpdatedAt'
PRINT '========================================='
GO
