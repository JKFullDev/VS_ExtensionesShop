# 🚨 DIAGNÓSTICO Y SOLUCIÓN - PROBLEMAS DEL PANEL ADMIN

## ❌ **PROBLEMAS DETECTADOS:**

### **1. SEGURIDAD CRÍTICA: Acceso sin login**
- ✅ **SOLUCIONADO** - AdminRoute mejorado con logs

### **2. DISEÑO ROTO: Todo apilado**
- ⚠️ **POSIBLE CAUSA:** CSS de AdminLayout en conflicto
- ✅ **VERIFICADO:** Estructura correcta (sidebar fixed + margin-left)

### **3. NO APARECEN DATOS: Dashboard vacío**
- ❌ **CAUSA:** Endpoints de API no devuelven datos
- ❌ **CAUSA:** Usuario no tiene rol "Admin" correcto
- ❌ **CAUSA:** Script SQL no ejecutado

---

## 🔧 **SOLUCIONES PASO A PASO:**

### **PASO 1: Verificar que ejecutaste el script SQL** ✅

Abre SQL Server Management Studio y ejecuta:

```sql
-- Ver tu usuario actual
SELECT Id, Email, FirstName, LastName, Role
FROM Users
WHERE Email = 'juan.carlos.alonso.hernando@students.thepower.education';

-- Si Role es NULL o 'User', ejecuta:
UPDATE Users
SET Role = 'Admin'
WHERE Email = 'juan.carlos.alonso.hernando@students.thepower.education';

-- Verificar
SELECT * FROM Users WHERE Role = 'Admin';
```

---

### **PASO 2: Verificar que tienes datos en la BD** ✅

```sql
-- Ver pedidos
SELECT COUNT(*) AS TotalPedidos FROM Orders;
SELECT TOP 5 * FROM Orders ORDER BY CreatedAt DESC;

-- Ver productos
SELECT COUNT(*) AS TotalProductos FROM Products;
SELECT TOP 5 * FROM Products;

-- Ver usuarios
SELECT COUNT(*) AS TotalUsuarios FROM Users;
SELECT * FROM Users;
```

**Si alguno está vacío:**
- Crea productos desde `/admin/productos`
- Crea categorías desde `/admin/categorias`
- Los pedidos se crean automáticamente al comprar

---

### **PASO 3: Limpiar caché y reiniciar** ✅

1. **Cierra Visual Studio**
2. **Elimina carpetas:**
   ```
   ExtensionesShop.Client\bin
   ExtensionesShop.Client\obj
   ExtensionesShop.Server\bin
   ExtensionesShop.Server\obj
   ```
3. **Abre Visual Studio**
4. **Build → Rebuild Solution**
5. **Ejecuta (F5)**

---

### **PASO 4: Verificar en consola del navegador** ✅

1. **Abre Chrome DevTools** (F12)
2. **Ve a Console**
3. **Busca estos mensajes:**

```
🔐 AdminRoute - Verificando acceso...
   IsAuthenticated: true
   IsAdmin: true
   CurrentUser: tu_email@dominio.com
   Role: Admin
✅ Acceso autorizado
```

**Si ves:**
```
❌ No es admin - Role actual: User
```
→ **SOLUCIÓN:** Ejecuta el UPDATE de SQL del Paso 1

**Si ves:**
```
❌ No está autenticado
```
→ **SOLUCIÓN:** Cierra sesión y vuelve a iniciar sesión

---

### **PASO 5: Verificar endpoints de API** ✅

Abre la consola del navegador (F12 → Network) y ve a `/admin`. Deberías ver estas llamadas:

1. `GET https://localhost:44385/api/orders` → ✅ 200 OK
2. `GET https://localhost:44385/api/products` → ✅ 200 OK
3. `GET https://localhost:44385/api/users` → ✅ 200 OK

**Si ves 401 Unauthorized:**
```json
{
  "message": "Unauthorized"
}
```

→ **PROBLEMA:** Falta JWT token en las requests
→ **SOLUCIÓN:** Verifica que `AuthorizationMessageHandler` está configurado

---

## 🐛 **DEBUGGING AVANZADO:**

### **Test 1: Verificar AuthService**

Abre Console del navegador y ejecuta:
```javascript
localStorage.getItem('currentUser')
```

Deberías ver:
```json
{
  "id": 5,
  "email": "tu_email@dominio.com",
  "firstName": "Juan",
  "lastName": "Carlos",
  "role": "Admin"  ← ¡IMPORTANTE!
}
```

**Si `role` es `"User"` o no existe:**
```javascript
// Eliminar y volver a iniciar sesión
localStorage.clear();
// Recarga la página y loguéate de nuevo
```

---

### **Test 2: Verificar que los endpoints funcionan**

Abre una nueva pestaña y ve directamente a:
```
https://localhost:44385/api/products
https://localhost:44385/api/orders
https://localhost:44385/api/users
```

**Deberías ver JSON con datos.**

**Si ves error 404:**
→ Los controladores no están registrados
→ Verifica que están en `ExtensionesShop.Server/Controllers/`

---

## 📝 **CHECKLIST DE VERIFICACIÓN:**

- [ ] ✅ Script SQL ejecutado (`ImplementarSistemaRoles.sql`)
- [ ] ✅ Usuario tiene `Role = 'Admin'` en BD
- [ ] ✅ Cerrar sesión y volver a iniciar sesión
- [ ] ✅ LocalStorage tiene `role: "Admin"`
- [ ] ✅ Hay datos en las tablas (Orders, Products, Users)
- [ ] ✅ Endpoints de API responden (200 OK)
- [ ] ✅ Caché limpiado (bin/obj eliminados)
- [ ] ✅ Aplicación recompilada (Rebuild)
- [ ] ✅ AuthorizationMessageHandler configurado

---

## 🎯 **SOLUCIÓN RÁPIDA (Si todo falla):**

### **Opción A: Reset completo**

```sql
-- 1. Asegurar que eres admin
UPDATE Users SET Role = 'Admin' WHERE Id = 1;

-- 2. Verificar datos de prueba
SELECT COUNT(*) FROM Orders;
SELECT COUNT(*) FROM Products;
SELECT COUNT(*) FROM Users;
```

```bash
# 3. Limpiar proyecto
rd /s /q ExtensionesShop.Client\bin
rd /s /q ExtensionesShop.Client\obj
rd /s /q ExtensionesShop.Server\bin
rd /s /q ExtensionesShop.Server\obj

# 4. Rebuild
dotnet build
```

```javascript
// 5. Limpiar navegador
localStorage.clear();
sessionStorage.clear();
// Ctrl + Shift + Delete → Clear cache
```

### **Opción B: Crear datos de prueba**

Si no tienes datos, crea algunos manualmente:

```sql
-- Crear categoría
INSERT INTO Categories (Name) VALUES ('Extensiones');

-- Crear producto
INSERT INTO Products (Name, Description, Price, Stock, CategoryId, ImageUrl)
VALUES ('Extensiones Remy 60cm', 'Extensiones de cabello natural', 89.99, 15, 1, 'https://via.placeholder.com/300');

-- Verificar
SELECT * FROM Products;
```

---

## 🚀 **DESPUÉS DE APLICAR LAS SOLUCIONES:**

1. **Cierra todas las pestañas** del navegador
2. **Detén la aplicación** (Shift + F5)
3. **Rebuild** (Ctrl + Shift + B)
4. **Ejecuta** (F5)
5. **Ve a `/cuenta`** y loguéate
6. **Ve a `/admin`**
7. **Deberías ver:**
   - ✅ Sidebar a la izquierda
   - ✅ Contenido centrado
   - ✅ Estadísticas con números
   - ✅ Tabla de pedidos con datos
   - ✅ Productos con stock bajo

---

## 📸 **CÓMO DEBE VERSE:**

```
┌─────────────────┬────────────────────────────────────────┐
│ 👑 Admin Panel  │  📦 Dashboard                         │
│                 │                                        │
│ 🏠 Dashboard    │  ┌────────┬────────┬────────┬────────┐│
│ 📦 Pedidos      │  │ 📦 25  │ 💰 $2k │ 🛍️ 68  │ 👥 42  ││
│ 🛍️ Productos    │  └────────┴────────┴────────┴────────┘│
│ 📁 Categorías   │                                        │
│ 👥 Usuarios     │  Pedidos Recientes                     │
│                 │  ┌────┬──────┬──────┬────────┬──────┐ │
│ ─────────────── │  │ #5 │ Juan │ $89  │ Pend.  │ 10/1 │ │
│ 🌐 Ver Tienda   │  └────┴──────┴──────┴────────┴──────┘ │
│                 │                                        │
│ 👤 Juan         │                                        │
│ Administrador   │                                        │
└─────────────────┴────────────────────────────────────────┘
```

---

## ❓ **SI AÚN NO FUNCIONA:**

Dime qué ves en:

1. **Consola del navegador** (F12 → Console)
2. **Network tab** (F12 → Network)
3. **El resultado de:**
   ```sql
   SELECT Email, Role FROM Users;
   ```

Y te daré la solución específica. 🚀
