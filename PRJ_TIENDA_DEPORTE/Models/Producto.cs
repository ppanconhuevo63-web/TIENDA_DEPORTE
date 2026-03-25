namespace PRJ_SEMANA_03_S01.Models
{
    public class Producto
    {
        public int idproducto { get; set; }
        public int idcategoria { get; set; }
        public string? nomproducto { get; set; }
        public decimal precio { get; set; }
        public int stock { get; set; }
        public string? estadoproducto { get; set; }
        public string? marca { get; set; }
        public string? talla { get; set; }
        public string? color { get; set; }
        public DateTime fechacreacion { get; set; }

        public string? nomcategoria { get; set; }
    }
}
