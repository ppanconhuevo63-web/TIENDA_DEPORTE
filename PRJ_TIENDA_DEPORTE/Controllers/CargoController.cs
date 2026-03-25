using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class CargoController : Controller
    {
        private readonly IConfiguration _configuration;

        public CargoController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string Conexion => _configuration.GetConnectionString("ConexionSql")!;
        private bool EsAdminPrincipal() => User.IsInRole("ADMIN");
        private bool EsAdministrador() => User.IsInRole("ADMINISTRADOR");
        private string UsuarioActual() => User.Identity?.Name ?? string.Empty;

        private static bool EsNombreReservado(string? nombre)
        {
            string valor = (nombre ?? string.Empty).Trim().ToUpperInvariant();
            return valor == "ADMIN" || valor == "ADMINISTRADOR";
        }

        private List<Cargo> CargarCargos()
        {
            List<Cargo> lista = new List<Cargo>();
            string filtro = EsAdministrador()
                ? " WHERE UPPER(nom_cargo) NOT IN ('ADMIN','ADMINISTRADOR') "
                : string.Empty;

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = $@"SELECT * FROM cargo {filtro} ORDER BY nom_cargo";
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

        private Cargo? ObtenerCargo(int id)
        {
            using SqlConnection cn = new SqlConnection(Conexion);
            using SqlCommand cmd = new SqlCommand("SELECT * FROM cargo WHERE id_cargo=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            return new Cargo
            {
                idcargo = Convert.ToInt32(dr["id_cargo"]),
                nomcargo = dr["nom_cargo"].ToString(),
                sueldo = Convert.ToDecimal(dr["sueldo_cargo"]),
                estadocargo = dr["estado_cargo"].ToString()
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

        private bool EsEmpleadoPropio(int idEmpleado)
        {
            int? idActual = ObtenerIdEmpleadoDelUsuarioActual();
            return idActual.HasValue && idActual.Value == idEmpleado;
        }

        private bool PuedeCrearCargo(string? nombreCargo)
        {
            if (EsAdminPrincipal()) return true;
            return !EsNombreReservado(nombreCargo);
        }

        private bool PuedeEditarOCrearCargoReservado(Cargo cargo)
        {
            if (EsAdminPrincipal()) return true;
            return !EsNombreReservado(cargo.nomcargo);
        }

        private bool AsignarSueldoDesdeFormulario(Cargo obj)
        {
            ModelState.Remove(nameof(obj.sueldo));

            string sueldoTexto = (Request.Form[nameof(obj.sueldo)].ToString() ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(sueldoTexto))
            {
                ModelState.AddModelError(nameof(obj.sueldo), "Ingrese el sueldo.");
                return false;
            }

            if (sueldoTexto.Contains(','))
            {
                ModelState.AddModelError(nameof(obj.sueldo), "El sueldo debe usar punto decimal. Ejemplo: 1050.20");
                return false;
            }

            if (!decimal.TryParse(sueldoTexto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal sueldoParseado))
            {
                ModelState.AddModelError(nameof(obj.sueldo), "Ingrese un sueldo válido con formato 1050.20");
                return false;
            }

            obj.sueldo = sueldoParseado;
            return true;
        }

        private void ValidarCargo(Cargo obj)
        {
            ValidacionHelper.SoloTexto(ModelState, nameof(obj.nomcargo), obj.nomcargo, "nombre del cargo", 60);
            ValidacionHelper.DecimalPositivo(ModelState, nameof(obj.sueldo), obj.sueldo, "sueldo");
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadocargo), obj.estadocargo, "un estado");

            if (obj.sueldo > 99999)
            {
                ModelState.AddModelError(nameof(obj.sueldo), "El sueldo no debe tener más de 5 dígitos.");
            }
            if (string.IsNullOrWhiteSpace(obj.nomcargo))
            {
                ModelState.AddModelError(nameof(obj.nomcargo), "El nombre del cargo es obligatorio.");
            }
            if (obj.sueldo < 0)
            {
                ModelState.AddModelError(nameof(obj.sueldo), "El sueldo no puede ser negativo.");
            }
            if (obj.sueldo > 99999999.99m)
            {
                ModelState.AddModelError(nameof(obj.sueldo), "El sueldo excede el límite permitido.");
            }
            var estadosValidos = new[] { "ACTIVO", "INACTIVO" };

            if (!estadosValidos.Contains(obj.estadocargo))
            {
                ModelState.AddModelError(nameof(obj.estadocargo), "Estado inválido.");
            }
            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"SELECT COUNT(*) FROM cargo 
               WHERE nom_cargo = @nom AND id_cargo <> @id";

            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@nom", obj.nomcargo ?? "");
            cmd.Parameters.AddWithValue("@id", obj.idcargo);

            cn.Open();
            int existe = Convert.ToInt32(cmd.ExecuteScalar());

            if (existe > 0)
            {
                ModelState.AddModelError(nameof(obj.nomcargo), "El cargo ya existe.");
            }
        }

        public IActionResult Index()
        {
            ViewBag.EsAdminPrincipal = EsAdminPrincipal();
            return View(CargarCargos());
        }

        public IActionResult Create()
        {
            return View(new Cargo { estadocargo = "ACTIVO" });
        }

        [HttpPost]
        public IActionResult Create(Cargo obj)
        {
            AsignarSueldoDesdeFormulario(obj);
            ValidarCargo(obj);

            if (EsNombreReservado(obj.nomcargo))
            {
                ModelState.AddModelError("nomcargo", "No se permite crear cargos con el nombre ADMIN o ADMINISTRADOR.");
            }

            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"INSERT INTO cargo(nom_cargo,sueldo_cargo,estado_cargo)
                           VALUES (@nom,@sueldo,@estado)";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@nom", obj.nomcargo ?? string.Empty);
            cmd.Parameters.AddWithValue("@sueldo", obj.sueldo);
            cmd.Parameters.AddWithValue("@estado", obj.estadocargo ?? "ACTIVO");
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Cargo? obj = ObtenerCargo(id);
            if (obj == null) return NotFound();

            if (!PuedeEditarOCrearCargoReservado(obj))
            {
                TempData["Error"] = "El usuario ADMINISTRADOR no puede editar cargos ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Cargo obj)
        {
            Cargo? original = ObtenerCargo(obj.idcargo);
            if (original == null) return NotFound();

            if (!PuedeEditarOCrearCargoReservado(original))
            {
                TempData["Error"] = "El usuario ADMINISTRADOR no puede editar cargos ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            AsignarSueldoDesdeFormulario(obj);
            ValidarCargo(obj);

            if (EsNombreReservado(obj.nomcargo) && !EsAdminPrincipal())
            {
                ModelState.AddModelError("nomcargo", "No se permite usar ADMIN o ADMINISTRADOR en el nombre del cargo.");
            }

            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            string sql = @"UPDATE cargo
                           SET nom_cargo=@nom,
                               sueldo_cargo=@sueldo,
                               estado_cargo=@estado
                           WHERE id_cargo=@id";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", obj.idcargo);
            cmd.Parameters.AddWithValue("@nom", obj.nomcargo ?? string.Empty);
            cmd.Parameters.AddWithValue("@sueldo", obj.sueldo);
            cmd.Parameters.AddWithValue("@estado", obj.estadocargo ?? "ACTIVO");
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            Cargo? cargo = ObtenerCargo(id);
            if (cargo == null) return NotFound();

            if (!PuedeEditarOCrearCargoReservado(cargo))
            {
                TempData["Error"] = "El usuario ADMINISTRADOR no puede eliminar cargos ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            using SqlConnection cn = new SqlConnection(Conexion);
            using SqlCommand cmd = new SqlCommand("DELETE FROM cargo WHERE id_cargo=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
    }
}
