using ExtensionesShop.Server.Services;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace ExtensionesShop.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController(IEmailService emailService, ILogger<ContactController> logger) : ControllerBase
{
    /// <summary>
    /// POST /api/contact - Recibir mensaje del formulario de contacto y enviar email al Owner
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendContactMessage([FromBody] ContactFormModel contactForm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Generar el HTML del email
            var htmlBody = emailService.GenerateContactEmailHtml(contactForm);

            // Obtener el email del owner
            var ownerEmail = emailService.GetOwnerEmail();

            // Enviar el email al owner
            var result = await emailService.SendEmailAsync(
                ownerEmail,
                $"📬 Nuevo Mensaje de Contacto - {contactForm.Asunto}",
                htmlBody
            );

            if (!result)
                return StatusCode(500, new { message = "Error al enviar el email. Por favor, intenta de nuevo más tarde." });

            logger.LogInformation("Mensaje de contacto recibido de {Email} ({Nombre}) - Asunto: {Asunto}", 
                contactForm.Email, contactForm.Nombre, contactForm.Asunto);

            return Ok(new { message = "Mensaje enviado correctamente. Te responderemos pronto." });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al procesar mensaje de contacto de {Email}", contactForm.Email);
            return StatusCode(500, new { message = "Error al procesar tu solicitud. Por favor, intenta de nuevo." });
        }
    }
}
