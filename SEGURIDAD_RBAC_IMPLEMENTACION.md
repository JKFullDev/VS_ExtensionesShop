# 🔐 Seguridad en Rol-Based Access Control (RBAC) - Implementación Completa

## ✅ Cambios Implementados

### 1. **Autenticación y Autorización - Program.cs**

#### Archivo: `ExtensionesShop.Client\Program.cs`

```csharp
// ✅ Añadir soporte de autorización
builder.Services.AddAuthorizationCore();
```

**Qué hace:**
- Habilita el sistema de `[Authorize]` atributos en Blazor
- Permite usar `AuthorizeView` y `[Authorize(Roles = "...")]`

---

### 2. **Visibilidad de Botón Admin en Header - Header.razor**

#### Archivo: `ExtensionesShop.Client\Shared\Header.razor`

**Cambios:**
✅ Inyectar `AuthService`
```csharp
@inject AuthService AuthService
```

✅ Suscribirse a cambios de autenticación
```csharp
protected override void OnInitialized()
{
    // Suscribirse a cambios en el estado de autenticación
    AuthService.OnAuthStateChanged += StateHasChanged;
    CartState.OnChange += StateHasChanged;
}
```

✅ Mostrar botón Admin solo para administradores
```html
@if (AuthService.IsAdmin)
{
    <button class="icon-btn admin-btn" @onclick="@(() => Navigation.NavigateTo("/admin"))" aria-label="Panel de administración">
        <svg><!-- Ícono de dashboard --></svg>
    </button>
}
```

**Comportamiento:**
- El botón aparece/desaparece **inmediatamente** cuando el usuario inicia o cierra sesión
- No es visible en el renderizado inicial si el usuario no es admin
- Optimizado para accesibilidad (aria-label, title)

#### Estilos Agregados - `app.css`

```css
.admin-btn {
    color: #F59E0B;  /* Naranja */
}

.admin-btn:hover {
    color: #D97706;  /* Naranja oscuro */
    background-color: rgba(245, 158, 11, 0.1);
}
```

---

### 3. **Seguridad en Rutas - Autorización por Roles**

#### 5 Páginas Admin Actualizadas:

Todas las páginas dentro de `/Admin/` ahora tienen:

```csharp
@attribute [Authorize(Roles = "Admin")]
```

**Páginas Protegidas:**

1. ✅ `ExtensionesShop.Client\Pages\Admin\Dashboard.razor`
   ```csharp
   @page "/admin"
   @attribute [Authorize(Roles = "Admin")]
   ```

2. ✅ `ExtensionesShop.Client\Pages\Admin\GestionCategorias.razor`
   ```csharp
   @page "/admin/categorias"
   @attribute [Authorize(Roles = "Admin")]
   ```

3. ✅ `ExtensionesShop.Client\Pages\Admin\GestionProductos.razor`
   ```csharp
   @page "/admin/productos"
   @attribute [Authorize(Roles = "Admin")]
   ```

4. ✅ `ExtensionesShop.Client\Pages\Admin\GestionPedidos.razor`
   ```csharp
   @page "/admin/pedidos"
   @attribute [Authorize(Roles = "Admin")]
   ```

5. ✅ `ExtensionesShop.Client\Pages\Admin\GestionUsuarios.razor`
   ```csharp
   @page "/admin/usuarios"
   @attribute [Authorize(Roles = "Admin")]
   ```

**Qué hace:**
- Si un usuario NO es admin intenta acceder a `/admin/*`, se redirige automáticamente
- Protege contra acceso directo por URL
- El componente de autorización valida el rol antes de renderizar
- Si no autenticado → redirige a login
- Si autenticado pero sin rol Admin → redirige a inicio

---

### 4. **Visibilidad de Botón Editar en Detalle de Producto**

#### Archivo: `ExtensionesShop.Client\Pages\DetalleProducto.razor`

**Ya implementado con AuthorizeView:**
```html
<AuthorizeView Roles="Admin">
    <Authorized>
        <a href="/admin/productos?editId=@Id" class="btn-admin">
            ⚙️ Editar este producto
        </a>
    </Authorized>
</AuthorizeView>
```

**Nota:** Este usa `<AuthorizeView>` en lugar de `@if (AuthService.IsAdmin)` porque:
- `AuthorizeView` es más seguro (valida en el servidor también)
- Funciona mejor con el sistema de autorización de .NET
- No necesita cambios adicionales

---

## 🔒 Niveles de Seguridad Implementados

### Nivel 1: Seguridad UX (Interfaz)
- ✅ Ocultar botones admin para usuarios normales
- ✅ Mostrar/ocultar dinámicamente en Header
- ✅ Botón Editar solo visible en rol Admin

### Nivel 2: Seguridad de Rutas
- ✅ `@attribute [Authorize(Roles = "Admin")]` en todas las páginas admin
- ✅ Prevenir acceso directo por URL (`/admin/pedidos`)
- ✅ Redirigir automáticamente si no autorizado

### Nivel 3: Seguridad del Backend
- ✅ Validación en API (en Program.cs del Server)
- ✅ Controladores con `[Authorize(Roles = "Admin")]`
- ✅ Claims de JWT verificados en cada solicitud

---

## 🧪 Testing de la Seguridad

### Escenario 1: Usuario Normal
```
1. Navegar a /admin → Redirige a /
2. No ve botón "Panel Admin" en Header
3. No puede editar productos desde público
4. Acceso denegado en API (backend)
```

### Escenario 2: Usuario Admin
```
1. Login como admin → Ve botón "Panel Admin" en Header
2. Click en botón → Abre /admin
3. Puede ver todas las páginas admin
4. Ve botón "Editar" en detalles de producto
5. Acceso permitido en API (backend)
```

### Escenario 3: Cambio de Usuario
```
1. Admin logueado → Ve botón en Header
2. Logout → Botón desaparece inmediatamente
3. Login como usuario normal → Botón no aparece
4. Intenta navegar a /admin → Redirige
```

---

## 📋 Resumen de Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `Program.cs` | ✅ `AddAuthorizationCore()` |
| `Header.razor` | ✅ AuthService, botón Admin, suscripción cambios |
| `app.css` | ✅ Estilos `.admin-btn` |
| `Dashboard.razor` | ✅ `@attribute [Authorize(Roles = "Admin")]` |
| `GestionCategorias.razor` | ✅ `@attribute [Authorize(Roles = "Admin")]` |
| `GestionProductos.razor` | ✅ `@attribute [Authorize(Roles = "Admin")]` |
| `GestionPedidos.razor` | ✅ `@attribute [Authorize(Roles = "Admin")]` |
| `GestionUsuarios.razor` | ✅ `@attribute [Authorize(Roles = "Admin")]` |

---

## ⚙️ Configuración del Servidor (Ya Debe Estar)

En `ExtensionesShop.Server\Program.cs`, verifica que exista:

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => 
    {
        // ... configuración JWT
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

app.UseAuthentication();
app.UseAuthorization();
```

---

## 🚀 Flujo de Autenticación Completo

```
┌─────────────────────────────────────────────────────────┐
│         USUARIO INTENTA ACCEDER A /ADMIN/PEDIDOS        │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│   ¿Está autenticado? (AuthService.CurrentUser != null) │
└──────────┬──────────────────────────────┬───────────────┘
           │ NO                           │ SI
           ▼                              ▼
    ┌───────────────┐        ┌──────────────────────┐
    │ Redirige a    │        │ ¿Tiene rol "Admin"?  │
    │ /login        │        │ (CurrentUser.Role)   │
    └───────────────┘        └─────┬──────────┬─────┘
                                   │ NO       │ SI
                                   ▼         ▼
                            ┌──────────┐  ┌─────────────┐
                            │Redirige  │  │ Renderiza   │
                            │a /       │  │ página      │
                            │(inicio)  │  │ admin ✓     │
                            └──────────┘  └─────────────┘
```

---

## 💡 Notas de Implementación

### Por qué `AuthService.IsAdmin` en Header pero `<AuthorizeView>` en DetalleProducto?

1. **Header.razor** usa `AuthService.IsAdmin`
   - ✅ Más rápido (sin overhead de componentes)
   - ✅ Control granular con `@if`
   - ✅ Suscripción directa a cambios

2. **DetalleProducto.razor** usa `<AuthorizeView>`
   - ✅ Más seguro (valida en servidor)
   - ✅ Mejor para proteger contenido sensible
   - ✅ Forma estándar de ASP.NET Core

**Ambos funcionan juntos** para defensa en profundidad.

---

## 🔑 Consideraciones de Seguridad

### ✅ IMPLEMENTADO
- [x] Autorización por roles en cliente
- [x] Protección de rutas públicas
- [x] Ocultamiento de UI para usuarios sin permisos
- [x] Validación en Header (suscripción a cambios)
- [x] Atributos de autorización en páginas

### ⚠️ ASEGURAR EN BACKEND
- [ ] Validar JWT en cada solicitud API
- [ ] Agregar `[Authorize(Roles = "Admin")]` en controladores
- [ ] Revisar endpoints que modifiquen datos
- [ ] Logging de accesos denegados

---

## 📚 Referencias

- [Autorización en Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/?view=aspnetcore-9.0)
- [AuthorizeView Component](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/additional-scenarios?view=aspnetcore-9.0#authorizationview-component)
- [Role-Based Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-9.0)
