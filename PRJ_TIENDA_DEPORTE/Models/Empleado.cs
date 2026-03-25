namespace PRJ_SEMANA_03_S01.Models
{
    public class Empleado
    {
        public int idempl { get; set; }
        public int idcargo { get; set; }

        public string? nomempl { get; set; }
        public string? apeempl { get; set; }
        public string? dniempl { get; set; }
        public string? estadoempleado { get; set; }

        // Solo para mostrar en el listado (JOIN con Cargo)
        public string? nomcargo { get; set; }
    }
}
