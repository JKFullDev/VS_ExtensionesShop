using ExtensionesShop.Server.Data;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Authorization;
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
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        if (subcategoryId.HasValue)
            query = query.Where(p => p.SubcategoryId == subcategoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.Id)
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
            .Include(p => p.Subcategory)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Images)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product is null ? NotFound() : Ok(product);
    }

    // GET api/products/{id}/variants
    [HttpGet("{id:int}/variants")]
    public async Task<ActionResult<IEnumerable<ProductVariant>>> GetProductVariants(int id)
    {
        var variants = await db.ProductVariants
            .Where(v => v.ProductId == id)
            .Include(v => v.Images)
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync();

        return Ok(variants);
    }

    // POST api/products/with-variants
    [Authorize(Roles = "Admin")]
    [HttpPost("with-variants")]
    public async Task<ActionResult<Product>> CreateProductWithVariants(
        [FromBody] CreateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            // 1. Crear o actualizar el producto base
            Product product;
            if (request.Id.HasValue && request.Id.Value > 0)
            {
                product = await db.Products
                    .Include(p => p.Variants)
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == request.Id.Value);

                if (product == null)
                    return NotFound(new { message = "Producto no encontrado" });

                // Actualizar propiedades
                product.Name = request.Name;
                product.Description = request.Description;
                product.Price = request.Price;
                product.ImageUrl = request.ImageUrl;
                product.CategoryId = request.CategoryId;
                product.SubcategoryId = request.SubcategoryId;
                product.Color = request.Color;
                product.Centimeters = request.Centimeters;
                product.StockValue = request.Stock;  // ✅ CRÍTICO: Guardar el stock manual en StockValue

                db.Products.Update(product);
            }
            else
            {
                // Crear nuevo producto
                product = new Product
                {
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    ImageUrl = request.ImageUrl,
                    CategoryId = request.CategoryId,
                    SubcategoryId = request.SubcategoryId,
                    Color = request.Color,
                    Centimeters = request.Centimeters,
                    StockValue = request.Stock  // ✅ CRÍTICO: Guardar el stock manual en StockValue
                };

                db.Products.Add(product);
            }

            await db.SaveChangesAsync();

            // 2. Gestionar variantes
            if (request.Variants != null && request.Variants.Any())
            {
                // Si es actualización, sincronizar variantes inteligentemente
                if (request.Id.HasValue && request.Id.Value > 0)
                {
                    // ✅ NUEVO: Cargar variantes existentes
                    var existingVariants = await db.ProductVariants
                        .Where(v => v.ProductId == product.Id)
                        .ToListAsync();

                    // Identificar variantes 'zombis' (en BD pero no en request)
                    var requestVariantIds = request.Variants
                        .Where(v => v.Id.HasValue && v.Id.Value > 0)
                        .Select(v => v.Id.Value)
                        .ToList();

                    var orphanedVariants = existingVariants
                        .Where(v => !requestVariantIds.Contains(v.Id))
                        .ToList();

                    // ✅ Eliminar variantes huérfanas
                    if (orphanedVariants.Any())
                    {
                        db.ProductVariants.RemoveRange(orphanedVariants);
                        Console.WriteLine($"🗑️ Eliminadas {orphanedVariants.Count} variantes huérfanas");
                    }

                    await db.SaveChangesAsync();
                }

                // Crear nuevas variantes
                var displayOrder = 0;
                foreach (var variantDto in request.Variants)
                {
                    var variant = new ProductVariant
                    {
                        ProductId = product.Id,
                        Color = variantDto.Color,
                        Centimeters = variantDto.Centimeters,
                        Stock = variantDto.Stock,
                        Price = variantDto.Price,
                        IsActive = variantDto.IsActive,
                        DisplayOrder = displayOrder++
                    };

                    db.ProductVariants.Add(variant);
                }

                await db.SaveChangesAsync();
            }
            else
            {
                // ✅ NUEVO: Si NO hay variantes, eliminar todas las existentes y guardar stock manual
                if (request.Id.HasValue && request.Id.Value > 0)
                {
                    var existingVariants = await db.ProductVariants
                        .Where(v => v.ProductId == product.Id)
                        .ToListAsync();

                    if (existingVariants.Any())
                    {
                        db.ProductVariants.RemoveRange(existingVariants);
                        Console.WriteLine($"🗑️ Eliminadas todas las variantes ({existingVariants.Count}) del producto");
                    }

                    await db.SaveChangesAsync();
                }

                // No hay variantes, por lo que el stock es manual (ya está guardado en request.Stock)
                Console.WriteLine($"📦 Producto sin variantes - Stock manual: {request.Stock}");
            }

            // 3. Gestionar imágenes
            if (request.Images != null && request.Images.Any())
            {
                // Si es actualización, eliminar imágenes existentes
                if (request.Id.HasValue && request.Id.Value > 0)
                {
                    var existingImages = await db.ProductImages
                        .Where(i => i.ProductId == product.Id)
                        .ToListAsync();
                    db.ProductImages.RemoveRange(existingImages);
                    await db.SaveChangesAsync();
                }

                // Crear nuevas imágenes
                foreach (var imageDto in request.Images)
                {
                    var image = new ProductImage
                    {
                        ProductId = product.Id,
                        ProductVariantId = imageDto.ProductVariantId,
                        ImageUrl = imageDto.ImageUrl,
                        AltText = imageDto.AltText,
                        DisplayOrder = imageDto.DisplayOrder,
                        IsActive = imageDto.IsActive
                    };

                    db.ProductImages.Add(image);
                }

                await db.SaveChangesAsync();
            }

            // Confirmar transacción
            await transaction.CommitAsync();

            // Retornar el producto completo
            var resultProduct = await db.Products
                .Include(p => p.Category)
                .Include(p => p.Subcategory)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Images)
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == product.Id);

            return CreatedAtAction(nameof(GetById), new { id = product.Id }, resultProduct);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new
            {
                message = "Error al guardar el producto",
                error = ex.Message
            });
        }
    }

    // PUT api/products/{id}
    [Authorize(Roles = "Admin")]
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
        existingProduct.Description = product.Description;
        existingProduct.Price = product.Price;
        existingProduct.ImageUrl = product.ImageUrl;
        existingProduct.CategoryId = product.CategoryId;
        existingProduct.SubcategoryId = product.SubcategoryId;
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
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await db.Products
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFound();

        using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            // 1. Eliminar imágenes de variantes
            var variantImages = await db.ProductImages
                .Where(i => i.ProductVariantId != null && 
                       db.ProductVariants.Where(v => v.ProductId == id).Select(v => v.Id).Contains(i.ProductVariantId.Value))
                .ToListAsync();
            db.ProductImages.RemoveRange(variantImages);
            await db.SaveChangesAsync();

            // 2. Eliminar imágenes del producto
            var productImages = await db.ProductImages
                .Where(i => i.ProductId == id && i.ProductVariantId == null)
                .ToListAsync();
            db.ProductImages.RemoveRange(productImages);
            await db.SaveChangesAsync();

            // 3. Eliminar variantes
            var variants = await db.ProductVariants
                .Where(v => v.ProductId == id)
                .ToListAsync();
            db.ProductVariants.RemoveRange(variants);
            await db.SaveChangesAsync();

            // 4. Eliminar producto
            db.Products.Remove(product);
            await db.SaveChangesAsync();

            // Confirmar transacción
            await transaction.CommitAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "Error al eliminar el producto", error = ex.Message });
        }
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
    public async Task<ActionResult<IEnumerable<Subcategory>>> GetAll()
        => Ok(await db.Subcategories.Include(s => s.Category).OrderBy(s => s.Name).ToListAsync());

    [HttpGet("by-category/{categoryId}")]
    public async Task<ActionResult<IEnumerable<Subcategory>>> GetByCategory(int categoryId)
        => Ok(await db.Subcategories
            .Where(s => s.CategoryId == categoryId)
            .OrderBy(s => s.Name)
            .ToListAsync());

    [HttpGet("{id}")]
    public async Task<ActionResult<Subcategory>> GetById(int id)
    {
        var subcategory = await db.Subcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.Id == id);

        return subcategory is null ? NotFound() : Ok(subcategory);
    }
}
