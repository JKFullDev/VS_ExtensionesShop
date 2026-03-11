# Plan: Backend para Carrito y Favoritos

## 📋 **Tareas a Implementar**

### 1️⃣ **Base de Datos**

#### Tabla `Favorites`
```sql
CREATE TABLE Favorites (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT FK_Favorites_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Favorites_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_Favorites_UserProduct UNIQUE (UserId, ProductId)
);
```

#### Tabla `CartItems`
```sql
CREATE TABLE CartItems (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    CONSTRAINT FK_CartItems_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_CartItems_UserProduct UNIQUE (UserId, ProductId),
    CONSTRAINT CK_CartItems_Quantity CHECK (Quantity > 0)
);
```

---

### 2️⃣ **Backend - Controllers**

#### `FavoritesController.cs`
```csharp
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    // GET /api/favorites - Obtener favoritos del usuario
    // POST /api/favorites/{productId} - Añadir a favoritos
    // DELETE /api/favorites/{productId} - Quitar de favoritos
}
```

#### `CartController.cs`
```csharp
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    // GET /api/cart - Obtener carrito (requiere auth)
    // POST /api/cart - Añadir producto (requiere auth)
    // PUT /api/cart/{productId} - Actualizar cantidad (requiere auth)
    // DELETE /api/cart/{productId} - Eliminar producto (requiere auth)
    // DELETE /api/cart - Vaciar carrito (requiere auth)
    // POST /api/cart/sync - Sincronizar carrito local con BD (al hacer login)
}
```

---

### 3️⃣ **Frontend - Servicios Modificados**

#### `FavoritosService.cs`
```csharp
public class FavoritosService
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;
    
    // Si NO está logueado:
    //   - Redirige a /cuenta o muestra mensaje
    
    // Si SÍ está logueado:
    //   - Usa endpoints del backend
    //   - Cache local para performance
}
```

#### `CartStateService.cs`
```csharp
public class CartStateService
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;
    private readonly IJSRuntime _js;
    
    // Si NO está logueado:
    //   - Usa localStorage (guest cart)
    
    // Si SÍ está logueado:
    //   - Usa endpoints del backend
    //   - Sincroniza con BD
    //   - Mantiene cache en localStorage para offline
    
    // Al hacer LOGIN:
    //   - Fusiona localStorage cart con BD cart
    //   - Limpia localStorage después de sincronizar
    
    // Al hacer LOGOUT:
    //   - Limpia cache
    //   - Vuelve a modo localStorage
}
```

---

### 4️⃣ **Flujos de Usuario**

#### **Favoritos:**
```
Usuario sin login → Hace clic en ❤️ → Redirige a /cuenta con mensaje
Usuario logueado → Hace clic en ❤️ → Guarda en BD → ✅ Confirmación
```

#### **Carrito:**
```
// SIN LOGIN (Guest)
Usuario → Añade producto → localStorage → Continúa comprando
Usuario → Hace login → Carrito local SE SINCRONIZA con BD → ✅

// CON LOGIN
Usuario logueado → Añade producto → Se guarda en BD + cache local → ✅
Usuario → Hace logout → Mantiene carrito local (guest mode) → ⚠️
```

---

## 🎯 **Decisión Requerida**

**¿Qué opción prefieres?**

### A) **Híbrida (Recomendada)** ⭐
- Favoritos solo con login
- Carrito funciona sin login + sincroniza al login
- Mejor UX y conversión
- **Tiempo:** ~4-6 horas de desarrollo

### B) **Solo Backend**
- Todo requiere login obligatorio
- Más simple
- Peor UX
- **Tiempo:** ~3-4 horas de desarrollo

### C) **Dejar como está**
- Solo localStorage
- No implementar backend
- **Tiempo:** 0 horas

---

## 📝 **Siguientes Pasos (si eliges A o B)**

1. Crear migraciones SQL (Favorites + CartItems)
2. Crear Controllers en backend
3. Modificar servicios en frontend
4. Implementar lógica de sincronización
5. Actualizar UI para mostrar estados (guest vs logueado)
6. Testing completo

---

**¿Qué opción prefieres implementar?** 🤔
