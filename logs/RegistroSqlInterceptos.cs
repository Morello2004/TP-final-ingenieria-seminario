
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace TPFinal.Api.Logs;

public class RegistroSqlInterceptor : DbCommandInterceptor
{
    private readonly IHttpContextAccessor _http;

    public RegistroSqlInterceptor(IHttpContextAccessor http)
    {
        _http = http;
    }

    // identifica la operacion mediante el metodo y la ruta de la solicitud actual
    private string ObtenerRutina()
    {
        var contexto = _http.HttpContext;

        if (contexto == null)
            return "SQL";

        var endpoint = contexto.GetEndpoint() as RouteEndpoint;

        string ruta = endpoint?.RoutePattern.RawText ?? "SinRuta";

        return $"{contexto.Request.Method} {ruta}";
    }

    // registra la consulta y los tipos de parametros sin guardar sus valores
    private void RegistrarSQL(DbCommand comando, string resultado = "Comando ejecutado; lector disponible, lectura y confirmacion pendientes")
    {
        Registro.Log(
            ObtenerRutina(),
            $"SQL: {comando.CommandText} | Parametros: " +
            string.Join(", ", comando.Parameters.Cast<DbParameter>().Select(p =>
                $"{p.ParameterName} ({p.DbType}, Size={p.Size}, Precision={p.Precision}, Scale={p.Scale})")) +
            $" | Resultado: {resultado}"
        );
    }

    // marca el error de la solicitud y delega su escritura sin duplicar la excepcion
    private void RegistrarError(
        DbCommand comando,
        Exception excepcion)
    {
        if (_http.HttpContext is { } contexto)
            contexto.Items[Registro.ErrorRegistrado] = true;
        Registro.Excepcion(ObtenerRutina(), excepcion, comando.CommandText);
    }

    // CONSULTAS SELECT ASINCRÓNICAS
    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        RegistrarSQL(command);

        return await base.ReaderExecutedAsync(
            command, eventData, result, cancellationToken);
    }

    // INSERT, UPDATE Y DELETE ASINCRÓNICOS
    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        RegistrarSQL(command, $"Filas afectadas: {result}; confirmacion de transaccion pendiente si corresponde");

        return await base.NonQueryExecutedAsync(
            command, eventData, result, cancellationToken);
    }

    // CONSULTAS ESCALARES ASINCRÓNICAS
    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        RegistrarSQL(command, "Escalar obtenido; valor omitido");

        return await base.ScalarExecutedAsync(
            command, eventData, result, cancellationToken);
    }

    // ERRORES SQL ASINCRÓNICOS
    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        RegistrarError(command, eventData.Exception);

        return base.CommandFailedAsync(
            command, eventData, cancellationToken);
    }

    // ERRORES SQL SINCRÓNICOS
    public override void CommandFailed(
        DbCommand command,
        CommandErrorEventData eventData)
    {
        RegistrarError(command, eventData.Exception);

        base.CommandFailed(command, eventData);
    }

    // CONSULTAS SELECT SINCRÓNICAS
    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        RegistrarSQL(command);

        return base.ReaderExecuted(command, eventData, result);
    }

    // INSERT, UPDATE Y DELETE SINCRÓNICOS
    public override int NonQueryExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result)
    {
        RegistrarSQL(command, $"Filas afectadas: {result}; confirmacion de transaccion pendiente si corresponde");

        return base.NonQueryExecuted(
            command, eventData, result);
    }

    // CONSULTAS ESCALARES SINCRÓNICAS
    public override object? ScalarExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result)
    {
        RegistrarSQL(command, "Escalar obtenido; valor omitido");

        return base.ScalarExecuted(
            command, eventData, result);
    }
}
