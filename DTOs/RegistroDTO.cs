namespace Sistema_de_Verificación_IMEI.DTOs
{
    public class RegistrarDispositivoDTO
    {
        public string IMEI { get; set; } = string.Empty;
        public int PersonaId { get; set; }
    }

    public class RegistrarPersonaDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public int EmpresaId { get; set; }
    }

    public class RegistrarEmpresaDTO
    {
        public string Nombre { get; set; } = string.Empty;
    }
    public class RegistrarDispositivoAdminDTO
    {
        public string IMEI { get; set; } = string.Empty;
        public int PersonaId { get; set; }
    }
    public class UpdateDispositivoDTO
    {
        public int? PersonaId { get; set; }
        public bool? Activo { get; set; }
    }
}