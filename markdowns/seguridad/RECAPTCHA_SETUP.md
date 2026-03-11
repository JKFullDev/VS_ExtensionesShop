# Configuración de Google reCAPTCHA v3

## Paso 1: Obtener las claves

1. Ve a: https://www.google.com/recaptcha/admin/create
2. Selecciona **reCAPTCHA v3**
3. Agrega los dominios:
   - `localhost` (para desarrollo)
   - Tu dominio de producción (cuando lo tengas)
4. Acepta los términos y crea

Recibirás:
- **Site Key** (pública) - Se usa en el cliente
- **Secret Key** (privada) - Se usa en el servidor

## Paso 2: Configurar las claves

### Site Key (pública):
Edita `ExtensionesShop.Server\appsettings.json` y reemplaza:
```json
"Recaptcha": {
  "SiteKey": "TU_SITE_KEY_PUBLICA_AQUI"
}
```

### Secret Key (privada):
Ejecuta en la terminal (reemplaza con tu clave):
```powershell
dotnet user-secrets set "Recaptcha:SecretKey" "TU_SECRET_KEY_PRIVADA" --project ExtensionesShop.Server
```

## Paso 3: Configurar el cliente (próximo paso)

Continúa con la implementación en el cliente Blazor.
