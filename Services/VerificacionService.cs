using Microsoft.EntityFrameworkCore;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;

namespace Sistema_de_Verificación_IMEI.Services
{
    public class VerificacionService : IVerificacionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<VerificacionService> _logger;
        private readonly IEncryptionService _encryptionService; 

        // CONSTRUCTOR CORREGIDO - Agregar IEncryptionService
        public VerificacionService(
            ApplicationDbContext context,
            ILogger<VerificacionService> logger,
            IEncryptionService encryptionService) 
        {
            _context = context;
            _logger = logger;
            _encryptionService = encryptionService; 
        }

        public async Task<VerificacionResponseDTO> VerificarIMEIAsync(string imei)
        {
            try
            {
                _logger.LogInformation($"Verificando IMEI recibido: {imei}");
                _logger.LogInformation($"Longitud IMEI recibido: {imei.Length}");

                // DEPURACIÓN: Mostrar qué estamos buscando
                _logger.LogInformation($"=== INICIO BÚSQUEDA IMEI ===");

                // Opción 1: Primero intentar buscar tal cual (por si viene encriptado)
                var dispositivo = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p!.Empresa)
                    .FirstOrDefaultAsync(d => d.IMEI == imei && d.Activo);

                _logger.LogInformation($"Búsqueda directa: {(dispositivo != null ? "ENCONTRADO" : "NO ENCONTRADO")}");

                // Opción 2: Si no encuentra, ENCRIPTAR el IMEI recibido y buscar
                if (dispositivo == null)
                {
                    try
                    {
                        string imeiEncriptado = _encryptionService.Encrypt(imei);
                        _logger.LogInformation($"IMEI encriptado para búsqueda: {imeiEncriptado}");
                        _logger.LogInformation($"Longitud encriptado: {imeiEncriptado.Length}");

                        dispositivo = await _context.Dispositivos
                            .Include(d => d.Persona)
                                .ThenInclude(p => p!.Empresa)
                            .FirstOrDefaultAsync(d => d.IMEI == imeiEncriptado && d.Activo);

                        _logger.LogInformation($"Búsqueda encriptada: {(dispositivo != null ? "ENCONTRADO" : "NO ENCONTRADO")}");
                    }
                    catch (Exception encryptEx)
                    {
                        _logger.LogError(encryptEx, $"Error al encriptar IMEI: {imei}");
                    }
                }

                // Opción 3: Si aún no encuentra, mostrar debug de qué hay en la BD
                if (dispositivo == null)
                {
                    _logger.LogWarning($"IMEI no encontrado después de 2 intentos: {imei}");

                    // DEPURACIÓN: Mostrar los primeros 5 IMEIs en la BD
                    var imeisEnBD = await _context.Dispositivos
                        .Where(d => d.Activo)
                        .Select(d => new {
                            d.Id,
                            IMEI = d.IMEI,
                            Longitud = d.IMEI.Length
                        })
                        .Take(5)
                        .ToListAsync();

                    _logger.LogInformation($"=== DEBUG - PRIMEROS 5 DISPOSITIVOS EN BD ===");
                    foreach (var disp in imeisEnBD)
                    {
                        _logger.LogInformation($"ID: {disp.Id}, IMEI: '{disp.IMEI}', Longitud: {disp.Longitud}");
                    }
                    _logger.LogInformation($"=== FIN DEBUG ===");

                    return new VerificacionResponseDTO
                    {
                        Valido = false,
                        Mensaje = "IMEI no encontrado en el sistema"
                    };
                }

                if (dispositivo.Persona == null)
                {
                    return new VerificacionResponseDTO
                    {
                        Valido = false,
                        Mensaje = "Dispositivo no asignado a una persona"
                    };
                }

                _logger.LogInformation($"✅ IMEI ENCONTRADO - Persona: {dispositivo.Persona.Nombre}, Empresa: {dispositivo.Persona.Empresa?.Nombre}");

                return new VerificacionResponseDTO
                {
                    Valido = true,
                    Mensaje = "IMEI verificado correctamente",
                    Persona = new PersonaDTO
                    {
                        Id = dispositivo.Persona.Id,
                        Nombre = dispositivo.Persona.Nombre,
                        Identificacion = dispositivo.Persona.Identificacion,
                        Telefono = dispositivo.Persona.Telefono
                    },
                    Empresa = new EmpresaDTO
                    {
                        Id = dispositivo.Persona.Empresa!.Id,
                        Nombre = dispositivo.Persona.Empresa.Nombre
                    },
                    Dispositivo = new DispositivoDTO
                    {
                        IMEI = dispositivo.IMEI,
                        FechaRegistro = dispositivo.FechaRegistro
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verificando IMEI: {imei}");
                throw;
            }
        }

        // resto de los métodos se mantienen igual 
        public async Task<Dispositivo> RegistrarDispositivoAsync(RegistrarDispositivoDTO registroDto)
        {
            // Verificar si el IMEI ya existe
            var existe = await _context.Dispositivos
                .AnyAsync(d => d.IMEI == registroDto.IMEI);

            if (existe)
            {
                throw new InvalidOperationException($"El IMEI {registroDto.IMEI} ya está registrado");
            }

            // Verificar que la persona existe
            var persona = await _context.Personas.FindAsync(registroDto.PersonaId);
            if (persona == null)
            {
                throw new KeyNotFoundException($"Persona con ID {registroDto.PersonaId} no encontrada");
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

            return dispositivo;
        }

        public async Task<Persona> RegistrarPersonaAsync(RegistrarPersonaDTO personaDto)
        {
            // Verificar si la identificación ya existe
            var existe = await _context.Personas
                .AnyAsync(p => p.Identificacion == personaDto.Identificacion);

            if (existe)
            {
                throw new InvalidOperationException($"La persona con identificación {personaDto.Identificacion} ya está registrada");
            }

            // Verificar que la empresa existe
            var empresa = await _context.Empresas.FindAsync(personaDto.EmpresaId);
            if (empresa == null)
            {
                throw new KeyNotFoundException($"Empresa con ID {personaDto.EmpresaId} no encontrada");
            }

            var persona = new Persona
            {
                Nombre = personaDto.Nombre,
                Identificacion = personaDto.Identificacion,
                Telefono = personaDto.Telefono,
                EmpresaId = personaDto.EmpresaId
            };

            _context.Personas.Add(persona);
            await _context.SaveChangesAsync();

            return persona;
        }

        public async Task<Empresa> RegistrarEmpresaAsync(RegistrarEmpresaDTO empresaDto)
        {
            var empresa = new Empresa
            {
                Nombre = empresaDto.Nombre,
                FechaCreacion = DateTime.UtcNow,
                Activo = true
            };

            _context.Empresas.Add(empresa);
            await _context.SaveChangesAsync();

            return empresa;
        }

        public async Task<IEnumerable<Dispositivo>> ObtenerDispositivosPorPersonaAsync(int personaId)
        {
            return await _context.Dispositivos
                .Where(d => d.PersonaId == personaId && d.Activo)
                .ToListAsync();
        }

        public async Task<IEnumerable<Persona>> ObtenerPersonasPorEmpresaAsync(int empresaId)
        {
            return await _context.Personas
                .Where(p => p.EmpresaId == empresaId)
                .Include(p => p.Dispositivos)
                .ToListAsync();
        }
    }
}