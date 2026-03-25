using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class CategoriaController : Controller
    {
        private readonly IConfiguration _configuration;

        public CategoriaController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<Categoria> lista = new List<Categoria>();
            string conexion = _configuration.GetConnectionString("ConexionSql")!;

            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from categoria";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new Categoria
                    {
                        idcategoria = Convert.ToInt32(dr["id_categoria"]),
                        nomcategoria = dr["nom_categoria"].ToString(),
                        descripcion = dr["descripcion_categoria"]?.ToString(),
                        estadocategoria = dr["estado_categoria"].ToString()
                    });
                }
            }

            return View(lista);
        }

        public IActionResult Create() => View(new Categoria { estadocategoria = "ACTIVO" });

        private void ValidarCategoria(Categoria obj)
        {
            ValidacionHelper.SoloTexto(ModelState, nameof(obj.nomcategoria), obj.nomcategoria, "nombre de la categoría", 60);
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.descripcion), obj.descripcion, "descripción de la categoría", 150);
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadocategoria), obj.estadocategoria, "un estado");
        }

        [HttpPost]
        public IActionResult Create(Categoria obj)
        {
            ValidarCategoria(obj);
            if (!ModelState.IsValid) return View(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = @"INSERT INTO categoria(nom_categoria, descripcion_categoria, estado_categoria)
                               VALUES (@nom,@desc,@estado)";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@nom", obj.nomcategoria ?? "");
                cmd.Parameters.AddWithValue("@desc", (object?)obj.descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", obj.estadocategoria ?? "ACTIVO");
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Categoria? obj = null;
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from categoria where id_categoria=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    obj = new Categoria
                    {
                        idcategoria = Convert.ToInt32(dr["id_categoria"]),
                        nomcategoria = dr["nom_categoria"].ToString(),
                        descripcion = dr["descripcion_categoria"]?.ToString(),
                        estadocategoria = dr["estado_categoria"].ToString()
                    };
                }
            }

            if (obj == null) return NotFound();
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Categoria obj)
        {
            ValidarCategoria(obj);
            if (!ModelState.IsValid) return View(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = @"UPDATE categoria
                               SET nom_categoria=@nom,
                                   descripcion_categoria=@desc,
                                   estado_categoria=@estado
                               WHERE id_categoria=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", obj.idcategoria);
                cmd.Parameters.AddWithValue("@nom", obj.nomcategoria ?? "");
                cmd.Parameters.AddWithValue("@desc", (object?)obj.descripcion ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", obj.estadocategoria ?? "ACTIVO");
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Delete(int id)
        {
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "delete from categoria where id_categoria=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
    }
}
