
// obtiene los campos y controles que utiliza el formulario de productos
const formularioProducto = document.getElementById("formProducto");

const codigoProducto = document.getElementById("codigo");
const descripcionProducto = document.getElementById("descripcion");
const stockProducto = document.getElementById("stock");
const stockMinimoProducto = document.getElementById("stockMinimo");
const unidadProducto = document.getElementById("unidad");
const precioProducto = document.getElementById("precio");

const mensajeProducto = document.getElementById("mensajeProducto");
const guardarProducto = document.getElementById("guardarProducto");

const coincidenciasCodigo = document.getElementById("coincidenciasCodigo");
const coincidenciasDescripcion = document.getElementById("coincidenciasDescripcion");

const dialogoProductos = document.getElementById("dialogoProductos");
const filtroProductos = document.getElementById("filtroProductos");
const resultadosProductos = document.getElementById("resultadosProductos");

let productosDisponibles = [];
// conserva el codigo original para localizar el producto aunque se cambie en el formulario
let codigoEnEdicion = null;

// =====================================
// CONSULTAR PRODUCTOS
// =====================================

async function cargarProductosDisponibles() {
    const respuesta = await fetch("/productos");

    if (!respuesta.ok) {
        throw new Error("No se pudieron cargar los productos.");
    }

    productosDisponibles = await respuesta.json();
}

// =====================================
// MENSAJES
// =====================================

function mostrarMensajeProducto(texto, error = false) {
    mensajeProducto.textContent = texto;
    mensajeProducto.className =
        error ? "mensaje-error" : "mensaje-exito";
}

// =====================================
// SELECCIONAR PRODUCTO
// =====================================

function cargarProductoSeleccionado(producto) {
    codigoEnEdicion = producto.codigo;

    codigoProducto.value = producto.codigo;
    descripcionProducto.value = producto.descripcion;
    stockProducto.value = producto.stock;
    stockMinimoProducto.value = producto.stock_Minimo;
    unidadProducto.value = producto.unidad;
    precioProducto.value = producto.precio;

    guardarProducto.textContent = "Actualizar Producto";

    coincidenciasCodigo.replaceChildren();
    coincidenciasDescripcion.replaceChildren();

    mensajeProducto.textContent = "";
    mensajeProducto.className = "";
}

// =====================================
// NUEVO PRODUCTO
// =====================================

function limpiarFormularioProducto() {
    formularioProducto.reset();

    codigoEnEdicion = null;
    stockMinimoProducto.value = "0";

    guardarProducto.textContent = "Guardar Producto";

    coincidenciasCodigo.replaceChildren();
    coincidenciasDescripcion.replaceChildren();

    mensajeProducto.textContent = "";
    mensajeProducto.className = "";
}

document.getElementById("nuevoProducto")
    .addEventListener("click", limpiarFormularioProducto);

// =====================================
// MAYÚSCULAS
// =====================================

codigoProducto.addEventListener("input", () => {
    codigoProducto.value = codigoProducto.value.toUpperCase();
});

descripcionProducto.addEventListener("input", () => {
    descripcionProducto.value =
        descripcionProducto.value.toUpperCase();
});

// =====================================
// AUTOCOMPLETADO
// =====================================

function mostrarCoincidenciasProductos(campo, contenedor, propiedad) {
    const texto = campo.value.trim().toUpperCase();

    contenedor.replaceChildren();

    if (!texto || codigoEnEdicion !== null) {
        return;
    }

    // limita las sugerencias a diez productos que comiencen con el texto ingresado
    const encontrados = productosDisponibles
        .filter(producto =>
            String(producto[propiedad])
                .toUpperCase()
                .startsWith(texto)
        )
        .slice(0, 10);

    for (const producto of encontrados) {
        const boton = document.createElement("button");
        boton.type = "button";
        boton.className = "coincidencia";

        boton.textContent =
            `${producto.codigo} - ${producto.descripcion}`;

        boton.addEventListener("click", () => {
            cargarProductoSeleccionado(producto);
        });

        contenedor.appendChild(boton);
    }
}

codigoProducto.addEventListener("input", () => {
    mostrarCoincidenciasProductos(
        codigoProducto,
        coincidenciasCodigo,
        "codigo"
    );
});

descripcionProducto.addEventListener("input", () => {
    mostrarCoincidenciasProductos(
        descripcionProducto,
        coincidenciasDescripcion,
        "descripcion"
    );
});

// =====================================
// DIÁLOGO MODAL
// =====================================

function mostrarResultadosProductos() {
    const texto = filtroProductos.value.trim().toUpperCase();

    resultadosProductos.replaceChildren();

    const encontrados = productosDisponibles.filter(producto =>
        producto.codigo.toUpperCase().startsWith(texto) ||
        producto.descripcion.toUpperCase().startsWith(texto)
    );

    if (encontrados.length === 0) {
        resultadosProductos.textContent =
            "No se encontraron productos.";
        return;
    }

    for (const producto of encontrados) {
        const boton = document.createElement("button");

        boton.type = "button";
        boton.className = "resultado-producto";

        boton.textContent =
            `${producto.codigo} - ${producto.descripcion}`;

        boton.addEventListener("click", () => {
            cargarProductoSeleccionado(producto);
            dialogoProductos.close();
        });

        resultadosProductos.appendChild(boton);
    }
}

document.getElementById("abrirBusquedaProducto")
    .addEventListener("click", async () => {
        try {
            await cargarProductosDisponibles();

            filtroProductos.value = "";
            mostrarResultadosProductos();

            dialogoProductos.showModal();
            filtroProductos.focus();
        } catch (error) {
            mostrarMensajeProducto(error.message, true);
        }
    });

filtroProductos.addEventListener(
    "input",
    mostrarResultadosProductos
);

document.getElementById("cerrarBusquedaProducto")
    .addEventListener("click", () => {
        dialogoProductos.close();
    });

// =====================================
// GUARDAR / ACTUALIZAR
// =====================================

formularioProducto.addEventListener("submit", async (evento) => {
    evento.preventDefault();

    const codigo = codigoProducto.value.trim();
    const descripcion = descripcionProducto.value.trim();
    // convierte los textos de los campos numericos antes de validarlos y enviarlos
    const stock = Number(stockProducto.value);
    const stockMinimo = Number(stockMinimoProducto.value);
    const unidad = unidadProducto.value;
    const precio = Number(precioProducto.value);

    if (!codigo || !descripcion) {
        mostrarMensajeProducto(
            "Código y descripción son obligatorios.", true
        );
        return;
    }
    // conserva el limite de la columna aunque el formulario se modifique desde el navegador
    if (codigo.length > 30) {
        mostrarMensajeProducto("El código no puede superar los 30 caracteres.", true);
        codigoProducto.focus();
        return;
    }
    // VALIDAR LONGITUD DE LA DESCRIPCIÓN
    if (descripcion.length > 50) {
        mostrarMensajeProducto(
            `La descripción no puede superar los 50 caracteres. ` +
            `Ingresaste ${descripcion.length} caracteres.`,
            true
        );
        return;
    }

    // identifica el campo que excede la capacidad de las columnas decimales
    for (const [nombre, valor, campo] of [
        ["Stock", stock, stockProducto],
        ["Stock mínimo", stockMinimo, stockMinimoProducto],
        ["Precio", precio, precioProducto]
    ]) {
        if (!Number.isFinite(valor) || valor < 0 || valor > 99999999.99) {
            mostrarMensajeProducto(
                `${nombre} debe ser un número entre 0 y 99999999,99.`,
                true
            );
            campo.focus();
            return;
        }
    }

    const producto = {
        codigo,
        descripcion,
        stock,
        stock_Minimo: stockMinimo,
        unidad,
        precio
    };

    const editando = codigoEnEdicion !== null;

    if (editando) {
        const confirmado = confirm(
            codigoEnEdicion !== codigo
                ? `¿Actualizar el producto y cambiar su código de ${codigoEnEdicion} a ${codigo}?`
                : "¿Actualizar este producto?"
        );

        if (!confirmado) return;
    }

    // utiliza el codigo original en la ruta cuando se modifica un producto existente
    const url = editando
        ? `/productos/${encodeURIComponent(codigoEnEdicion)}`
        : "/productos";

    try {
        guardarProducto.disabled = true;

        const respuesta = await fetch(url, {
            method: editando ? "PUT" : "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(producto)
        });

        if (!respuesta.ok) {
            const error = await respuesta.text();

            throw new Error(
                `No se pudo guardar el producto. ${error}`
            );
        }

        await cargarProductosDisponibles();

        limpiarFormularioProducto();

        mostrarMensajeProducto(
            editando
                ? "Producto actualizado correctamente."
                : "Producto guardado correctamente."
        );

    } catch (error) {
        mostrarMensajeProducto(error.message, true);
    } finally {
        // permite volver a guardar despues de recibir una respuesta o un error
        guardarProducto.disabled = false;
    }
});

// =====================================
// INICIALIZACIÓN
// =====================================

cargarProductosDisponibles().catch(error => {
    mostrarMensajeProducto(error.message, true);
});
