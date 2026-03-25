using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace PRJ_SEMANA_03_S01.Helpers
{
    public static class ValidacionHelper
    {
        public static string Limpiar(string? valor) => (valor ?? string.Empty).Trim();

        public static void Requerido(ModelStateDictionary ms, string campo, string? valor, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(valor)) ms.AddModelError(campo, mensaje);
        }

        public static void SoloTexto(ModelStateDictionary ms, string campo, string? valor, string nombreCampo, int max = 60, bool permitirEspacios = true)
        {
            string v = Limpiar(valor);
            if (string.IsNullOrWhiteSpace(v)) { ms.AddModelError(campo, $"El {nombreCampo} es obligatorio."); return; }
            if (v.Length > max) ms.AddModelError(campo, $"El {nombreCampo} no debe superar {max} caracteres.");
            string patron = permitirEspacios ? @"^[A-Za-zÁÉÍÓÚáéíóúÑñ ]+$" : @"^[A-Za-zÁÉÍÓÚáéíóúÑñ]+$";
            if (!Regex.IsMatch(v, patron)) ms.AddModelError(campo, $"El {nombreCampo} solo admite letras.");
        }

        public static void TextoLibre(ModelStateDictionary ms, string campo, string? valor, string nombreCampo, int max, bool obligatorio = false)
        {
            string v = Limpiar(valor);
            if (obligatorio && string.IsNullOrWhiteSpace(v)) { ms.AddModelError(campo, $"El {nombreCampo} es obligatorio."); return; }
            if (!string.IsNullOrWhiteSpace(v) && v.Length > max) ms.AddModelError(campo, $"El {nombreCampo} no debe superar {max} caracteres.");
        }

        public static void SoloEnteros(ModelStateDictionary ms, string campo, string? valor, string nombreCampo, int digitos, bool obligatorio = true)
        {
            string v = Limpiar(valor);
            if (obligatorio && string.IsNullOrWhiteSpace(v)) { ms.AddModelError(campo, $"El {nombreCampo} es obligatorio."); return; }
            if (!string.IsNullOrWhiteSpace(v))
            {
                if (!Regex.IsMatch(v, @"^\d+$")) ms.AddModelError(campo, $"El {nombreCampo} solo admite números.");
                else if (v.Length != digitos) ms.AddModelError(campo, $"El {nombreCampo} debe tener {digitos} dígitos.");
            }
        }

        public static void Correo(ModelStateDictionary ms, string campo, string? valor, string nombreCampo, int max = 100, bool obligatorio = false)
        {
            string v = Limpiar(valor);
            if (obligatorio && string.IsNullOrWhiteSpace(v)) { ms.AddModelError(campo, $"El {nombreCampo} es obligatorio."); return; }
            if (!string.IsNullOrWhiteSpace(v))
            {
                if (v.Length > max) ms.AddModelError(campo, $"El {nombreCampo} no debe superar {max} caracteres.");
                if (!Regex.IsMatch(v, @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) ms.AddModelError(campo, $"El {nombreCampo} no tiene un formato válido.");
            }
        }

        public static void Seleccion(ModelStateDictionary ms, string campo, int valor, string nombreCampo)
        {
            if (valor <= 0) ms.AddModelError(campo, $"Debe seleccionar {nombreCampo}.");
        }

        public static void DecimalPositivo(ModelStateDictionary ms, string campo, decimal valor, string nombreCampo, bool permitirCero = false)
        {
            if ((permitirCero && valor < 0) || (!permitirCero && valor <= 0)) ms.AddModelError(campo, $"El {nombreCampo} debe ser mayor a {(permitirCero ? "o igual a 0" : "0")}.");
        }

        public static void EnteroPositivo(ModelStateDictionary ms, string campo, int valor, string nombreCampo, bool permitirCero = false)
        {
            if ((permitirCero && valor < 0) || (!permitirCero && valor <= 0)) ms.AddModelError(campo, $"El {nombreCampo} debe ser mayor a {(permitirCero ? "o igual a 0" : "0")}.");
        }

        public static void OpcionTexto(ModelStateDictionary ms, string campo, string? valor, string nombreCampo, int max = 20)
        {
            string v = Limpiar(valor);
            if (string.IsNullOrWhiteSpace(v)) ms.AddModelError(campo, $"Debe seleccionar {nombreCampo}.");
            else if (v.Length > max) ms.AddModelError(campo, $"El {nombreCampo} no debe superar {max} caracteres.");
        }

        public static void Usuario(ModelStateDictionary ms, string campo, string? valor, int max = 30)
        {
            string v = Limpiar(valor);
            if (string.IsNullOrWhiteSpace(v)) { ms.AddModelError(campo, "El nombre de usuario es obligatorio."); return; }
            if (v.Length > max) ms.AddModelError(campo, $"El nombre de usuario no debe superar {max} caracteres.");
            if (!Regex.IsMatch(v, @"^[A-Za-z0-9._]+$")) ms.AddModelError(campo, "El nombre de usuario solo admite letras, números, punto y guion bajo.");
        }

        public static void Password(ModelStateDictionary ms, string campo, string? valor, int max = 30)
        {
            string v = Limpiar(valor);
            if (string.IsNullOrWhiteSpace(v)) { ms.AddModelError(campo, "La contraseña es obligatoria."); return; }
            if (v.Length > max) ms.AddModelError(campo, $"La contraseña no debe superar {max} caracteres.");
        }


    }
}
