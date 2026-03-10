-- ============================================================
-- SCRIPT PARA ARREGLAR EL TAMAÑO DEL CAMPO PHONE EN LA TABLA USERS
-- Ejecutar en SQL Server Management Studio o con:
-- sqlcmd -S (localdb)\MSSQLLocalDB -d ExtensionesShopDb -i fix-users-table.sql
-- ============================================================

USE ExtensionesShopDb;
GO

-- Verificar si la columna Phone existe y tiene un tamaño pequeño
IF EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Users' 
    AND COLUMN_NAME = 'Phone'
)
BEGIN
    PRINT 'Actualizando tamaño del campo Phone en la tabla Users...';
    
    -- Cambiar el tamaño de la columna Phone a NVARCHAR(20)
    ALTER TABLE Users
    ALTER COLUMN Phone NVARCHAR(20) NULL;
    
    PRINT 'Campo Phone actualizado correctamente a NVARCHAR(20).';
END
ELSE
BEGIN
    PRINT 'La columna Phone no existe en la tabla Users.';
END
GO

-- Verificar otros campos de la tabla Users y ajustar si es necesario
PRINT 'Verificando estructura completa de la tabla Users...';

-- Email debe ser NVARCHAR(256)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Email')
BEGIN
    ALTER TABLE Users ALTER COLUMN Email NVARCHAR(256) NOT NULL;
    PRINT 'Campo Email ajustado a NVARCHAR(256).';
END

-- FirstName debe ser NVARCHAR(100)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'FirstName')
BEGIN
    ALTER TABLE Users ALTER COLUMN FirstName NVARCHAR(100) NOT NULL;
    PRINT 'Campo FirstName ajustado a NVARCHAR(100).';
END

-- LastName debe ser NVARCHAR(100)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'LastName')
BEGIN
    ALTER TABLE Users ALTER COLUMN LastName NVARCHAR(100) NOT NULL;
    PRINT 'Campo LastName ajustado a NVARCHAR(100).';
END

-- Address debe ser NVARCHAR(200)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Address')
BEGIN
    ALTER TABLE Users ALTER COLUMN Address NVARCHAR(200) NULL;
    PRINT 'Campo Address ajustado a NVARCHAR(200).';
END
ELSE
BEGIN
    -- Si no existe, agregarlo
    ALTER TABLE Users ADD Address NVARCHAR(200) NULL;
    PRINT 'Campo Address agregado como NVARCHAR(200).';
END

-- City debe ser NVARCHAR(100)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'City')
BEGIN
    ALTER TABLE Users ALTER COLUMN City NVARCHAR(100) NULL;
    PRINT 'Campo City ajustado a NVARCHAR(100).';
END
ELSE
BEGIN
    ALTER TABLE Users ADD City NVARCHAR(100) NULL;
    PRINT 'Campo City agregado como NVARCHAR(100).';
END

-- PostalCode debe ser NVARCHAR(10)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PostalCode')
BEGIN
    ALTER TABLE Users ALTER COLUMN PostalCode NVARCHAR(10) NULL;
    PRINT 'Campo PostalCode ajustado a NVARCHAR(10).';
END
ELSE
BEGIN
    ALTER TABLE Users ADD PostalCode NVARCHAR(10) NULL;
    PRINT 'Campo PostalCode agregado como NVARCHAR(10).';
END

-- PasswordResetToken debe ser NVARCHAR(MAX)
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordResetToken')
BEGIN
    PRINT 'Campo PasswordResetToken ya existe.';
END
ELSE
BEGIN
    ALTER TABLE Users ADD PasswordResetToken NVARCHAR(MAX) NULL;
    PRINT 'Campo PasswordResetToken agregado.';
END

-- PasswordResetTokenExpiry debe ser DATETIME2
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'PasswordResetTokenExpiry')
BEGIN
    PRINT 'Campo PasswordResetTokenExpiry ya existe.';
END
ELSE
BEGIN
    ALTER TABLE Users ADD PasswordResetTokenExpiry DATETIME2 NULL;
    PRINT 'Campo PasswordResetTokenExpiry agregado.';
END

PRINT '';
PRINT '============================================================';
PRINT 'ESTRUCTURA ACTUALIZADA CORRECTAMENTE';
PRINT '============================================================';
PRINT 'Estructura final de la tabla Users:';
PRINT '- Id: INT IDENTITY';
PRINT '- Email: NVARCHAR(256) NOT NULL';
PRINT '- PasswordHash: NVARCHAR(MAX) NOT NULL';
PRINT '- FirstName: NVARCHAR(100) NOT NULL';
PRINT '- LastName: NVARCHAR(100) NOT NULL';
PRINT '- Phone: NVARCHAR(20) NULL';
PRINT '- Address: NVARCHAR(200) NULL';
PRINT '- City: NVARCHAR(100) NULL';
PRINT '- PostalCode: NVARCHAR(10) NULL';
PRINT '- CreatedAt: DATETIME2 NOT NULL';
PRINT '- PasswordResetToken: NVARCHAR(MAX) NULL';
PRINT '- PasswordResetTokenExpiry: DATETIME2 NULL';
PRINT '============================================================';
GO

-- Mostrar la estructura actual de la tabla
SELECT 
    COLUMN_NAME as 'Columna',
    DATA_TYPE as 'Tipo',
    CHARACTER_MAXIMUM_LENGTH as 'Tamaño Max',
    IS_NULLABLE as 'Nullable'
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;
GO
