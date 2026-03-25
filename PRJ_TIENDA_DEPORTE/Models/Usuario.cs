namespace PRJ_SEMANA_03_S01.Models
{
    public class Usuario
    {
        public int idusuario { get; set; }
        public int? idempl { get; set; }
        public string? username { get; set; }
        public string? password { get; set; }
        public int idrol { get; set; }
        public string? estadousuario { get; set; }
        public DateTime? fechacreacion { get; set; }

        public string? nombreempleado { get; set; }
        public string? nombrerol { get; set; }
    }
}
