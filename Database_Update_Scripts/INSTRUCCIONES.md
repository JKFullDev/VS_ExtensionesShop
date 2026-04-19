# 📋 Guía de Ejecución del Script de Base de Datos

## 🎯 Objetivo
Actualizar la base de datos para agregar el campo `Province` (Provincia) a las tablas `Users` y `Orders`.

---

## 📊 Cambios que se Realizarán

### Tabla: `Users`
- **Campo a agregar**: `Province` (nvarchar(100), nullable)
- **Descripción**: Almacena la provincia del usuario

### Tabla: `Orders`
- **Campo a agregar**: `Province` (nvarchar(100), nullable, default: '')
- **Descripción**: Almacena la provincia de envío del pedido

---

## 🔧 Métodos de Ejecución

### Opción 1: SQL Server Management Studio (SSMS) - RECOMENDADO ✅

1. **Abre SQL Server Management Studio**
   - Inicio → Busca "SQL Server Management Studio"
   - O abre desde tu instalación de SQL Server

2. **Conecta a tu servidor**
   - Server name: `localhost` (o tu servidor SQL)
   - Authentication: Windows Authentication (o SQL Server Authentication)
   - Haz clic en "Connect"

3. **Abre el archivo del script**
   - File → Open → File...
   - Navega a: `C:\Users\jcah0\OneDrive\Desktop\VS_ExtensionesShop\Database_Update_Scripts\AddProvinceFeld.sql`
   - Haz clic en "Open"

4. **Selecciona la base de datos correcta**
   - En el dropdown "Available Databases" (arriba del editor)
   - Busca tu base de datos (ej: `ExtensionesShop`, `ExtensionesShopDb`, etc.)

5. **Ejecuta el script**
   - Presiona `F5` o haz clic en "Execute" (▶)
   - O selecciona el texto y presiona `Ctrl+E`

6. **Verifica los resultados**
   - Deberías ver mensajes de confirmación indicando que se agregaron los campos
   - Los mensajes aparecerán en la pestaña "Messages"

---

### Opción 2: Visual Studio - SQL Server Object Explorer

1. **En Visual Studio**
   - Abre "SQL Server Object Explorer" (View → SQL Server Object Explorer)
   - O presiona `Ctrl+\` + `Ctrl+S`

2. **Expande tu conexión**
   - Localiza tu servidor SQL en el árbol
   - Expande la base de datos correcta

3. **Abre una nueva consulta**
   - Haz clic derecho en la base de datos
   - Selecciona "New Query..."

4. **Copia y pega el script**
   - Abre el archivo: `AddProvinceFeld.sql`
   - Copia todo el contenido
   - Pégalo en la ventana de consulta

5. **Ejecuta**
   - Presiona `Ctrl+Shift+E` o haz clic en "Execute Query"

---

### Opción 3: PowerShell / Command Line

```powershell
# Navega a la carpeta del proyecto
cd "C:\Users\jcah0\OneDrive\Desktop\VS_ExtensionesShop"

# Ejecuta el script usando sqlcmd
sqlcmd -S localhost -d ExtensionesShop -i "Database_Update_Scripts\AddProvinceFeld.sql"

# Si usas autenticación de Windows
sqlcmd -S localhost -d ExtensionesShop -E -i "Database_Update_Scripts\AddProvinceFeld.sql"

# Si usas SQL Server Authentication (proporciona usuario y contraseña)
sqlcmd -S localhost -U sa -P "tu_password" -d ExtensionesShop -i "Database_Update_Scripts\AddProvinceFeld.sql"
```

---

### Opción 4: Azure Data Studio

1. **Abre Azure Data Studio**
   - O descárgalo desde: https://learn.microsoft.com/en-us/sql/azure-data-studio/download

2. **Conéctate a tu servidor SQL**
   - Haz clic en "Add Connection"
   - Completa los datos de tu servidor

3. **Abre el script**
   - File → Open File...
   - Selecciona `AddProvinceFeld.sql`

4. **Ejecuta**
   - Presiona `Ctrl+Shift+E` o haz clic en el botón "Run"

---

## ⚙️ Si Necesitas Especificar el Servidor

Si tu base de datos no es la predeterminada, **antes de ejecutar el script**, ejecuta este comando:

```sql
-- Ver la base de datos actual
SELECT DB_NAME() AS [Base de Datos Actual];

-- Cambiar a la base de datos correcta (reemplaza "ExtensionesShop" con el nombre real)
USE ExtensionesShop;
GO
```

---

## 🔍 Verificación Post-Ejecución

Después de ejecutar el script, verifica que los cambios se aplicaron correctamente:

```sql
-- Ver estructura de la tabla Users
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;

-- Ver estructura de la tabla Orders
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders'
ORDER BY ORDINAL_POSITION;

-- Verificar que el campo Province existe
SELECT COUNT(*) as [Province en Users]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'Province';

SELECT COUNT(*) as [Province en Orders]
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders' AND COLUMN_NAME = 'Province';
```

**Resultado esperado**: Ambas consultas deberán retornar `1`.

---

## ✅ Checklist Final

- [ ] He ubicado el archivo `AddProvinceFeld.sql`
- [ ] He seleccionado la base de datos correcta
- [ ] He ejecutado el script exitosamente
- [ ] He verificado que no hay errores en los mensajes
- [ ] He comprobado que los campos existen con la consulta de verificación
- [ ] Voy a compilar la aplicación .NET (Build)
- [ ] Voy a probar la funcionalidad de Checkout

---

## 🚨 Si Algo Sale Mal

### Error: "Database 'X' does not exist"
- Verifica el nombre exacto de tu base de datos
- En SSMS, expande "Databases" para ver todas disponibles

### Error: "Column 'Province' already exists"
- El campo ya fue agregado anteriormente
- El script está diseñado para evitar este error (verifica primero)
- No necesitas ejecutar de nuevo

### Error: "Incorrect syntax"
- Asegúrate de estar ejecutando en la base de datos correcta
- Copia y pega el script íntegro sin modificaciones

### Error de Autenticación
- Verifica que tu usuario SQL tiene permisos para ALTER TABLE
- Usa un usuario con rol `db_ddladmin` o `dbo`

---

## 📝 Cambios en el Código (Ya Realizados)

El equipo de desarrollo ya ha actualizado:

✅ **Modelos C#**
- `User` → Agregado campo `Province`
- `Order` → Agregado campo `Province`

✅ **Servicios de Autenticación**
- `UserData` → Agregado campo `Province`
- `RegisterData` → Agregado campo `Province`
- `UpdateProfileData` → Agregado campo `Province`

✅ **Páginas Blazor**
- `Checkout.razor` → Carga automática de `Province` desde perfil
- `PedidoConfirmado.razor` → Rediseño compacto

✅ **Controladores**
- `OrdersController` → Guarda `Province` en pedidos

✅ **Emails**
- `EmailService` → Formato de moneda actualizado a `25€`

---

## 🎉 ¡Listo!

Una vez ejecutado el script, tu base de datos estará lista para usar la nueva funcionalidad de Provincia.

**Próximos pasos:**
1. ✅ Ejecuta este script SQL
2. Compila la solución en Visual Studio (Rebuild)
3. Prueba el flujo de Checkout
4. Verifica que la provincia se carga correctamente en el perfil

---

**Preguntas o problemas?** Revisa los logs en Visual Studio o en el Output window de SSMS.

