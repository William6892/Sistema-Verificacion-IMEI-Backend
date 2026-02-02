using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Services;

namespace Sistema_de_Verificación_IMEI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ← Requiere autenticación para TODOS los endpoints
    public class VerificacionController : ControllerBase
    {
        private readonly IVerificacionService _verificacionService;
        private readonly ILogger<VerificacionController> _logger;

        public VerificacionController(
            IVerificacionService verificacionService,
            ILogger<VerificacionController> logger)
        {
            _verificacionService = verificacionService;
            _logger = logger;
        }

        // POST: api/Verificacion/verificar
        // Cualquier usuario autenticado puede verificar IMEIs
        [HttpPost("verificar")]
        public async Task<IActionResult> VerificarIMEI([FromBody] VerificacionRequestDTO request)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var username = User.Identity?.Name;
                var userRol = User.FindFirst("rol")?.Value;

                _logger.LogInformation($"Usuario {username} (ID: {userId}, Rol: {userRol}) verificando IMEI: {request.IMEI}");

                if (string.IsNullOrWhiteSpace(request.IMEI))
                {
                    return BadRequest(new { mensaje = "El IMEI es requerido" });
                }

                // Validar formato IMEI (15 dígitos normalmente)
                if (request.IMEI.Length < 10 || request.IMEI.Length > 20 || !request.IMEI.All(char.IsDigit))
                {
                    return BadRequest(new { mensaje = "Formato de IMEI inválido. Debe contener solo números (10-20 dígitos)" });
                }

                var resultado = await _verificacionService.VerificarIMEIAsync(request.IMEI);

                // Log del resultado
                _logger.LogInformation($"Verificación IMEI {request.IMEI}: {(resultado.Valido ? "VÁLIDO" : "NO VÁLIDO")} - Usuario: {username}");

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verificando IMEI: {request.IMEI}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Verificacion/registrar-dispositivo
        // SOLO Admin pueden registrar dispositivos
        [HttpPost("registrar-dispositivo")]
        [Authorize(Roles = "Admin")] // ← Solo admins
        public async Task<IActionResult> RegistrarDispositivo([FromBody] RegistrarDispositivoDTO registroDto)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var username = User.Identity?.Name;

                _logger.LogInformation($"Admin {username} (ID: {userId}) registrando dispositivo IMEI: {registroDto.IMEI} para persona ID: {registroDto.PersonaId}");

                if (string.IsNullOrWhiteSpace(registroDto.IMEI))
                {
                    return BadRequest(new { mensaje = "El IMEI es requerido" });
                }

                // Validar formato IMEI
                if (registroDto.IMEI.Length < 10 || registroDto.IMEI.Length > 20 || !registroDto.IMEI.All(char.IsDigit))
                {
                    return BadRequest(new { mensaje = "Formato de IMEI inválido. Debe contener solo números (10-20 dígitos)" });
                }

                var dispositivo = await _verificacionService.RegistrarDispositivoAsync(registroDto);

                _logger.LogInformation($"Dispositivo registrado: ID {dispositivo.Id}, IMEI: {dispositivo.IMEI} por admin: {username}");

                return Ok(new
                {
                    mensaje = "Dispositivo registrado exitosamente",
                    id = dispositivo.Id,
                    imei = dispositivo.IMEI,
                    fechaRegistro = dispositivo.FechaRegistro,
                    registradoPor = username
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Conflicto registrando dispositivo IMEI: {registroDto.IMEI} - {ex.Message}");
                return Conflict(new { mensaje = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"Recurso no encontrado al registrar dispositivo: {ex.Message}");
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registrando dispositivo IMEI: {registroDto.IMEI}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Verificacion/registrar-persona
        // SOLO Admin pueden registrar personas
        [HttpPost("registrar-persona")]
        [Authorize(Roles = "Admin")] // ← Solo admins
        public async Task<IActionResult> RegistrarPersona([FromBody] RegistrarPersonaDTO personaDto)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var username = User.Identity?.Name;

                _logger.LogInformation($"Admin {username} (ID: {userId}) registrando persona: {personaDto.Nombre} (ID: {personaDto.Identificacion}) para empresa ID: {personaDto.EmpresaId}");

                if (string.IsNullOrWhiteSpace(personaDto.Nombre) || string.IsNullOrWhiteSpace(personaDto.Identificacion))
                {
                    return BadRequest(new { mensaje = "Nombre e Identificación son requeridos" });
                }

                // Validar formato de identificación
                if (personaDto.Identificacion.Length < 5 || personaDto.Identificacion.Length > 20)
                {
                    return BadRequest(new { mensaje = "La identificación debe tener entre 5 y 20 caracteres" });
                }


                var persona = await _verificacionService.RegistrarPersonaAsync(personaDto);

                _logger.LogInformation($"Persona registrada: ID {persona.Id}, Nombre: {persona.Nombre} por admin: {username}");

                return Ok(new
                {
                    mensaje = "Persona registrada exitosamente",
                    id = persona.Id,
                    nombre = persona.Nombre,
                    identificacion = persona.Identificacion,
                    empresaId = persona.EmpresaId,
                    registradoPor = username
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Conflicto registrando persona ID: {personaDto.Identificacion} - {ex.Message}");
                return Conflict(new { mensaje = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning($"Recurso no encontrado al registrar persona: {ex.Message}");
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error registrando persona: {personaDto.Nombre}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Verificacion/dispositivos/{personaId}
        // Usuarios normales solo ven sus propios dispositivos, admins ven todos
        [HttpGet("dispositivos/{personaId}")]
        public async Task<IActionResult> GetDispositivosPorPersona(int personaId)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var userRol = User.FindFirst("rol")?.Value;
                var username = User.Identity?.Name;

                // Si no es admin, verificar que está consultando sus propios dispositivos
                // (esto requeriría relacionar usuarios con personas, por ahora solo admins)
                if (userRol != "Admin" && userRol != "SuperAdmin")
                {
                    return Forbid("Solo administradores pueden consultar dispositivos de otras personas");
                }

                _logger.LogInformation($"Usuario {username} consultando dispositivos de persona ID: {personaId}");

                var dispositivos = await _verificacionService.ObtenerDispositivosPorPersonaAsync(personaId);

                return Ok(dispositivos.Select(d => new
                {
                    d.Id,
                    d.IMEI,
                    d.FechaRegistro,
                    d.Activo,
                    personaId = d.PersonaId
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo dispositivos para persona ID: {personaId}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Verificacion/personas/{empresaId}
        // Usuarios normales ven solo personas de su empresa, admins ven todas
        [HttpGet("personas/{empresaId}")]
        public async Task<IActionResult> GetPersonasPorEmpresa(int empresaId)
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                var userRol = User.FindFirst("rol")?.Value;
                var userEmpresaId = User.FindFirst("empresaId")?.Value;
                var username = User.Identity?.Name;

                // Si no es admin, solo puede ver personas de su propia empresa
                if (userRol != "Admin" && userRol != "SuperAdmin")
                {
                    if (userEmpresaId == null || int.Parse(userEmpresaId) != empresaId)
                    {
                        return Forbid("Solo puedes consultar personas de tu propia empresa");
                    }
                }

                _logger.LogInformation($"Usuario {username} consultando personas de empresa ID: {empresaId}");

                var personas = await _verificacionService.ObtenerPersonasPorEmpresaAsync(empresaId);

                return Ok(personas.Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Identificacion,
                    p.Telefono,
                    cantidadDispositivos = p.Dispositivos?.Count(d => d.Activo) ?? 0
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo personas para empresa ID: {empresaId}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

    }
}