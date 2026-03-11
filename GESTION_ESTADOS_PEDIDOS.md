# 📊 GESTIÓN DE ESTADOS DE PEDIDOS

## 🎯 **Estados Disponibles:**

| Código | Estado | Descripción |
|--------|--------|-------------|
| 0 | **Pendiente** | Pedido recibido, esperando confirmación |
| 1 | **Confirmado** | Pedido confirmado por el cliente |
| 2 | **En Proceso** | Preparando el pedido |
| 3 | **Enviado** | Pedido enviado al cliente |
| 4 | **Entregado** | Pedido entregado exitosamente |
| 5 | **Cancelado** | Pedido cancelado |

---

## ✅ **OPCIÓN 1: Panel de Administración (RECOMENDADO)**

### **Paso 1: Acceder al panel**

Ve a: **`https://localhost:44385/admin/pedidos`**

### **Paso 2: Cambiar estado**

1. Busca el pedido en la tabla
2. Usa el dropdown de "Estado" para seleccionar el nuevo estado
3. ✅ Se guarda automáticamente

### **Características:**

- ✅ **Interfaz visual** fácil de usar
- ✅ **Filtro por estado** para ver solo pendientes, enviados, etc.
- ✅ **Botón "Ver detalle"** para ver información completa
- ✅ **Links clicables** para email y teléfono del cliente
- ✅ **Actualización en tiempo real**

### **Captura de pantalla del panel:**

```
┌─────────────────────────────────────────────────────────────────┐
│ 📦 Gestión de Pedidos                                          │
├─────────────────────────────────────────────────────────────────┤
│ Filtrar por estado: [Todos ▼]                                  │
├────┬──────────┬────────────┬──────────┬────────┬────────┬──────┤
│ ID │ Cliente  │ Email      │ Teléfono │ Total  │ Estado │ Accs │
├────┼──────────┼────────────┼──────────┼────────┼────────┼──────┤
│ #1 │ Juan     │ juan@...   │ 555-1234 │ $89.99 │ [▼]    │ 👁️   │
│ #2 │ María    │ maria@...  │ 555-5678 │ $45.00 │ [▼]    │ 👁️   │
└────┴──────────┴────────────┴──────────┴────────┴────────┴──────┘
```

---

## 🔧 **OPCIÓN 2: Actualización Manual en Base de Datos**

### **SQL Server Management Studio:**

```sql
-- Ver todos los pedidos
SELECT Id, CustomerName, Status, Total, CreatedAt
FROM Orders
ORDER BY CreatedAt DESC;

-- Cambiar estado de un pedido específico
UPDATE Orders
SET Status = 1  -- 1 = Confirmado
WHERE Id = 5;   -- Reemplaza con el ID del pedido

-- Cambiar a "Enviado" y registrar fecha
UPDATE Orders
SET Status = 3,
    ShippedAt = GETDATE()
WHERE Id = 5;

-- Cambiar a "Entregado" y registrar fecha
UPDATE Orders
SET Status = 4,
    DeliveredAt = GETDATE()
WHERE Id = 5;
```

### **Consultas útiles:**

```sql
-- Ver pedidos pendientes
SELECT * FROM Orders WHERE Status = 0;

-- Ver pedidos de hoy
SELECT * FROM Orders 
WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE);

-- Cambiar múltiples pedidos a "Confirmado"
UPDATE Orders
SET Status = 1
WHERE Status = 0 AND CreatedAt > '2024-01-01';
```

---

## 🌐 **OPCIÓN 3: API con Postman/Thunder Client**

### **Endpoint:**

```
PUT https://localhost:44385/api/orders/{id}/status
```

### **Body (JSON):**

```json
{
  "status": 1
}
```

### **Ejemplo en Postman:**

1. **URL:** `PUT https://localhost:44385/api/orders/5/status`
2. **Headers:** `Content-Type: application/json`
3. **Body (raw JSON):**
   ```json
   {
     "status": 3
   }
   ```
4. **Send** ✅

### **Con PowerShell (desde terminal):**

```powershell
# Cambiar pedido #5 a "Confirmado"
$body = @{ status = 1 } | ConvertTo-Json
Invoke-RestMethod -Uri "https://localhost:44385/api/orders/5/status" `
                  -Method PUT `
                  -Body $body `
                  -ContentType "application/json"
```

---

## 📧 **NOTIFICACIONES POR EMAIL (Próxima mejora)**

### **Cuando cambias un estado, podrías enviar email al cliente:**

- **Confirmado (1)** → "Tu pedido ha sido confirmado"
- **En Proceso (2)** → "Estamos preparando tu pedido"
- **Enviado (3)** → "Tu pedido está en camino. Número de seguimiento: XXX"
- **Entregado (4)** → "¡Tu pedido ha sido entregado!"
- **Cancelado (5)** → "Tu pedido ha sido cancelado. Motivo: XXX"

---

## 🎯 **RECOMENDACIÓN:**

### **Para tu caso (Extensiones Shop):**

1. ✅ **Usa el Panel de Admin** (`/admin/pedidos`)
2. ✅ **Workflow típico:**
   - Cliente hace pedido → Estado: **Pendiente (0)**
   - Tú lo confirmas → Cambias a **Confirmado (1)**
   - Cliente paga → Cambias a **En Proceso (2)**
   - Envías el paquete → Cambias a **Enviado (3)**
   - Cliente lo recibe → Cambias a **Entregado (4)**

3. ✅ **Próximas mejoras sugeridas:**
   - Agregar campo "Número de tracking"
   - Enviar emails automáticos en cada cambio de estado
   - Agregar notas internas (solo para ti)
   - Historial de cambios de estado

---

## 🔐 **IMPORTANTE: Seguridad del Panel de Admin**

### **Paso 1: Agregar rol de Admin al usuario**

```sql
-- Crear tabla de roles (si no existe)
CREATE TABLE UserRoles (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId INT NOT NULL,
    Role NVARCHAR(50) NOT NULL,
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Hacer admin al usuario ID 1
INSERT INTO UserRoles (UserId, Role)
VALUES (1, 'Admin');
```

### **Paso 2: Proteger la ruta en el código**

En `GestionPedidos.razor`, descomenta:

```csharp
protected override async Task OnInitializedAsync()
{
    // Verificar que sea admin
    if (!AuthService.IsAdmin)
    {
        Navigation.NavigateTo("/");
        return;
    }
    
    await CargarPedidos();
}
```

---

## ✅ **PARA EMPEZAR AHORA:**

1. **Ve a:** `https://localhost:44385/admin/pedidos`
2. **Verás todos los pedidos** en una tabla
3. **Cambia el estado** con el dropdown
4. **¡Listo!** Se guarda automáticamente

**¿Necesitas agregar seguridad al panel? Avísame y te ayudo a implementar el sistema de roles de admin.** 🔒
