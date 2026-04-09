using ExtensionesShop.Server.Data;
using ExtensionesShop.Server.Services;
using ExtensionesShop.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


namespace ExtensionesShop.Server.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UsersController> _logger;
    private readonly IRecaptchaService _recaptchaService;

    public UsersController(
        AppDbContext context, 
        IEmailService emailService, 
        IConfiguration configuration,
        ILogger<UsersController> logger,
        IRecaptchaService recaptchaService)
    {
        _context = context;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
        _recaptchaService = recaptchaService;
    }

    // POST: api/users/register
    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validar el modelo
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            _logger.LogWarning("Registro fallido por datos inválidos: {Email}", request.Email);
            return BadRequest(new { message = "Datos inválidos", errors });
        }

        try
        {
            // Verificar reCAPTCHA:
            // - En PRODUCCIÓN: siempre se verifica si la SecretKey está configurada
            // - En DESARROLLO: se omite para facilitar pruebas locales
            var secretKey = _configuration["Recaptcha:SecretKey"];
            var isRecaptchaConfigured = !string.IsNullOrEmpty(secretKey)
                && !secretKey.StartsWith("PON_AQUI")
                && secretKey.Length > 20;

            // Bypass automático en entorno de desarrollo
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            if (isRecaptchaConfigured && !isDevelopment)
            {
                var recaptchaResponse = await _recaptchaService.VerifyTokenAsync(request.RecaptchaToken);

                if (!recaptchaResponse.Success)
                {
                    _logger.LogWarning("Verificación de reCAPTCHA fallida para {Email}: {Errors}", 
                        request.Email, 
                        string.Join(", ", recaptchaResponse.ErrorCodes ?? Array.Empty<string>()));
                    return BadRequest(new { message = "Verificación de seguridad fallida. Por favor, inténtalo de nuevo." });
                }

                const float minScore = 0.5f;
                if (recaptchaResponse.Score < minScore)
                {
                    _logger.LogWarning("Score de reCAPTCHA bajo ({Score}) para {Email}", recaptchaResponse.Score, request.Email);
                    return BadRequest(new { message = "Verificación de seguridad fallida. Si eres humano, inténtalo de nuevo." });
                }

                _logger.LogInformation("reCAPTCHA verificado para {Email} con score {Score}", request.Email, recaptchaResponse.Score);
            }
            else
            {
                _logger.LogWarning("reCAPTCHA OMITIDO para {Email} (entorno: {Env}, SecretKey configurada: {Configured})",
                    request.Email, 
                    Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown",
                    isRecaptchaConfigured);
            }


            // Validar que el email no exista
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

            if (existingUser != null)
            {
                _logger.LogWarning("Intento de registro con email duplicado: {Email}", request.Email);
                return BadRequest(new { message = "Este email ya está registrado" });
            }

            // Normalización de teléfono (eliminar todo excepto dígitos y +)
            var normalizedPhone = Regex.Replace(request.Phone, @"[^\d+]", "");

            // Generar token de verificación de email
            var verificationToken = Guid.NewGuid().ToString("N");

            // Crear el nuevo usuario con BCrypt
            var user = new User
            {
                Email = request.Email.ToLower(),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                Phone = normalizedPhone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow,
                EmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Enviar email de verificación
            var clientUrl = _configuration["ClientUrl"] ?? "https://localhost:59871";
            var verificationLink = $"{clientUrl}/verificar-email?token={verificationToken}";

            var emailBody = $@"
                <h2>¡Bienvenido/a a Extensiones Shop!</h2>
                <p>Hola {user.FirstName},</p>
                <p>Gracias por crear tu cuenta. Para completar el registro, por favor verifica tu email haciendo clic en el siguiente enlace:</p>
                <p><a href=""{verificationLink}"" style=""background: #E8607A; color: white; padding: 12px 24px; text-decoration: none; border-radius: 50px; display: inline-block;"">Verificar Email</a></p>
                <p>Este enlace expirará en 24 horas.</p>
                <p>Si no creaste esta cuenta, ignora este email.</p>
                <p>Saludos,<br/>El equipo de Extensiones Shop</p>
            ";

            await _emailService.SendEmailAsync(
                user.Email,
                "Verifica tu email - Extensiones Shop",
                emailBody
            );

            _logger.LogInformation("Usuario registrado exitosamente: {Email}, ID: {UserId}", user.Email, user.Id);

            return Ok(new 
            { 
                message = "Cuenta creada exitosamente. Revisa tu email para verificar tu cuenta.", 
                userId = user.Id,
                emailSent = true
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Error de base de datos al registrar usuario {Email}", request.Email);
            return BadRequest(new { message = "Error al crear la cuenta. Verifica que todos los campos sean válidos." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al registrar usuario {Email}", request.Email);
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
            _logger.LogWarning("Intento de login con email no registrado: {Email}", request.Email);
            return Unauthorized(new { message = "Email o contraseña incorrectos" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Intento de login con contraseña incorrecta: {Email}", request.Email);
            return Unauthorized(new { message = "Email o contraseña incorrectos" });
        }

        if (!user.EmailVerified)
        {
            _logger.LogWarning("Intento de login con email no verificado: {Email}", request.Email);
            return Unauthorized(new { message = "Debes verificar tu email antes de iniciar sesión. Revisa tu bandeja de entrada." });
        }

        _logger.LogInformation("Login exitoso para usuario {Email}", user.Email);

        // ✅ Generar JWT Token
        var token = GenerateJwtToken(user);

        return Ok(new
        {
            message = "Login exitoso",
            token, // ✅ Devolver token
            user = new
            {
                user.Id,
                user.Email,
                user.FirstName,
                user.LastName,
                user.Phone,
                user.Address,
                user.City,
                user.PostalCode,
                user.Role // ✅ Incluir Role para que el cliente pueda detectar si es Admin
            }
        });
    }

    /// <summary>
    /// Genera un token JWT para el usuario
    /// </summary>
    private string GenerateJwtToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? "DefaultKeyForDevelopmentOnly12345678901234567890"));

        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}"),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim(ClaimTypes.Role, user.Role ?? "User")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "1440")),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
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

        _logger.LogInformation("Contraseña restablecida para usuario {Email}", user.Email);

        return Ok(new { message = "Contraseña actualizada exitosamente" });
    }

    // POST: api/users/verify-email
    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token);

        if (user == null || user.EmailVerificationTokenExpiry == null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
        {
            _logger.LogWarning("Intento de verificación con token inválido o expirado");
            return BadRequest(new { message = "Token de verificación inválido o expirado" });
        }

        user.EmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Email verificado exitosamente para usuario {Email}", user.Email);

        return Ok(new { message = "Email verificado exitosamente. Ya puedes iniciar sesión." });
    }

    // GET: api/users/profile (requiere autenticación)
    [Authorize]
    [HttpGet("profile/{id}")]
    public async Task<IActionResult> GetProfile(int id)
    {
        // 🔒 CANDADO DE SEGURIDAD: Verificar que el usuario solo vea su propio perfil
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserIdStr != id.ToString())
        {
            return Forbid(); // 403: No puedes ver perfiles de otros
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

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
    [Authorize]
    [HttpPut("profile/{id}")]
    public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
    {
        // 🔒 CANDADO DE SEGURIDAD: Verificar que el usuario solo edite su propio perfil
        var currentUserIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserIdStr != id.ToString())
        {
            return Forbid(); // 403: No puedes editar perfiles de otros
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Phone = request.Phone;
        user.Address = request.Address;
        user.City = request.City;
        user.PostalCode = request.PostalCode;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Perfil actualizado exitosamente" });
    }
    // GET: api/users - Obtener todos los usuarios (para admin)
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IEnumerable<User>>> GetAll()
    {
        try
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    u.Phone,
                    u.Address,
                    u.City,
                    u.PostalCode,
                    u.Role,
                    u.EmailVerified,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuarios");
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // GET: api/users/{id} - Obtener un usuario por ID
    [Authorize(Roles = "Admin")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetById(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

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
                user.Role,
                user.EmailVerified,
                user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo usuario {UserId}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
    }

    // PUT: api/users/{id} - Actualizar usuario (incluye cambio de rol)
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] User updatedUser)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Usuario no encontrado" });

            // Actualizar campos
            user.FirstName = updatedUser.FirstName;
            user.LastName = updatedUser.LastName;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;
            user.City = updatedUser.City;
            user.PostalCode = updatedUser.PostalCode;
            user.Role = updatedUser.Role; // Permitir cambio de rol

            await _context.SaveChangesAsync();

            return Ok(new { message = "Usuario actualizado correctamente" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando usuario {UserId}", id);
            return StatusCode(500, new { message = "Error interno del servidor" });
        }
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

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Phone(ErrorMessage = "El formato del teléfono no es válido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "El token de reCAPTCHA es obligatorio")]
    public string RecaptchaToken { get; set; } = string.Empty;
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

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "El token es obligatorio")]
    public string Token { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [MaxLength(100, ErrorMessage = "Los apellidos no pueden exceder 100 caracteres")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono es obligatorio")]
    [Phone(ErrorMessage = "El formato del teléfono no es válido")]
    [MaxLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "La dirección no puede exceder 200 caracteres")]
    public string? Address { get; set; }

    [MaxLength(100, ErrorMessage = "La ciudad no puede exceder 100 caracteres")]
    public string? City { get; set; }

    [MaxLength(10, ErrorMessage = "El código postal no puede exceder 10 caracteres")]
    public string? PostalCode { get; set; }
}
