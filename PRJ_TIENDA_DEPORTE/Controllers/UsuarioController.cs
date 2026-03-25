using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class UsuarioController : Controller
    {
        private readonly IConfiguration _configuration;

        public UsuarioController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private bool EsAdminPrincipal() => User.IsInRole("ADMIN");
        private bool EsAdministrador() => User.IsInRole("ADMINISTRADOR");
        private string UsuarioActual() => User.Identity?.Name ?? string.Empty;
        private string ObtenerConexion() => _configuration.GetConnectionString("ConexionSql")!;

        private int ObtenerIdRol(string nombreRol)
        {
            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 id_rol FROM roles WHERE nombre_rol=@nombre", cn);
            cmd.Parameters.AddWithValue("@nombre", nombreRol);
            cn.Open();
            object? value = cmd.ExecuteScalar();
            return value == null ? 0 : Convert.ToInt32(value);
        }

        private int? ObtenerIdEmpleadoDelUsuarioActual()
        {
            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            using SqlCommand cmd = new SqlCommand("SELECT TOP 1 id_empl FROM usuarios WHERE username=@username", cn);
            cmd.Parameters.AddWithValue("@username", UsuarioActual());
            cn.Open();
            object? value = cmd.ExecuteScalar();
            return value == null || value == DBNull.Value ? null : Convert.ToInt32(value);
        }

        private string ObtenerRolSegunEmpleado(int idEmpleado)
        {
            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            string sql = @"SELECT e.nom_empl, e.ape_empl, c.nom_cargo
                           FROM empleado e
                           INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                           WHERE e.id_empl = @id";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", idEmpleado);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                string nombre = dr["nom_empl"].ToString()?.Trim().ToUpper() ?? "";
                string apellido = dr["ape_empl"].ToString()?.Trim().ToUpper() ?? "";
                string cargo = dr["nom_cargo"].ToString()?.Trim().ToUpper() ?? "";

                if (nombre == "DANTEX" && apellido == "ESQUIVAL") return "ADMIN";
                if (cargo == "ADMINISTRADOR") return "ADMINISTRADOR";
            }
            return "CAJERO";
        }

        private List<Empleado> CargarEmpleadosDisponiblesParaCrear()
        {
            List<Empleado> lista = new List<Empleado>();
            string filtroRol = EsAdministrador() ? " AND UPPER(c.nom_cargo) = 'CAJERO' " : "";

            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            string sql = $@"SELECT e.id_empl, e.nom_empl, e.ape_empl, c.nom_cargo
                            FROM empleado e
                            INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                            LEFT JOIN usuarios u ON e.id_empl = u.id_empl
                            WHERE e.estado_empleado = 'ACTIVO'
                              AND u.id_usuario IS NULL
                              {filtroRol}
                            ORDER BY e.nom_empl, e.ape_empl";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Empleado
                {
                    idempl = Convert.ToInt32(dr["id_empl"]),
                    nomempl = dr["nom_empl"].ToString(),
                    apeempl = dr["ape_empl"].ToString(),
                    nomcargo = dr["nom_cargo"].ToString()
                });
            }
            return lista;
        }

        private List<Empleado> CargarEmpleadosParaEditar(int? idEmpleadoActual)
        {
            List<Empleado> lista = new List<Empleado>();
            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            string sql = @"SELECT e.id_empl, e.nom_empl, e.ape_empl, c.nom_cargo
                           FROM empleado e
                           INNER JOIN cargo c ON e.id_cargo = c.id_cargo
                           WHERE e.estado_empleado = 'ACTIVO'
                             AND (e.id_empl = @idActual OR NOT EXISTS(SELECT 1 FROM usuarios u WHERE u.id_empl = e.id_empl))
                           ORDER BY e.nom_empl, e.ape_empl";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idActual", (object?)idEmpleadoActual ?? DBNull.Value);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Empleado
                {
                    idempl = Convert.ToInt32(dr["id_empl"]),
                    nomempl = dr["nom_empl"].ToString(),
                    apeempl = dr["ape_empl"].ToString(),
                    nomcargo = dr["nom_cargo"].ToString()
                });
            }
            return lista;
        }

        private Usuario? ObtenerUsuarioPorId(int id)
        {
            Usuario? obj = null;
            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            string sql = @"SELECT u.id_usuario, u.id_empl, u.username, u.password, u.id_rol, u.estado_usuario, u.fecha_creacion,
                                  r.nombre_rol,
                                  ISNULL(e.nom_empl,'') + ' ' + ISNULL(e.ape_empl,'') AS nombre_empleado
                           FROM usuarios u
                           INNER JOIN roles r ON u.id_rol = r.id_rol
                           LEFT JOIN empleado e ON u.id_empl = e.id_empl
                           WHERE u.id_usuario=@id";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                obj = new Usuario
                {
                    idusuario = Convert.ToInt32(dr["id_usuario"]),
                    idempl = dr["id_empl"] == DBNull.Value ? null : Convert.ToInt32(dr["id_empl"]),
                    username = dr["username"].ToString(),
                    password = dr["password"].ToString(),
                    idrol = Convert.ToInt32(dr["id_rol"]),
                    estadousuario = dr["estado_usuario"].ToString(),
                    fechacreacion = dr["fecha_creacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_creacion"]),
                    nombrerol = dr["nombre_rol"].ToString(),
                    nombreempleado = dr["nombre_empleado"].ToString()?.Trim()
                };
            }
            return obj;
        }

        private bool PuedeAdministrarUsuario(Usuario usuario)
        {
            if (EsAdminPrincipal())
            {
                return !string.Equals(usuario.nombrerol, "ADMIN", StringComparison.OrdinalIgnoreCase)
                       || string.Equals(usuario.username, UsuarioActual(), StringComparison.OrdinalIgnoreCase);
            }

            if (!EsAdministrador()) return false;

            bool esPropio = string.Equals(usuario.username, UsuarioActual(), StringComparison.OrdinalIgnoreCase);
            if (esPropio) return true;

            return string.Equals(usuario.nombrerol, "CAJERO", StringComparison.OrdinalIgnoreCase);
        }

        private void CargarDatosVista(int? idEmpleadoActual = null, int? idRolSeleccionado = null, bool bloquearEmpleado = false)
        {
            ViewBag.Empleados = CargarEmpleadosParaEditar(idEmpleadoActual);
            ViewBag.RolSeleccionado = idRolSeleccionado ?? 0;
            ViewBag.BloquearEmpleado = bloquearEmpleado;
        }

        private void ValidarUsuario(Usuario obj, bool requiereEmpleado)
        {
            // Validar empleado si aplica
            if (requiereEmpleado)
            {
                ValidacionHelper.Seleccion(ModelState, nameof(obj.idempl), obj.idempl ?? 0, "un empleado");
            }

            // Validar username: requerido y único
            ValidacionHelper.Usuario(ModelState, nameof(obj.username), obj.username);

            // Validar password: requerido y mínimo 6 caracteres por ejemplo
            ValidacionHelper.Password(ModelState, nameof(obj.password), obj.password);

            // Validar rol: requerido, mayor que 0
            if (obj.idrol <= 0)
            {
                ModelState.AddModelError(nameof(obj.idrol), "Debe seleccionar un rol válido.");
            }

            // Validar estado_usuario: solo ACTIVO o INACTIVO
            if (string.IsNullOrEmpty(obj.estadousuario) ||
                (obj.estadousuario != "ACTIVO" && obj.estadousuario != "INACTIVO"))
            {
                ModelState.AddModelError(nameof(obj.estadousuario), "El estado debe ser ACTIVO o INACTIVO.");
            }
        }
        public IActionResult Index()
        {
            List<Usuario> lista = new List<Usuario>();
            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            string sql = @"SELECT u.id_usuario, u.id_empl, u.username, u.password, u.id_rol, u.estado_usuario, u.fecha_creacion,
                                  r.nombre_rol,
                                  ISNULL(e.nom_empl,'') + ' ' + ISNULL(e.ape_empl,'') AS nombre_empleado
                           FROM usuarios u
                           INNER JOIN roles r ON u.id_rol = r.id_rol
                           LEFT JOIN empleado e ON u.id_empl = e.id_empl
                           ORDER BY u.id_usuario DESC";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                Usuario item = new Usuario
                {
                    idusuario = Convert.ToInt32(dr["id_usuario"]),
                    idempl = dr["id_empl"] == DBNull.Value ? null : Convert.ToInt32(dr["id_empl"]),
                    username = dr["username"].ToString(),
                    password = dr["password"].ToString(),
                    idrol = Convert.ToInt32(dr["id_rol"]),
                    estadousuario = dr["estado_usuario"].ToString(),
                    fechacreacion = dr["fecha_creacion"] == DBNull.Value ? null : Convert.ToDateTime(dr["fecha_creacion"]),
                    nombrerol = dr["nombre_rol"].ToString(),
                    nombreempleado = dr["nombre_empleado"].ToString()?.Trim()
                };

                if (EsAdminPrincipal())
                {
                    lista.Add(item);
                }
                else if (string.Equals(item.nombrerol, "CAJERO", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(item.username, UsuarioActual(), StringComparison.OrdinalIgnoreCase))
                {
                    lista.Add(item);
                }
            }

            ViewBag.EsAdminPrincipal = EsAdminPrincipal();
            ViewBag.UsuarioActual = UsuarioActual();
            return View(lista);
        }

        public IActionResult Create()
        {
            ViewBag.Empleados = CargarEmpleadosDisponiblesParaCrear();
            return View(new Usuario { estadousuario = "ACTIVO" });
        }
        [HttpPost]
        public IActionResult Create(Usuario obj)
        {

            ViewBag.Empleados = CargarEmpleadosDisponiblesParaCrear();
            if (obj.idempl.HasValue && obj.idempl.Value > 0)
            {
                string rolNombre = ObtenerRolSegunEmpleado(obj.idempl.Value);
                obj.idrol = ObtenerIdRol(rolNombre);

                if (EsAdministrador() && rolNombre != "CAJERO")
                {
                    ModelState.AddModelError("", "Como ADMINISTRADOR, solo puedes crear usuarios para empleados con cargo CAJERO.");
                    return View(obj);
                }
            }


            ValidarUsuario(obj, true);

            if (!ModelState.IsValid)
            {
                return View(obj);
            }

            try
            {
                using SqlConnection cn = new SqlConnection(ObtenerConexion());
                string sql = @"INSERT INTO usuarios(id_empl, username, password, id_rol, estado_usuario)
                       VALUES (@idempl, @username, @password, @idrol, @estado)";

                using SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@idempl", obj.idempl.Value);
                cmd.Parameters.AddWithValue("@username", obj.username ?? "");
                cmd.Parameters.AddWithValue("@password", obj.password ?? "");
                cmd.Parameters.AddWithValue("@idrol", obj.idrol);
                cmd.Parameters.AddWithValue("@estado", obj.estadousuario ?? "ACTIVO");

                cn.Open();
                cmd.ExecuteNonQuery();

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error al guardar en la base de datos: " + ex.Message);
                return View(obj);
            }
        }

        public IActionResult Edit(int id)
        {
            Usuario? obj = ObtenerUsuarioPorId(id);
            if (obj == null) return NotFound();
            if (!PuedeAdministrarUsuario(obj)) return Forbid();

            bool esPropio = string.Equals(obj.username, UsuarioActual(), StringComparison.OrdinalIgnoreCase);
            bool bloquearEmpleado = EsAdministrador() || esPropio || string.Equals(obj.nombrerol, "ADMIN", StringComparison.OrdinalIgnoreCase);

            CargarDatosVista(obj.idempl, obj.idrol, bloquearEmpleado);
            ViewBag.NombreRol = obj.nombrerol;
            ViewBag.EsEdicionPropia = esPropio;
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Usuario obj)
        {
            Usuario? original = ObtenerUsuarioPorId(obj.idusuario);
            if (original == null) return NotFound();
            if (!PuedeAdministrarUsuario(original)) return Forbid();

            bool esPropio = string.Equals(original.username, UsuarioActual(), StringComparison.OrdinalIgnoreCase);

            // Determinar valores finales (rescatando de 'original' lo que la vista bloqueó)
            string rolFinal = original.nombrerol ?? "CAJERO";
            int? empleadoFinal = original.idempl;

            if (EsAdminPrincipal())
            {
                if (!string.Equals(original.nombrerol, "ADMIN", StringComparison.OrdinalIgnoreCase) && obj.idempl.HasValue && obj.idempl > 0)
                {
                    empleadoFinal = obj.idempl;
                    rolFinal = ObtenerRolSegunEmpleado(obj.idempl.Value);
                }
            }
            else if (EsAdministrador())
            {
                empleadoFinal = original.idempl;
                rolFinal = original.nombrerol ?? "CAJERO";
            }

            // Sincronizar objeto antes de validar
            obj.idrol = ObtenerIdRol(rolFinal);
            obj.idempl = empleadoFinal;

            if (string.IsNullOrEmpty(obj.username)) obj.username = original.username;

            ValidarUsuario(obj, false);

            if (!ModelState.IsValid)
            {
                bool bloquear = EsAdministrador() || esPropio || string.Equals(original.nombrerol, "ADMIN", StringComparison.OrdinalIgnoreCase);
                ViewBag.BloquearEmpleado = bloquear;
                CargarDatosVista(original.idempl, original.idrol, bloquear);
                ViewBag.NombreRol = original.nombrerol;
                return View(obj);
            }

            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            string sql = @"UPDATE usuarios 
                   SET id_empl=@idempl, username=@username, password=@password, id_rol=@idrol, estado_usuario=@estado 
                   WHERE id_usuario=@id";

            using SqlCommand cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", obj.idusuario);
            cmd.Parameters.AddWithValue("@idempl", (object?)obj.idempl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@username", obj.username ?? "");
            cmd.Parameters.AddWithValue("@password", obj.password ?? "");
            cmd.Parameters.AddWithValue("@idrol", obj.idrol);
            cmd.Parameters.AddWithValue("@estado", obj.estadousuario ?? "ACTIVO");

            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            Usuario? obj = ObtenerUsuarioPorId(id);
            if (obj == null) return NotFound();
            if (!PuedeAdministrarUsuario(obj)) return Forbid();

            bool esPropio = string.Equals(obj.username, UsuarioActual(), StringComparison.OrdinalIgnoreCase);
            if (esPropio)
            {
                TempData["Error"] = "No puede eliminar su propio usuario.";
                return RedirectToAction("Index");
            }

            if (string.Equals(obj.nombrerol, "ADMIN", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "No se permite eliminar al ADMIN PRINCIPAL.";
                return RedirectToAction("Index");
            }

            if (EsAdministrador() && !string.Equals(obj.nombrerol, "CAJERO", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "El ADMINISTRADOR no puede eliminar usuarios ADMIN o ADMINISTRADOR.";
                return RedirectToAction("Index");
            }

            using SqlConnection cn = new SqlConnection(ObtenerConexion());
            using SqlCommand cmd = new SqlCommand("DELETE FROM usuarios WHERE id_usuario=@id", cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            cmd.ExecuteNonQuery();

            return RedirectToAction("Index");
        }
    }
}
