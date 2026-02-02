using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;

namespace Sistema_de_Verificación_IMEI.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginRequestDTO loginDto);
        Task<Usuario> RegisterAsync(RegisterRequestDTO registerDto);
        Task<bool> UserExistsAsync(string username);
        string GenerateJwtToken(Usuario usuario);
    }
}