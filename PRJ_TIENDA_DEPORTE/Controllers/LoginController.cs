using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;

        public LoginController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(UsuarioLogin model)
        {
            if (!ModelState.IsValid) return View(model);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            UsuarioLogin? usuario = null;

            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = @"SELECT u.id_empl, u.username, u.password, r.nombre_rol,
                                      ISNULL(e.nom_empl,'') + ' ' + ISNULL(e.ape_empl,'') AS nombre_completo,
                                      ISNULL(c.nom_cargo,'') AS cargo
                               FROM usuarios u
                               INNER JOIN roles r ON u.id_rol = r.id_rol
                               LEFT JOIN empleado e ON u.id_empl = e.id_empl
                               LEFT JOIN cargo c ON e.id_cargo = c.id_cargo
                               WHERE u.username = @username AND u.estado_usuario = 'ACTIVO'";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@username", model.username ?? "");
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    usuario = new UsuarioLogin
                    {
                        idempl = dr["id_empl"] == DBNull.Value ? null : Convert.ToInt32(dr["id_empl"]),
                        username = dr["username"].ToString(),
                        password = dr["password"].ToString(),
                        rol = dr["nombre_rol"].ToString(),
                        cargo = dr["cargo"].ToString(),
                        nombrecompleto = dr["nombre_completo"].ToString()?.Trim()
                    };
                }
            }

            if (usuario == null || usuario.password != model.password)
            {
                ViewBag.Error = "Usuario o contraseña incorrectos.";
                return View(model);
            }

            string nombreVisible = string.IsNullOrWhiteSpace(usuario.nombrecompleto) ? usuario.username ?? "" : usuario.nombrecompleto!;
            string cargoVisible = string.IsNullOrWhiteSpace(usuario.cargo) ? string.Empty : usuario.cargo!.Trim();
            string empleadoVisible = string.IsNullOrWhiteSpace(cargoVisible)
                ? nombreVisible
                : $"{nombreVisible}({cargoVisible})";

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.username ?? ""),
                new Claim(ClaimTypes.Role, usuario.rol ?? "CAJERO"),
                new Claim("NombreCompleto", nombreVisible),
                new Claim("EmpleadoNombreCargo", empleadoVisible)
            };

            if (usuario.idempl.HasValue)
            {
                claims.Add(new Claim("IdEmpleado", usuario.idempl.Value.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(cargoVisible))
            {
                claims.Add(new Claim("Cargo", cargoVisible));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        public IActionResult AccessDenied() => View();
    }
}
