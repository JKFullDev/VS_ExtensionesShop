using ExtensionesShop.Server.Data;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ExtensionesShop.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class FavoritesController : ControllerBase
{
    private readonly AppDbContext _context;

    public FavoritesController(AppDbContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// GET /api/favorites - Obtener todos los favoritos del usuario
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetFavorites()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Include(f => f.Product)
                .ThenInclude(p => p!.Category)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => f.Product!)
            .ToListAsync();

        return Ok(favorites);
    }

    /// <summary>
    /// GET /api/favorites/ids - Obtener solo los IDs de productos favoritos
    /// </summary>
    [HttpGet("ids")]
    public async Task<ActionResult<List<int>>> GetFavoriteIds()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var ids = await _context.Favorites
            .Where(f => f.UserId == userId)
            .Select(f => f.ProductId)
            .ToListAsync();

        return Ok(ids);
    }

    /// <summary>
    /// GET /api/favorites/check/{productId} - Verificar si un producto es favorito
    /// </summary>
    [HttpGet("check/{productId:int}")]
    public async Task<ActionResult<bool>> IsFavorite(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var exists = await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

        return Ok(exists);
    }

    /// <summary>
    /// POST /api/favorites/{productId} - Añadir producto a favoritos
    /// </summary>
    [HttpPost("{productId:int}")]
    public async Task<ActionResult<OperationResult>> AddFavorite(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        // Verificar que el producto existe
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
        if (!productExists)
            return NotFound(new OperationResult 
            { 
                Success = false, 
                Message = "Producto no encontrado" 
            });

        // Verificar si ya existe
        var alreadyExists = await _context.Favorites
            .AnyAsync(f => f.UserId == userId && f.ProductId == productId);

        if (alreadyExists)
            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = "El producto ya estaba en favoritos" 
            });

        // Añadir favorito
        var favorite = new Favorite
        {
            UserId = userId,
            ProductId = productId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = "Producto añadido a favoritos" 
        });
    }

    /// <summary>
    /// DELETE /api/favorites/{productId} - Quitar producto de favoritos
    /// </summary>
    [HttpDelete("{productId:int}")]
    public async Task<ActionResult<OperationResult>> RemoveFavorite(int productId)
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);

        if (favorite == null)
            return NotFound(new OperationResult 
            { 
                Success = false, 
                Message = "Favorito no encontrado" 
            });

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = "Producto quitado de favoritos" 
        });
    }

    /// <summary>
    /// DELETE /api/favorites - Limpiar todos los favoritos del usuario
    /// </summary>
    [HttpDelete]
    public async Task<ActionResult<OperationResult>> ClearFavorites()
    {
        var userId = GetCurrentUserId();
        if (userId == 0)
            return Unauthorized();

        var favorites = await _context.Favorites
            .Where(f => f.UserId == userId)
            .ToListAsync();

        if (!favorites.Any())
            return Ok(new OperationResult 
            { 
                Success = true, 
                Message = "No hay favoritos para eliminar" 
            });

        _context.Favorites.RemoveRange(favorites);
        await _context.SaveChangesAsync();

        return Ok(new OperationResult 
        { 
            Success = true, 
            Message = $"{favorites.Count} favoritos eliminados" 
        });
    }
}
