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
        private readonly IEncryptionService _encryptionService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            IAuthService authService,
            IEncryptionService encryptionService,
            ILogger<AdminController> logger)
        {
            _context = context;
            _authService = authService;
            _encryptionService = encryptionService;
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

                // CORREGIDO: Verificar que el usuario actual sea Admin
                var currentUserRol = User.FindFirst("rol")?.Value;
                if (registerDto.Rol == "Admin" && currentUserRol != "Admin")
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

        // ========== MÉTODOS PARA PERSONAS (CON IDENTIFICACIÓN ENCRIPTADA) ==========

        // GET: api/Admin/personas - Listar todas las personas
        [HttpGet("personas")]
        public async Task<IActionResult> GetPersonas(
            [FromQuery] int? empresaId = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Personas
                    .Include(p => p.Empresa)
                    .Include(p => p.Dispositivos)
                    .AsQueryable();

                if (empresaId.HasValue)
                {
                    query = query.Where(p => p.EmpresaId == empresaId);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();
                    // Filtrar por nombre (en memoria para identificaciones)
                    var personasTemp = await query.ToListAsync();

                    var personasFiltradas = personasTemp
                        .Where(p => p.Nombre.ToLower().Contains(search) ||
                                   _encryptionService.Decrypt(p.Identificacion).Contains(search))
                        .Select(p => new
                        {
                            p.Id,
                            p.Nombre,
                            Identificacion = _encryptionService.Decrypt(p.Identificacion), // Desencriptar
                            p.Telefono,
                            p.Email,
                            p.Activo,
                            Empresa = p.Empresa != null ? new { p.Empresa.Id, p.Empresa.Nombre } : null,
                            TotalDispositivos = p.Dispositivos?.Count ?? 0
                        })
                        .ToList();

                    return Ok(personasFiltradas);
                }

                var personas = await query
                    .Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        Identificacion = _encryptionService.Decrypt(p.Identificacion), // Desencriptar
                        p.Telefono,
                        p.Email,
                        p.Activo,
                        Empresa = p.Empresa != null ? new { p.Empresa.Id, p.Empresa.Nombre } : null,
                        TotalDispositivos = p.Dispositivos.Count
                    })
                    .ToListAsync();

                return Ok(personas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo personas");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Admin/crear-persona - Crear persona con identificación encriptada
        [HttpPost("crear-persona")]
        public async Task<IActionResult> CrearPersona([FromBody] RegistrarPersonaDTO personaDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(personaDto.Nombre) ||
                    string.IsNullOrWhiteSpace(personaDto.Identificacion))
                {
                    return BadRequest(new { mensaje = "Nombre e identificación son requeridos" });
                }

                // Encriptar la identificación
                var identificacionEncriptada = _encryptionService.Encrypt(personaDto.Identificacion);

                // Verificar si ya existe
                var existe = await _context.Personas
                    .AnyAsync(p => p.Identificacion == identificacionEncriptada);

                if (existe)
                {
                    return Conflict(new { mensaje = "La identificación ya está registrada" });
                }

                // Verificar empresa
                var empresa = await _context.Empresas.FindAsync(personaDto.EmpresaId);
                if (empresa == null)
                {
                    return NotFound(new { mensaje = $"Empresa con ID {personaDto.EmpresaId} no encontrada" });
                }

                var persona = new Persona
                {
                    Nombre = personaDto.Nombre,
                    Identificacion = identificacionEncriptada, // Guardar ENCRIPTADO
                    Telefono = personaDto.Telefono,
                    Email = personaDto.Email,
                    EmpresaId = personaDto.EmpresaId,
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                _context.Personas.Add(persona);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Persona creada: {persona.Nombre}, ID: {personaDto.Identificacion}");

                return Ok(new
                {
                    mensaje = "Persona creada exitosamente",
                    id = persona.Id,
                    nombre = persona.Nombre,
                    empresaId = persona.EmpresaId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando persona");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Admin/registrar-dispositivo - Registrar dispositivo con IMEI encriptado
        [HttpPost("registrar-dispositivo")]
        public async Task<IActionResult> RegistrarDispositivoAdmin([FromBody] RegistrarDispositivoAdminDTO registroDto)
        {
            try
            {
                // Validar IMEI
                if (string.IsNullOrWhiteSpace(registroDto.IMEI) || registroDto.IMEI.Length != 15 || !registroDto.IMEI.All(char.IsDigit))
                {
                    return BadRequest(new { mensaje = "IMEI inválido. Debe tener 15 dígitos numéricos" });
                }

                // Encriptar el IMEI antes de guardar
                var imeiEncriptado = _encryptionService.Encrypt(registroDto.IMEI);

                // Verificar si el IMEI ya existe (comparando encriptado con encriptado)
                var existe = await _context.Dispositivos
                    .AnyAsync(d => d.IMEI == imeiEncriptado);

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

                // Guardar el IMEI ENCRIPTADO en la columna IMEI normal
                var dispositivo = new Dispositivo
                {
                    IMEI = imeiEncriptado,  // ¡Guardas el encriptado!
                    PersonaId = registroDto.PersonaId,
                    FechaRegistro = DateTime.UtcNow,
                    Activo = registroDto.Activo ?? true
                };

                _context.Dispositivos.Add(dispositivo);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Dispositivo registrado por admin. IMEI: {registroDto.IMEI}, Persona: {persona.Nombre}");

                return Ok(new
                {
                    mensaje = "Dispositivo registrado exitosamente",
                    id = dispositivo.Id,
                    imeiOriginal = registroDto.IMEI, // Solo en respuesta
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
                _logger.LogError(ex, "Error registrando dispositivo");
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

        // GET: api/Admin/dispositivos - Listar dispositivos con IMEI desencriptado (solo para admin)
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

                // Buscar por IMEI (desencriptado) o nombre de persona/empresa
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.ToLower();

                    // Primero obtenemos todos los dispositivos y filtramos en memoria
                    var dispositivosTemp = await query.ToListAsync();

                    // Filtramos desencriptando cada IMEI
                    var dispositivosFiltrados = dispositivosTemp
                        .Where(d =>
                            _encryptionService.Decrypt(d.IMEI).ToLower().Contains(search) ||
                            d.Persona.Nombre.ToLower().Contains(search) ||
                            d.Persona.Empresa.Nombre.ToLower().Contains(search))
                        .Skip((page - 1) * limit)
                        .Take(limit)
                        .Select(d => new
                        {
                            d.Id,
                            IMEI = _encryptionService.Decrypt(d.IMEI), // Desencriptar
                            IMEIHash = _encryptionService.GenerateHash(_encryptionService.Decrypt(d.IMEI)),
                            PersonaId = d.Persona.Id,
                            PersonaNombre = d.Persona.Nombre,
                            // CORREGIDO: Desencriptar la identificación de la persona
                            PersonaIdentificacion = _encryptionService.Decrypt(d.Persona.Identificacion),
                            EmpresaId = d.Persona.Empresa.Id,
                            EmpresaNombre = d.Persona.Empresa.Nombre,
                            d.Activo,
                            d.FechaRegistro
                        })
                        .ToList();

                    var total = dispositivosTemp.Count;

                    return Ok(new
                    {
                        dispositivos = dispositivosFiltrados,
                        total,
                        page,
                        limit,
                        totalPages = (int)Math.Ceiling(total / (double)limit)
                    });
                }

                // Calcular total para paginación
                var totalSinFiltro = await query.CountAsync();

                // Paginación
                var dispositivos = await query
                    .OrderByDescending(d => d.FechaRegistro)
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(d => new
                    {
                        d.Id,
                        IMEI = _encryptionService.Decrypt(d.IMEI), // Desencriptar
                        IMEIHash = _encryptionService.GenerateHash(_encryptionService.Decrypt(d.IMEI)),
                        PersonaId = d.Persona.Id,
                        PersonaNombre = d.Persona.Nombre,
                        // CORREGIDO: Desencriptar la identificación de la persona
                        PersonaIdentificacion = _encryptionService.Decrypt(d.Persona.Identificacion),
                        PersonaTelefono = d.Persona.Telefono,
                        EmpresaId = d.Persona.Empresa.Id,
                        EmpresaNombre = d.Persona.Empresa.Nombre,
                        d.Activo,
                        d.FechaRegistro
                    })
                    .ToListAsync();

                return Ok(new
                {
                    dispositivos,
                    total = totalSinFiltro,
                    page,
                    limit,
                    totalPages = (int)Math.Ceiling(totalSinFiltro / (double)limit)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo dispositivos");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Admin/dispositivos/{id} - Obtener dispositivo por ID (con IMEI desencriptado)
        [HttpGet("dispositivos/{id}")]
        public async Task<IActionResult> GetDispositivo(int id)
        {
            try
            {
                var dispositivo = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p.Empresa)
                    .FirstOrDefaultAsync(d => d.Id == id);

                if (dispositivo == null)
                {
                    return NotFound(new { mensaje = $"Dispositivo con ID {id} no encontrado" });
                }

                var resultado = new
                {
                    dispositivo.Id,
                    IMEI = _encryptionService.Decrypt(dispositivo.IMEI), // Desencriptar
                    IMEIHash = _encryptionService.GenerateHash(_encryptionService.Decrypt(dispositivo.IMEI)),
                    PersonaId = dispositivo.Persona.Id,
                    PersonaNombre = dispositivo.Persona.Nombre,
                    // CORREGIDO: Desencriptar la identificación
                    PersonaIdentificacion = _encryptionService.Decrypt(dispositivo.Persona.Identificacion),
                    PersonaTelefono = dispositivo.Persona.Telefono,
                    EmpresaId = dispositivo.Persona.Empresa.Id,
                    EmpresaNombre = dispositivo.Persona.Empresa.Nombre,
                    dispositivo.Activo,
                    dispositivo.FechaRegistro
                };

                return Ok(resultado);
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
                    .ToListAsync();

                var resultado = dispositivos.Select(d => new
                {
                    d.Id,
                    IMEI = _encryptionService.Decrypt(d.IMEI), // Desencriptar
                    IMEIHash = _encryptionService.GenerateHash(_encryptionService.Decrypt(d.IMEI)),
                    PersonaId = d.Persona.Id,
                    PersonaNombre = d.Persona.Nombre,
                    // CORREGIDO: Desencriptar la identificación
                    PersonaIdentificacion = _encryptionService.Decrypt(d.Persona.Identificacion),
                    EmpresaId = d.Persona.Empresa.Id,
                    EmpresaNombre = d.Persona.Empresa.Nombre,
                    d.Activo,
                    d.FechaRegistro
                }).ToList();

                return Ok(resultado);
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
                // Validar IMEI
                if (string.IsNullOrWhiteSpace(imei) || imei.Length != 15 || !imei.All(char.IsDigit))
                {
                    return BadRequest(new { mensaje = "IMEI inválido. Debe tener 15 dígitos numéricos" });
                }

                // Encriptar el IMEI para buscar en la BD
                var imeiEncriptado = _encryptionService.Encrypt(imei);

                var dispositivo = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p.Empresa)
                    .FirstOrDefaultAsync(d => d.IMEI == imeiEncriptado);

                var existe = dispositivo != null;

                var resultado = existe ? new
                {
                    dispositivo.Id,
                    IMEI = imei, // El original que recibiste
                    IMEIHash = _encryptionService.GenerateHash(imei),
                    PersonaId = dispositivo.Persona.Id,
                    PersonaNombre = dispositivo.Persona.Nombre,
                    // CORREGIDO: Desencriptar la identificación
                    PersonaIdentificacion = _encryptionService.Decrypt(dispositivo.Persona.Identificacion),
                    EmpresaId = dispositivo.Persona.Empresa.Id,
                    EmpresaNombre = dispositivo.Persona.Empresa.Nombre,
                    dispositivo.Activo,
                    dispositivo.FechaRegistro
                } : null;

                return Ok(new
                {
                    existe,
                    dispositivo = resultado
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
                    activo = dispositivo.Activo
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error {(activo ? "activando" : "desactivando")} dispositivo ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // DELETE: api/Admin/dispositivos/{id} - Eliminar dispositivo (solo admin)
        [HttpDelete("dispositivos/{id}")]
        [Authorize(Roles = "Admin")]
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

                _logger.LogInformation($"Dispositivo eliminado por Admin. ID: {id}");

                return Ok(new
                {
                    mensaje = "Dispositivo eliminado exitosamente",
                    id = dispositivo.Id
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