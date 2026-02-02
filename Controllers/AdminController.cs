using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;
using Sistema_de_Verificación_IMEI.Services;

namespace Sistema_de_Verificación_IMEI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Solo admins pueden acceder
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IAuthService authService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
        }

        // GET: api/Admin/usuarios - Listar todos los usuarios
        [HttpGet("usuarios")]
        public async Task<IActionResult> GetUsuarios()
        {
            try
            {
                var usuarios = await _context.Usuarios
                    .Include(u => u.Empresa)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Rol,
                        Empresa = u.Empresa != null ? new { u.Empresa.Id, u.Empresa.Nombre } : null,
                        u.Activo,
                        u.FechaCreacion
                    })
                    .ToListAsync();

                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Admin/crear-usuario - Crear nuevo usuario (solo admin)
        [HttpPost("crear-usuario")]
        public async Task<IActionResult> CrearUsuario([FromBody] RegisterRequestDTO registerDto)
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

                // Verificar que no sea rol Admin (solo superadmin puede crear admins)
                var currentUserRol = User.FindFirst("rol")?.Value;
                if (registerDto.Rol == "Admin" && currentUserRol != "SuperAdmin")
                {
                    return Forbid("No tienes permiso para crear usuarios Admin");
                }

                var usuario = await _authService.RegisterAsync(registerDto);

                return Ok(new
                {
                    mensaje = "Usuario creado exitosamente",
                    id = usuario.Id,
                    username = usuario.Username,
                    rol = usuario.Rol,
                    empresaId = usuario.EmpresaId
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
                _logger.LogError(ex, "Error creando usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // PUT: api/Admin/activar-usuario/{id} - Activar/desactivar usuario
        [HttpPut("activar-usuario/{id}")]
        public async Task<IActionResult> ActivarUsuario(int id, [FromBody] bool activo)
        {
            try
            {
                var usuario = await _context.Usuarios.FindAsync(id);
                if (usuario == null)
                {
                    return NotFound(new { mensaje = "Usuario no encontrado" });
                }

                // No permitir desactivarse a sí mismo
                var currentUserId = int.Parse(User.FindFirst("userId")?.Value ?? "0");
                if (usuario.Id == currentUserId)
                {
                    return BadRequest(new { mensaje = "No puedes desactivar tu propia cuenta" });
                }

                usuario.Activo = activo;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = activo ? "Usuario activado" : "Usuario desactivado",
                    usuario.Id,
                    usuario.Username,
                    usuario.Activo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error {(activo ? "activando" : "desactivando")} usuario ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Admin/registrar-dispositivo - Registrar dispositivo (admin puede registrar cualquier IMEI)
        [HttpPost("registrar-dispositivo")]
        public async Task<IActionResult> RegistrarDispositivoAdmin([FromBody] RegistrarDispositivoAdminDTO registroDto)
        {
            try
            {
                // Verificar si el IMEI ya existe
                var existe = await _context.Dispositivos
                    .AnyAsync(d => d.IMEI == registroDto.IMEI);

                if (existe)
                {
                    return Conflict(new { mensaje = $"El IMEI {registroDto.IMEI} ya está registrado" });
                }

                // Verificar que la persona existe
                var persona = await _context.Personas
                    .Include(p => p.Empresa)
                    .FirstOrDefaultAsync(p => p.Id == registroDto.PersonaId);

                if (persona == null)
                {
                    return NotFound(new { mensaje = $"Persona con ID {registroDto.PersonaId} no encontrada" });
                }

                var dispositivo = new Dispositivo
                {
                    IMEI = registroDto.IMEI,
                    PersonaId = registroDto.PersonaId,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = true
                };

                _context.Dispositivos.Add(dispositivo);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Dispositivo registrado por admin. IMEI: {registroDto.IMEI}, Persona: {persona.Nombre}");

                return Ok(new
                {
                    mensaje = "Dispositivo registrado exitosamente",
                    id = dispositivo.Id,
                    imei = dispositivo.IMEI,
                    persona = new
                    {
                        persona.Id,
                        persona.Nombre,
                        empresa = persona.Empresa?.Nombre
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registrando dispositivo desde admin");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Admin/estadisticas - Estadísticas del sistema
        [HttpGet("estadisticas")]
        public async Task<IActionResult> GetEstadisticas()
        {
            try
            {
                var estadisticas = new
                {
                    TotalEmpresas = await _context.Empresas.CountAsync(),
                    TotalPersonas = await _context.Personas.CountAsync(),
                    TotalDispositivos = await _context.Dispositivos.CountAsync(),
                    DispositivosActivos = await _context.Dispositivos.CountAsync(d => d.Activo),
                    TotalUsuarios = await _context.Usuarios.CountAsync(),
                    UsuariosActivos = await _context.Usuarios.CountAsync(u => u.Activo),
                    UsuariosPorRol = await _context.Usuarios
                        .GroupBy(u => u.Rol)
                        .Select(g => new { Rol = g.Key, Cantidad = g.Count() })
                        .ToListAsync()
                };

                return Ok(estadisticas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo estadísticas");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
        // En tu AdminController.cs - AGREGA ESTOS MÉTODOS:

        // GET: api/Admin/dispositivos - Listar dispositivos
        [HttpGet("dispositivos")]
        public async Task<IActionResult> GetDispositivos(
            [FromQuery] int? empresaId = null,
            [FromQuery] bool? activo = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 20)
        {
            try
            {
                var query = _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p.Empresa)
                    .AsQueryable();

                // Filtrar por empresa
                if (empresaId.HasValue && empresaId > 0)
                {
                    query = query.Where(d => d.Persona.EmpresaId == empresaId);
                }

                // Filtrar por estado activo
                if (activo.HasValue)
                {
                    query = query.Where(d => d.Activo == activo);
                }

                // Buscar por IMEI o nombre de persona
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    query = query.Where(d =>
                        d.IMEI.Contains(search) ||
                        d.Persona.Nombre.ToLower().Contains(search) ||
                        d.Persona.Empresa.Nombre.ToLower().Contains(search)
                    );
                }

                // Calcular total para paginación
                var total = await query.CountAsync();

                // Paginación
                var dispositivos = await query
                    .OrderByDescending(d => d.FechaRegistro)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(d => new
                    {
                        d.Id,
                        d.IMEI,
                        PersonaId = d.Persona.Id,
                        PersonaNombre = d.Persona.Nombre,
                        EmpresaId = d.Persona.Empresa.Id,
                        EmpresaNombre = d.Persona.Empresa.Nombre,
                        d.Activo,
                        d.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    dispositivos,
                    total,
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling(total / (double)limit)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dispositivos");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Admin/dispositivos/{id} - Obtener dispositivo por ID
        [HttpGet("dispositivos/{id}")]
        public async Task<IActionResult> GetDispositivo(int id)
        {
            try
            {
                var dispositivo = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p.Empresa)
                    .Where(d => d.Id == id)
                    .Select(d => new
                    {
                        d.Id,
                        d.IMEI,
                        PersonaId = d.Persona.Id,
                        PersonaNombre = d.Persona.Nombre,
                        PersonaIdentificacion = d.Persona.Identificacion,
                        PersonaTelefono = d.Persona.Telefono,
                        EmpresaId = d.Persona.Empresa.Id,
                        EmpresaNombre = d.Persona.Empresa.Nombre,
                        d.Activo,
                        d.FechaRegistro
                    })
                    .FirstOrDefaultAsync();

                if (dispositivo == null)
                {
                    return NotFound(new { mensaje = $"Dispositivo con ID {id} no encontrado" });
                }

                return Ok(dispositivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo dispositivo ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Admin/dispositivos/persona/{personaId} - Dispositivos por persona
        [HttpGet("dispositivos/persona/{personaId}")]
        public async Task<IActionResult> GetDispositivosPorPersona(int personaId)
        {
            try
            {
                var dispositivos = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p.Empresa)
                    .Where(d => d.PersonaId == personaId)
                    .Select(d => new
                    {
                        d.Id,
                        d.IMEI,
                        PersonaId = d.Persona.Id,
                        PersonaNombre = d.Persona.Nombre,
                        EmpresaId = d.Persona.Empresa.Id,
                        EmpresaNombre = d.Persona.Empresa.Nombre,
                        d.Activo,
                        d.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(dispositivos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo dispositivos para persona ID: {personaId}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Admin/verificar-imei/{imei} - Verificar si IMEI existe
        [HttpGet("verificar-imei/{imei}")]
        public async Task<IActionResult> VerificarIMEI(string imei)
        {
            try
            {
                var dispositivo = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p.Empresa)
                    .Where(d => d.IMEI == imei)
                    .Select(d => new
                    {
                        d.Id,
                        d.IMEI,
                        PersonaId = d.Persona.Id,
                        PersonaNombre = d.Persona.Nombre,
                        EmpresaId = d.Persona.Empresa.Id,
                        EmpresaNombre = d.Persona.Empresa.Nombre,
                        d.Activo,
                        d.FechaRegistro
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    existe = dispositivo != null,
                    dispositivo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verificando IMEI: {imei}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // PUT: api/Admin/dispositivos/{id} - Actualizar dispositivo
        [HttpPut("dispositivos/{id}")]
        public async Task<IActionResult> UpdateDispositivo(int id, [FromBody] UpdateDispositivoDTO updateDto)
        {
            try
            {
                var dispositivo = await _context.Dispositivos.FindAsync(id);
                if (dispositivo == null)
                {
                    return NotFound(new { mensaje = $"Dispositivo con ID {id} no encontrado" });
                }

                // Verificar que la nueva persona existe
                if (updateDto.PersonaId.HasValue)
                {
                    var persona = await _context.Personas.FindAsync(updateDto.PersonaId.Value);
                    if (persona == null)
                    {
                        return NotFound(new { mensaje = $"Persona con ID {updateDto.PersonaId} no encontrada" });
                    }
                    dispositivo.PersonaId = updateDto.PersonaId.Value;
                }

                // Actualizar estado si se envía
                if (updateDto.Activo.HasValue)
                {
                    dispositivo.Activo = updateDto.Activo.Value;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Dispositivo actualizado exitosamente",
                    id = dispositivo.Id,
                    imei = dispositivo.IMEI,
                    personaId = dispositivo.PersonaId,
                    activo = dispositivo.Activo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error actualizando dispositivo ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // PATCH: api/Admin/dispositivos/{id}/activo - Cambiar estado activo
        [HttpPatch("dispositivos/{id}/activo")]
        public async Task<IActionResult> ToggleDispositivoActivo(int id, [FromBody] bool activo)
        {
            try
            {
                var dispositivo = await _context.Dispositivos.FindAsync(id);
                if (dispositivo == null)
                {
                    return NotFound(new { mensaje = $"Dispositivo con ID {id} no encontrado" });
                }

                dispositivo.Activo = activo;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = activo ? "Dispositivo activado" : "Dispositivo desactivado",
                    id = dispositivo.Id,
                    imei = dispositivo.IMEI,
                    activo = dispositivo.Activo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error {(activo ? "activando" : "desactivando")} dispositivo ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // DELETE: api/Admin/dispositivos/{id} - Eliminar dispositivo (solo superadmin)
        [HttpDelete("dispositivos/{id}")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteDispositivo(int id)
        {
            try
            {
                var dispositivo = await _context.Dispositivos.FindAsync(id);
                if (dispositivo == null)
                {
                    return NotFound(new { mensaje = $"Dispositivo con ID {id} no encontrado" });
                }

                _context.Dispositivos.Remove(dispositivo);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Dispositivo eliminado exitosamente",
                    id = dispositivo.Id,
                    imei = dispositivo.IMEI
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error eliminando dispositivo ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
    }
}