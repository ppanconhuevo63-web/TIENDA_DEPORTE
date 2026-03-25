using System.ComponentModel.DataAnnotations;

namespace PRJ_SEMANA_03_S01.Models
{
    public class UsuarioLogin
    {
        [Required]
        public string? username { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? password { get; set; }

        public int? idempl { get; set; }
        public string? cargo { get; set; }
        public string? nombrecompleto { get; set; }
        public string? rol { get; set; }
    }
}
