namespace Sistema_de_Verificación_IMEI.DTOs
{
    public class VerificacionRequestDTO
    {
        public string IMEI { get; set; } = string.Empty;
    }

    public class VerificacionResponseDTO
    {
        public bool Valido { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public PersonaDTO? Persona { get; set; }
        public EmpresaDTO? Empresa { get; set; }
        public DispositivoDTO? Dispositivo { get; set; }
    }

    public class PersonaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Identificacion { get; set; } = string.Empty;
        public string? Telefono { get; set; }

        public int CantidadDispositivos { get; set; }
    }

    public class EmpresaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        public DateTime FechaCreacion { get; set; }
    }

    public class DispositivoDTO
    {
        public string IMEI { get; set; } = string.Empty;
        public DateTime FechaRegistro { get; set; }
    }
}