using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;

namespace Sistema_de_Verificación_IMEI.Services
{
    public interface IVerificacionService
    {
        Task<VerificacionResponseDTO> VerificarIMEIAsync(string imei);
        Task<Dispositivo> RegistrarDispositivoAsync(RegistrarDispositivoDTO registroDto);
        Task<Persona> RegistrarPersonaAsync(RegistrarPersonaDTO personaDto);
        Task<Empresa> RegistrarEmpresaAsync(RegistrarEmpresaDTO empresaDto);
        Task<IEnumerable<Dispositivo>> ObtenerDispositivosPorPersonaAsync(int personaId);
        Task<IEnumerable<Persona>> ObtenerPersonasPorEmpresaAsync(int empresaId);
    }
}