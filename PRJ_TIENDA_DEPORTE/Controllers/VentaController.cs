using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR,CAJERO")]
    public class VentaController : Controller
    {
        private readonly IConfiguration _configuration;

        public VentaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string Conexion => _configuration.GetConnectionString("ConexionSql")!;

        private List<Cliente> CargarClientes()
        {
            List<Cliente> lista = new List<Cliente>();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = "select id_cliente, nom_cliente, ape_cliente from cliente order by nom_cliente, ape_cliente";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cliente
                {
                    idcliente = Convert.ToInt32(dr["id_cliente"]),
                    nomcliente = dr["nom_cliente"].ToString(),
                    apecliente = dr["ape_cliente"].ToString()
                });
            }
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
            ViewBag.Clientes = CargarClientes();
            ViewBag.EmpleadoActual = empleadoActual;
            ViewBag.EmpleadoActualTexto = empleadoActual == null
                ? string.Empty
                : $"{empleadoActual.nomempl} {empleadoActual.apeempl}({empleadoActual.nomcargo})";
        }

        private void ValidarVenta(Venta obj)
        {
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idcliente), obj.idcliente, "un cliente");
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idempl), obj.idempl, "un empleado");
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.metodopago), obj.metodopago, "un método de pago", 30);
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadoventa), obj.estadoventa, "un estado", 20);
        }

        public IActionResult Index()
        {
            List<Venta> lista = new List<Venta>();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT v.id_venta, v.id_cliente, v.id_empl, v.fecha_venta, v.total_venta, v.metodo_pago, v.estado_venta,
                                  c.nom_cliente, c.ape_cliente, e.nom_empl, e.ape_empl, cg.nom_cargo
                           FROM orden_venta v
                           INNER JOIN cliente c ON v.id_cliente = c.id_cliente
                           INNER JOIN empleado e ON v.id_empl = e.id_empl
                           INNER JOIN cargo cg ON e.id_cargo = cg.id_cargo
                           ORDER BY v.id_venta DESC";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Venta
                {
                    idventa = Convert.ToInt32(dr["id_venta"]),
                    idcliente = Convert.ToInt32(dr["id_cliente"]),
                    idempl = Convert.ToInt32(dr["id_empl"]),
                    fechaventa = Convert.ToDateTime(dr["fecha_venta"]),
                    total = Convert.ToDecimal(dr["total_venta"]),
                    metodopago = dr["metodo_pago"].ToString(),
                    estadoventa = dr["estado_venta"].ToString(),
                    nomcliente = dr["nom_cliente"].ToString(),
                    apecliente = dr["ape_cliente"].ToString(),
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
            return View(new Venta
            {
                idempl = empleadoActual.idempl,
                fechaventa = DateTime.Now,
                total = 0,
                metodopago = "EFECTIVO",
                estadoventa = "EMITIDA"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Venta obj)
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            obj.idempl = empleadoActual.idempl;
            ValidarVenta(obj);
            if (!ModelState.IsValid)
            {
                PrepararVistaFormulario(empleadoActual);
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"INSERT INTO orden_venta(id_cliente, id_empl, fecha_venta, total_venta, metodo_pago, estado_venta)
                           VALUES (@idcli, @idempl, @fecha, 0, @metodo, @estado)";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idcli", obj.idcliente);
            cmd.Parameters.AddWithValue("@idempl", obj.idempl);
            cmd.Parameters.AddWithValue("@fecha", obj.fechaventa == default ? DateTime.Now : obj.fechaventa);
            cmd.Parameters.AddWithValue("@metodo", obj.metodopago ?? "EFECTIVO");
            cmd.Parameters.AddWithValue("@estado", obj.estadoventa ?? "EMITIDA");
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            Venta? obj = null;
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = "select * from orden_venta where id_venta=@id";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                obj = new Venta
                {
                    idventa = Convert.ToInt32(dr["id_venta"]),
                    idcliente = Convert.ToInt32(dr["id_cliente"]),
                    idempl = empleadoActual.idempl,
                    fechaventa = Convert.ToDateTime(dr["fecha_venta"]),
                    total = Convert.ToDecimal(dr["total_venta"]),
                    metodopago = dr["metodo_pago"].ToString(),
                    estadoventa = dr["estado_venta"].ToString()
                };
            }
            if (obj == null) return NotFound();

            PrepararVistaFormulario(empleadoActual);
            return View(obj);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Venta obj)
        {
            Empleado? empleadoActual = ObtenerEmpleadoActual();
            if (empleadoActual == null) return Forbid();

            obj.idempl = empleadoActual.idempl;
            ValidarVenta(obj);
            if (!ModelState.IsValid)
            {
                PrepararVistaFormulario(empleadoActual);
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"UPDATE orden_venta
                           SET id_cliente=@idcli,
                               id_empl=@idempl,
                               fecha_venta=@fecha,
                               metodo_pago=@metodo,
                               estado_venta=@estado
                           WHERE id_venta=@id";
            SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", obj.idventa);
            cmd.Parameters.AddWithValue("@idcli", obj.idcliente);
            cmd.Parameters.AddWithValue("@idempl", obj.idempl);
            cmd.Parameters.AddWithValue("@fecha", obj.fechaventa == default ? DateTime.Now : obj.fechaventa);
            cmd.Parameters.AddWithValue("@metodo", obj.metodopago ?? "EFECTIVO");
            cmd.Parameters.AddWithValue("@estado", obj.estadoventa ?? "EMITIDA");
            cn.Open();
            cmd.ExecuteNonQuery();
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            cn.Open();
            using SqlTransaction tx = cn.BeginTransaction();
            try
            {
                new SqlCommand("delete from detalle_venta where id_venta=@id", cn, tx) { Parameters = { new SqlParameter("@id", id) } }.ExecuteNonQuery();
                new SqlCommand("delete from orden_venta where id_venta=@id", cn, tx) { Parameters = { new SqlParameter("@id", id) } }.ExecuteNonQuery();
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
