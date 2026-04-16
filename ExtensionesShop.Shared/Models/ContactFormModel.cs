using System.ComponentModel.DataAnnotations;

namespace ExtensionesShop.Shared.Models;

/// <summary>
/// Modelo para el formulario de contacto.
/// </summary>
public class ContactFormModel
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MinLength(3, ErrorMessage = "Mínimo 3 caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Email no válido")]
    public string Email { get; set; } = string.Empty;

    public string? Telefono { get; set; }

    [Required(ErrorMessage = "Selecciona un asunto")]
    public string Asunto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El mensaje es obligatorio")]
    [MinLength(10, ErrorMessage = "Mínimo 10 caracteres")]
    public string Mensaje { get; set; } = string.Empty;
}
