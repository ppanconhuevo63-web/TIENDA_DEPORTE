using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class RolController : Controller
    {
        private readonly IConfiguration _configuration;

        public RolController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private bool EsAdminPrincipal() => User.IsInRole("ADMIN");

        public IActionResult Index()
        {
            List<Rol> lista = new List<Rol>();
            string conexion = _configuration.GetConnectionString("ConexionSql")!;

            using SqlConnection cn = new SqlConnection(conexion);
            string sql = @"SELECT *
                           FROM roles
                           WHERE nombre_rol IN ('ADMIN','ADMINISTRADOR','CAJERO')
                           ORDER BY CASE nombre_rol WHEN 'ADMIN' THEN 1 WHEN 'ADMINISTRADOR' THEN 2 WHEN 'CAJERO' THEN 3 ELSE 4 END";
            using SqlCommand cmd = new SqlCommand(sql, cn);
            cn.Open();
            using SqlDataReader dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(new Rol
                {
                    idrol = Convert.ToInt32(dr["id_rol"]),
                    nombrerol = dr["nombre_rol"].ToString()
                });
            }

            ViewBag.EsAdminPrincipal = EsAdminPrincipal();
            return View(lista);
        }

        public IActionResult Create()
        {
            TempData["Error"] = "Los roles están bloqueados en el sistema: ADMIN, ADMINISTRADOR y CAJERO.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Create(Rol obj)
        {
            TempData["Error"] = "No se permite crear nuevos roles porque la vista Rol quedó fija con ADMIN, ADMINISTRADOR y CAJERO.";
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            TempData["Error"] = "Los roles fijos no se pueden editar desde la aplicación.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Edit(Rol obj)
        {
            TempData["Error"] = "Los roles fijos no se pueden editar desde la aplicación.";
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            TempData["Error"] = "Los roles fijos no se pueden eliminar desde la aplicación.";
            return RedirectToAction("Index");
        }
    }
}
