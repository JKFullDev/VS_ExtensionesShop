// ABRIR DEVELOPER TOOLS EN EL NAVEGADOR (F12)
// 1. Ve a la pestaña "Console"
// 2. Pega este código y presiona Enter:

fetch('/api/products')
  .then(response => response.json())
  .then(data => {
    console.log('✅ Productos cargados:', data.length);
    console.table(data);
  })
  .catch(error => {
    console.error('❌ Error:', error);
  });

// Si ves productos en la consola → El problema está en Productos.razor
// Si ves un error → El problema está en la API o la BD
