using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class OrdenCompraController : Controller
    {
        private readonly IConfiguration _configuration;
        public OrdenCompraController(IConfiguration configuration) => _configuration = configuration;
        private string Conexion => _configuration.GetConnectionString("ConexionSql")!;

        private List<Proveedor> CargarProveedores()
        {
            List<Proveedor> lista = new();
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select id_proveedor, razon_social from proveedor order by razon_social", cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read()) lista.Add(new Proveedor { idproveedor = Convert.ToInt32(dr["id_proveedor"]), razonsocial = dr["razon_social"].ToString() });
            return lista;
        }

        private Empleado? ObtenerEmpleadoActual()
        {
            string? idEmpleadoClaim = User.FindFirstValue("IdEmpleado");
            if (string.IsNullOrWhiteSpace(idEmpleadoClaim) || !int.TryParse(idEmpleadoClaim, out int idEmpleado))
            {
                return null;
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT e.id_empl, e.id_cargo, e.nom_empl, e.ape_empl, c.nom_cargo
                           FROM empleado e
                           INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                           WHERE e.id_empl = @idempl";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idempl", idEmpleado);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            return new Empleado
            {
                idempl = Convert.ToInt32(dr["id_empl"]),
                idcargo = Convert.ToInt32(dr["id_cargo"]),
                nomempl = dr["nom_empl"].ToString(),
                apeempl = dr["ape_empl"].ToString(),
                nomcargo = dr["nom_cargo"].ToString()
            };
        }

        private void PrepararVistaFormulario(Empleado? empleadoActual)
        {
            ViewBag.Proveedores = CargarProveedores();
            ViewBag.EmpleadoActual = empleadoActual;
            ViewBag.EmpleadoActualTexto = empleadoActual == null
                ? string.Empty
                : $"{empleadoActual.nomempl} {empleadoActual.apeempl}({empleadoActual.nomcargo})";
        }

        private void ValidarOrdenCompra(OrdenCompra obj)
        {
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idproveedor), obj.idproveedor, "un proveedor");
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idempl), obj.idempl, "un empleado");
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estado), obj.estado, "un estado");
        }

        public IActionResult Index()
        {
            List<OrdenCompra> lista = new();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT oc.id_orden_compra, oc.id_proveedor, oc.id_empl, oc.fecha_orden, oc.total_orden, oc.estado_orden,
                                  p.razon_social, e.nom_empl, e.ape_empl, c.nom_cargo
                           FROM orden_compra oc
                           INNER JOIN proveedor p ON oc.id_proveedor = p.id_proveedor
                           INNER JOIN empleado e ON oc.id_empl = e.id_empl
                           INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                           ORDER BY oc.id_orden_compra DESC";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new OrdenCompra
                {
                    idordencompra = Convert.ToInt32(dr["id_orden_compra"]),
                    idproveedor = Convert.ToInt32(dr["id_proveedor"]),
                    idempl = Convert.ToInt32(dr["id_empl"]),
                    fechaorden = Convert.ToDateTime(dr["fecha_orden"]),
                    total = Convert.ToDecimal(dr["total_orden"]),
                    estado = dr["estado_orden"].ToString(),
                    razonsocial = dr["razon_social"].ToString(),
                    nomempl = dr["nom_empl"].ToString(),
                    apeempl = dr["ape_empl"].ToString(),
                    nomcargo = dr["nom_cargo"].ToString()
                });
            }
            return View(lista);
        }

        public IActionResult Create()
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            PrepararVistaFormulario(empleadoActual);
            return View(new OrdenCompra
            {
                idempl = empleadoActual.idempl,
                fechaorden = DateTime.Now,
                total = 0,
                estado = "REGISTRADA"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrdenCompra obj)
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            obj.idempl = empleadoActual.idempl;
            ValidarOrdenCompra(obj);
            if (!ModelState.IsValid)
            {
                PrepararVistaFormulario(empleadoActual);
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"INSERT INTO orden_compra(id_proveedor, id_empl, fecha_orden, total_orden, estado_orden)
                           VALUES (@idprov, @idempl, @fecha, 0, @estado)";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idprov", obj.idproveedor);
            cmd.Parameters.AddWithValue("@idempl", obj.idempl);
            cmd.Parameters.AddWithValue("@fecha", obj.fechaorden == default ? DateTime.Now : obj.fechaorden);
            cmd.Parameters.AddWithValue("@estado", obj.estado ?? "REGISTRADA");
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            OrdenCompra? obj = null;
            using SqlConnection cn = new SqlConnection(Conexion);
            SqlCommand cmd = new SqlCommand("select * from orden_compra where id_orden_compra=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                obj = new OrdenCompra
                {
                    idordencompra = Convert.ToInt32(dr["id_orden_compra"]),
                    idproveedor = Convert.ToInt32(dr["id_proveedor"]),
                    idempl = empleadoActual.idempl,
                    fechaorden = Convert.ToDateTime(dr["fecha_orden"]),
                    total = Convert.ToDecimal(dr["total_orden"]),
                    estado = dr["estado_orden"].ToString()
                };
            }
            if (obj == null) return NotFound();

            PrepararVistaFormulario(empleadoActual);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(OrdenCompra obj)
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            obj.idempl = empleadoActual.idempl;
            ValidarOrdenCompra(obj);
            if (!ModelState.IsValid)
            {
                PrepararVistaFormulario(empleadoActual);
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"UPDATE orden_compra
                           SET id_proveedor=@idprov, id_empl=@idempl, fecha_orden=@fecha, estado_orden=@estado
                           WHERE id_orden_compra=@id";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", obj.idordencompra);
            cmd.Parameters.AddWithValue("@idprov", obj.idproveedor);
            cmd.Parameters.AddWithValue("@idempl", obj.idempl);
            cmd.Parameters.AddWithValue("@fecha", obj.fechaorden == default ? DateTime.Now : obj.fechaorden);
            cmd.Parameters.AddWithValue("@estado", obj.estado ?? "REGISTRADA");
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            new SqlCommand("delete from detalle_compra where id_orden_compra=@id", cn, tx) { Parameters = { new SqlParameter("@id", id) } }.ExecuteNonQuery();
            new SqlCommand("delete from orden_compra where id_orden_compra=@id", cn, tx) { Parameters = { new SqlParameter("@id", id) } }.ExecuteNonQuery();
            tx.Commit();
            return RedirectToAction("Index");
        }
    }
}
