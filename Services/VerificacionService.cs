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

        public VerificacionService(ApplicationDbContext context, ILogger<VerificacionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<VerificacionResponseDTO> VerificarIMEIAsync(string imei)
        {
            try
            {
                _logger.LogInformation($"Verificando IMEI: {imei}");

                var dispositivo = await _context.Dispositivos
                    .Include(d => d.Persona)
                        .ThenInclude(p => p!.Empresa)
                    .FirstOrDefaultAsync(d => d.IMEI == imei && d.Activo);

                if (dispositivo == null)
                {
                    return new VerificacionResponseDTO
                    {
                        Valido = false,
                        Mensaje = "IMEI no encontrado o dispositivo inactivo"
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