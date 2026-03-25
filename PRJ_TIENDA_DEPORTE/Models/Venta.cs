namespace PRJ_SEMANA_03_S01.Models
{
    public class Venta
    {
        public int idventa { get; set; }
        public int idcliente { get; set; }
        public int idempl { get; set; }
        public DateTime fechaventa { get; set; }
        public decimal total { get; set; }
        public string? metodopago { get; set; }
        public string? estadoventa { get; set; }

        public string? nomcliente { get; set; }
        public string? apecliente { get; set; }
        public string? nomempl { get; set; }
        public string? apeempl { get; set; }
        public string? nomcargo { get; set; }
    }
}
