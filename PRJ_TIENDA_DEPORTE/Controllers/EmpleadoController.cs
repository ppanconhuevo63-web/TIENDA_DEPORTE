using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class EmpleadoController : Controller
    {
        private readonly IConfiguration _configuration;

        public EmpleadoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string Conexion => _configuration.GetConnectionString("ConexionSql")!;
        private bool EsAdminPrincipal() => User.IsInRole("ADMIN");
        private bool EsAdministrador() => User.IsInRole("ADMINISTRADOR");
        private string UsuarioActual() => User.Identity?.Name ?? string.Empty;

        private List<Cargo> CargarCargos()
        {
            List<Cargo> lista = new List<Cargo>();
            string filtro = EsAdministrador()
                ? " WHERE UPPER(nom_cargo) NOT IN ('ADMIN','ADMINISTRADOR') "
                : string.Empty;

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = $"SELECT * FROM cargo {filtro} ORDER BY nom_cargo";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Cargo
                {
                    idcargo = Convert.ToInt32(dr["id_cargo"]),
                    nomcargo = dr["nom_cargo"].ToString(),
                    sueldo = Convert.ToDecimal(dr["sueldo_cargo"]),
                    estadocargo = dr["estado_cargo"].ToString()
                });
            }
            return lista;
        }

        private Empleado? ObtenerEmpleado(int id)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT e.id_empl, e.id_cargo, e.nom_empl, e.ape_empl, e.nro_dni_empl, e.estado_empleado,
                                  c.nom_cargo
                           FROM empleado e
                           INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                           WHERE e.id_empl=@id";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            return new Empleado
            {
                idempl = Convert.ToInt32(dr["id_empl"]),
                idcargo = Convert.ToInt32(dr["id_cargo"]),
                nomempl = dr["nom_empl"].ToString(),
                apeempl = dr["ape_empl"].ToString(),
                dniempl = dr["nro_dni_empl"].ToString(),
                estadoempleado = dr["estado_empleado"].ToString(),
                nomcargo = dr["nom_cargo"].ToString()
            };
        }

        private int? ObtenerIdEmpleadoDelUsuarioActual()
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 id_empl FROM usuarios WHERE username=@username", cn);
            cmd.Parameters.AddWithValue("@username", UsuarioActual());
            cn.Open();
            object? value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value) return null;
            return Convert.ToInt32(value);
        }

        private bool EsEmpleadoProtegido(Empleado empleado)
        {
            bool esCargoProtegido = string.Equals(empleado.nomcargo, "ADMINISTRADOR", StringComparison.OrdinalIgnoreCase)
                                   || string.Equals(empleado.nomcargo, "ADMIN", StringComparison.OrdinalIgnoreCase);
            if (EsAdminPrincipal()) return false;
            if (!EsAdministrador()) return true;

            bool esPropio = ObtenerIdEmpleadoDelUsuarioActual() == empleado.idempl;
            return esCargoProtegido || esPropio;
        }

        private void ValidarEmpleado(Empleado obj)
        {
            ValidacionHelper.Seleccion(ModelState, nameof(obj.idcargo), obj.idcargo, "un cargo");
            ValidacionHelper.SoloTexto(ModelState, nameof(obj.nomempl), obj.nomempl, "nombre del empleado", 60);
            ValidacionHelper.SoloTexto(ModelState, nameof(obj.apeempl), obj.apeempl, "apellido del empleado", 60);
            ValidacionHelper.SoloEnteros(ModelState, nameof(obj.dniempl), obj.dniempl, "DNI del empleado", 8);
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadoempleado), obj.estadoempleado, "un estado");
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT COUNT(*) FROM empleado 
                   WHERE nro_dni_empl = @dni AND id_empl <> @id";

            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@dni", obj.dniempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@id", obj.idempl);

            cn.Open();
            int cantidad = Convert.ToInt32(cmd.ExecuteScalar());

            if (cantidad > 0)
            {
                ModelState.AddModelError(nameof(obj.dniempl), "El DNI ya está registrado.");
            }
        }

        public IActionResult Index()
        {
            List<Empleado> lista = new List<Empleado>();
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT e.id_empl, e.id_cargo, e.nom_empl, e.ape_empl, e.nro_dni_empl, e.estado_empleado,
                                  c.nom_cargo
                           FROM empleado e
                           INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                           ORDER BY e.id_empl DESC";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Empleado
                {
                    idempl = Convert.ToInt32(dr["id_empl"]),
                    idcargo = Convert.ToInt32(dr["id_cargo"]),
                    nomempl = dr["nom_empl"].ToString(),
                    apeempl = dr["ape_empl"].ToString(),
                    dniempl = dr["nro_dni_empl"].ToString(),
                    estadoempleado = dr["estado_empleado"].ToString(),
                    nomcargo = dr["nom_cargo"].ToString()
                });
            }

            ViewBag.EsAdminPrincipal = EsAdminPrincipal();
            ViewBag.IdEmpleadoActual = ObtenerIdEmpleadoDelUsuarioActual();
            return View(lista);
        }

        public IActionResult Create()
        {
            ViewBag.Cargos = CargarCargos();
            return View(new Empleado { estadoempleado = "ACTIVO" });
        }

        [HttpPost]
        public IActionResult Create(Empleado obj)
        {
            ViewBag.Cargos = CargarCargos();
            ValidarEmpleado(obj);

            if (EsAdministrador())
            {
                var cargo = CargarCargos().FirstOrDefault(x => x.idcargo == obj.idcargo);
                if (cargo == null)
                {
                    ModelState.AddModelError("idcargo", "El ADMINISTRADOR solo puede crear empleados con cargos permitidos.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"INSERT INTO empleado(id_cargo, nom_empl, ape_empl, nro_dni_empl, estado_empleado)
                           VALUES (@idcargo, @nom, @ape, @dni, @estado)";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idcargo", obj.idcargo);
            cmd.Parameters.AddWithValue("@nom", obj.nomempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@ape", obj.apeempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@dni", obj.dniempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@estado", obj.estadoempleado ?? "ACTIVO");
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Empleado? obj = ObtenerEmpleado(id);
            if (obj == null) return NotFound();

            if (EsEmpleadoProtegido(obj))
            {
                TempData["Error"] = "El usuario ADMINISTRADOR no puede editar su propio empleado ni empleados con cargo ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            ViewBag.Cargos = CargarCargos();
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Empleado obj)
        {
            Empleado? original = ObtenerEmpleado(obj.idempl);
            if (original == null) return NotFound();

            if (EsEmpleadoProtegido(original))
            {
                TempData["Error"] = "El usuario ADMINISTRADOR no puede editar su propio empleado ni empleados con cargo ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            ViewBag.Cargos = CargarCargos();
            ValidarEmpleado(obj);
            if (EsAdministrador() && !CargarCargos().Any(x => x.idcargo == obj.idcargo))
            {
                ModelState.AddModelError("idcargo", "Cargo no permitido para el usuario ADMINISTRADOR.");
            }

            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"UPDATE empleado
                           SET id_cargo=@idcargo,
                               nom_empl=@nom,
                               ape_empl=@ape,
                               nro_dni_empl=@dni,
                               estado_empleado=@estado
                           WHERE id_empl=@id";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", obj.idempl);
            cmd.Parameters.AddWithValue("@idcargo", obj.idcargo);
            cmd.Parameters.AddWithValue("@nom", obj.nomempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@ape", obj.apeempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@dni", obj.dniempl ?? string.Empty);
            cmd.Parameters.AddWithValue("@estado", obj.estadoempleado ?? "ACTIVO");
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            Empleado? empleado = ObtenerEmpleado(id);
            if (empleado == null) return NotFound();

            if (EsEmpleadoProtegido(empleado))
            {
                TempData["Error"] = "El usuario ADMINISTRADOR no puede eliminar su propio empleado ni empleados con cargo ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            using SqlCommand cmd = new SqlCommand("DELETE FROM empleado WHERE id_empl=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
    }
}
