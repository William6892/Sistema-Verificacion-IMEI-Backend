// Models/Dispositivo.cs
namespace Sistema_de_Verificación_IMEI.Models
{
    public class Dispositivo
    {
        public int Id { get; set; }

        // Columna IMEI normal - guardará el IMEI ENCRIPTADO
        public string IMEI { get; set; } = string.Empty;

        public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;
        public int PersonaId { get; set; }
        public virtual Persona? Persona { get; set; }

        // Elimina TODAS las propiedades [NotMapped] y métodos privados
    }
}