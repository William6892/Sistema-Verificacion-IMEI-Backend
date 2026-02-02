using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;

namespace Sistema_de_Verificación_IMEI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ← Requiere autenticación para TODOS los endpoints
    public class EmpresasController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmpresasController> _logger;

        public EmpresasController(
            ApplicationDbContext context,
            ILogger<EmpresasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Empresas
        // Cualquier usuario autenticado puede ver empresas
        [HttpGet]
        public async Task<IActionResult> GetEmpresas()
        {
            try
            {
                _logger.LogInformation($"Usuario {User.Identity?.Name} consultando empresas");

                var empresas = await _context.Empresas
                    .Where(e => e.Activo)
                    .Select(e => new EmpresaDTO
                    {
                        Id = e.Id,
                        Nombre = e.Nombre,
                        FechaCreacion = e.FechaCreacion
                    })
                    .OrderBy(e => e.Nombre)
                    .ToListAsync();

                return Ok(empresas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo empresas");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Empresas/{id}
        // Cualquier usuario autenticado puede ver una empresa específica
        [HttpGet("{id}")]
        public async Task<IActionResult> GetEmpresa(int id)
        {
            try
            {
                var empresa = await _context.Empresas
                    .Where(e => e.Id == id && e.Activo)
                    .Select(e => new EmpresaDTO
                    {
                        Id = e.Id,
                        Nombre = e.Nombre,
                        FechaCreacion = e.FechaCreacion
                    })
                    .FirstOrDefaultAsync();

                if (empresa == null)
                {
                    return NotFound(new { mensaje = $"Empresa con ID {id} no encontrada" });
                }

                return Ok(empresa);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo empresa ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // POST: api/Empresas
        // SOLO Admin  pueden crear empresas
        [HttpPost]
        [Authorize(Roles = "Admin")] 
        public async Task<IActionResult> CreateEmpresa([FromBody] RegistrarEmpresaDTO empresaDto)
        {
            try
            {
                _logger.LogInformation($"Usuario {User.Identity?.Name} creando empresa: {empresaDto.Nombre}");

                if (string.IsNullOrWhiteSpace(empresaDto.Nombre))
                {
                    return BadRequest(new { mensaje = "El nombre de la empresa es requerido" });
                }

                // Verificar si ya existe una empresa con ese nombre
                var existe = await _context.Empresas
                    .AnyAsync(e => e.Nombre.ToLower() == empresaDto.Nombre.ToLower() && e.Activo);

                if (existe)
                {
                    return Conflict(new { mensaje = $"Ya existe una empresa con el nombre '{empresaDto.Nombre}'" });
                }

                var empresa = new Empresa
                {
                    Nombre = empresaDto.Nombre.Trim(),
                    FechaCreacion = DateTime.UtcNow,
                    Activo = true
                };

                _context.Empresas.Add(empresa);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Empresa creada: ID {empresa.Id}, Nombre: {empresa.Nombre}");

                return Ok(new
                {
                    mensaje = "Empresa creada exitosamente",
                    id = empresa.Id,
                    nombre = empresa.Nombre,
                    fechaCreacion = empresa.FechaCreacion
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creando empresa: {empresaDto.Nombre}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // PUT: api/Empresas/{id}
        // SOLO Admin y SuperAdmin pueden actualizar empresas
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmpresa(int id, [FromBody] RegistrarEmpresaDTO empresaDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empresaDto.Nombre))
                {
                    return BadRequest(new { mensaje = "El nombre de la empresa es requerido" });
                }

                var empresa = await _context.Empresas.FindAsync(id);
                if (empresa == null || !empresa.Activo)
                {
                    return NotFound(new { mensaje = $"Empresa con ID {id} no encontrada" });
                }

                // Verificar si otro empresa ya tiene ese nombre
                var nombreExiste = await _context.Empresas
                    .AnyAsync(e => e.Id != id &&
                                   e.Nombre.ToLower() == empresaDto.Nombre.ToLower() &&
                                   e.Activo);

                if (nombreExiste)
                {
                    return Conflict(new { mensaje = $"Ya existe otra empresa con el nombre '{empresaDto.Nombre}'" });
                }

                empresa.Nombre = empresaDto.Nombre.Trim();
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Empresa actualizada exitosamente",
                    id = empresa.Id,
                    nombre = empresa.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error actualizando empresa ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // DELETE: api/Empresas/{id}
        // SOLO Admin puede eliminar empresas (soft delete)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")] // ← Solo Admin
        public async Task<IActionResult> DeleteEmpresa(int id)
        {
            try
            {
                var empresa = await _context.Empresas
                    .Include(e => e.Personas)
                    .FirstOrDefaultAsync(e => e.Id == id && e.Activo);

                if (empresa == null)
                {
                    return NotFound(new { mensaje = $"Empresa con ID {id} no encontrada" });
                }

                // Verificar si tiene personas asociadas
                if (empresa.Personas != null && empresa.Personas.Any(p => p.Dispositivos != null && p.Dispositivos.Any()))
                {
                    return BadRequest(new
                    {
                        mensaje = "No se puede eliminar la empresa porque tiene personas con dispositivos asignados"
                    });
                }

                // Soft delete: desactivar en lugar de eliminar
                empresa.Activo = false;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    mensaje = "Empresa desactivada exitosamente",
                    id = empresa.Id,
                    nombre = empresa.Nombre
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error eliminando empresa ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }   
        }

        // GET: api/Empresas/{id}/personas
        // Cualquier usuario autenticado puede ver personas de una empresa
        [HttpGet("{id}/personas")]
        public async Task<IActionResult> GetPersonasPorEmpresa(int id)
        {
            try
            {
                var empresaExiste = await _context.Empresas
                    .AnyAsync(e => e.Id == id && e.Activo);

                if (!empresaExiste)
                {
                    return NotFound(new { mensaje = $"Empresa con ID {id} no encontrada" });
                }

                var personas = await _context.Personas
                    .Where(p => p.EmpresaId == id)
                    .Select(p => new PersonaDTO
                    {
                        Id = p.Id,
                        Nombre = p.Nombre,
                        Identificacion = p.Identificacion,                        
                        Telefono = p.Telefono,
                        CantidadDispositivos = p.Dispositivos != null ? p.Dispositivos.Count(d => d.Activo) : 0
                    })
                    .OrderBy(p => p.Nombre)
                    .ToListAsync();

                return Ok(personas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error obteniendo personas de empresa ID: {id}");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
    }
}