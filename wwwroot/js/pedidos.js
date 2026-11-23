// obtiene los controles para seleccionar el cliente y armar el pedido
const formularioPedido = document.getElementById("formPedido");

const buscarCliente = document.getElementById("buscarClientePedido");
const buscarProducto = document.getElementById("buscarProductoPedido");

const coincidenciasCliente =
    document.getElementById("coincidenciasClientePedido");

const coincidenciasProducto =
    document.getElementById("coincidenciasProductoPedido");

const botonAgregar = document.getElementById("agregarProducto");
const tablaDetalle = document.getElementById("detallePedido");
const mensajePedido = document.getElementById("mensajePedido");

const campoCantidad = document.getElementById("cantidad");
const etiquetaCantidad = document.getElementById("etiquetaCantidad");
const campoOperador = document.getElementById("operador");
let ultimaCantidadEntera = "";

let clientesPedido = [];
let productosPedido = [];

let clienteSeleccionado = null;
let productoSeleccionado = null;

// mantiene los productos agregados hasta que se envia o se limpia el pedido
const detalles = [];

// adapta los controles de cantidad a la unidad del producto seleccionado
function configurarCantidad(unidad = null) {
    const porUnidades = unidad === null || unidad === "UNIDADES";
    etiquetaCantidad.textContent = unidad ? `Cantidad (${unidad})` : "Cantidad";
    campoCantidad.step = porUnidades ? "1" : "0.01";
    campoCantidad.min = porUnidades ? "1" : "0.01";
    campoCantidad.max = porUnidades ? "99999999" : "99999999.99";
    campoCantidad.inputMode = porUnidades ? "numeric" : "decimal";
    if (porUnidades && !/^[0-9]*$/.test(campoCantidad.value)) {
        campoCantidad.value = "";
    }
    ultimaCantidadEntera = /^[0-9]*$/.test(campoCantidad.value) ? campoCantidad.value : "";
}

// bloquea separadores decimales y notacion cientifica cuando se ingresan unidades
campoCantidad.addEventListener("beforeinput", evento => {
    if (productoSeleccionado?.unidad === "UNIDADES" && evento.data && /[^0-9]/.test(evento.data)) {
        evento.preventDefault();
        mostrarErrorPedido("Para UNIDADES, ingresá una cantidad entera como 1, 2 o 3.", campoCantidad);
    }
});

// rechaza el pegado de fracciones sin convertirlas en otra cantidad
campoCantidad.addEventListener("paste", evento => {
    if (productoSeleccionado?.unidad === "UNIDADES" &&
        !/^[0-9]+$/.test(evento.clipboardData.getData("text"))) {
        evento.preventDefault();
        mostrarErrorPedido("Para UNIDADES, ingresá una cantidad entera como 1, 2 o 3.", campoCantidad);
    }
});

// restaura el valor anterior si otro metodo de entrada introduce una fraccion en unidades
campoCantidad.addEventListener("input", () => {
    if (productoSeleccionado?.unidad !== "UNIDADES") return;
    if (campoCantidad.validity.badInput || !/^[0-9]*$/.test(campoCantidad.value)) {
        campoCantidad.value = ultimaCantidadEntera;
        mostrarErrorPedido("Para UNIDADES, ingresá una cantidad entera como 1, 2 o 3.", campoCantidad);
        return;
    }
    ultimaCantidadEntera = campoCantidad.value;
});


// ---------------------------------
// CARGAR DATOS
// ---------------------------------

async function cargarDatosPedido() {
    // obtiene listas actuales y comprueba los errores antes de utilizarlas
    const respuestaClientes = await fetch("/clientes", { cache: "no-store" });
    if (!respuestaClientes.ok) {
        throw new Error("No se pudieron cargar los clientes del pedido. Volvé a intentar.");
    }

    const respuestaProductos = await fetch("/productos", { cache: "no-store" });
    if (!respuestaProductos.ok) {
        throw new Error("No se pudieron cargar los productos del pedido. Volvé a intentar.");
    }

    clientesPedido = await respuestaClientes.json();
    productosPedido = await respuestaProductos.json();

    // actualiza las sugerencias si se escribio mientras se cargaban los productos
    if (document.activeElement === buscarProducto && productoSeleccionado === null) {
        buscarProducto.dispatchEvent(new Event("input"));
    }
}

cargarDatosPedido().catch(error => {
    mostrarErrorPedido(error.message, buscarProducto);
});


// ---------------------------------
// BUSCAR CLIENTE
// CUIT O RAZÓN SOCIAL
// ---------------------------------

buscarCliente.addEventListener("input", () => {
    const texto = buscarCliente.value.trim().toUpperCase();

    clienteSeleccionado = null;
    coincidenciasCliente.innerHTML = "";

    if (texto === "") {
        return;
    }

    const encontrados = clientesPedido.filter(cliente =>
        cliente.cuit.includes(texto) ||
        cliente.razonSocial.toUpperCase().includes(texto)
    );

    encontrados.forEach(cliente => {
        const opcion = document.createElement("div");

        opcion.className = "coincidencia";

        opcion.textContent =
            `${cliente.cuit} - ${cliente.razonSocial}`;

        opcion.addEventListener("click", () => {
            clienteSeleccionado = cliente;

            buscarCliente.value =
                `${cliente.cuit} - ${cliente.razonSocial}`;

            coincidenciasCliente.innerHTML = "";
            limpiarMensajePedido();
        });

        coincidenciasCliente.appendChild(opcion);
    });
});


// ---------------------------------
// BUSCAR PRODUCTO POR NOMBRE
// ---------------------------------

buscarProducto.addEventListener("input", () => {
    const texto = buscarProducto.value.trim().toUpperCase();

    productoSeleccionado = null;
    configurarCantidad();
    coincidenciasProducto.innerHTML = "";

    const encontrados = productosPedido.filter(producto =>
        producto.descripcion.toUpperCase().includes(texto)
    );

    // distingue una lista vacia de una busqueda que no tiene coincidencias
    if (encontrados.length === 0) {
        const aviso = document.createElement("div");
        aviso.className = "coincidencia";
        aviso.textContent = productosPedido.length === 0
            ? "No hay productos cargados en la lista del pedido."
            : "No se encontraron productos con ese nombre.";
        coincidenciasProducto.appendChild(aviso);
        return;
    }

    encontrados.forEach(producto => {
        const opcion = document.createElement("div");

        opcion.className = "coincidencia";

        opcion.textContent =
            `${producto.codigo} - ${producto.descripcion}`;

        opcion.addEventListener("click", () => {
            productoSeleccionado = producto;
            configurarCantidad(producto.unidad);

            buscarProducto.value =
                producto.descripcion;

            coincidenciasProducto.innerHTML = "";
            limpiarMensajePedido();
        });

        coincidenciasProducto.appendChild(opcion);
    });
});

// muestra los productos disponibles al entrar en el campo sin borrar una seleccion existente
buscarProducto.addEventListener("focus", () => {
    if (productoSeleccionado === null) {
        buscarProducto.dispatchEvent(new Event("input"));
    }
});


// ---------------------------------
// MENSAJES
// ---------------------------------

function mostrarErrorPedido(texto, campo) {
    mensajePedido.textContent = texto;
    mensajePedido.className = "mensaje-error";

    document.querySelectorAll(".campo-error").forEach(elemento => {
        elemento.classList.remove("campo-error");
    });

    campo.classList.add("campo-error");
    campo.focus();

    window.scrollTo({
        top: 0,
        behavior: "smooth"
    });
}

function mostrarExitoPedido(texto) {
    mensajePedido.textContent = texto;
    mensajePedido.className = "mensaje-exito";
}

function limpiarMensajePedido() {
    mensajePedido.textContent = "";
    mensajePedido.className = "";

    document.querySelectorAll(".campo-error").forEach(elemento => {
        elemento.classList.remove("campo-error");
    });
}

// ---------------------------------
// AGREGAR PRODUCTO
// ---------------------------------

botonAgregar.addEventListener("click", () => {
    const cantidad = Number(campoCantidad.value);

    if (productoSeleccionado === null) {
        mostrarErrorPedido(
            "Seleccione un producto de la lista.",
            buscarProducto
        );
        return;
    }

    // evita repetir la clave del producto dentro del mismo pedido
    if (detalles.some(detalle => detalle.codigo_Producto === productoSeleccionado.codigo)) {
        mostrarErrorPedido(
            "El producto ya está agregado al pedido. Eliminá la fila anterior si querés cambiar la cantidad.",
            buscarProducto
        );
        return;
    }

    const porUnidades = productoSeleccionado.unidad === "UNIDADES";
    const minimo = porUnidades ? 1 : 0.01;
    const maximo = porUnidades ? 99999999 : 99999999.99;

    // evita cantidades que no se pueden almacenar o no corresponden a la unidad
    if (!Number.isFinite(cantidad) || cantidad < minimo || cantidad > maximo) {
        mostrarErrorPedido(
            porUnidades
                ? "La cantidad debe ser un número entero entre 1 y 99999999."
                : "La cantidad debe ser un número entre 0,01 y 99999999,99.",
            campoCantidad
        );
        return;
    }

    // impide agregar fracciones cuando el producto se vende por unidades
    if (porUnidades && !Number.isInteger(cantidad)) {
        mostrarErrorPedido("Para UNIDADES, ingresá una cantidad entera como 1, 2 o 3.", campoCantidad);
        return;
    }

    // respeta los dos decimales admitidos para cantidades fraccionarias
    if (campoCantidad.validity.stepMismatch) {
        mostrarErrorPedido(
            "La cantidad debe tener como máximo dos decimales.",
            campoCantidad
        );
        return;
    }

    const detalle = {
        codigo_Producto: productoSeleccionado.codigo,
        descripcion: productoSeleccionado.descripcion,
        cantidad: cantidad,
        unidad: productoSeleccionado.unidad,
        precio: productoSeleccionado.precio
    };

    detalles.push(detalle);

    mostrarDetalles();

    productoSeleccionado = null;

    buscarProducto.value = "";
    campoCantidad.value = "";
    configurarCantidad();

    limpiarMensajePedido();
});


// ---------------------------------
// MOSTRAR DETALLE
// ---------------------------------

function mostrarDetalles() {
    tablaDetalle.innerHTML = "";

    detalles.forEach((detalle, indice) => {
        const fila = document.createElement("tr");

        fila.innerHTML = `
            <td>${detalle.codigo_Producto}</td>
            <td>${detalle.descripcion}</td>
            <td>${detalle.cantidad}</td>
            <td>${detalle.unidad}</td>
            <td>${detalle.precio}</td>
            <td>
                <button
                    type="button"
                    onclick="eliminarDetalle(${indice})"
                >
                    Eliminar
                </button>
            </td>
        `;

        tablaDetalle.appendChild(fila);
    });
}


// quita la fila seleccionada del pedido pendiente y actualiza la tabla
function eliminarDetalle(indice) {
    detalles.splice(indice, 1);
    mostrarDetalles();
}


// ---------------------------------
// GRABAR PEDIDO
// ---------------------------------

formularioPedido.addEventListener("submit", async (evento) => {
    evento.preventDefault();

    if (clienteSeleccionado === null) {
        mostrarErrorPedido(
            "Seleccione un cliente de la lista.",
            buscarCliente
        );
        return;
    }

    if (campoOperador.value.trim() === "") {
        mostrarErrorPedido(
            "Ingrese el operador.",
            campoOperador
        );
        return;
    }

    if (detalles.length === 0) {
        mostrarErrorPedido(
            "El pedido debe contener al menos un producto.",
            buscarProducto
        );
        return;
    }

    // envia los codigos y cantidades para que el servidor obtenga la unidad y el precio
    const pedido = {
        cuit: clienteSeleccionado.cuit,

        operador:
            campoOperador.value.trim().toUpperCase(),

        detalles: detalles.map(detalle => ({
            codigo_Producto: detalle.codigo_Producto,
            cantidad: detalle.cantidad
        }))
    };


    // Confirmación antes de grabar
    const confirmar = confirm(
        `¿Confirma el pedido para ${clienteSeleccionado.razonSocial}?`
    );

    if (!confirmar) {
        return;
    }


    const respuesta = await fetch("/pedidos", {
        method: "POST",

        headers: {
            "Content-Type": "application/json"
        },

        body: JSON.stringify(pedido)
    });


    if (respuesta.ok) {
        const pedidoGuardado = await respuesta.json();

        mostrarExitoPedido(
            `Pedido ${pedidoGuardado.iD_Pedido} guardado correctamente.`
        );

        formularioPedido.reset();

        // vacia los detalles y las selecciones despues de confirmar el guardado
        detalles.length = 0;

        clienteSeleccionado = null;
        productoSeleccionado = null;
        configurarCantidad();

        mostrarDetalles();
    } else {
        // muestra los rechazos informados por el servidor sin exponer excepciones internas
        const contenido = await respuesta.json().catch(() => null);
        const mensaje = typeof contenido === "string"
            ? contenido
            : typeof contenido?.detail === "string"
                ? contenido.detail
                : `No se pudo guardar el pedido (HTTP ${respuesta.status}).`;
        mostrarErrorPedido(
            mensaje,
            buscarCliente
        );
    }
});
