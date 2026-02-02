using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Services;
using System.Security.Claims;

namespace Sistema_de_Verificación_IMEI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Solo Admin puede acceder
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsersAsync();
                return Ok(users.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Rol,
                    u.Activo,
                    u.EmpresaId,
                    EmpresaNombre = u.Empresa?.Nombre,
                    u.FechaCreacion
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // GET: api/Users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Rol,
                    user.Activo,
                    user.EmpresaId,
                    EmpresaNombre = user.Empresa?.Nombre,
                    user.FechaCreacion
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // PUT: api/Users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDTO updateDto)
        {
            try
            {
                var updatedUser = await _userService.UpdateUserAsync(id, updateDto);

                return Ok(new
                {
                    mensaje = "Usuario actualizado exitosamente",
                    user = new
                    {
                        updatedUser.Id,
                        updatedUser.Username,
                        updatedUser.Rol,
                        updatedUser.Activo,
                        updatedUser.EmpresaId
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }

        // DELETE: api/Users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                await _userService.DeleteUserAsync(id);
                return Ok(new { mensaje = "Usuario eliminado exitosamente" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { mensaje = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { mensaje = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario");
                return StatusCode(500, new { mensaje = "Error interno del servidor" });
            }
        }
    }
}