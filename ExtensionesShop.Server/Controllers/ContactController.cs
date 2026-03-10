using ExtensionesShop.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(IEmailService emailService) : ControllerBase
{
    // POST api/contact
    [HttpPost]
    public async Task<ActionResult> SendContactForm([FromBody] ContactFormRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await emailService.SendContactFormAsync(
                request.Name,
                request.Email,
                request.Phone,
                request.Message
            );

            return Ok(new { message = "Mensaje enviado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al enviar el mensaje", error = ex.Message });
        }
    }
}

public class ContactFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
