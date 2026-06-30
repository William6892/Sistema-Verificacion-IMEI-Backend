using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sistema_de_Verificación_IMEI.Models
{
    [Table("historial_escaneos")]
    public class HistorialEscaneo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("imei")]
        [MaxLength(100)]
        public string IMEI { get; set; } = string.Empty;

        [Column("fecha_escaneo")]
        public DateTime FechaEscaneo { get; set; } = DateTime.UtcNow;

        [Column("resultado")]
        public bool Resultado { get; set; }

        [Column("usuario_id")]
        public int? UsuarioId { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        [Column("detalles")]
        [MaxLength(500)]
        public string Detalles { get; set; } = string.Empty;

        // Propiedad de navegación
        [ForeignKey("UsuarioId")]
        public virtual Usuario? Usuario { get; set; }
    }
}
