namespace Sistema_de_Verificación_IMEI.Models
{
    public class Empresa
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
        public bool Activo { get; set; } = true;

        public virtual ICollection<Persona>? Personas { get; set; }
    }
}