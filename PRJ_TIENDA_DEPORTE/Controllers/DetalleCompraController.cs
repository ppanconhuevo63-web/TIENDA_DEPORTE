using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Helpers;
using PRJ_SEMANA_03_S01.Models;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class DetalleCompraController : Controller
    {
        private readonly IConfiguration _configuration;
        public DetalleCompraController(IConfiguration configuration) => _configuration = configuration;
        private string Conexion => _configuration.GetConnectionString("ConexionSql")!;

        private List<OrdenCompra> CargarOrdenes()
        {
            List<OrdenCompra> lista = new();
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select id_orden_compra from orden_compra order by id_orden_compra desc", cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new OrdenCompra { idordencompra = Convert.ToInt32(dr["id_orden_compra"]) });
            return lista;
        }

        private List<Producto> CargarProductos()
        {
            List<Producto> lista = new();
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select id_producto, nom_producto from productos order by nom_producto", cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new Producto { idproducto = Convert.ToInt32(dr["id_producto"]), nomproducto = dr["nom_producto"].ToString() });
            return lista;
        }

        private void ActualizarTotal(int idOrden, SqlConnection cn, SqlTransaction tx)
        {
            SqlCommand cmd = new SqlCommand(@"UPDATE orden_compra SET total_orden = ISNULL((SELECT SUM(subtotal) FROM detalle_compra WHERE id_orden_compra=@id),0) WHERE id_orden_compra=@id", cn, tx);
            cmd.Parameters.AddWithValue("@id", idOrden);
            cmd.ExecuteNonQuery();
        }

        private void PrepararCombos(int? idProducto = null, int? idOrden = null)
        {
            ViewBag.Ordenes = new SelectList(CargarOrdenes(), "idordencompra", "idordencompra", idOrden);
            ViewBag.Productos = new SelectList(CargarProductos(), "idproducto", "nomproducto", idProducto);
        }

        private void ValidarDetalleCompra(DetalleCompra obj)
        {
            // Usamos el Helper para validar
            ValidacionHelper.Seleccion(ModelState, "idordencompra", obj.idordencompra, "una orden de compra");
            ValidacionHelper.Seleccion(ModelState, "idproducto", obj.idproducto, "un producto");
            ValidacionHelper.EnteroPositivo(ModelState, "cantidad", obj.cantidad, "cantidad");

            // Aquí validamos que el costo sea mayor a 0 (permitirCero = false)
            ValidacionHelper.DecimalPositivo(ModelState, "costounitario", obj.costounitario, "costo unitario", false);
            // 🔥 Validar límite de DECIMAL(10,2)
            if (obj.costounitario > 99999999.99m)
            {
                ModelState.AddModelError(nameof(obj.costounitario),
                    "El precio unitario excede el valor máximo permitido (99,999,999.99).");
            }

            // 🔥 Validar máximo 2 decimales
            decimal valor = obj.costounitario;
            int decimales = BitConverter.GetBytes(decimal.GetBits(valor)[3])[2];

            if (decimales > 2)
            {
                ModelState.AddModelError(nameof(obj.costounitario),
                    "El precio unitario no debe tener más de 2 decimales.");
            }
        }

        public IActionResult Index()
        {
            List<DetalleCompra> lista = new();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT dc.id_detalle_compra, dc.id_orden_compra, dc.id_producto, dc.cantidad, dc.costo_unitario, dc.subtotal, p.nom_producto
                           FROM detalle_compra dc
                           INNER JOIN productos p ON dc.id_producto = p.id_producto
                           ORDER BY dc.id_detalle_compra DESC";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new DetalleCompra
                {
                    iddetallecompra = Convert.ToInt32(dr["id_detalle_compra"]),
                    idordencompra = Convert.ToInt32(dr["id_orden_compra"]),
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    cantidad = Convert.ToInt32(dr["cantidad"]),
                    costounitario = Convert.ToDecimal(dr["costo_unitario"]),
                    subtotal = Convert.ToDecimal(dr["subtotal"]),
                    nomproducto = dr["nom_producto"].ToString()
                });
            }
            return View(lista);
        }

        public IActionResult Create()
        {
            PrepararCombos();
            return View(new DetalleCompra { cantidad = 1 });
        }

        [HttpPost]
        public IActionResult Create(DetalleCompra obj)
        {
            ValidarDetalleCompra(obj);

            if (!ModelState.IsValid)
            {
                PrepararCombos(obj.idproducto, obj.idordencompra);
                return View(obj);
            }


            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            SqlCommand cmd = new SqlCommand(@"INSERT INTO detalle_compra(id_orden_compra, id_producto, cantidad, costo_unitario) VALUES (@idoc,@idprod,@cant,@costo)", cn, tx);
            cmd.Parameters.AddWithValue("@idoc", obj.idordencompra);
            cmd.Parameters.AddWithValue("@idprod", obj.idproducto);
            cmd.Parameters.AddWithValue("@cant", obj.cantidad);
            cmd.Parameters.AddWithValue("@costo", obj.costounitario);
            cmd.ExecuteNonQuery();
            ActualizarTotal(obj.idordencompra, cn, tx);
            tx.Commit();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {

            DetalleCompra? obj = null;
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select * from detalle_compra where id_detalle_compra=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                obj = new DetalleCompra
                {
                    iddetallecompra = Convert.ToInt32(dr["id_detalle_compra"]),
                    idordencompra = Convert.ToInt32(dr["id_orden_compra"]),
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    cantidad = Convert.ToInt32(dr["cantidad"]),
                    costounitario = Convert.ToDecimal(dr["costo_unitario"]),
                    subtotal = Convert.ToDecimal(dr["subtotal"])
                };
            }
            if (obj == null) return NotFound();
            PrepararCombos(obj.idproducto, obj.idordencompra);
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(DetalleCompra obj)
        {
            ValidarDetalleCompra(obj);

            if (!ModelState.IsValid)
            {
                PrepararCombos(obj.idproducto, obj.idordencompra);
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            SqlCommand cmd = new SqlCommand(@"UPDATE detalle_compra SET id_orden_compra=@idoc, id_producto=@idprod, cantidad=@cant, costo_unitario=@costo WHERE id_detalle_compra=@id", cn, tx);
            cmd.Parameters.AddWithValue("@id", obj.iddetallecompra);
            cmd.Parameters.AddWithValue("@idoc", obj.idordencompra);
            cmd.Parameters.AddWithValue("@idprod", obj.idproducto);
            cmd.Parameters.AddWithValue("@cant", obj.cantidad);
            cmd.Parameters.AddWithValue("@costo", obj.costounitario);
            cmd.ExecuteNonQuery();
            ActualizarTotal(obj.idordencompra, cn, tx);
            tx.Commit();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            SqlCommand cmdId = new SqlCommand("select id_orden_compra from detalle_compra where id_detalle_compra=@id", cn, tx);
            cmdId.Parameters.AddWithValue("@id", id);
            int idOrden = Convert.ToInt32(cmdId.ExecuteScalar());
            SqlCommand cmd = new SqlCommand("delete from detalle_compra where id_detalle_compra=@id", cn, tx);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
            ActualizarTotal(idOrden, cn, tx);
            tx.Commit();
            return RedirectToAction("Index");
        }
    }
}
