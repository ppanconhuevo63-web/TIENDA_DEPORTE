namespace PRJ_SEMANA_03_S01.Models
{
    public class OrdenCompra
    {
        public int idordencompra { get; set; }
        public int idproveedor { get; set; }
        public int idempl { get; set; }
        public DateTime fechaorden { get; set; }
        public decimal total { get; set; }
        public string? estado { get; set; }

        // para mostrar (JOIN)
        public string? razonsocial { get; set; }
        public string? nomempl { get; set; }
        public string? apeempl { get; set; }
        public string? nomcargo { get; set; }
        public string empleadomostrado => string.IsNullOrWhiteSpace(nomcargo)
            ? $"{nomempl} {apeempl}".Trim()
            : $"{nomempl} {apeempl}({nomcargo})".Trim();
    }
}
