using Microsoft.AspNetCore.Mvc;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Services;
using System.Security.Claims;

namespace Sistema_de_Verificación_IMEI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                {
                    return BadRequest(new { mensaje = "Usuario y contraseña son requeridos" });
                }

                var resultado = await _authService.LoginAsync(loginDto);

                if (!resultado.Success)
                {
                    return Unauthorized(new { mensaje = resultado.Mensaje });
                }

                _logger.LogInformation($"Login exitoso para usuario: {loginDto.Username}");

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequestDTO registerDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(registerDto.Username) || string.IsNullOrWhiteSpace(registerDto.Password))
                {
                    return BadRequest(new { mensaje = "Usuario y contraseña son requeridos" });
                }

                if (registerDto.Password.Length < 6)
                {
                    return BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });
                }

                var usuario = await _authService.RegisterAsync(registerDto);

                return Ok(new
                {
                    mensaje = "Usuario registrado exitosamente",
                    id = usuario.Id,
                    username = usuario.Username,
                    rol = usuario.Rol
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { mensaje = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en registro");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        [HttpPost("validate")]
        public IActionResult ValidateToken()
        {
            var userId = User.FindFirst("userId")?.Value;
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var rol = User.FindFirst("rol")?.Value;

            return Ok(new
            {
                valid = true,
                userId,
                username,
                rol,
                mensaje = "Token válido"
            });
        }
    }
}