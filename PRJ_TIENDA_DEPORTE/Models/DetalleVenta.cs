namespace PRJ_SEMANA_03_S01.Models
{
    public class DetalleVenta
    {
        public int iddetalleventa { get; set; }
        public int idventa { get; set; }
        public int idproducto { get; set; }
        public int cantidad { get; set; }
        public decimal preciounitario { get; set; }
        public decimal subtotal { get; set; }

        public string? nomproducto { get; set; }
    }
}
