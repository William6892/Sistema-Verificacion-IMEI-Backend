using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Verificación_IMEI.DTOs
{
    // DTO para creación normal de dispositivos
    public class RegistrarDispositivoDTO
    {
        [Required(ErrorMessage = "El IMEI es requerido")]
        [StringLength(15, MinimumLength = 15, ErrorMessage = "El IMEI debe tener 15 dígitos")]
        [RegularExpression(@"^\d{15}$", ErrorMessage = "El IMEI debe contener solo números")]
        public string IMEI { get; set; } = string.Empty;

        [Required(ErrorMessage = "El ID de la persona es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de persona inválido")]
        public int PersonaId { get; set; }
    }

    // DTO específico para admin (puede tener validaciones diferentes)
    public class RegistrarDispositivoAdminDTO : RegistrarDispositivoDTO
    {
        public bool? Activo { get; set; }
    }

    public class RegistrarPersonaDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "La identificación es requerida")]
        [StringLength(20, MinimumLength = 5, ErrorMessage = "La identificación debe tener entre 5 y 20 caracteres")]
        public string Identificacion { get; set; } = string.Empty;

        [StringLength(15, ErrorMessage = "El teléfono no debe exceder 15 caracteres")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string? Telefono { get; set; }

        // AGREGAR ESTA PROPIEDAD
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        [MaxLength(100, ErrorMessage = "El email no debe exceder 100 caracteres")]
        public string? Email { get; set; } 

        [Required(ErrorMessage = "El ID de empresa es requerido")]
        [Range(1, int.MaxValue, ErrorMessage = "ID de empresa inválido")]
        public int EmpresaId { get; set; }
    }

    public class RegistrarEmpresaDTO
    {
        [Required(ErrorMessage = "El nombre de la empresa es requerido")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;
    }

    public class UpdateDispositivoDTO
    {
        [Range(1, int.MaxValue, ErrorMessage = "ID de persona inválido")]
        public int? PersonaId { get; set; }

        public bool? Activo { get; set; }
    }

    // ===== DTOs PARA RESPUESTAS =====

    // Respuesta básica sin IMEI en texto plano
    public class DispositivoResponseDTO
    {
        public int Id { get; set; }
        public string IMEIMasked { get; set; } = string.Empty; // Ej: "351234*******89"
        public int PersonaId { get; set; }
        public string PersonaNombre { get; set; } = string.Empty;
        public string EmpresaNombre { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }

    // Respuesta solo para admin (con IMEI completo)
    public class DispositivoAdminResponseDTO : DispositivoResponseDTO
    {
        public string IMEI { get; set; } = string.Empty; // Solo para admins
    }

    // DTO para búsqueda
    public class BuscarDispositivoDTO
    {
        [StringLength(15, ErrorMessage = "El IMEI debe tener 15 dígitos")]
        public string? IMEI { get; set; }

        public int? PersonaId { get; set; }
        public int? EmpresaId { get; set; }
        public bool? Activo { get; set; }
    }
}