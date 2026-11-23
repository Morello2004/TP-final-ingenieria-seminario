
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace TPFinal.Api.Logs;

public static class Registro
{
    // evita que solicitudes simultaneas mezclen sus escrituras en el archivo
    private static readonly object Bloqueo = new();

    // ubica los archivos de registro junto a los binarios de la aplicacion
    private static readonly string Carpeta =
        Path.Combine(AppContext.BaseDirectory, "logs");

    private static readonly ConditionalWeakTable<Exception, object> Errores = new();
    public const string ErrorRegistrado = "TPFinal.ErrorRegistrado";

    // SQL Server puede incluir valores de claves o datos truncados en el mensaje.
    public static string Describir(Exception excepcion)
    {
        var causa = excepcion.GetBaseException();
        SqlException? sql = null;
        for (Exception? actual = excepcion; actual != null; actual = actual.InnerException)
        {
            if (actual is SqlException errorSql) { sql = errorSql; break; }
        }
        // Incluye apóstrofes dentro de valores y errores SQL con una excepción Win32 interna.
        string mensaje = Regex.Replace((sql ?? causa).Message, @"'.*'", "'[omitido]'", RegexOptions.Singleline);
        mensaje = Regex.Replace(mensaje, @"\(.*\)", "([omitido])", RegexOptions.Singleline);
        return $"Excepcion: {excepcion.GetType().Name} | Codigo: " +
            (sql != null ? sql.Number.ToString() : causa.GetType().Name) +
            $" | Descripcion: {mensaje}";
    }

    // registra una sola vez la causa compartida por el interceptor y el endpoint
    public static void Excepcion(string rutina, Exception excepcion, string? sql = null)
    {
        lock (Bloqueo)
        {
            var causa = excepcion.GetBaseException();
            if (Errores.TryGetValue(causa, out _)) return;
            Errores.Add(causa, new object());
            Error(rutina, "Resultado: ERROR | " + Describir(excepcion) +
                (sql == null ? "" : $" | SQL: {sql}"));
        }
    }

    public static void Log(string rutina, string mensaje)
    {
        Escribir("Log", "LOG", rutina, mensaje);
    }

    public static void Error(string rutina, string mensaje)
    {
        Escribir("Error", "Error", rutina, mensaje);
    }

    private static void Escribir(
        string tipoArchivo,
        string tipoMensaje,
        string rutina,
        string mensaje)
    {
        DateTime fecha = DateTime.Now;

        // separa las operaciones y los errores en archivos diarios
        string nombre =
            $"INGENIERIA_{tipoArchivo}_{fecha:yyyyMMdd}";

        string extension =
            tipoArchivo == "Error" ? ".err" : ".log";

        string ruta = Path.Combine(
            Carpeta, nombre + extension);

        rutina = Normalizar(rutina);
        mensaje = Normalizar(mensaje);

        // aplica el formato de fecha rutina y mensaje solicitado para los registros
        string contenido =
            new string('-', tipoArchivo == "Error" ? 149 : 144) + Environment.NewLine +
            $"F/H: {fecha:dd/MM/yyyy HH:mm:ss} - " +
            $"Rutina: {rutina} - " +
            $"{tipoMensaje}: {mensaje}" +
            Environment.NewLine;

        try
        {
            lock (Bloqueo)
            {
                Directory.CreateDirectory(Carpeta);

                File.AppendAllText(
                    ruta,
                    contenido,
                    Encoding.UTF8);
            }
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine(
                $"Error escribiendo log: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.Error.WriteLine(
                $"Sin permisos para escribir log: {ex.Message}");
        }
    }

    // mantiene cada entrada en una sola linea aunque el mensaje contenga saltos
    private static string Normalizar(string texto)
    {
        return texto
            .Replace("\r", " ")
            .Replace("\n", " ");
    }
}
