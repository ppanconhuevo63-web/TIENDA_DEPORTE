using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Helpers;
using PRJ_SEMANA_03_S01.Models;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR,CAJERO")]
    public class DetalleVentaController : Controller
    {
        private readonly IConfiguration _configuration;

        public DetalleVentaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string Conexion => _configuration.GetConnectionString("ConexionSql")!;

        private List<Venta> CargarVentas()
        {
            List<Venta> lista = new List<Venta>();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = "select id_venta from orden_venta order by id_venta desc";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new Venta { idventa = Convert.ToInt32(dr["id_venta"]) });
            return lista;
        }

        private List<Producto> CargarProductos()
        {
            List<Producto> lista = new List<Producto>();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = "select id_producto, nom_producto, precio_producto from productos where estado_producto='ACTIVO' order by nom_producto";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Producto
                {
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    nomproducto = dr["nom_producto"].ToString(),
                    precio = Convert.ToDecimal(dr["precio_producto"])
                });
            }
            return lista;
        }

        private decimal ObtenerPrecioProducto(int idProducto)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select precio_producto from productos where id_producto=@id", cn);
            cmd.Parameters.AddWithValue("@id", idProducto);
            cn.Open();
            object? valor = cmd.ExecuteScalar();
            return valor == null ? 0 : Convert.ToDecimal(valor);
        }

        private void ActualizarTotalVenta(int idVenta, SqlConnection cn, SqlTransaction tx)
        {
            string sql = @"UPDATE orden_venta
                           SET total_venta = ISNULL((SELECT SUM(subtotal) FROM detalle_venta WHERE id_venta=@idventa),0)
                           WHERE id_venta=@idventa";
            SqlCommand cmd = new SqlCommand(sql, cn, tx);
            cmd.Parameters.AddWithValue("@idventa", idVenta);
            cmd.ExecuteNonQuery();
        }

        private void PrepararCombos(int? idProducto = null, int? idVenta = null)
        {
            ViewBag.Ventas = new SelectList(CargarVentas(), "idventa", "idventa", idVenta);
            ViewBag.Productos = new SelectList(CargarProductos(), "idproducto", "nomproducto", idProducto);
        }

        private void ValidarDetalleVenta(DetalleVenta obj)
        {
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idventa), obj.idventa, "una venta");
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idproducto), obj.idproducto, "un producto");
            ValidacionHelper.EnteroPositivo(ModelState, nameof(obj.cantidad), obj.cantidad, "cantidad");
            ValidacionHelper.DecimalPositivo(ModelState, nameof(obj.preciounitario), obj.preciounitario, "precio unitario");
            // 🔥 Validar límite de DECIMAL(10,2)
            if (obj.preciounitario > 99999999.99m)
            {
                ModelState.AddModelError(nameof(obj.preciounitario),
                    "El precio unitario excede el valor máximo permitido (99,999,999.99).");
            }

            // 🔥 Validar máximo 2 decimales
            decimal valor = obj.preciounitario;
            int decimales = BitConverter.GetBytes(decimal.GetBits(valor)[3])[2];

            if (decimales > 2)
            {
                ModelState.AddModelError(nameof(obj.preciounitario),
                    "El precio unitario no debe tener más de 2 decimales.");
            }
        }

        public IActionResult Index()
        {
            List<DetalleVenta> lista = new List<DetalleVenta>();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT dv.id_detalle_venta, dv.id_venta, dv.id_producto, dv.cantidad, dv.precio_unitario, dv.subtotal, p.nom_producto
                           FROM detalle_venta dv
                           INNER JOIN productos p ON dv.id_producto = p.id_producto
                           ORDER BY dv.id_detalle_venta DESC";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new DetalleVenta
                {
                    iddetalleventa = Convert.ToInt32(dr["id_detalle_venta"]),
                    idventa = Convert.ToInt32(dr["id_venta"]),
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    cantidad = Convert.ToInt32(dr["cantidad"]),
                    preciounitario = Convert.ToDecimal(dr["precio_unitario"]),
                    subtotal = Convert.ToDecimal(dr["subtotal"]),
                    nomproducto = dr["nom_producto"].ToString()
                });
            }
            return View(lista);
        }

        public IActionResult Create()
        {
            PrepararCombos();
            return View(new DetalleVenta { cantidad = 1 });
        }

        [HttpPost]
        public IActionResult Create(DetalleVenta obj)
        {
            ValidarDetalleVenta(obj); // Ejecuta tu método de validación

            if (!ModelState.IsValid)
            {
                PrepararCombos(obj.idproducto, obj.idventa);
                return View(obj);
            }

            if (obj.preciounitario <= 0) obj.preciounitario = ObtenerPrecioProducto(obj.idproducto);
            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                string sql = @"INSERT INTO detalle_venta(id_venta, id_producto, cantidad, precio_unitario)
                               VALUES (@idventa, @idprod, @cant, @precio)";
                SqlCommand cmd = new SqlCommand(sql, cn, tx);
                cmd.Parameters.AddWithValue("@idventa", obj.idventa);
                cmd.Parameters.AddWithValue("@idprod", obj.idproducto);
                cmd.Parameters.AddWithValue("@cant", obj.cantidad);
                cmd.Parameters.AddWithValue("@precio", obj.preciounitario);
                cmd.ExecuteNonQuery();
                ActualizarTotalVenta(obj.idventa, cn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            DetalleVenta? obj = null;
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select * from detalle_venta where id_detalle_venta=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                obj = new DetalleVenta
                {
                    iddetalleventa = Convert.ToInt32(dr["id_detalle_venta"]),
                    idventa = Convert.ToInt32(dr["id_venta"]),
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    cantidad = Convert.ToInt32(dr["cantidad"]),
                    preciounitario = Convert.ToDecimal(dr["precio_unitario"]),
                    subtotal = Convert.ToDecimal(dr["subtotal"])
                };
            }
            if (obj == null) return NotFound();
            PrepararCombos(obj.idproducto, obj.idventa);
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(DetalleVenta obj)
        {
            ValidarDetalleVenta(obj);
            if (!ModelState.IsValid)
            {
                PrepararCombos(obj.idproducto, obj.idventa);
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                string sql = @"UPDATE detalle_venta
                               SET id_venta=@idventa,
                                   id_producto=@idprod,
                                   cantidad=@cant,
                                   precio_unitario=@precio
                               WHERE id_detalle_venta=@id";
                SqlCommand cmd = new SqlCommand(sql, cn, tx);
                cmd.Parameters.AddWithValue("@id", obj.iddetalleventa);
                cmd.Parameters.AddWithValue("@idventa", obj.idventa);
                cmd.Parameters.AddWithValue("@idprod", obj.idproducto);
                cmd.Parameters.AddWithValue("@cant", obj.cantidad);
                cmd.Parameters.AddWithValue("@precio", obj.preciounitario);
                cmd.ExecuteNonQuery();
                ActualizarTotalVenta(obj.idventa, cn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                SqlCommand cmdVenta = new SqlCommand("select id_venta from detalle_venta where id_detalle_venta=@id", cn, tx);
                cmdVenta.Parameters.AddWithValue("@id", id);
                int idVenta = Convert.ToInt32(cmdVenta.ExecuteScalar());

                SqlCommand cmd = new SqlCommand("delete from detalle_venta where id_detalle_venta=@id", cn, tx);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();

                ActualizarTotalVenta(idVenta, cn, tx);
                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
            return RedirectToAction("Index");
        }
    }
}
