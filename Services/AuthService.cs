using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sistema_de_Verificación_IMEI.Data;
using Sistema_de_Verificación_IMEI.DTOs;
using Sistema_de_Verificación_IMEI.Helpers;
using Sistema_de_Verificación_IMEI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sistema_de_Verificación_IMEI.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            ApplicationDbContext context,
            IOptions<JwtSettings> jwtSettings,
            ILogger<AuthService> logger)
        {
            _context = context;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO loginDto)
        {
            try
            {
                _logger.LogInformation($"Intento de login para usuario: {loginDto.Username}");

                // Buscar usuario
                var usuario = await _context.Usuarios
                    .Include(u => u.Empresa)
                    .FirstOrDefaultAsync(u => u.Username == loginDto.Username && u.Activo);

                if (usuario == null)
                {
                    _logger.LogWarning($"Usuario no encontrado: {loginDto.Username}");
                    return new LoginResponseDTO
                    {
                        Success = false,
                        Mensaje = "Usuario o contraseña incorrectos"
                    };
                }

                // Verificar contraseña
                if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, usuario.PasswordHash))
                {
                    _logger.LogWarning($"Contraseña incorrecta para usuario: {loginDto.Username}");
                    return new LoginResponseDTO
                    {
                        Success = false,
                        Mensaje = "Usuario o contraseña incorrectos"
                    };
                }

                // Generar token JWT
                var token = GenerateJwtToken(usuario);

                _logger.LogInformation($"Login exitoso para usuario: {loginDto.Username}");

                return new LoginResponseDTO
                {
                    Success = true,
                    Token = token,
                    Mensaje = "Login exitoso",
                    Usuario = new UserInfoDTO
                    {
                        Id = usuario.Id,
                        Username = usuario.Username,
                        Rol = usuario.Rol,
                        EmpresaId = usuario.EmpresaId,
                        EmpresaNombre = usuario.Empresa?.Nombre
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error en login para usuario: {loginDto.Username}");
                throw;
            }
        }

        public async Task<Usuario> RegisterAsync(RegisterRequestDTO registerDto)
        {
            // Verificar si el usuario ya existe
            if (await UserExistsAsync(registerDto.Username))
            {
                throw new InvalidOperationException($"El usuario '{registerDto.Username}' ya existe");
            }

            // Verificar empresa si se especifica
            if (registerDto.EmpresaId.HasValue)
            {
                var empresa = await _context.Empresas.FindAsync(registerDto.EmpresaId.Value);
                if (empresa == null)
                {
                    throw new KeyNotFoundException($"Empresa con ID {registerDto.EmpresaId} no encontrada");
                }
            }

            // Hashear contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            var usuario = new Usuario
            {
                Username = registerDto.Username,
                PasswordHash = passwordHash,
                Rol = registerDto.Rol,
                EmpresaId = registerDto.EmpresaId,
                Activo = true,
                FechaCreacion = DateTime.UtcNow
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Usuario registrado: {registerDto.Username}");

            return usuario;
        }

        public async Task<bool> UserExistsAsync(string username)
        {
            return await _context.Usuarios.AnyAsync(u => u.Username == username);
        }

        public string GenerateJwtToken(Usuario usuario)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            // CLAIMS CORRECTAS PARA .NET
            var claims = new List<Claim>
    {
        new Claim(JwtRegisteredClaimNames.Sub, usuario.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim("userId", usuario.Id.ToString()),
        new Claim("rol", usuario.Rol), // Para frontend
        // ¡¡IMPORTANTE!! Necesitas ClaimTypes.Role para [Authorize(Roles = "...")]
        new Claim(ClaimTypes.Role, usuario.Rol), // ← ¡¡AGREGA ESTA LÍNEA!!
        new Claim("empresaId", usuario.EmpresaId?.ToString() ?? "0")
    };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtSettings.ExpireHours),
                signingCredentials: credentials
            );

            _logger.LogInformation($"Token generado para usuario: {usuario.Username}, Rol: {usuario.Rol}");

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}