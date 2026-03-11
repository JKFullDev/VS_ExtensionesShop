# 🛡️ Configuración de Archivos Sensibles

## ⚠️ IMPORTANTE: Antes de Ejecutar el Proyecto

Los archivos `appsettings.json` y `appsettings.Development.json` contienen información sensible y **NO están incluidos en el repositorio** por seguridad.

### 📋 Pasos para Configurar:

1. **Copia los archivos de ejemplo:**
   ```bash
   cd ExtensionesShop.Server
   copy appsettings.json.example appsettings.json
   copy appsettings.Development.json.example appsettings.Development.json
   ```

2. **Edita `appsettings.json`:**
   - Actualiza `Email.OwnerEmail` con tu email
   - Actualiza `Email.FromEmail` con tu email
   - Actualiza `Email.SmtpUser` con tu email
   - Actualiza `Email.SmtpPassword` con tu contraseña de aplicación de Gmail

3. **Configura Gmail para SMTP:**
   - Ve a https://myaccount.google.com/apppasswords
   - Crea una "Contraseña de aplicación"
   - Úsala en `Email.SmtpPassword`

4. **Verifica la Connection String:**
   - Por defecto usa `(localdb)\MSSQLLocalDB`
   - Ajusta si usas otra instancia de SQL Server

### 🔐 Seguridad

- ❌ **NUNCA** hagas commit de `appsettings.json` con datos reales
- ❌ **NUNCA** subas contraseñas o API keys
- ✅ Solo sube los archivos `.example`
- ✅ Usa variables de entorno en producción

### 📧 Configuración de Email (Gmail)

Para enviar emails de recuperación de contraseña:

1. Activa la verificación en 2 pasos en tu cuenta de Google
2. Crea una contraseña de aplicación específica
3. Usa esa contraseña en `SmtpPassword`

**Alternativas a Gmail:**
- Outlook: `smtp.office365.com` puerto `587`
- SendGrid, Mailgun, etc. (recomendado para producción)

### 🗄️ Base de Datos

Ejecuta el script de configuración:
```bash
sqlcmd -S (localdb)\MSSQLLocalDB -i setup-database.sql
```

O ejecuta desde SQL Server Management Studio.

### ✅ Verificación

Antes de hacer commit, verifica:
```bash
git status
```

**NO deberías ver:**
- `appsettings.json`
- `appsettings.Development.json`

**SÍ deberías ver:**
- `appsettings.json.example`
- `appsettings.Development.json.example`

---

**Para más información, ver:** `VALIDACIONES_IMPLEMENTADAS.md` y `actualizaciones10marzo.md`
