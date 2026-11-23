using Microsoft.EntityFrameworkCore;
using TPFinal.Api.Datos;
using TPFinal.Api.Entidades;
using TPFinal.Api.Logs;
#if DEBUG
// busca los archivos del sitio desde la carpeta del proyecto al depurar
var carpetaProyecto = Path.GetFullPath(
    Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

if (Directory.Exists(Path.Combine(carpetaProyecto, "wwwroot")))
{
    Directory.SetCurrentDirectory(carpetaProyecto);
}
#endif
var builder = WebApplication.CreateBuilder(args);


// =====================================
// CONFIGURACIÓN DE LOGS
// =====================================

// =====================================
// LOGS PERSISTENTES
// =====================================



// permite asociar los registros de sql con la solicitud que los origino
builder.Services.AddHttpContextAccessor();
// reemplaza los proveedores de registro por el formato de archivos del proyecto
builder.Logging.ClearProviders();
builder.Services.AddSingleton<ILoggerProvider, RegistroLoggerProvider>();

builder.Services.AddSingleton<RegistroSqlInterceptor>();

// obtiene la conexion desde la configuracion que puede ser reemplazada por variables de entorno
builder.Services.AddDbContext<TpDbContext>((servicios, opciones) =>
{
    opciones.UseSqlServer(
        builder.Configuration.GetConnectionString("Ingenieria"));

    opciones.AddInterceptors(
        servicios.GetRequiredService<RegistroSqlInterceptor>());
});



var app = builder.Build();

// Un solo middleware registra el resultado y captura errores fuera de los endpoints.
app.Use(async (contexto, siguiente) =>
{
    var endpoint = contexto.GetEndpoint() as RouteEndpoint;
    string rutina = contexto.Request.Method + " " +
        (endpoint?.RoutePattern.RawText ?? "SinRuta");
    Registro.Log(rutina, "Inicio de operacion");
    try
    {
        await siguiente(contexto);
    }
    catch (Exception excepcion)
    {
        contexto.Items[Registro.ErrorRegistrado] = true;
        Registro.Excepcion(rutina, excepcion);
        if (contexto.Response.HasStarted) throw;
        contexto.Response.Clear();
        contexto.Response.StatusCode = 500;
        await contexto.Response.WriteAsJsonAsync(new
        {
            mensaje = "Error interno del servidor."
        });
    }
    finally
    {
        // registra el estado final y evita repetir un error que ya fue informado
        if (contexto.Response.StatusCode >= 500 &&
            !contexto.Items.ContainsKey(Registro.ErrorRegistrado))
            Registro.Error(rutina, $"Resultado: HTTP {contexto.Response.StatusCode}; sin excepcion disponible");
        Registro.Log(rutina, $"Fin de operacion - HTTP {contexto.Response.StatusCode}");
    }
});
// permite servir la pagina inicial y sus archivos estaticos
app.UseDefaultFiles();
app.UseStaticFiles();

// =====================================
// CLIENTES - CONSULTAR
// =====================================

app.MapGet("/clientes", async (
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "ConsultarClientes: inicio. Sin parámetros."
    );

    try
    {
        var clientes = await db.Cliente.ToListAsync();

        logger.LogInformation(
            "ConsultarClientes: éxito. Cantidad={Cantidad}",
            clientes.Count
        );

        return Results.Ok(clientes);
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "ConsultarClientes: error al consultar la base de datos."
        );

        return Results.Problem(
            "No se pudieron consultar los clientes."
        );
    }
});

// =====================================
// CLIENTES - ALTA
// =====================================

app.MapPost("/clientes", async (
    Cliente cliente,
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "CrearCliente: inicio. CUIT={Cuit}",
        cliente.CUIT
    );

    try
    {
        // normaliza el codigo postal antes de validar su estructura
        cliente.CP = (cliente.CP ?? "").Trim().ToUpperInvariant();



        // acepta cuatro numeros o una letra y cuatro numeros con tres letras finales opcionales
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                cliente.CP,
                @"\A(?:[0-9]{4}|[A-Z][0-9]{4}(?:[A-Z]{3})?)\z"))
        {
            logger.LogWarning("CrearCliente: validacion rechazada por formato de codigo postal");
            return Results.BadRequest(
                "Código postal inválido. Ejemplos: 6700, B6700 o B6700ABC.");
        }

        cliente.RazonSocial = cliente.RazonSocial.ToUpper();
        cliente.Domicilio = cliente.Domicilio.ToUpper();
        cliente.Localidad = cliente.Localidad.ToUpper();
        cliente.CP = cliente.CP.ToUpper();

        // El email conserva mayúsculas y minúsculas.
        cliente.Fecha_Alta = DateTime.Now;
        cliente.Fecha_Baja = null;

        db.Cliente.Add(cliente);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "CrearCliente: éxito. CUIT={Cuit}",
            cliente.CUIT
        );

        return Results.Created(
            $"/clientes/{cliente.CUIT}",
            cliente
        );
    }
    catch (DbUpdateException excepcion)
    {
        logger.LogError(
            excepcion,
            "CrearCliente: error de base de datos. CUIT={Cuit}",
            cliente.CUIT
        );

        return Results.Conflict(
            "No se pudo guardar el cliente. Verifique los datos."
        );
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "CrearCliente: excepción. CUIT={Cuit}",
            cliente.CUIT
        );

        return Results.Problem(
            "No se pudo guardar el cliente."
        );
    }


});

// =====================================
// CLIENTES - MODIFICACIÓN
// =====================================

app.MapPut("/clientes/{cuitOriginal}", async (
    string cuitOriginal,
    Cliente datos,
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "ActualizarCliente: inicio. CUIT original={CuitOriginal}, nuevo={CuitNuevo}",
        cuitOriginal,
        datos.CUIT
    );

    try
    {
        if (string.IsNullOrWhiteSpace(datos.CUIT) ||
            !System.Text.RegularExpressions.Regex.IsMatch(
                datos.CUIT, @"^\d{11}$"))
        {
            logger.LogWarning(
                "ActualizarCliente: CUIT inválido."
            );

            return Results.BadRequest("CUIT inválido.");
        }

        // aplica al modificar el cliente la misma normalizacion del codigo postal que en el alta
        datos.CP = (datos.CP ?? "").Trim().ToUpperInvariant();

        // rechaza codigos postales con longitudes o posiciones de letras incorrectas
        if (!System.Text.RegularExpressions.Regex.IsMatch(
                datos.CP,
                @"\A(?:[0-9]{4}|[A-Z][0-9]{4}(?:[A-Z]{3})?)\z"))
        {
            logger.LogWarning("ActualizarCliente: validacion rechazada por formato de codigo postal");
            return Results.BadRequest(
                "Código postal inválido. Ejemplos: 6700, B6700 o B6700ABC.");
        }

        var cliente = await db.Cliente
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CUIT == cuitOriginal);

        if (cliente is null)
        {
            logger.LogWarning(
                "ActualizarCliente: cliente no encontrado. CUIT={Cuit}",
                cuitOriginal
            );

            return Results.NotFound(
                "Cliente no encontrado."
            );
        }

        if (cuitOriginal != datos.CUIT)
        {
            bool existe = await db.Cliente
                .AnyAsync(c => c.CUIT == datos.CUIT);

            if (existe)
            {
                logger.LogWarning(
                    "ActualizarCliente: CUIT duplicado."
                );

                return Results.Conflict(
                    "El nuevo CUIT ya pertenece a otro cliente."
                );
            }
        }

        // La base migrada usa NO ACTION. Conservamos el cambio de clave original
        // copiando el principal y trasladando sus referencias en una transacción.
        await using var transaccion = cuitOriginal != datos.CUIT
            ? await db.Database.BeginTransactionAsync() : null;
        if (transaccion != null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO dbo.CLIENTE
                (CUIT, RazonSocial, Domicilio, Localidad, CP, Celular, Email, Fecha_Alta, Fecha_Baja)
                SELECT {datos.CUIT}, RazonSocial, Domicilio, Localidad, CP, Celular, Email, Fecha_Alta, Fecha_Baja
                FROM dbo.CLIENTE WHERE CUIT = {cuitOriginal};
                """);
            await db.Pedidos_Cabecera.Where(p => p.CUIT == cuitOriginal)
                .ExecuteUpdateAsync(c => c.SetProperty(p => p.CUIT, datos.CUIT));
            await db.Cliente.Where(c => c.CUIT == cuitOriginal).ExecuteDeleteAsync();
        }

        // actualiza los datos usando la clave que quedo vigente luego del posible cambio de cuit
        int modificados = await db.Cliente
            .Where(c => c.CUIT == datos.CUIT)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(c => c.CUIT, datos.CUIT)
                .SetProperty(c => c.RazonSocial,
                    datos.RazonSocial.ToUpper())
                .SetProperty(c => c.Domicilio,
                    datos.Domicilio.ToUpper())
                .SetProperty(c => c.Localidad,
                    datos.Localidad.ToUpper())
                .SetProperty(c => c.CP,
                    datos.CP.ToUpper())
                .SetProperty(c => c.Celular, datos.Celular)
                .SetProperty(c => c.Email, datos.Email));

        if (modificados == 0)
        {
            logger.LogWarning(
                "ActualizarCliente: no se modificaron registros."
            );

            return Results.NotFound(
                "Cliente no encontrado."
            );
        }

        if (transaccion != null) await transaccion.CommitAsync();

        logger.LogInformation(
            "ActualizarCliente: éxito. CUIT anterior={Anterior}, nuevo={Nuevo}",
            cuitOriginal,
            datos.CUIT
        );

        return Results.Ok(new
        {
            mensaje = "Cliente actualizado correctamente.",
            cuitAnterior = cuitOriginal,
            cuitNuevo = datos.CUIT
        });
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "ActualizarCliente: excepción. CUIT original={Cuit}",
            cuitOriginal
        );

        return Results.Problem(
            "No se pudo actualizar el cliente."
        );
    }
});

// =====================================
// PRODUCTOS - CONSULTAR
// =====================================

app.MapGet("/productos", async (
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "ConsultarProductos: inicio. Sin parámetros."
    );

    try
    {
        var productos = await db.Producto.ToListAsync();

        logger.LogInformation(
            "ConsultarProductos: éxito. Cantidad={Cantidad}",
            productos.Count
        );

        return Results.Ok(productos);
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "ConsultarProductos: error de consulta."
        );

        return Results.Problem(
            "No se pudieron consultar los productos."
        );
    }
});

// =====================================
// PRODUCTOS - ALTA
// =====================================

app.MapPost("/productos", async (
    Producto producto,
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "CrearProducto: inicio. Código={Codigo}, Unidad={Unidad}, Stock={Stock}, Precio={Precio}",
        producto.Codigo,
        producto.Unidad,
        producto.Stock,
        producto.Precio
    );

    try
    {
        producto.Descripcion = producto.Descripcion.ToUpper();
        producto.Unidad = producto.Unidad.ToUpper();

        producto.Fecha_Alta = DateTime.Now;
        producto.Fecha_Baja = null;

        db.Producto.Add(producto);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "CrearProducto: éxito. Código={Codigo}",
            producto.Codigo
        );

        return Results.Created(
            $"/productos/{producto.Codigo}",
            producto
        );
    }

    catch (DbUpdateException excepcion)
    {
        logger.LogError(
            excepcion,
            "CrearProducto: error de base de datos. Código={Codigo}",
            producto.Codigo
        );

        return Results.Conflict(
            "No se pudo guardar el producto. Verifique los datos."
        );
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "CrearProducto: excepción. Código={Codigo}",
            producto.Codigo
        );

        return Results.Problem(
            "No se pudo guardar el producto."
        );
    }

});

// =====================================
// PRODUCTOS - MODIFICACIÓN
// =====================================

app.MapPut("/productos/{codigoOriginal}", async (
    string codigoOriginal,
    Producto datos,
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "ActualizarProducto: inicio. Código original={Original}, nuevo={Nuevo}, Stock={Stock}, Precio={Precio}",
        codigoOriginal,
        datos.Codigo,
        datos.Stock,
        datos.Precio
    );

    try
    {
        if (string.IsNullOrWhiteSpace(datos.Codigo) ||
            string.IsNullOrWhiteSpace(datos.Descripcion))
        {
            logger.LogWarning(
                "ActualizarProducto: código o descripción vacíos."
            );

            return Results.BadRequest(
                "El código y la descripción son obligatorios."
            );
        }

        if (datos.Stock < 0 ||
            datos.Stock_Minimo < 0 ||
            datos.Precio < 0)
        {
            logger.LogWarning(
                "ActualizarProducto: valores negativos."
            );

            return Results.BadRequest(
                "Stock, stock mínimo y precio no pueden ser negativos."
            );
        }

        string[] unidadesValidas =
            { "KG", "MTS", "LITROS", "UNIDADES" };

        if (!unidadesValidas.Contains(datos.Unidad))
        {
            logger.LogWarning(
                "ActualizarProducto: unidad inválida. Unidad={Unidad}",
                datos.Unidad
            );

            return Results.BadRequest(
                "Unidad inválida."
            );
        }

        var existente = await db.Producto
            .AsNoTracking()
            .FirstOrDefaultAsync(p =>
                p.Codigo == codigoOriginal);

        if (existente is null)
        {
            logger.LogWarning(
                "ActualizarProducto: producto no encontrado. Código={Codigo}",
                codigoOriginal
            );

            return Results.NotFound(
                "Producto no encontrado."
            );
        }

        if (codigoOriginal != datos.Codigo &&
            await db.Producto.AnyAsync(p =>
                p.Codigo == datos.Codigo))
        {
            logger.LogWarning(
                "ActualizarProducto: código duplicado. Código={Codigo}",
                datos.Codigo
            );

            return Results.Conflict(
                "Ya existe un producto con ese código."
            );
        }

        // agrupa el cambio de codigo y sus referencias para confirmar o revertir todo junto
        await using var transaccion = codigoOriginal != datos.Codigo
            ? await db.Database.BeginTransactionAsync() : null;
        if (transaccion != null)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO dbo.PRODUCTOS
                (Codigo, Descripcion, Stock, Stock_Minimo, Unidad, Precio, Fecha_Alta, Fecha_Baja)
                SELECT {datos.Codigo}, Descripcion, Stock, Stock_Minimo, Unidad, Precio, Fecha_Alta, Fecha_Baja
                FROM dbo.PRODUCTOS WHERE Codigo = {codigoOriginal};
                """);
            // traslada las referencias al nuevo codigo antes de eliminar el registro anterior
            await db.Pedidos_Detalle.Where(d => d.Codigo_Producto == codigoOriginal)
                .ExecuteUpdateAsync(c => c.SetProperty(d => d.Codigo_Producto, datos.Codigo));
            await db.Producto.Where(p => p.Codigo == codigoOriginal).ExecuteDeleteAsync();
        }

        int modificados = await db.Producto
            .Where(p => p.Codigo == datos.Codigo)
            .ExecuteUpdateAsync(cambios => cambios
                .SetProperty(p => p.Codigo, datos.Codigo)
                .SetProperty(p => p.Descripcion,
                    datos.Descripcion.ToUpper())
                .SetProperty(p => p.Stock, datos.Stock)
                .SetProperty(p => p.Stock_Minimo,
                    datos.Stock_Minimo)
                .SetProperty(p => p.Unidad, datos.Unidad)
                .SetProperty(p => p.Precio, datos.Precio));

        if (modificados == 0)
        {
            logger.LogWarning(
                "ActualizarProducto: no se modificaron registros."
            );

            return Results.NotFound(
                "Producto no encontrado."
            );
        }

        if (transaccion != null) await transaccion.CommitAsync();

        logger.LogInformation(
            "ActualizarProducto: éxito. Código anterior={Anterior}, nuevo={Nuevo}",
            codigoOriginal,
            datos.Codigo
        );

        return Results.Ok(new
        {
            mensaje = "Producto actualizado correctamente.",
            codigoAnterior = codigoOriginal,
            codigoNuevo = datos.Codigo
        });
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "ActualizarProducto: excepción. Código={Codigo}",
            codigoOriginal
        );

        return Results.Problem(
            "No se pudo actualizar el producto."
        );
    }
});

// =====================================
// PEDIDOS - CONSULTAR
// =====================================

app.MapGet("/pedidos", async (
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "ConsultarPedidos: inicio. Sin parámetros."
    );

    try
    {
        var pedidos = await db.Pedidos_Cabecera.ToListAsync();

        logger.LogInformation(
            "ConsultarPedidos: éxito. Cantidad={Cantidad}",
            pedidos.Count
        );

        return Results.Ok(pedidos);
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "ConsultarPedidos: error de consulta."
        );

        return Results.Problem(
            "No se pudieron consultar los pedidos."
        );
    }
});

// =====================================
// PEDIDOS - ALTA
// =====================================

app.MapPost("/pedidos", async (
    NuevoPedido nuevoPedido,
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "CrearPedido: inicio. CUIT={Cuit}, Operador={Operador}, CantidadDetalles={Cantidad}",
        nuevoPedido.CUIT,
        nuevoPedido.Operador,
        nuevoPedido.Detalles?.Count()
    );

    try
    {
        // conserva la numeracion original sumando uno al mayor numero de pedido existente
        int nuevoID = await db.Pedidos_Cabecera.AnyAsync()
            ? await db.Pedidos_Cabecera
                .MaxAsync(p => p.ID_Pedido) + 1
            : 1;

        var cabecera = new PedidoCabecera
        {
            ID_Pedido = nuevoID,
            Fecha_Hora = DateTime.Now,
            CUIT = nuevoPedido.CUIT,
            Operador = nuevoPedido.Operador.ToUpper(),
            Estado = "PENDIENTE"
        };

        db.Pedidos_Cabecera.Add(cabecera);

        // controla que cada producto aparezca una sola vez en el pedido
        var productosIncluidos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in nuevoPedido.Detalles!)
        {
            logger.LogInformation(
                "CrearPedido: detalle. Producto={Codigo}, Cantidad={Cantidad}",
                item.Codigo_Producto,
                item.Cantidad
            );

            var producto = await db.Producto
                .FirstOrDefaultAsync(p =>
                    p.Codigo == item.Codigo_Producto);

            if (producto is null)
            {
                logger.LogWarning(
                    "CrearPedido: producto inexistente. Código={Codigo}",
                    item.Codigo_Producto
                );

                return Results.BadRequest(
                    $"No existe el producto {item.Codigo_Producto}."
                );
            }

            // evita el conflicto de seguimiento antes de agregar otro detalle con la misma clave
            if (!productosIncluidos.Add(producto.Codigo))
            {
                logger.LogWarning("CrearPedido: validacion rechazada por producto repetido");
                return Results.BadRequest(
                    "El pedido contiene un producto repetido. Cada producto debe aparecer en una sola fila.");
            }

            // rechaza fracciones en productos vendidos por unidades incluso en llamadas directas a la api
            if (producto.Unidad == "UNIDADES" && item.Cantidad != decimal.Truncate(item.Cantidad))
            {
                logger.LogWarning("CrearPedido: validacion rechazada por cantidad decimal en unidades");
                return Results.BadRequest("Para UNIDADES, ingresá una cantidad entera como 1, 2 o 3.");
            }

            // toma la unidad y el precio del producto almacenado en la base
            var detalle = new PedidoDetalle
            {
                ID_Pedido = nuevoID,
                Codigo_Producto = producto.Codigo,
                Cantidad = item.Cantidad,
                Unidad = producto.Unidad,
                Precio = producto.Precio
            };

            db.Pedidos_Detalle.Add(detalle);
        }

        // Mantiene MAX + 1 y la columna IDENTITY existente sin cambiar el esquema.
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var transaccion = await db.Database.BeginTransactionAsync();
            bool identityInsert = false;
            try
            {
                // habilita el numero calculado por la aplicacion en esta misma conexion
                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.PEDIDOS_CABECERA ON");
                identityInsert = true;
                await db.SaveChangesAsync();
                await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.PEDIDOS_CABECERA OFF");
                identityInsert = false;
                await transaccion.CommitAsync();
            }
            finally
            {
                if (identityInsert)
                {
                    // Primero revierte una transacción fallida, luego restablece la sesión.
                    try { await transaccion.RollbackAsync(); }
                    catch (Exception limpieza) { Registro.Excepcion("CrearPedido.Rollback", limpieza); }
                    try
                    {
                        await db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT dbo.PEDIDOS_CABECERA OFF");
                    }
                    catch (Exception limpieza)
                    {
                        Registro.Excepcion("CrearPedido.IdentityInsert", limpieza);
                        // impide reutilizar una conexion cuya configuracion de identidad no pudo restaurarse
                        Microsoft.Data.SqlClient.SqlConnection.ClearPool(
                            (Microsoft.Data.SqlClient.SqlConnection)db.Database.GetDbConnection());
                    }
                }
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }

        logger.LogInformation(
            "CrearPedido: éxito. ID={IdPedido}",
            nuevoID
        );

        return Results.Created(
            $"/pedidos/{nuevoID}",
            cabecera
        );
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "CrearPedido: excepción. CUIT={Cuit}",
            nuevoPedido.CUIT
        );

        return Results.Problem(
            "No se pudo guardar el pedido."
        );
    }
});

// =====================================
// PEDIDOS - CONSULTAR DETALLES
// =====================================

app.MapGet("/pedidos/detalles", async (
    TpDbContext db,
    ILogger<Program> logger) =>
{
    logger.LogInformation(
        "ConsultarDetallesPedidos: inicio. Sin parámetros."
    );

    try
    {
        var detalles = await db.Pedidos_Detalle.ToListAsync();

        logger.LogInformation(
            "ConsultarDetallesPedidos: éxito. Cantidad={Cantidad}",
            detalles.Count
        );

        return Results.Ok(detalles);
    }
    catch (Exception excepcion)
    {
        logger.LogError(
            excepcion,
            "ConsultarDetallesPedidos: excepción."
        );

        return Results.Problem(
            "No se pudieron consultar los detalles."
        );
    }
});

app.Run();
