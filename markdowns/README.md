# Extensiones Shop — Blazor WebAssembly Hosted (.NET 9)

Proyecto base de e-commerce para extensiones de cabello. Arquitectura **Blazor WASM Hosted** con tres capas separadas.

---

## 🚀 Requisitos

| Herramienta | Versión mínima |
|---|---|
| Visual Studio Community | 2022 (v17.8+) |
| .NET SDK | 9.0 |
| SQL Server | LocalDB / Express / Developer |
| Workload VS | **ASP.NET and web development** |

---

## ▶️ Primeros pasos en Visual Studio

### 1. Abrir la solución
Doble clic en `ExtensionesShop.sln`

### 2. Restaurar paquetes NuGet
Visual Studio lo hace automáticamente. Si no:
```
Menú → Tools → NuGet Package Manager → Restore NuGet Packages
```

### 3. Configurar la cadena de conexión
Edita `ExtensionesShop.Server/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ExtensionesShopDb;Trusted_Connection=True;"
}
```

> Para SQL Server Express usa: `Server=.\\SQLEXPRESS;Database=ExtensionesShopDb;Trusted_Connection=True;`

### 4. Crear la base de datos (Migrations)

Abre la **Package Manager Console** (`Tools → NuGet → Package Manager Console`) y ejecuta:

```powershell
# Asegúrate de tener seleccionado ExtensionesShop.Server como proyecto por defecto
Add-Migration InitialCreate -Project ExtensionesShop.Server -StartupProject ExtensionesShop.Server
Update-Database -Project ExtensionesShop.Server -StartupProject ExtensionesShop.Server
```

### 5. Establecer proyecto de inicio
Clic derecho sobre `ExtensionesShop.Server` → **Set as Startup Project**

### 6. Ejecutar
Pulsa **F5** o el botón ▶ verde.

La aplicación arrancará en:
- `https://localhost:7001` (HTTPS)
- `http://localhost:5000` (HTTP)
- `https://localhost:7001/swagger` (API Explorer)

---

## 📁 Estructura del proyecto

```
ExtensionesShop.sln
├── ExtensionesShop.Client/        ← Blazor WASM (UI)
│   ├── Pages/
│   │   └── Index.razor            Landing page
│   ├── Shared/
│   │   ├── MainLayout.razor       Layout principal
│   │   ├── Header.razor           Header sticky
│   │   └── Footer.razor           Footer
│   ├── wwwroot/
│   │   ├── css/app.css            Design system completo
│   │   └── index.html             Host page
│   └── Program.cs
│
├── ExtensionesShop.Server/        ← ASP.NET Core (API + Host)
│   ├── Controllers/
│   │   └── ProductsController.cs  GET /api/products
│   ├── Data/
│   │   └── AppDbContext.cs        EF Core + SQL Server
│   └── Program.cs
│
└── ExtensionesShop.Shared/        ← Modelos C# compartidos
    └── Models/
        └── Models.cs              Product, Category, CartItem
```

---

## 🎨 Stack tecnológico

- **Frontend**: Blazor WebAssembly + Bootstrap 5 + CSS Custom Properties
- **Tipografía**: Cormorant Garamond (display) + DM Sans (body)
- **Color corporativo**: Rosa `#E8607A` sobre blanco puro `#FFFFFF`
- **Backend**: ASP.NET Core 9 Web API
- **ORM**: Entity Framework Core 9
- **Base de datos**: SQL Server (LocalDB para desarrollo)
- **Documentación API**: Swagger / OpenAPI

---

## 🔜 Próximos pasos sugeridos

- [ ] Agregar autenticación con ASP.NET Core Identity
- [ ] Implementar servicio de carrito (StateContainer o localStorage)
- [ ] Página `/productos` con filtros por categoría
- [ ] Página de detalle de producto `/productos/{slug}`
- [ ] Integración con pasarela de pago (Stripe / Redsys)
- [ ] Panel de administración con CRUD de productos
- [ ] Subida de imágenes a Azure Blob Storage
