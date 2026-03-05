using ExtensionesShop.Server.Data;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // GET api/products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] bool? featured,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24)
    {
        var query = db.Products
            .Include(p => p.Category)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category!.Slug == category);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

        if (featured.HasValue)
            query = query.Where(p => p.IsFeatured == featured.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        Response.Headers.Append("X-Total-Count", total.ToString());
        return Ok(items);
    }

    // GET api/products/{slug}
    [HttpGet("{slug}")]
    public async Task<ActionResult<Product>> GetBySlug(string slug)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug);

        return product is null ? NotFound() : Ok(product);
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll()
        => Ok(await db.Categories.OrderBy(c => c.SortOrder).ToListAsync());
}
