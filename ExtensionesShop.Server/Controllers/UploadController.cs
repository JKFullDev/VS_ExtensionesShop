using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UploadController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<object>> UploadImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        // Validar tipo de archivo
        var allowedMimes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (!allowedMimes.Contains(file.ContentType?.ToLower() ?? ""))
            return BadRequest(new { message = "Solo se permiten formatos: JPG, PNG, WebP, GIF" });

        // Validar tamaño (máximo 10MB)
        const long maxSize = 10 * 1024 * 1024;
        if (file.Length > maxSize)
            return BadRequest(new { message = $"La imagen debe ser menor a 10MB (actual: {(file.Length / (1024 * 1024)):F2}MB)" });

        try
        {
            // Generar nombre único
            var extension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Ruta de almacenamiento
            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

            // Crear directorio si no existe
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var filePath = Path.Combine(uploadsPath, uniqueFileName);

            // Guardar archivo físicamente
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Retornar URL relativa
            var imageUrl = $"/images/products/{uniqueFileName}";
            return Ok(new { success = true, imageUrl = imageUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al cargar la imagen", error = ex.Message });
        }
    }
}
