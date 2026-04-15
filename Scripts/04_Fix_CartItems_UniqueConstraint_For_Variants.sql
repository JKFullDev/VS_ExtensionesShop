-- =============================================
-- Script: Corregir restricción UNIQUE en CartItems para permitir múltiples variantes
-- Fecha: Diciembre 2024
-- Propósito: Permitir que el mismo producto tenga múltiples líneas en el carrito
--           si son variantes diferentes (ProductVariantId diferente)
-- =============================================

-- PASO 1: Eliminar la restricción UNIQUE KEY (no el índice)
-- Esto eliminará automáticamente el índice asociado
IF EXISTS (
    SELECT * FROM sys.key_constraints 
    WHERE name = 'UQ_CartItems_UserProduct' AND parent_object_id = OBJECT_ID('CartItems')
)
BEGIN
    ALTER TABLE CartItems 
    DROP CONSTRAINT UQ_CartItems_UserProduct;
    PRINT '✅ Restricción UQ_CartItems_UserProduct eliminada';
END

-- PASO 2: Crear índice único para variantes (ProductVariantId NOT NULL)
-- Esto permite múltiples filas con el mismo ProductId pero diferente ProductVariantId
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_CartItems_UserProductVariant' AND object_id = OBJECT_ID('CartItems')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_CartItems_UserProductVariant
    ON CartItems(UserId, ProductId, ProductVariantId)
    WHERE ProductVariantId IS NOT NULL;

    PRINT '✅ Índice UQ_CartItems_UserProductVariant creado (con variantes)';
END
ELSE
BEGIN
    PRINT '⚠️ Índice UQ_CartItems_UserProductVariant ya existe';
END

-- PASO 3: Crear índice único para sin variantes (ProductVariantId IS NULL)
-- Esto asegura que solo haya una fila por producto SIN variantes
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'UQ_CartItems_UserProductNull' AND object_id = OBJECT_ID('CartItems')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_CartItems_UserProductNull
    ON CartItems(UserId, ProductId)
    WHERE ProductVariantId IS NULL;

    PRINT '✅ Índice UQ_CartItems_UserProductNull creado (sin variantes)';
END
ELSE
BEGIN
    PRINT '⚠️ Índice UQ_CartItems_UserProductNull ya existe';
END

-- VERIFICACIÓN
PRINT '
=== VERIFICACIÓN DE ÍNDICES ===
Estos son los índices actuales en CartItems:
';

SELECT 
    name AS [Índice],
    CASE WHEN is_unique = 1 THEN 'UNIQUE' ELSE 'No Unique' END AS [Tipo],
    filter_definition AS [Condición]
FROM sys.indexes
WHERE object_id = OBJECT_ID('CartItems') AND name LIKE 'UQ_%'
ORDER BY name;

PRINT '
=== RESULTADO ESPERADO ===
Deberías ver 2 índices:
- UQ_CartItems_UserProductVariant (WHERE ProductVariantId IS NOT NULL)
- UQ_CartItems_UserProductNull (WHERE ProductVariantId IS NULL)

Ahora puedes:
✅ Agregar Producto A sin variante (ProductVariantId = NULL)
✅ Agregar Producto A con Variante 1 (ProductVariantId = 40)
✅ Agregar Producto A con Variante 2 (ProductVariantId = 41)

Pero no puedes:
❌ Agregar dos filas con UserId=X, ProductId=A, ProductVariantId=40
❌ Agregar dos filas con UserId=X, ProductId=A, ProductVariantId=NULL
';
