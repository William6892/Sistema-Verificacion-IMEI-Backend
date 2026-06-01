public class CargaMasivaRequest
{
    public List<CargaFilaDTO> Datos { get; set; }
}

public class CargaFilaDTO
{
    public string Empresa { get; set; }
    public string NombresYApellidos { get; set; }
    public string Cedula { get; set; }
    public string IMEI01 { get; set; }
    public string IMEI02 { get; set; }
}

public class PersonaRequest
{
    public string Cedula { get; set; }
    public string Nombre { get; set; }
    public int EmpresaId { get; set; }
}