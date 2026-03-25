namespace PRJ_SEMANA_03_S01.Models
{
    public class DetalleCompra
    {
        public int iddetallecompra { get; set; }
        public int idordencompra { get; set; }
        public int idproducto { get; set; }
        public int cantidad { get; set; }
        public decimal costounitario { get; set; }
        public decimal subtotal { get; set; }

        // para mostrar (JOIN)
        public string? nomproducto { get; set; }
    }
}
