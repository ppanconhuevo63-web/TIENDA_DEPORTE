using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class ProductoController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProductoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private List<Categoria> CargarCategorias()
        {
            List<Categoria> lista = new List<Categoria>();
            string conexion = _configuration.GetConnectionString("ConexionSql")!;

            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from categoria order by nom_categoria";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new Categoria
                    {
                        idcategoria = Convert.ToInt32(dr["id_categoria"]),
                        nomcategoria = dr["nom_categoria"].ToString(),
                        descripcion = dr["descripcion_categoria"]?.ToString(),
                        estadocategoria = dr["estado_categoria"].ToString()
                    });
                }
            }
            return lista;
        }

        public IActionResult Index()
        {
            List<Producto> lista = new List<Producto>();
            string conexion = _configuration.GetConnectionString("ConexionSql")!;

            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = @"SELECT p.id_producto, p.id_categoria, p.nom_producto, p.precio_producto, p.stock_producto,
                                      p.estado_producto, p.marca, p.talla, p.color, p.fecha_creacion, c.nom_categoria
                               FROM productos p
                               INNER JOIN categoria c ON p.id_categoria = c.id_categoria
                               ORDER BY p.id_producto DESC";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new Producto
                    {
                        idproducto = Convert.ToInt32(dr["id_producto"]),
                        idcategoria = Convert.ToInt32(dr["id_categoria"]),
                        nomproducto = dr["nom_producto"].ToString(),
                        precio = Convert.ToDecimal(dr["precio_producto"]),
                        stock = Convert.ToInt32(dr["stock_producto"]),
                        estadoproducto = dr["estado_producto"].ToString(),
                        marca = dr["marca"]?.ToString(),
                        talla = dr["talla"]?.ToString(),
                        color = dr["color"]?.ToString(),
                        fechacreacion = Convert.ToDateTime(dr["fecha_creacion"]),
                        nomcategoria = dr["nom_categoria"].ToString()
                    });
                }
            }
            return View(lista);
        }

        public IActionResult Create()
        {
            ViewBag.Categorias = CargarCategorias();
            return View(new Producto { estadoproducto = "ACTIVO" });
        }

        private void ValidarProducto(Producto obj)
        {
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idcategoria), obj.idcategoria, "una categoría");
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.nomproducto), obj.nomproducto, "nombre del producto", 80, obligatorio: true);
            ValidacionHelper.DecimalPositivo(ModelState, nameof(obj.precio), obj.precio, "precio");
            ValidacionHelper.EnteroPositivo(ModelState, nameof(obj.stock), obj.stock, "stock", permitirCero: true);
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadoproducto), obj.estadoproducto, "un estado");
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.marca), obj.marca, "marca", 45);
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.talla), obj.talla, "talla", 10);
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.color), obj.color, "color", 30);
            // Validar máximo valor 99,999,999.99 (10 dígitos, 2 decimales)
            if (obj.precio > 99999999.99m)
            {
                ModelState.AddModelError(nameof(obj.precio), "El precio no puede superar 99,999,999.99");
            }

            // Validar máximo 2 decimales
            int decimales = BitConverter.GetBytes(decimal.GetBits(obj.precio)[3])[2];
            if (decimales > 2)
            {
                ModelState.AddModelError(nameof(obj.precio), "El precio no debe tener más de 2 decimales");
            }
            if (!string.IsNullOrWhiteSpace(obj.estadoproducto) && obj.estadoproducto.Length > 15)
            {
                ModelState.AddModelError(nameof(obj.estadoproducto), "El estado no puede superar 15 caracteres");
            }
        }

        [HttpPost]
        public IActionResult Create(Producto obj)
        {
            ValidarProducto(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                cn.Open();

                // Validar duplicado
                string sqlValidar = @"SELECT COUNT(*) FROM productos 
                             WHERE nom_producto = @nom AND id_categoria = @idcat";
                using (SqlCommand cmdValidar = new SqlCommand(sqlValidar, cn))
                {
                    cmdValidar.Parameters.AddWithValue("@nom", obj.nomproducto ?? "");
                    cmdValidar.Parameters.AddWithValue("@idcat", obj.idcategoria);

                    int existe = (int)cmdValidar.ExecuteScalar();
                    if (existe > 0)
                    {
                        ModelState.AddModelError("nomproducto", "Ya existe un producto con ese nombre en esta categoría");
                    }
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Categorias = CargarCategorias();
                    return View(obj);
                }

                // Insertar
                string sql = @"INSERT INTO productos(id_categoria, nom_producto, precio_producto, stock_producto, estado_producto, marca, talla, color)
                       VALUES (@idcat,@nom,@precio,@stock,@estado,@marca,@talla,@color)";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@idcat", obj.idcategoria);
                    cmd.Parameters.AddWithValue("@nom", obj.nomproducto ?? "");
                    cmd.Parameters.AddWithValue("@precio", obj.precio);
                    cmd.Parameters.AddWithValue("@stock", obj.stock);
                    cmd.Parameters.AddWithValue("@estado", obj.estadoproducto ?? "ACTIVO");
                    cmd.Parameters.AddWithValue("@marca", (object?)obj.marca ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@talla", (object?)obj.talla ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@color", (object?)obj.color ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
        public IActionResult Edit(int id)
        {
            Producto? obj = null;
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from productos where id_producto=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    obj = new Producto
                    {
                        idproducto = Convert.ToInt32(dr["id_producto"]),
                        idcategoria = Convert.ToInt32(dr["id_categoria"]),
                        nomproducto = dr["nom_producto"].ToString(),
                        precio = Convert.ToDecimal(dr["precio_producto"]),
                        stock = Convert.ToInt32(dr["stock_producto"]),
                        estadoproducto = dr["estado_producto"].ToString(),
                        marca = dr["marca"]?.ToString(),
                        talla = dr["talla"]?.ToString(),
                        color = dr["color"]?.ToString(),
                        fechacreacion = Convert.ToDateTime(dr["fecha_creacion"])
                    };
                }
            }

            if (obj == null) return NotFound();
            ViewBag.Categorias = CargarCategorias();
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Producto obj)
        {
            ValidarProducto(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                cn.Open();

                // Validar duplicado (excluyendo el producto actual)
                string sqlValidar = @"SELECT COUNT(*) FROM productos 
                             WHERE nom_producto = @nom 
                             AND id_categoria = @idcat 
                             AND id_producto != @id";
                using (SqlCommand cmdValidar = new SqlCommand(sqlValidar, cn))
                {
                    cmdValidar.Parameters.AddWithValue("@nom", obj.nomproducto ?? "");
                    cmdValidar.Parameters.AddWithValue("@idcat", obj.idcategoria);
                    cmdValidar.Parameters.AddWithValue("@id", obj.idproducto);

                    int existe = (int)cmdValidar.ExecuteScalar();
                    if (existe > 0)
                    {
                        ModelState.AddModelError("nomproducto", "Ya existe otro producto con ese nombre en esta categoría");
                    }
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Categorias = CargarCategorias();
                    return View(obj);
                }

                // Actualizar
                string sql = @"UPDATE productos
                       SET id_categoria=@idcat,
                           nom_producto=@nom,
                           precio_producto=@precio,
                           stock_producto=@stock,
                           estado_producto=@estado,
                           marca=@marca,
                           talla=@talla,
                           color=@color
                       WHERE id_producto=@id";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@id", obj.idproducto);
                    cmd.Parameters.AddWithValue("@idcat", obj.idcategoria);
                    cmd.Parameters.AddWithValue("@nom", obj.nomproducto ?? "");
                    cmd.Parameters.AddWithValue("@precio", obj.precio);
                    cmd.Parameters.AddWithValue("@stock", obj.stock);
                    cmd.Parameters.AddWithValue("@estado", obj.estadoproducto ?? "ACTIVO");
                    cmd.Parameters.AddWithValue("@marca", (object?)obj.marca ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@talla", (object?)obj.talla ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@color", (object?)obj.color ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "delete from productos where id_producto=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
    }
}
