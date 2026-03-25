namespace PRJ_SEMANA_03_S01.Models
{
    public class Kardex
    {
        public int idkardex { get; set; }
        public int idproducto { get; set; }
        public DateTime fechamov { get; set; }
        public string? tipomov { get; set; }
        public int cantidad { get; set; }
        public decimal costounitario { get; set; }
        public string? nfactura { get; set; }
        public string? observacion { get; set; }

        public string? nomproducto { get; set; }
    }
}
