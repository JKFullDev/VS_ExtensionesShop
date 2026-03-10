using ExtensionesShop.Server.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DiagnosticController : ControllerBase
{
    private readonly AppDbContext _db;

    public DiagnosticController(AppDbContext db)
    {
        _db = db;
    }

    // GET api/diagnostic/test
    [HttpGet("test")]
    public IActionResult Test()
    {
        return Ok(new
        {
            Status = "OK",
            Message = "API funcionando correctamente",
            Timestamp = DateTime.UtcNow
        });
    }

    // GET api/diagnostic/database
    [HttpGet("database")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            // Verificar conexión
            var canConnect = await _db.Database.CanConnectAsync();
            
            if (!canConnect)
            {
                return StatusCode(500, new { Error = "No se puede conectar a la base de datos" });
            }

            // Contar productos
            var productCount = await _db.Products.CountAsync();
            var categoryCount = await _db.Categories.CountAsync();
            var subcategoryCount = await _db.Subcategories.CountAsync();

            return Ok(new
            {
                Status = "Connected",
                Products = productCount,
                Categories = categoryCount,
                Subcategories = subcategoryCount,
                ConnectionString = _db.Database.GetConnectionString()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = "Error al conectar con la base de datos",
                Message = ex.Message,
                InnerException = ex.InnerException?.Message
            });
        }
    }

    // GET api/diagnostic/products-simple
    [HttpGet("products-simple")]
    public async Task<IActionResult> GetProductsSimple()
    {
        try
        {
            var products = await _db.Products
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Stock,
                    p.CategoryId
                })
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                Count = products.Count,
                Products = products
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = "Error al cargar productos",
                Message = ex.Message,
                InnerException = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    }

    // GET api/diagnostic/products-full
    [HttpGet("products-full")]
    public async Task<IActionResult> GetProductsFull()
    {
        try
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Take(10)
                .ToListAsync();

            return Ok(new
            {
                Count = products.Count,
                Products = products
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                Error = "Error al cargar productos con relaciones",
                Message = ex.Message,
                InnerException = ex.InnerException?.Message,
                StackTrace = ex.StackTrace
            });
        }
    }
}
