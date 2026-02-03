using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.Models;
using Sistema_de_Verificación_IMEI.Services;

namespace Sistema_de_Verificación_IMEI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PersonasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PersonasController> _logger;
        private readonly IEncryptionService _encryptionService;

        public PersonasController(
            ApplicationDbContext context,
            ILogger<PersonasController> logger,
            IEncryptionService encryptionService)
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        private bool IsAdmin()
        {
            var userRol = User.FindFirst("rol")?.Value;
            return userRol == "Admin";
        }

        [HttpGet]
        public async Task<IActionResult> GetPersonas()
        {
            try
            {
                var userRol = User.FindFirst("rol")?.Value;
                var userEmpresaId = User.FindFirst("empresaId")?.Value;

                IQueryable<Persona> query = _context.Personas;

                if (userRol != "Admin")
                {
                    if (!string.IsNullOrEmpty(userEmpresaId) &&
                        int.TryParse(userEmpresaId, out int empresaId))
                    {
                        query = query.Where(p => p.EmpresaId == empresaId);
                    }
                }

                var personas = await query
                    .Include(p => p.Empresa)
                    .ToListAsync();

                // CORRECCIÓN: Verificar si la identificación no es nula antes de desencriptar
                var personasDesencriptadas = personas.Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    Identificacion = !string.IsNullOrEmpty(p.Identificacion)
                        ? _encryptionService.Decrypt(p.Identificacion)
                        : string.Empty,
                    p.Telefono,
                    EmpresaId = p.EmpresaId,
                    EmpresaNombre = p.Empresa != null ? p.Empresa.Nombre : "Sin empresa"
                })
                .ToList();

                return Ok(personasDesencriptadas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo personas");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPersona(int id)
        {
            try
            {
                var userRol = User.FindFirst("rol")?.Value;
                var userEmpresaId = User.FindFirst("empresaId")?.Value;

                var persona = await _context.Personas
                    .Include(p => p.Empresa)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (persona == null)
                    return NotFound(new { mensaje = "Persona no encontrada" });

                if (userRol != "Admin" && !string.IsNullOrEmpty(userEmpresaId))
                {
                    if (int.TryParse(userEmpresaId, out int empresaId))
                    {
                        if (persona.EmpresaId != empresaId)
                            return Forbid("No tienes permiso para ver esta persona");
                    }
                }

                // CORRECCIÓN: Verificar si la identificación no es nula
                return Ok(new
                {
                    persona.Id,
                    persona.Nombre,
                    Identificacion = !string.IsNullOrEmpty(persona.Identificacion)
                        ? _encryptionService.Decrypt(persona.Identificacion)
                        : string.Empty,
                    persona.Telefono,
                    empresaId = persona.EmpresaId,
                    empresaNombre = persona.Empresa?.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo persona {id}");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreatePersona([FromBody] CreatePersonaDTO personaDto)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid("Solo los administradores pueden crear personas");

                if (string.IsNullOrWhiteSpace(personaDto.Nombre))
                    return BadRequest(new { mensaje = "Nombre requerido" });

                if (string.IsNullOrWhiteSpace(personaDto.Identificacion))
                    return BadRequest(new { mensaje = "Identificación requerida" });

                // Encriptar la identificación para buscar
                var identificacionEncriptada = _encryptionService.Encrypt(personaDto.Identificacion);

                // Verificar si ya existe (comparando encriptado con encriptado)
                var existeIdentificacion = await _context.Personas
                    .AnyAsync(p => p.Identificacion == identificacionEncriptada);

                if (existeIdentificacion)
                    return Conflict(new { mensaje = $"Identificación {personaDto.Identificacion} ya existe" });

                var empresa = await _context.Empresas.FindAsync(personaDto.EmpresaId);
                if (empresa == null)
                    return NotFound(new { mensaje = $"Empresa {personaDto.EmpresaId} no encontrada" });

                var persona = new Persona
                {
                    Nombre = personaDto.Nombre.Trim(),
                    Identificacion = identificacionEncriptada,
                    Telefono = personaDto.Telefono?.Trim(),
                    EmpresaId = personaDto.EmpresaId,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Persona creada: {persona.Id} - {persona.Nombre}");

                return CreatedAtAction(nameof(GetPersona), new { id = persona.Id }, new
                {
                    mensaje = "Persona creada exitosamente",
                    id = persona.Id,
                    persona.Nombre,
                    Identificacion = personaDto.Identificacion,
                    persona.Telefono,
                    empresaId = persona.EmpresaId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando persona");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePersona(int id, [FromBody] UpdatePersonaDTO personaDto)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid("Solo los administradores pueden actualizar personas");

                var persona = await _context.Personas.FindAsync(id);
                if (persona == null)
                    return NotFound(new { mensaje = "Persona no encontrada" });

                // Si cambia la identificación, verificar que no exista
                if (!string.IsNullOrWhiteSpace(personaDto.Identificacion))
                {
                    var nuevaIdentificacionEncriptada = _encryptionService.Encrypt(personaDto.Identificacion);

                    var existeIdentificacion = await _context.Personas
                        .AnyAsync(p => p.Identificacion == nuevaIdentificacionEncriptada && p.Id != id);

                    if (existeIdentificacion)
                        return Conflict(new { mensaje = $"Identificación {personaDto.Identificacion} ya existe" });

                    persona.Identificacion = nuevaIdentificacionEncriptada;
                }

                if (!string.IsNullOrWhiteSpace(personaDto.Nombre))
                    persona.Nombre = personaDto.Nombre.Trim();

                if (personaDto.Telefono != null)
                    persona.Telefono = personaDto.Telefono.Trim();

                if (personaDto.EmpresaId.HasValue && personaDto.EmpresaId.Value > 0)
                {
                    var empresa = await _context.Empresas.FindAsync(personaDto.EmpresaId.Value);
                    if (empresa == null)
                        return NotFound(new { mensaje = $"Empresa {personaDto.EmpresaId} no encontrada" });

                    persona.EmpresaId = personaDto.EmpresaId.Value;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Persona actualizada",
                    id = persona.Id,
                    persona.Nombre,
                    Identificacion = !string.IsNullOrEmpty(persona.Identificacion)
                        ? _encryptionService.Decrypt(persona.Identificacion)
                        : string.Empty,
                    persona.Telefono,
                    empresaId = persona.EmpresaId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error actualizando persona {id}");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePersona(int id)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid("Solo los administradores pueden eliminar personas");

                var persona = await _context.Personas
                    .Include(p => p.Dispositivos)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (persona == null)
                    return NotFound(new { mensaje = "Persona no encontrada" });

                if (persona.Dispositivos != null && persona.Dispositivos.Any())
                    return BadRequest(new { mensaje = "No se puede eliminar, tiene dispositivos asociados" });

                _context.Personas.Remove(persona);
                await _context.SaveChangesAsync();

                return Ok(new { mensaje = "Persona eliminada" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error eliminando persona {id}");
                return StatusCode(500, new { mensaje = "Error interno" });
            }
        }

        [HttpGet("buscar")]
        public async Task<IActionResult> BuscarPersona([FromQuery] string identificacion)
        {
            try
            {
                var identificacionEncriptada = _encryptionService.Encrypt(identificacion);

                var persona = await _context.Personas
                    .Where(p => p.Identificacion == identificacionEncriptada)
                    .Include(p => p.Empresa)
                    .FirstOrDefaultAsync();

                if (persona == null)
                    return NotFound(new { mensaje = "Persona no encontrada" });

                return Ok(new
                {
                    persona.Id,
                    persona.Nombre,
                    Identificacion = identificacion,
                    persona.Telefono,
                    empresaId = persona.EmpresaId,
                    empresaNombre = persona.Empresa?.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error buscando persona por identificación");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
    }

    public class CreatePersonaDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int EmpresaId { get; set; }
    }

    public class UpdatePersonaDTO
    {
        public string? Nombre { get; set; }
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public int? EmpresaId { get; set; }
    }
}