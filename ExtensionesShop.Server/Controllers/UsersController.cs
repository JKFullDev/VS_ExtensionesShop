using ExtensionesShop.Server.Data;
using ExtensionesShop.Server.Services;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;

namespace ExtensionesShop.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UsersController(AppDbContext context, IEmailService emailService, IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
    }

    // POST: api/users/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validar el modelo
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(new { message = "Datos inválidos", errors });
        }

        try
        {
            // Validar que el email no exista
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                return BadRequest(new { message = "Este email ya está registrado" });
            }

            // Crear el nuevo usuario con BCrypt
            var user = new User
            {
                Email = request.Email.ToLower(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = request.Phone?.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cuenta creada exitosamente", userId = user.Id });
        }
        catch (DbUpdateException ex)
        {
            // Log the error (considera usar ILogger en producción)
            Console.WriteLine($"Error al guardar usuario: {ex.InnerException?.Message ?? ex.Message}");
            return BadRequest(new { message = "Error al crear la cuenta. Verifica que todos los campos sean válidos." });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inesperado: {ex.Message}");
            return StatusCode(500, new { message = "Error interno del servidor. Por favor, inténtalo más tarde." });
        }
    }

    // POST: api/users/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            return Unauthorized(new { message = "Email o contraseña incorrectos" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Email o contraseña incorrectos" });
        }

        return Ok(new
        {
            message = "Login exitoso",
            user = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Phone,
                user.Address,
                user.City,
                user.PostalCode
            }
        });
    }

    // POST: api/users/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            // Por seguridad, no revelamos si el email existe o no
            return Ok(new { message = "Si el email existe, recibirás instrucciones para recuperar tu contraseña" });
        }

        // Generar token único
        var token = Guid.NewGuid().ToString("N");
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1); // Expira en 1 hora

        await _context.SaveChangesAsync();

        // Obtener la URL del cliente desde la configuración
        var clientUrl = _configuration["ClientUrl"] ?? "https://localhost:59871";
        var resetLink = $"{clientUrl}/restablecer-password?token={token}";

        var emailBody = $@"
            <h2>Recuperación de Contraseña</h2>
            <p>Hola {user.FirstName},</p>
            <p>Has solicitado restablecer tu contraseña. Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
            <p><a href=""{resetLink}"" style=""background: #E8607A; color: white; padding: 12px 24px; text-decoration: none; border-radius: 50px; display: inline-block;"">Restablecer Contraseña</a></p>
            <p>Este enlace expirará en 1 hora.</p>
            <p>Si no solicitaste restablecer tu contraseña, ignora este email.</p>
            <p>Saludos,<br/>El equipo de Extensiones Shop</p>
        ";

        await _emailService.SendEmailAsync(
            user.Email,
            "Recuperación de Contraseña - Extensiones Shop",
            emailBody
        );

        return Ok(new { message = "Si el email existe, recibirás instrucciones para recuperar tu contraseña" });
    }

    // POST: api/users/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

        if (user == null || user.PasswordResetTokenExpiry == null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
        {
            return BadRequest(new { message = "Token inválido o expirado" });
        }

        // Actualizar contraseña con BCrypt
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Contraseña actualizada exitosamente" });
    }

    // GET: api/users/profile (requiere autenticación - por ahora sin token)
    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Address,
            user.City,
            user.PostalCode,
            user.CreatedAt
        });
    }

    // PUT: api/users/profile/{id}
    [HttpPut("profile/{id}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = "Usuario no encontrado" });
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Address = request.Address;
        user.City = request.City;
        user.PostalCode = request.PostalCode;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Perfil actualizado exitosamente" });
    }
}

// DTOs para las peticiones
public class RegisterRequest
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    [MaxLength(256, ErrorMessage = "El email no puede exceder 256 caracteres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [MaxLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El formato del teléfono no es válido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Phone { get; set; }
}

public class LoginRequest
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "El token es obligatorio")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    [MaxLength(100, ErrorMessage = "La contraseña no puede exceder 100 caracteres")]
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [MaxLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El formato del teléfono no es válido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string? Phone { get; set; }

    [MaxLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
    public string? City { get; set; }

    [MaxLength(10, ErrorMessage = "El código postal no puede exceder 10 caracteres")]
    public string? PostalCode { get; set; }
}
