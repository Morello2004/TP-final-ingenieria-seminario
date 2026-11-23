using System.Text.RegularExpressions;

namespace TPFinal.Api.Logs;

// Conserva las llamadas existentes de los endpoints sin guardar sus datos personales.
public sealed class RegistroLoggerProvider(IHttpContextAccessor http) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, http);
    public void Dispose() { }

    private sealed class Logger(string categoria, IHttpContextAccessor http) : ILogger
    {
        // limita este proveedor a los mensajes emitidos por los endpoints
        public bool IsEnabled(LogLevel logLevel) => categoria == "Program" && logLevel != LogLevel.None;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            // obtiene la plantilla del mensaje sin incluir los valores personales recibidos
            string plantilla = state is IEnumerable<KeyValuePair<string, object?>> campos
                ? campos.FirstOrDefault(c => c.Key == "{OriginalFormat}").Value?.ToString() ?? "Operacion HTTP"
                : "Operacion HTTP";
            string rutina = plantilla.Split(':', 2)[0];
            if (exception != null)
            {
                if (http.HttpContext is { } contexto)
                    contexto.Items[Registro.ErrorRegistrado] = true;
                Registro.Excepcion(rutina, exception);
                return;
            }
            // sustituye los marcadores de parametros para no escribir sus valores
            Registro.Log(rutina, Regex.Replace(plantilla, @"\{[^{}]+\}", "[omitido]"));
        }
    }
}
