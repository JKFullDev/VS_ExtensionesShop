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
        [FromQuery] int? categoryId,
        [FromQuery] int? subcategoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24)
    {
        var query = db.Products
            .Include(p => p.Category)
            .Include(p => p.Subcategory)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (subcategoryId.HasValue)
            query = query.Where(p => p.SubcategoryId == subcategoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Id)
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
    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<Product>> GetBySlug(string slug)
    {
        return NotFound(new { message = "Slug endpoint removed. Use ID instead." });
    }

    // POST api/products
    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

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

        existingProduct.Name = product.Name;
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.ImageUrl = product.ImageUrl;
        existingProduct.CategoryId = product.CategoryId;
        existingProduct.SubcategoryId = product.SubcategoryId;
        existingProduct.Stock = product.Stock;
        existingProduct.Color = product.Color;
        existingProduct.Centimeters = product.Centimeters;

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
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll()
        => Ok(await db.Categories.Include(c => c.Subcategories).ToListAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Category>> GetById(int id)
    {
        var category = await db.Categories
            .Include(c => c.Subcategories)
            .FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? NotFound() : Ok(category);
    }
}

[ApiController]
[Route("api/[controller]")]
public class SubcategoriesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Subcategory>>> GetAll([FromQuery] int? categoryId)
    {
        var query = db.Subcategories.Include(s => s.Category).AsQueryable();
        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);
        return Ok(await query.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Subcategory>> GetById(int id)
    {
        var subcategory = await db.Subcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id);
        return subcategory is null ? NotFound() : Ok(subcategory);
    }
}