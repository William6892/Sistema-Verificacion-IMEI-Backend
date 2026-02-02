// Services/UserService.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Models;
using System;

namespace Sistema_de_Verificación_IMEI.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(ApplicationDbContext context, ILogger<UserService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Usuario>> GetAllUsersAsync()
        {
            try
            {
                var users = await _context.Usuarios
                    .Include(u => u.Empresa) // Incluir datos de la empresa
                    .OrderBy(u => u.Id)
                    .ToListAsync();

                _logger.LogInformation($"Obtenidos {users.Count} usuarios");
                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener todos los usuarios");
                throw;
            }
        }

        public async Task<Usuario> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Usuarios
                    .Include(u => u.Empresa) // Incluir datos de la empresa
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario con ID {id} no encontrado");
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                _logger.LogInformation($"Usuario encontrado: {user.Username}");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener usuario con ID {id}");
                throw;
            }
        }

        public async Task<Usuario> UpdateUserAsync(int id, UpdateUserDTO updateDto)
        {
            try
            {
                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario con ID {id} no encontrado para actualizar");
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Actualizar solo los campos que se proporcionaron
                if (!string.IsNullOrWhiteSpace(updateDto.Rol))
                {
                    // Validar que el rol sea válido
                    var rolesValidos = new[] { "Admin", "Supervisor", "Usuario" };
                    if (rolesValidos.Contains(updateDto.Rol))
                    {
                        user.Rol = updateDto.Rol;
                        _logger.LogInformation($"Rol actualizado a: {updateDto.Rol}");
                    }
                    else
                    {
                        _logger.LogWarning($"Rol inválido: {updateDto.Rol}");
                        throw new ArgumentException($"Rol inválido. Debe ser: {string.Join(", ", rolesValidos)}");
                    }
                }

                if (updateDto.Activo.HasValue)
                {
                    user.Activo = updateDto.Activo.Value;
                    _logger.LogInformation($"Estado actualizado a: {(updateDto.Activo.Value ? "Activo" : "Inactivo")}");
                }

                if (updateDto.EmpresaId.HasValue)
                {
                    // Verificar que la empresa existe
                    var empresaExiste = await _context.Empresas
                        .AnyAsync(e => e.Id == updateDto.EmpresaId.Value);

                    if (empresaExiste)
                    {
                        user.EmpresaId = updateDto.EmpresaId.Value;
                        _logger.LogInformation($"Empresa ID actualizado a: {updateDto.EmpresaId.Value}");
                    }
                    else
                    {
                        _logger.LogWarning($"Empresa con ID {updateDto.EmpresaId.Value} no encontrada");
                        throw new KeyNotFoundException($"Empresa con ID {updateDto.EmpresaId.Value} no encontrada");
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuario {user.Username} actualizado exitosamente");
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar usuario con ID {id}");
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Usuarios
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    _logger.LogWarning($"Usuario con ID {id} no encontrado para eliminar");
                    throw new KeyNotFoundException($"Usuario con ID {id} no encontrado");
                }

                // Verificar si es el último admin
                if (user.Rol == "Admin")
                {
                    var adminCount = await _context.Usuarios
                        .CountAsync(u => u.Rol == "Admin" && u.Activo);

                    if (adminCount <= 1)
                    {
                        _logger.LogWarning("No se puede eliminar el último administrador activo");
                        throw new InvalidOperationException("No se puede eliminar el último administrador activo");
                    }
                }

                // Verificar si está intentando eliminarse a sí mismo
                var currentUserId = GetCurrentUserId(); // Necesitarás implementar esto
                if (currentUserId.HasValue && currentUserId.Value == id)
                {
                    _logger.LogWarning("No se puede eliminar el propio usuario");
                    throw new InvalidOperationException("No se puede eliminar el propio usuario");
                }

                _context.Usuarios.Remove(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Usuario {user.Username} eliminado exitosamente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al eliminar usuario con ID {id}");
                throw;
            }
        }

        // Método auxiliar para obtener el ID del usuario actual
        private int? GetCurrentUserId()
        {
            // Esto depende de cómo manejes la autenticación
            // Si usas HttpContext, podrías hacer:
            // var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("userId");
            // return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;

            // Por ahora, retornamos null (implementa según tu sistema)
            return null;
        }
    }
}