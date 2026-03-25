using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PRJ_SEMANA_03_S01.Models;
using PRJ_SEMANA_03_S01.Helpers;

namespace PRJ_SEMANA_03_S01.Controllers
{
    [Authorize(Roles = "ADMIN,ADMINISTRADOR,CAJERO")]
    public class ClienteController : Controller
    {
        private readonly IConfiguration _configuration;

        public ClienteController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            List<Cliente> lista = new List<Cliente>();
            string conexion = _configuration.GetConnectionString("ConexionSql")!;

            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from cliente";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    lista.Add(new Cliente
                    {
                        idcliente = Convert.ToInt32(dr["id_cliente"]),
                        nomcliente = dr["nom_cliente"].ToString(),
                        apecliente = dr["ape_cliente"].ToString(),
                        dnicliente = dr["nro_dni_cliente"].ToString(),
                        telefonocliente = dr["telefono_cliente"]?.ToString(),
                        emailcliente = dr["email_cliente"]?.ToString(),
                        direccioncliente = dr["direccion_cliente"]?.ToString(),
                        estadocliente = dr["estado_cliente"].ToString()
                    });
                }
            }

            return View(lista);
        }

        public IActionResult Create() => View(new Cliente { estadocliente = "ACTIVO" });

        private void ValidarCliente(Cliente obj)
        {
            if (!string.IsNullOrEmpty(obj.dnicliente))
                obj.dnicliente = obj.dnicliente.Replace(" ", "");

            if (!string.IsNullOrEmpty(obj.telefonocliente))
                obj.telefonocliente = obj.telefonocliente.Replace(" ", "");

            ValidacionHelper.SoloTexto(ModelState, nameof(obj.nomcliente), obj.nomcliente, "nombre del cliente", 60);
            ValidacionHelper.SoloTexto(ModelState, nameof(obj.apecliente), obj.apecliente, "apellido del cliente", 60);
            ValidacionHelper.SoloEnteros(ModelState, nameof(obj.dnicliente), obj.dnicliente, "DNI del cliente", 8);
            ValidacionHelper.SoloEnteros(ModelState, nameof(obj.telefonocliente), obj.telefonocliente, "teléfono del cliente", 9, obligatorio: false);
            ValidacionHelper.Correo(ModelState, nameof(obj.emailcliente), obj.emailcliente, "correo del cliente", max: 80, obligatorio: false);
            ValidacionHelper.TextoLibre(ModelState, nameof(obj.direccioncliente), obj.direccioncliente, "dirección del cliente", 120, obligatorio: false);
            ValidacionHelper.OpcionTexto(ModelState, nameof(obj.estadocliente), obj.estadocliente, "estado del cliente", max: 15);

            // 2. Validaciones de Unicidad (Si el formato está bien)
            if (ModelState.IsValid)
            {
                // Validamos DNI (Obligatorio)
                if (ExisteEnBD("nro_dni_cliente", obj.dnicliente, obj.idcliente))
                    ModelState.AddModelError(nameof(obj.dnicliente), "El DNI ya está registrado.");

                // Validamos Teléfono (Solo si se escribió algo)
                if (!string.IsNullOrWhiteSpace(obj.telefonocliente))
                    if (ExisteEnBD("telefono_cliente", obj.telefonocliente, obj.idcliente))
                        ModelState.AddModelError(nameof(obj.telefonocliente), "El teléfono ya está registrado.");

                // Validar Email (Solo si se escribió algo)
                if (!string.IsNullOrWhiteSpace(obj.emailcliente))
                    if (ExisteEnBD("email_cliente", obj.emailcliente, obj.idcliente))
                        ModelState.AddModelError(nameof(obj.emailcliente), "El correo ya está registrado.");
            }
        }
        private bool ExisteEnBD(string columna, string valor, int idExcluir)
        {
            int conteo = 0;
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                // Buscamos si existe el valor, pero ignoramos el ID actual (por si es una edición)
                string sql = $"SELECT COUNT(*) FROM cliente WHERE {columna} = @valor AND id_cliente <> @id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@valor", valor);
                cmd.Parameters.AddWithValue("@id", idExcluir);
                cn.Open();
                conteo = (int)cmd.ExecuteScalar();
            }
            return conteo > 0;
        }


        [HttpPost]
        public IActionResult Create(Cliente obj)
        {
            ValidarCliente(obj);
            if (!ModelState.IsValid) return View(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = @"INSERT INTO cliente(nom_cliente, ape_cliente, nro_dni_cliente, telefono_cliente, email_cliente, direccion_cliente, estado_cliente)
                               VALUES (@nom,@ape,@dni,@tel,@email,@dir,@estado)";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@nom", obj.nomcliente ?? "");
                cmd.Parameters.AddWithValue("@ape", obj.apecliente ?? "");
                cmd.Parameters.AddWithValue("@dni", obj.dnicliente ?? "");
                cmd.Parameters.AddWithValue("@tel", (object?)obj.telefonocliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@email", (object?)obj.emailcliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dir", (object?)obj.direccioncliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", obj.estadocliente ?? "ACTIVO");

                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }

        public IActionResult Edit(int id)
        {
            Cliente? obj = null;
            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = "select * from cliente where id_cliente=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    obj = new Cliente
                    {
                        idcliente = Convert.ToInt32(dr["id_cliente"]),
                        nomcliente = dr["nom_cliente"].ToString(),
                        apecliente = dr["ape_cliente"].ToString(),
                        dnicliente = dr["nro_dni_cliente"].ToString(),
                        telefonocliente = dr["telefono_cliente"]?.ToString(),
                        emailcliente = dr["email_cliente"]?.ToString(),
                        direccioncliente = dr["direccion_cliente"]?.ToString(),
                        estadocliente = dr["estado_cliente"].ToString()
                    };
                }
            }

            if (obj == null) return NotFound();
            return View(obj);
        }

        [HttpPost]
        public IActionResult Edit(Cliente obj)
        {
            ValidarCliente(obj);
            if (!ModelState.IsValid) return View(obj);

            string conexion = _configuration.GetConnectionString("ConexionSql")!;
            using (SqlConnection cn = new SqlConnection(conexion))
            {
                string sql = @"UPDATE cliente
                               SET nom_cliente=@nom,
                                   ape_cliente=@ape,
                                   nro_dni_cliente=@dni,
                                   telefono_cliente=@tel,
                                   email_cliente=@email,
                                   direccion_cliente=@dir,
                                   estado_cliente=@estado
                               WHERE id_cliente=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", obj.idcliente);
                cmd.Parameters.AddWithValue("@nom", obj.nomcliente ?? "");
                cmd.Parameters.AddWithValue("@ape", obj.apecliente ?? "");
                cmd.Parameters.AddWithValue("@dni", obj.dnicliente ?? "");
                cmd.Parameters.AddWithValue("@tel", (object?)obj.telefonocliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@email", (object?)obj.emailcliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@dir", (object?)obj.direccioncliente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@estado", obj.estadocliente ?? "ACTIVO");

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
                string sql = "delete from cliente where id_cliente=@id";
                SqlCommand cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@id", id);
                cn.Open();
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Index");
        }
    }
}
