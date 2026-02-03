// Models/Persona.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Verificación_IMEI.Models
{
    public class Persona
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Nombre { get; set; } = string.Empty;

        // Mapea explícitamente las columnas
        [Column("identificacion")]
        public string Identificacion { get; set; } = string.Empty;

        [MaxLength(20)]
        [Column("telefono")]
        public string? Telefono { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        [Column("email")]
        public string? Email { get; set; }

        [Column("fechacreacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("empresaid")]
        public int EmpresaId { get; set; }

        public virtual Empresa? Empresa { get; set; }
        public virtual ICollection<Dispositivo>? Dispositivos { get; set; }
    }
}