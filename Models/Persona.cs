namespace Sistema_de_Verificación_IMEI.Models
{
    public class Persona
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int EmpresaId { get; set; }

        public virtual Empresa? Empresa { get; set; }
        public virtual ICollection<Dispositivo>? Dispositivos { get; set; }
    }
}