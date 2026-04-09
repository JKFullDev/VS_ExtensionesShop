-- ============================================================
-- CORRECCIÓN DE USUARIOS SEED
-- Problema: los usuarios seed tienen hash de ASP.NET Identity,
-- pero la app usa BCrypt → son incompatibles.
-- 
-- OPCIÓN A (Recomendada): Borra los usuarios seed y regístrate 
-- de nuevo desde el formulario /registro. Luego ejecuta el 
-- bloque de abajo para darte rol Admin.
--
-- OPCIÓN B: Ejecuta un endpoint de registro manual vía Swagger:
-- POST /api/users/register
-- ============================================================

USE ExtensionesShopDb;
GO

-- PASO 1: Eliminar los usuarios seed con hash incorrecto
DELETE FROM Users WHERE Email IN (
    'test@extensionesshop.com',
    'juan.carlos.alonso.hernando@students.thepower.education'
);
PRINT 'Usuarios seed eliminados correctamente.';

-- ============================================================
-- PASO 2: Después de registrarte desde /registro con tu email,
-- ejecuta esto para darte rol Admin y verificar el email:
-- ============================================================

-- DESCOMENTA y ajusta con tu email real:
/*
UPDATE Users
SET [Role] = 'Admin', EmailVerified = 1
WHERE Email = 'TU_EMAIL_AQUI';

SELECT Id, Email, [Role], EmailVerified FROM Users;
*/
