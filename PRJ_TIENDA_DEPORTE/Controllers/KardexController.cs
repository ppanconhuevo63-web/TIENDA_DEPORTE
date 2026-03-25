using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Helpers;
using PRJ_SEMANA_03_S01.Models;
using System.Text.RegularExpressions;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class KardexController : Controller
    {
        private readonly IConfiguration _configuration;
        public KardexController(IConfiguration configuration) => _configuration = configuration;

        private List<Producto> CargarProductos()
        {
            List<Producto> lista = new List<Producto>();
            using SqlConnection cn = new SqlConnection(_configuration.GetConnectionString("ConexionSql")!);
            SqlCommand cmd = new SqlCommand("select id_producto, nom_producto from productos order by nom_producto", cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Producto { idproducto = Convert.ToInt32(dr["id_producto"]), nomproducto = dr["nom_producto"].ToString() });
            }
            return lista;
        }

        private void ValidarKardex(Kardex obj)
        {
            // 1. Producto (Obligatorio)
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idproducto), obj.idproducto, "un producto");

            // 2. Tipo de Movimiento (SQL es VARCHAR(10))
            // Bajamos de 20 a 10 para que coincida con la base de datos
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.tipomov), obj.tipomov, "un tipo de movimiento", 10);

            // 3. Cantidad y Costo (Obligatorios y positivos)
            ValidacionHelper.EnteroPositivo(ModelState, nameof(obj.cantidad), obj.cantidad, "cantidad");
            ValidacionHelper.DecimalPositivo(ModelState, nameof(obj.costounitario), obj.costounitario, "costo unitario");

            // 4. N_factura (SQL es VARCHAR(60) y permite NULL)
            // Eliminamos la línea de 30 y dejamos solo la de 60. "false" significa que no es obligatorio.
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.nfactura), obj.nfactura, "número de factura", 60, false);

            // 5. Observación (SQL es VARCHAR(200))
            // Subimos de 150 a 200 para aprovechar el espacio de tu tabla
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.observacion), obj.observacion, "observación", 200, false);
            if (obj.cantidad > 2147483647)
                ModelState.AddModelError(nameof(obj.cantidad), "La cantidad no puede superar 2,147,483,647.");
            if (obj.costounitario > 99999999.99m)
                ModelState.AddModelError(nameof(obj.costounitario), "El costo unitario no puede superar 99,999,999.99.");
            int decimales = BitConverter.GetBytes(decimal.GetBits(obj.costounitario)[3])[2];
            if (decimales > 2)
                ModelState.AddModelError(nameof(obj.costounitario), "El costo unitario no debe tener más de 2 decimales.");
            if (obj.tipomov != "ENTRADA" && obj.tipomov != "SALIDA")
                ModelState.AddModelError(nameof(obj.tipomov), "El tipo de movimiento debe ser ENTRADA o SALIDA.");
        }

        public IActionResult Index()
        {
            List<Kardex> lista = new List<Kardex>();
            using SqlConnection cn = new SqlConnection(_configuration.GetConnectionString("ConexionSql")!);
            string sql = @"SELECT k.id_kardex, k.id_producto, k.fecha_mov, k.tipo_mov, k.cantidad, k.costo_unitario, k.N_factura, k.observacion, p.nom_producto
                           FROM kardex k
                           INNER JOIN productos p ON k.id_producto = p.id_producto
                           ORDER BY k.id_kardex DESC";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Kardex
                {
                    idkardex = Convert.ToInt32(dr["id_kardex"]),
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    fechamov = Convert.ToDateTime(dr["fecha_mov"]),
                    tipomov = dr["tipo_mov"].ToString(),
                    cantidad = Convert.ToInt32(dr["cantidad"]),
                    costounitario = Convert.ToDecimal(dr["costo_unitario"]),
                    nfactura = dr["N_factura"]?.ToString(),
                    observacion = dr["observacion"]?.ToString(),
                    nomproducto = dr["nom_producto"].ToString()
                });
            }
            return View(lista);
        }

        public IActionResult Create()
        {
            ViewBag.Productos = CargarProductos();
            return View(new Kardex { fechamov = DateTime.Now, tipomov = "ENTRADA" });
        }

        [HttpPost]
        public IActionResult Create(Kardex obj)
        {
            ValidarKardex(obj);
            if (!string.IsNullOrWhiteSpace(obj.nfactura) && !Regex.IsMatch(obj.nfactura, @"^[A-Z0-9-]+$"))
            {
                ModelState.AddModelError(nameof(obj.nfactura), "El formato de factura no es válido (solo letras, números y guiones).");
            }
            if (obj.tipomov != "ENTRADA" && obj.tipomov != "SALIDA")
            {
                ModelState.AddModelError(nameof(obj.tipomov), "Debe seleccionar ENTRADA o SALIDA.");
            }
            if (!ModelState.IsValid) { ViewBag.Productos = CargarProductos(); return View(obj); }

            using SqlConnection cn = new SqlConnection(_configuration.GetConnectionString("ConexionSql")!);
            string sql = @"INSERT INTO kardex(id_producto, fecha_mov, tipo_mov, cantidad, costo_unitario, N_factura, observacion)
                           VALUES (@idprod, @fecha, @tipo, @cant, @costo, @fact, @obs)";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idprod", obj.idproducto);
            cmd.Parameters.AddWithValue("@fecha", obj.fechamov == default ? DateTime.Now : obj.fechamov);
            cmd.Parameters.AddWithValue("@tipo", obj.tipomov ?? "ENTRADA");
            cmd.Parameters.AddWithValue("@cant", obj.cantidad);
            cmd.Parameters.AddWithValue("@costo", obj.costounitario);
            cmd.Parameters.AddWithValue("@fact", (object?)obj.nfactura ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@obs", (object?)obj.observacion ?? DBNull.Value);
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Kardex? obj = null;
            using SqlConnection cn = new SqlConnection(_configuration.GetConnectionString("ConexionSql")!);
            SqlCommand cmd = new SqlCommand("select * from kardex where id_kardex=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                obj = new Kardex
                {
                    idkardex = Convert.ToInt32(dr["id_kardex"]),
                    idproducto = Convert.ToInt32(dr["id_producto"]),
                    fechamov = Convert.ToDateTime(dr["fecha_mov"]),
                    tipomov = dr["tipo_mov"].ToString(),
                    cantidad = Convert.ToInt32(dr["cantidad"]),
                    costounitario = Convert.ToDecimal(dr["costo_unitario"]),
                    nfactura = dr["N_factura"]?.ToString(),
                    observacion = dr["observacion"]?.ToString()
                };
            }
            if (obj == null) return NotFound();
            ViewBag.Productos = CargarProductos();
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Kardex obj)
        {
            ValidarKardex(obj);
            if (!ModelState.IsValid) { ViewBag.Productos = CargarProductos(); return View(obj); }

            using SqlConnection cn = new SqlConnection(_configuration.GetConnectionString("ConexionSql")!);
            string sql = @"UPDATE kardex
                           SET id_producto=@idprod, fecha_mov=@fecha, tipo_mov=@tipo, cantidad=@cant,
                               costo_unitario=@costo, N_factura=@fact, observacion=@obs
                           WHERE id_kardex=@id";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", obj.idkardex);
            cmd.Parameters.AddWithValue("@idprod", obj.idproducto);
            cmd.Parameters.AddWithValue("@fecha", obj.fechamov == default ? DateTime.Now : obj.fechamov);
            cmd.Parameters.AddWithValue("@tipo", obj.tipomov ?? "ENTRADA");
            cmd.Parameters.AddWithValue("@cant", obj.cantidad);
            cmd.Parameters.AddWithValue("@costo", obj.costounitario);
            cmd.Parameters.AddWithValue("@fact", (object?)obj.nfactura ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@obs", (object?)obj.observacion ?? DBNull.Value);
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            using SqlConnection cn = new SqlConnection(_configuration.GetConnectionString("ConexionSql")!);
            SqlCommand cmd = new SqlCommand("delete from kardex where id_kardex=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }
    }
}
