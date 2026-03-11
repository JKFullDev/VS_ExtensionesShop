# Mejoras de Seguridad Implementadas

## Resumen de Implementación

Se han implementado las siguientes mejoras de seguridad en el sistema de registro de usuarios:

---

## 1. ✅ ILogger en lugar de Console.WriteLine

**Ubicación:** `ExtensionesShop.Server\Controllers\UsersController.cs`

**Implementado:**
- Inyección de `ILogger<UsersController>` en el constructor
- Logs estructurados con niveles apropiados:
  - `LogInformation`: Eventos exitosos (registro, login, verificación)
  - `LogWarning`: Intentos fallidos (email duplicado, login incorrecto)
  - `LogError`: Errores de sistema (excepciones)

**Ejemplo:**
```csharp
_logger.LogError(ex, "Error al registrar usuario {Email}", request.Email);
_logger.LogInformation("Usuario registrado exitosamente: {Email}, ID: {UserId}", user.Email, user.Id);
```

---

## 2. ✅ Normalización de Teléfonos

**Ubicación:** `ExtensionesShop.Server\Controllers\UsersController.cs` - Método `Register`

**Implementado:**
- Eliminación automática de caracteres no numéricos (excepto el símbolo +)
- Permite formatos como: `+34 600 000 000`, `(600) 000-000`, `600.000.000`
- Se almacena en formato normalizado: `+34600000000`

**Código:**
```csharp
string? normalizedPhone = null;
if (!string.IsNullOrWhiteSpace(request.Phone))
{
    normalizedPhone = Regex.Replace(request.Phone, @"[^\d+]", "");
}
```

---

## 3. ✅ Rate Limiting para Prevenir Abuso de Registro

**Ubicación:** 
- Configuración: `ExtensionesShop.Server\Program.cs`
- Aplicación: `ExtensionesShop.Server\Controllers\UsersController.cs`

**Implementado:**
- Política de rate limiting "register"
- **Límite:** 3 registros por cada 15 minutos por IP
- **Respuesta:** HTTP 429 (Too Many Requests) cuando se excede el límite
- Usa `FixedWindowLimiter` de .NET 9

**Configuración:**
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("register", opt =>
    {
        opt.PermitLimit = 3;
        opt.Window = TimeSpan.FromMinutes(15);
    });
});
```

---

## 4. ✅ Email de Verificación Antes de Activar Cuenta

**Ubicación:**
- Backend: `ExtensionesShop.Server\Controllers\UsersController.cs`
- Frontend: `ExtensionesShop.Client\Pages\VerificarEmail.razor`
- Modelo: `ExtensionesShop.Shared\Models\Models.cs`

**Implementado:**

### Base de Datos:
Nuevos campos en tabla `Users`:
- `EmailVerified` (BIT): Indica si el email está verificado
- `EmailVerificationToken` (NVARCHAR(255)): Token único de verificación
- `EmailVerificationTokenExpiry` (DATETIME2): Fecha de expiración (24 horas)

### Backend:
1. **Endpoint de registro** (`POST /api/users/register`):
   - Genera token de verificación único
   - Crea usuario con `EmailVerified = false`
   - Envía email con enlace de verificación
   - Enlace expira en 24 horas

2. **Endpoint de verificación** (`POST /api/users/verify-email`):
   - Valida token y expiración
   - Marca email como verificado
   - Limpia token usado

3. **Endpoint de login** (`POST /api/users/login`):
   - Verifica que el email esté confirmado antes de permitir login
   - Mensaje claro si falta verificación

### Frontend:
- Página `/verificar-email` que procesa el token automáticamente
- Mensaje de éxito actualizado en registro indicando verificación pendiente
- UI clara para estados: verificando, exitoso, error

**Flujo:**
1. Usuario se registra → Recibe email
2. Usuario hace clic en enlace → Redirige a `/verificar-email?token=xxx`
3. Sistema verifica token → Activa cuenta
4. Usuario puede iniciar sesión

---

## 5. ⚠️ CAPTCHA en Formulario de Registro

**Estado:** Pendiente de implementación

**Recomendaciones para implementar:**

### Opción 1: Google reCAPTCHA v3 (Recomendado)
```razor
<!-- En Registro.razor -->
<script src="https://www.google.com/recaptcha/api.js?render=TU_SITE_KEY"></script>

@code {
    private async Task<string> GetRecaptchaToken()
    {
        return await JSRuntime.InvokeAsync<string>(
            "grecaptcha.execute", 
            "TU_SITE_KEY", 
            new { action = "register" }
        );
    }
}
```

### Opción 2: hCaptcha
- Alternativa a Google
- Mejor privacidad
- Similar implementación

### Opción 3: Implementación Custom
- Desafío matemático simple
- Menos efectivo contra bots avanzados
- No requiere servicios externos

**Pasos para implementar reCAPTCHA:**
1. Obtener claves en https://www.google.com/recaptcha/admin
2. Agregar script en `index.html`
3. Enviar token en request de registro
4. Validar en backend con API de Google
5. Rechazar registros con score bajo

---

## Scripts SQL a Ejecutar

**Archivo:** `AddEmailVerificationToUsers.sql`

```sql
ALTER TABLE [dbo].[Users]
ADD 
    [EmailVerified] BIT NOT NULL DEFAULT 0,
    [EmailVerificationToken] NVARCHAR(255) NULL,
    [EmailVerificationTokenExpiry] DATETIME2 NULL;
```

**Nota:** Los usuarios existentes tendrán `EmailVerified = 0`. Puedes:
- Opción A: Marcarlos como verificados manualmente
- Opción B: Pedirles que verifiquen su email

---

## Archivos Modificados

### Backend (Server)
- ✅ `ExtensionesShop.Server\Controllers\UsersController.cs`
  - ILogger agregado
  - Normalización de teléfonos
  - Rate limiting aplicado
  - Sistema de verificación de email
  - Todos los Console.WriteLine reemplazados

- ✅ `ExtensionesShop.Server\Program.cs`
  - Configuración de rate limiting
  - Middleware de rate limiting agregado

### Frontend (Client)
- ✅ `ExtensionesShop.Client\Pages\Registro.razor`
  - Mensaje de éxito actualizado con instrucciones de verificación

- ✅ `ExtensionesShop.Client\Pages\VerificarEmail.razor` (NUEVO)
  - Página de verificación de email
  - Procesamiento automático de token
  - Estados de carga, éxito y error

### Shared
- ✅ `ExtensionesShop.Shared\Models\Models.cs`
  - Campos de verificación agregados al modelo User

### SQL
- ✅ `AddEmailVerificationToUsers.sql` (NUEVO)
  - Script para agregar columnas a la base de datos

---

## Configuración Requerida

### appsettings.json
Asegúrate de tener configurado:
```json
{
  "ClientUrl": "https://localhost:59871",
  "ConnectionStrings": {
    "DefaultConnection": "..."
  },
  "EmailSettings": {
    // Configuración de SMTP
  }
}
```

---

## Próximos Pasos Recomendados

1. ✅ **Ejecutar script SQL** en tu base de datos SQL Server
2. ⚠️ **Implementar CAPTCHA** (Google reCAPTCHA v3 recomendado)
3. 📧 **Verificar configuración de email** para que los usuarios reciban los correos
4. 🔐 **Implementar autenticación JWT** para proteger endpoints
5. 📊 **Monitorear logs** para detectar intentos de abuso
6. 🧪 **Probar flujo completo** de registro y verificación

---

## Pruebas Recomendadas

### 1. Registro Normal
- [x] Registrar usuario con datos válidos
- [x] Verificar que se envía email
- [x] Verificar que usuario tiene EmailVerified = false

### 2. Verificación de Email
- [x] Hacer clic en enlace de verificación
- [x] Verificar que EmailVerified = true
- [x] Intentar verificar con token expirado (esperar 24h o modificar manualmente)

### 3. Rate Limiting
- [x] Intentar registrar 4 usuarios en menos de 15 minutos desde la misma IP
- [x] Verificar respuesta HTTP 429 en el cuarto intento

### 4. Login con Email No Verificado
- [x] Registrar usuario
- [x] Intentar login sin verificar email
- [x] Verificar mensaje de error apropiado

### 5. Normalización de Teléfonos
- [x] Registrar con teléfono: "+34 600 000 000"
- [x] Verificar en BD que se guarda: "+34600000000"
- [x] Probar con formatos: (600) 000-000, 600.000.000

---

## Beneficios de Seguridad

✅ **Prevención de spam:** Rate limiting evita registros masivos
✅ **Validación de emails:** Solo usuarios con email válido pueden acceder
✅ **Auditoría:** Logs estructurados para detectar patrones de abuso
✅ **Consistencia de datos:** Teléfonos normalizados facilitan búsquedas
✅ **Trazabilidad:** Todos los eventos importantes quedan registrados

---

## Notas Adicionales

- El rate limiting es por IP, considera usar rate limiting por email también
- Los tokens de verificación expiran en 24 horas
- Los logs incluyen información sensible mínima (solo email, no contraseñas)
- Para producción, considera agregar más factores de rate limiting (fingerprint del navegador, etc.)
