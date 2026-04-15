using ExtensionesShop.Server.Data;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExtensionesShop.Server.Controllers;

// ⚠️ NO usar [Authorize] aquí - algunos métodos son para guests
[ApiController]
[Route("api/[controller]")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _context;

    public CartController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private bool IsAuthenticated => User.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// GET /api/cart - Obtener carrito del usuario
    /// </summary>
    [Authorize] // ✅ Este sí requiere login
    [HttpGet]
    public async Task<ActionResult<List<CartItemResponse>>> GetCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
                .ThenInclude(p => p!.Category)
            .Include(c => c.Product)
                .ThenInclude(p => p!.Variants)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        // Convertir a DTO CartItemResponse (frontend)
        var result = new List<CartItemResponse>();

        foreach (var c in cartItems)
        {
            var cartItem = new CartItemResponse
            {
                ProductId = c.ProductId,
                ProductVariantId = c.ProductVariantId,  // ✅ INCLUIR VARIANTE ID
                ProductName = c.Product!.Name,
                ImageUrl = c.Product!.ImageUrl,
                Quantity = c.Quantity,
                UnitPrice = c.UnitPrice,  // ✅ USAR PRECIO GUARDADO (variante o base)
                VariantColor = c.VariantColor,  // ✅ USAR COLOR GUARDADO
                VariantCentimeters = c.VariantCentimeters  // ✅ USAR MEDIDA GUARDADA
            };

            result.Add(cartItem);
        }

        return Ok(result);
    }

    /// <summary>
    /// POST /api/cart - Añadir producto al carrito
    /// </summary>
    [Authorize] // ✅ Requiere login
    [HttpPost]
    public async Task<ActionResult<OperationResult>> AddToCart([FromBody] AddToCartRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        // Validar
        if (request.Quantity <= 0)
            return BadRequest(new OperationResult 
            { 
                Success = false, 
                Message = "La cantidad debe ser mayor a 0" 
            });

        // Verificar que el producto existe y tiene stock
        var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == request.ProductId);
        if (product == null)
            return NotFound(new OperationResult 
            { 
                Success = false, 
                Message = "Producto no encontrado" 
            });

        // 🔑 Validar stock según si hay variante seleccionada
        int availableStock = product.Stock;
        decimal unitPrice = product.Price;  // ✅ NUEVO: Inicializar con precio del producto
        string? variantColor = null;
        decimal? variantCentimeters = null;

        if (request.ProductVariantId.HasValue)
        {
            // Validar stock de la variante específica
            var variant = product.Variants.FirstOrDefault(v => v.Id == request.ProductVariantId.Value);
            if (variant == null)
                return NotFound(new OperationResult 
                { 
                    Success = false, 
                    Message = "Variante no encontrada" 
                });

            availableStock = variant.Stock;
            unitPrice = variant.Price;  // ✅ USAR PRECIO DE LA VARIANTE
            variantColor = variant.Color;
            variantCentimeters = variant.Centimeters;
        }

        if (availableStock < request.Quantity)
            return BadRequest(new OperationResult 
            { 
                Success = false, 
                Message = $"Stock insuficiente. Solo hay {availableStock} unidades disponibles" 
            });

        // Buscar si ya existe en el carrito (con la misma variante si es el caso)
        var existing = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && 
                                      c.ProductId == request.ProductId &&
                                      c.ProductVariantId == request.ProductVariantId);

        if (existing != null)
        {
            // Actualizar cantidad
            var newQuantity = existing.Quantity + request.Quantity;

            if (newQuantity > availableStock)
                return BadRequest(new OperationResult 
                { 
                    Success = false, 
                    Message = $"No puedes añadir más. Stock máximo: {availableStock}" 
                });

            existing.Quantity = newQuantity;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Crear nuevo item
            var cartItem = new CartItemEntity
            {
                UserId = userId,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                UnitPrice = unitPrice,  // ✅ GUARDAR PRECIO (variante o base)
                VariantColor = variantColor,  // ✅ GUARDAR COLOR DE VARIANTE
                VariantCentimeters = variantCentimeters,  // ✅ GUARDAR MEDIDA DE VARIANTE
                Quantity = request.Quantity,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CartItems.Add(cartItem);
        }

        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = "Producto añadido al carrito" 
        });
    }

    /// <summary>
    /// PUT /api/cart/{productId} - Actualizar cantidad de un producto
    /// </summary>
    [Authorize] // ✅ Requiere login
    [HttpPut("{productId:int}")]
    public async Task<ActionResult<OperationResult>> UpdateQuantity(int productId, [FromBody] AddToCartRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        // ✅ CRÍTICO: Incluir tanto Product como ProductVariants para validación correcta
        var cartItem = await _context.CartItems
            .Include(c => c.Product)
                .ThenInclude(p => p!.Variants)  // ✅ NUEVO: Incluir variantes del producto
            .FirstOrDefaultAsync(c => c.UserId == userId && 
                                      c.ProductId == productId &&
                                      c.ProductVariantId == request.ProductVariantId);

        if (cartItem == null)
            return NotFound(new OperationResult 
            { 
                Success = false, 
                Message = "Producto no encontrado en el carrito" 
            });

        if (request.Quantity <= 0)
        {
            // Si la cantidad es 0 o negativa, eliminar del carrito
            _context.CartItems.Remove(cartItem);
            Console.WriteLine($"🗑️ Producto {productId} (Variante: {request.ProductVariantId}) eliminado del carrito");
        }
        else
        {
            // ✅ CRÍTICO: Validación de stock inteligente
            int availableStock = 0;
            string stockSource = "";

            if (request.ProductVariantId.HasValue && cartItem.Product?.Variants != null)
            {
                // Si es una variante, validar stock de la variante
                var variant = cartItem.Product.Variants.FirstOrDefault(v => v.Id == request.ProductVariantId.Value);
                if (variant != null)
                {
                    availableStock = variant.Stock;
                    stockSource = $"variante {request.ProductVariantId}";
                }
                else
                {
                    return BadRequest(new OperationResult 
                    { 
                        Success = false, 
                        Message = "La variante no existe" 
                    });
                }
            }
            else
            {
                // Si es un producto simple, validar stock del producto
                availableStock = cartItem.Product!.StockValue;  // ✅ Usar StockValue para productos sin variantes
                stockSource = "producto base";
            }

            // Validar que hay suficiente stock
            if (request.Quantity > availableStock)
            {
                Console.WriteLine($"❌ Stock insuficiente: Solicitado {request.Quantity}, disponible {availableStock} de {stockSource}");
                return BadRequest(new OperationResult 
                { 
                    Success = false, 
                    Message = $"❌ No hay suficiente stock. Solo hay {availableStock} unidades disponibles de esta {stockSource}" 
                });
            }

            cartItem.Quantity = request.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

            Console.WriteLine($"✅ Cantidad actualizada: Producto {productId} (Variante: {request.ProductVariantId}) → {request.Quantity} unidades");
        }

        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = "Cantidad actualizada" 
        });
    }

    /// <summary>
    /// DELETE /api/cart/{productId} - Eliminar producto del carrito
    /// </summary>
    [Authorize] // ✅ Requiere login
    [HttpDelete("{productId:int}")]
    public async Task<ActionResult<OperationResult>> RemoveFromCart(int productId, [FromQuery] int? variantId = null)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        // ✅ CRÍTICO: Buscar por ProductId Y ProductVariantId para eliminar la línea correcta
        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(c => c.UserId == userId && 
                                      c.ProductId == productId &&
                                      c.ProductVariantId == variantId);


        if (cartItem == null)
            return NotFound(new OperationResult 
            { 
                Success = false, 
                Message = "Producto no encontrado en el carrito" 
            });

        _context.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = "Producto eliminado del carrito" 
        });
    }

    /// <summary>
    /// DELETE /api/cart - Vaciar carrito completo
    /// </summary>
    [Authorize] // ✅ Requiere login
    [HttpDelete]
    public async Task<ActionResult<OperationResult>> ClearCart()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var cartItems = await _context.CartItems
            .Where(c => c.UserId == userId)
            .ToListAsync();

        if (!cartItems.Any())
            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = "El carrito ya estaba vacío" 
            });

        _context.CartItems.RemoveRange(cartItems);
        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = $"{cartItems.Count} productos eliminados del carrito" 
        });
    }

    /// <summary>
    /// POST /api/cart/sync - Sincronizar carrito local con BD (al hacer login)
    /// </summary>
    [Authorize] // ✅ Requiere login (solo se llama después de login)
    [HttpPost("sync")]
    public async Task<ActionResult<OperationResult>> SyncCart([FromBody] SyncCartRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        try
        {
            // Obtener carrito actual de BD
            var existingCart = await _context.CartItems
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var merged = 0;
            var added = 0;

            foreach (var item in request.Items)
            {
                // Verificar que el producto existe
                var product = await _context.Products.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == item.ProductId);
                if (product == null || product.Stock < item.Quantity)
                    continue;

                var existing = existingCart.FirstOrDefault(c => c.ProductId == item.ProductId);

                if (existing != null)
                {
                    // Fusionar cantidades (tomar la mayor)
                    var newQuantity = Math.Max(existing.Quantity, item.Quantity);
                    
                    if (newQuantity <= product.Stock)
                    {
                        existing.Quantity = newQuantity;
                        existing.UpdatedAt = DateTime.UtcNow;
                        merged++;
                    }
                }
                else
                {
                    // Añadir nuevo item
                    var cartItem = new CartItemEntity
                    {
                        UserId = userId,
                        ProductId = item.ProductId,
                        Quantity = Math.Min(item.Quantity, product.Stock),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                    added++;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = $"Carrito sincronizado: {added} productos añadidos, {merged} fusionados" 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new OperationResult 
            { 
                Success = false, 
                Message = $"Error al sincronizar: {ex.Message}" 
            });
        }
    }
}
