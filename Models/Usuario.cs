using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Verificación_IMEI.Models
{
    [Table("usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("rol")]
        [MaxLength(50)]
        public string Rol { get; set; } = "Usuario"; // Admin, Usuario

        [Column("empresa_id")]  // ← IMPORTANTE: mismo nombre que en la BD
        public int? EmpresaId { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        // Navegación
        [ForeignKey("EmpresaId")]
        public virtual Empresa? Empresa { get; set; }
    }
}