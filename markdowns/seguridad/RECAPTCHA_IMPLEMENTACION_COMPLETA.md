# ✅ Implementación de Google reCAPTCHA v3 - COMPLETADA

## 🎯 ¿Qué se implementó?

**Google reCAPTCHA v3** - Protección invisible contra bots en el formulario de registro.

### Características:
- ✅ **Invisible** - El usuario no ve ningún desafío
- ✅ **Scoring** - Puntuación de 0.0 a 1.0 (configurable el mínimo)
- ✅ **Integrado** - Frontend y backend completamente funcionales
- ✅ **Seguro** - Secret Key guardada en User Secrets
- ✅ **Logs** - Registro completo de verificaciones

---

## 📋 PASOS FINALES PARA ACTIVAR

### 1. Obtener claves de Google reCAPTCHA

1. **Ve a**: https://www.google.com/recaptcha/admin/create
2. **Selecciona**: reCAPTCHA v3
3. **Nombre**: Extensiones Shop (o el que prefieras)
4. **Dominios**:
   - `localhost` (para desarrollo)
   - Tu dominio de producción cuando lo tengas
5. **Aceptar** términos y crear

Recibirás:
- **Site Key** (pública) - Ejemplo: `6LcXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX`
- **Secret Key** (privada) - Ejemplo: `6LcYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY`

---

### 2. Configurar la Site Key (pública)

#### A) En `appsettings.json`:

Abre: `ExtensionesShop.Server\appsettings.json`

Reemplaza `TU_SITE_KEY_AQUI` con tu Site Key real:

```json
"Recaptcha": {
  "SiteKey": "TU_SITE_KEY_REAL_AQUI"
}
```

#### B) En `index.html` (2 lugares):

Abre: `ExtensionesShop.Client\wwwroot\index.html`

**Línea ~31 (script de carga):**
```html
<script src="https://www.google.com/recaptcha/api.js?render=TU_SITE_KEY_AQUI"></script>
```
Reemplaza `TU_SITE_KEY_AQUI` con tu Site Key

**Línea ~84 (función JavaScript):**
```javascript
grecaptcha.execute('TU_SITE_KEY_AQUI', { action: action })
```
Reemplaza `TU_SITE_KEY_AQUI` con tu Site Key

---

### 3. Configurar la Secret Key (privada) en User Secrets

Ejecuta en PowerShell (reemplaza con tu Secret Key):

```powershell
dotnet user-secrets set "Recaptcha:SecretKey" "TU_SECRET_KEY_AQUI" --project ExtensionesShop.Server
```

---

### 4. Reiniciar la aplicación

1. **Detén** la aplicación si está corriendo
2. **Reinicia** para cargar las nuevas configuraciones
3. **Prueba** el registro

---

## 🔧 Configuración Avanzada

### Ajustar el score mínimo

El score va de 0.0 (bot) a 1.0 (humano).

**Ubicación**: `ExtensionesShop.Server\Controllers\UsersController.cs` línea ~73

```csharp
const float minScore = 0.5f; // <-- Ajusta este valor
```

**Recomendaciones**:
- `0.3` - Muy permisivo (acepta más registros, pero puede dejar pasar algunos bots)
- `0.5` - Balanceado (recomendado) ⭐
- `0.7` - Estricto (puede rechazar algunos humanos)
- `0.9` - Muy estricto (solo humanos muy claros)

---

## 📁 Archivos Modificados/Creados

### Backend (Server)
- ✅ `Services/RecaptchaService.cs` - Servicio de verificación (NUEVO)
- ✅ `Program.cs` - Registro del servicio
- ✅ `Controllers/UsersController.cs` - Validación de reCAPTCHA
- ✅ `appsettings.json` - Configuración de Site Key

### Frontend (Client)
- ✅ `wwwroot/index.html` - Script de reCAPTCHA
- ✅ `Pages/Registro.razor` - Obtención de token
- ✅ `Services/AuthService.cs` - Modelo actualizado

---

## 🧪 Cómo Probar

### 1. Registro Normal
1. Abre el formulario de registro
2. Completa todos los campos
3. Haz clic en "Crear Cuenta"
4. **Debería funcionar** y ver en los logs el score de reCAPTCHA

### 2. Ver el Score en Logs
En la consola de Visual Studio verás:
```
reCAPTCHA verificado exitosamente para test@example.com con score 0.9
```

### 3. Forzar Rechazo (Para testing)
Temporalmente cambia el minScore a 1.0:
```csharp
const float minScore = 1.0f; // Rechazará todos
```
Intenta registrarte y verás el mensaje de error.

---

## 📊 Monitoreo

### Ver estadísticas de reCAPTCHA:
1. Ve a https://www.google.com/recaptcha/admin
2. Selecciona tu sitio
3. Ver gráficas de:
   - Requests procesados
   - Score distribution
   - Tipos de ataques bloqueados

---

## 🐛 Troubleshooting

### Error: "Verificación de seguridad fallida"

**Posibles causas**:
1. Site Key no configurada correctamente
2. Secret Key no configurada en User Secrets
3. Dominios no autorizados en Google reCAPTCHA
4. JavaScript bloqueado por adblockers

**Solución**:
- Verifica las claves en Google reCAPTCHA Admin
- Asegúrate que `localhost` esté en los dominios permitidos
- Revisa los logs del servidor para ver el error específico

### Error: "grecaptcha is not defined"

**Causa**: Script de reCAPTCHA no cargó

**Solución**:
- Verifica que el script esté en `index.html`
- Revisa la consola del navegador para errores de carga
- Asegúrate de tener conexión a internet

### Score muy bajo (0.1 - 0.3)

**Posible causa**: 
- Navegador en modo incógnito
- VPN activa
- Comportamiento automatizado

**Solución**:
- Para desarrollo, ajusta minScore a 0.3
- En producción, mantén 0.5 o superior

---

## 🔐 Seguridad

✅ **Secret Key** guardada en User Secrets (NO en código)
✅ **Site Key** pública (puede estar en código)
✅ **Logs** no incluyen tokens completos
✅ **Rate Limiting** ya implementado como capa adicional

---

## 🚀 Próximos Pasos Opcionales

1. **Implementar reCAPTCHA en Login** (opcional, misma técnica)
2. **Monitorear estadísticas** en Google reCAPTCHA Admin
3. **Ajustar score mínimo** según estadísticas reales
4. **Agregar CAPTCHA visual (v2)** como fallback si v3 falla (avanzado)

---

## 📝 Resumen de Configuración

| Concepto | Ubicación | Valor |
|----------|-----------|-------|
| Site Key | `appsettings.json` | Tu clave pública |
| Site Key | `index.html` (2x) | Tu clave pública |
| Secret Key | User Secrets | Tu clave privada |
| Score Mínimo | `UsersController.cs` | 0.5 (recomendado) |

---

## ✨ Resultado Final

Cuando todo esté configurado:
1. Usuario completa el formulario
2. Al hacer clic en "Crear Cuenta", se obtiene un token invisible de reCAPTCHA
3. El backend verifica el token con Google
4. Si el score es ≥ 0.5, se procesa el registro
5. Si el score es < 0.5, se rechaza con mensaje amigable
6. Todo queda registrado en logs

**¡Protección completa contra bots sin molestar a usuarios reales!** 🎉
