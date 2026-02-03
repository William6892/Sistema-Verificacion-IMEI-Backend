// Models/Persona.cs
using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Verificación_IMEI.Models
{
    public class Persona
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        // SOLO ESTA COLUMNA existe en tu BD
        public string Identificacion { get; set; } = string.Empty; // Aquí guardarás ENCRIPTADO

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string? Email { get; set; }

        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
        public int EmpresaId { get; set; }

        public virtual Empresa? Empresa { get; set; }
        public virtual ICollection<Dispositivo>? Dispositivos { get; set; }
    }
}