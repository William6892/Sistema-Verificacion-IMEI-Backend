using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;

namespace Sistema_de_Verificación_IMEI.Services
{
    public interface IUserService
    {
        Task<IEnumerable<Usuario>> GetAllUsersAsync();
        Task<Usuario> GetUserByIdAsync(int id);
        Task<Usuario> UpdateUserAsync(int id, UpdateUserDTO updateDto);
        Task<bool> DeleteUserAsync(int id);
    }
}