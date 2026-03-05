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

    // GET api/products/{id:int}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product is null ? NotFound() : Ok(product);
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

    // POST api/products
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Generar slug si no existe
        if (string.IsNullOrWhiteSpace(product.Slug))
        {
            product.Slug = GenerateSlug(product.Name);
        }

        // Verificar que el slug sea único
        if (await db.Products.AnyAsync(p => p.Slug == product.Slug))
        {
            return BadRequest(new { message = "Ya existe un producto con ese slug" });
        }

        product.CreatedAt = DateTime.UtcNow;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    // PUT api/products/{id}
    [HttpPut("{id}")]
    public async Task<ActionResult<Product>> Update(int id, [FromBody] Product product)
    {
        if (id != product.Id)
            return BadRequest(new { message = "El ID no coincide" });

        var existingProduct = await db.Products.FindAsync(id);
        if (existingProduct == null)
            return NotFound();

        // Actualizar propiedades
        existingProduct.Name = product.Name;
        existingProduct.Slug = product.Slug;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.OriginalPrice = product.OriginalPrice;
        existingProduct.ImageUrl = product.ImageUrl;
        existingProduct.CategoryId = product.CategoryId;
        existingProduct.IsNew = product.IsNew;
        existingProduct.IsFeatured = product.IsFeatured;
        existingProduct.Stock = product.Stock;
        existingProduct.HairType = product.HairType;
        existingProduct.Length = product.Length;
        existingProduct.Weight = product.Weight;
        existingProduct.Color = product.Color;
        existingProduct.ApplicationMethod = product.ApplicationMethod;

        try
        {
            await db.SaveChangesAsync();
            return Ok(existingProduct);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await db.Products.AnyAsync(p => p.Id == id))
                return NotFound();
            throw;
        }
    }

    // DELETE api/products/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products.FindAsync(id);
        if (product == null)
            return NotFound();

        db.Products.Remove(product);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("á", "a").Replace("é", "e").Replace("í", "i")
            .Replace("ó", "o").Replace("ú", "u").Replace("ñ", "n");
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