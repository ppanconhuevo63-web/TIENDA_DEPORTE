using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR")]
    public class ProveedorController : Controller
    {
        private readonly IConfiguration _configuration;

        public ProveedorController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<Proveedor> lista = new List<Proveedor>();
            string conexion = _configuration.GetConnectionString("ConexionSql")!;

            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from proveedor";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new Proveedor
                    {
                        idproveedor = Convert.ToInt32(dr["id_proveedor"]),
                        ruc = dr["ruc_proveedor"].ToString(),
                        razonsocial = dr["razon_social"].ToString(),
                        telefono = dr["telefono_proveedor"]?.ToString(),
                        email = dr["email_proveedor"]?.ToString(),
                        direccion = dr["direccion_proveedor"]?.ToString(),
                        estadoproveedor = dr["estado_proveedor"].ToString()
                    });
                }
            }
            return View(lista);
        }

        public IActionResult Create() => View(new Proveedor { estadoproveedor = "ACTIVO" });

        private void ValidarProveedor(Proveedor obj)
        {
            ValidacionHelper.SoloEnteros(ModelState, nameof(obj.ruc), obj.ruc, "RUC", 11);
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.razonsocial), obj.razonsocial, "razón social", 100, obligatorio: true);
            ValidacionHelper.SoloEnteros(ModelState, nameof(obj.telefono), obj.telefono, "teléfono", 9, obligatorio: false);
            ValidacionHelper.Correo(ModelState, nameof(obj.email), obj.email, "correo del proveedor");
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.direccion), obj.direccion, "dirección del proveedor", 120);
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadoproveedor), obj.estadoproveedor, "un estado");
        }

        [HttpPost]
        public IActionResult Create(Proveedor obj)
        {
            ValidarProveedor(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                cn.Open();

                // Validar RUC único
                string sqlRuc = "SELECT COUNT(*) FROM proveedor WHERE ruc_proveedor = @ruc";
                using (SqlCommand cmdRuc = new SqlCommand(sqlRuc, cn))
                {
                    cmdRuc.Parameters.AddWithValue("@ruc", obj.ruc ?? "");
                    int existeRuc = (int)cmdRuc.ExecuteScalar();
                    if (existeRuc > 0)
                    {
                        ModelState.AddModelError("ruc", "El RUC ya está registrado");
                    }
                }

                // Validar teléfono único (solo si tiene valor)
                if (!string.IsNullOrEmpty(obj.telefono))
                {
                    string sqlTel = "SELECT COUNT(*) FROM proveedor WHERE telefono_proveedor = @tel";
                    using (SqlCommand cmdTel = new SqlCommand(sqlTel, cn))
                    {
                        cmdTel.Parameters.AddWithValue("@tel", obj.telefono);
                        int existeTel = (int)cmdTel.ExecuteScalar();
                        if (existeTel > 0)
                        {
                            ModelState.AddModelError("telefono", "El teléfono ya está registrado");
                        }
                    }
                }

                // Validar email único (solo si tiene valor)
                if (!string.IsNullOrEmpty(obj.email))
                {
                    string sqlEmail = "SELECT COUNT(*) FROM proveedor WHERE email_proveedor = @email";
                    using (SqlCommand cmdEmail = new SqlCommand(sqlEmail, cn))
                    {
                        cmdEmail.Parameters.AddWithValue("@email", obj.email);
                        int existeEmail = (int)cmdEmail.ExecuteScalar();
                        if (existeEmail > 0)
                        {
                            ModelState.AddModelError("email", "El email ya está registrado");
                        }
                    }
                }

                if (!ModelState.IsValid)
                {
                    return View(obj);
                }

                // Insertar
                string sql = @"INSERT INTO proveedor(ruc_proveedor, razon_social, telefono_proveedor, email_proveedor, direccion_proveedor, estado_proveedor)
                       VALUES (@ruc,@rs,@tel,@email,@dir,@estado)";
                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@ruc", obj.ruc ?? "");
                    cmd.Parameters.AddWithValue("@rs", obj.razonsocial ?? "");
                    cmd.Parameters.AddWithValue("@tel", (object?)obj.telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", (object?)obj.email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dir", (object?)obj.direccion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@estado", obj.estadoproveedor ?? "ACTIVO");
                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Proveedor? obj = null;
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from proveedor where id_proveedor=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    obj = new Proveedor
                    {
                        idproveedor = Convert.ToInt32(dr["id_proveedor"]),
                        ruc = dr["ruc_proveedor"].ToString(),
                        razonsocial = dr["razon_social"].ToString(),
                        telefono = dr["telefono_proveedor"]?.ToString(),
                        email = dr["email_proveedor"]?.ToString(),
                        direccion = dr["direccion_proveedor"]?.ToString(),
                        estadoproveedor = dr["estado_proveedor"].ToString()
                    };
                }
            }

            if (obj == null) return NotFound();
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Proveedor obj)
        {
            ValidarProveedor(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                cn.Open();

                // ✅ Validar RUC único (excluyendo el mismo ID)
                string sqlRuc = @"SELECT COUNT(*) 
                          FROM proveedor 
                          WHERE ruc_proveedor = @ruc 
                          AND id_proveedor <> @id";

                using (SqlCommand cmdRuc = new SqlCommand(sqlRuc, cn))
                {
                    cmdRuc.Parameters.AddWithValue("@ruc", obj.ruc ?? "");
                    cmdRuc.Parameters.AddWithValue("@id", obj.idproveedor);

                    int existeRuc = (int)cmdRuc.ExecuteScalar();
                    if (existeRuc > 0)
                    {
                        ModelState.AddModelError("ruc", "El RUC ya está registrado");
                    }
                }

                // ✅ Validar teléfono único
                if (!string.IsNullOrEmpty(obj.telefono))
                {
                    string sqlTel = @"SELECT COUNT(*) 
                              FROM proveedor 
                              WHERE telefono_proveedor = @tel 
                              AND id_proveedor <> @id";

                    using (SqlCommand cmdTel = new SqlCommand(sqlTel, cn))
                    {
                        cmdTel.Parameters.AddWithValue("@tel", obj.telefono);
                        cmdTel.Parameters.AddWithValue("@id", obj.idproveedor);

                        int existeTel = (int)cmdTel.ExecuteScalar();
                        if (existeTel > 0)
                        {
                            ModelState.AddModelError("telefono", "El teléfono ya está registrado");
                        }
                    }
                }

                // ✅ Validar email único
                if (!string.IsNullOrEmpty(obj.email))
                {
                    string sqlEmail = @"SELECT COUNT(*) 
                                FROM proveedor 
                                WHERE email_proveedor = @email 
                                AND id_proveedor <> @id";

                    using (SqlCommand cmdEmail = new SqlCommand(sqlEmail, cn))
                    {
                        cmdEmail.Parameters.AddWithValue("@email", obj.email);
                        cmdEmail.Parameters.AddWithValue("@id", obj.idproveedor);

                        int existeEmail = (int)cmdEmail.ExecuteScalar();
                        if (existeEmail > 0)
                        {
                            ModelState.AddModelError("email", "El email ya está registrado");
                        }
                    }
                }

                // 🚫 Si hay errores, regresar a la vista
                if (!ModelState.IsValid)
                {
                    return View(obj);
                }

                // ✅ UPDATE
                string sql = @"UPDATE proveedor
                       SET ruc_proveedor=@ruc,
                           razon_social=@rs,
                           telefono_proveedor=@tel,
                           email_proveedor=@email,
                           direccion_proveedor=@dir,
                           estado_proveedor=@estado
                       WHERE id_proveedor=@id";

                using (SqlCommand cmd = new SqlCommand(sql, cn))
                {
                    cmd.Parameters.AddWithValue("@id", obj.idproveedor);
                    cmd.Parameters.AddWithValue("@ruc", obj.ruc ?? "");
                    cmd.Parameters.AddWithValue("@rs", obj.razonsocial ?? "");
                    cmd.Parameters.AddWithValue("@tel", (object?)obj.telefono ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@email", (object?)obj.email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@dir", (object?)obj.direccion ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@estado", obj.estadoproveedor ?? "ACTIVO");

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }
        public IActionResult Delete(int id)
        {
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "delete from proveedor where id_proveedor=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
    }
}
