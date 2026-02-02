namespace Sistema_de_Verificación_IMEI.DTOs
{
    public class LoginRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDTO
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public UserInfoDTO Usuario { get; set; } = new();
    }

    public class UserInfoDTO
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
    }

    public class RegisterRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Rol { get; set; } = "Usuario";
        public int? EmpresaId { get; set; }
    }
    public class UpdateUserDTO
    {
        public string? Rol { get; set; }
        public bool? Activo { get; set; }
        public int? EmpresaId { get; set; }
    }
    public class UserDTO
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Nombre { get; set; }
        public string Rol { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
    }
}