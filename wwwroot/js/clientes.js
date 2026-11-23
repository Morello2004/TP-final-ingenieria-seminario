
// obtiene los campos y controles que utiliza el formulario de clientes
const formulario = document.getElementById("formCliente");
const campos = formulario.querySelectorAll("input");

const campoCuit = document.getElementById("cuit");
const mensaje = document.getElementById("mensaje");
const coincidenciasCuit = document.getElementById("coincidenciasCuit");

const dialogo = document.getElementById("dialogoClientes");
const filtroClientes = document.getElementById("filtroClientes");
const resultadosClientes = document.getElementById("resultadosClientes");
const botonGuardar = document.getElementById("guardarCliente");

let clientes = [];
// distingue un alta de una modificacion y conserva la clave del cliente seleccionado
let cuitEnEdicion = null;

// ---------------------------------
// CARGAR CLIENTES EXISTENTES
// ---------------------------------

async function cargarClientesExistentes() {
    const respuesta = await fetch("/clientes");

    if (!respuesta.ok) {
        throw new Error("No se pudieron cargar los clientes.");
    }

    clientes = await respuesta.json();
}

cargarClientesExistentes().catch(console.error);

// ---------------------------------
// MAYÚSCULAS EXCEPTO EMAIL
// ---------------------------------

campos.forEach(campo => {
    if (campo.id === "email") return;

    campo.addEventListener("input", () => {
        campo.value = campo.value.toUpperCase();
    });
});

// ---------------------------------
// MENSAJES
// ---------------------------------

function limpiarErrores() {
    document.querySelectorAll("#formCliente .campo-error")
        .forEach(elemento => {
            elemento.classList.remove("campo-error");
        });
}

function mostrarError(texto, campo) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje-error";

    limpiarErrores();

    if (campo) {
        campo.classList.add("campo-error");
        campo.focus();
    }
}

function mostrarExito(texto) {
    mensaje.textContent = texto;
    mensaje.className = "mensaje-exito";
    limpiarErrores();
}

// ---------------------------------
// CARGAR CLIENTE PARA EDICIÓN
// ---------------------------------

function cargarCliente(cliente) {
    // Guardamos el CUIT original para identificar
    // al cliente aunque se modifique el campo.
    cuitEnEdicion = cliente.cuit;

    campoCuit.value = cliente.cuit;
    campoCuit.readOnly = false;

    document.getElementById("razonSocial").value =
        cliente.razonSocial;

    document.getElementById("domicilio").value =
        cliente.domicilio;

    document.getElementById("localidad").value =
        cliente.localidad;

    document.getElementById("cp").value = cliente.cp;

    document.getElementById("celular").value =
        cliente.celular ?? "";

    document.getElementById("email").value =
        cliente.email ?? "";

    botonGuardar.textContent = "Actualizar Cliente";

    coincidenciasCuit.innerHTML = "";
    mensaje.textContent = "";
    mensaje.className = "";

    limpiarErrores();
}

// ---------------------------------
// NUEVO CLIENTE
// ---------------------------------

function nuevoCliente() {
    formulario.reset();

    cuitEnEdicion = null;
    campoCuit.readOnly = false;

    botonGuardar.textContent = "Guardar Cliente";

    coincidenciasCuit.innerHTML = "";
    mensaje.textContent = "";
    mensaje.className = "";

    limpiarErrores();
}

document.getElementById("nuevoCliente")
    .addEventListener("click", nuevoCliente);

// ---------------------------------
// AUTOCOMPLETADO POR CUIT
// ---------------------------------

campoCuit.addEventListener("input", () => {
    coincidenciasCuit.innerHTML = "";

    if (cuitEnEdicion !== null) return;

    const texto = campoCuit.value.trim();

    if (!texto) return;

    const encontrados = clientes.filter(cliente =>
        cliente.cuit.startsWith(texto)
    );

    encontrados.forEach(cliente => {
        const opcion = document.createElement("div");

        opcion.className = "coincidencia";

        opcion.textContent =
            `${cliente.cuit} - ${cliente.razonSocial}`;

        opcion.addEventListener("click", () => {
            cargarCliente(cliente);
        });

        coincidenciasCuit.appendChild(opcion);
    });
});

// ---------------------------------
// DIÁLOGO MODAL DE BÚSQUEDA
// ---------------------------------

document.getElementById("abrirBusquedaCliente")
    .addEventListener("click", async () => {
        try {
            await cargarClientesExistentes();

            filtroClientes.value = "";
            mostrarResultados(clientes);

            dialogo.showModal();
            filtroClientes.focus();

        } catch (error) {
            console.error(error);
            mostrarError("No se pudo abrir la búsqueda.");
        }
    });

document.getElementById("cerrarBusquedaCliente")
    .addEventListener("click", () => {
        dialogo.close();
    });

// ---------------------------------
// FILTRAR CLIENTES POR PREFIJO
// ---------------------------------

filtroClientes.addEventListener("input", () => {
    const texto = filtroClientes.value.trim().toLowerCase();

    const encontrados = clientes.filter(cliente =>
        [
            cliente.cuit,
            cliente.razonSocial,
            cliente.domicilio,
            cliente.localidad
        ].some(valor =>
            String(valor ?? "")
                .toLowerCase()
                .startsWith(texto)
        )
    );

    mostrarResultados(encontrados);
});

// ---------------------------------
// MOSTRAR RESULTADOS
// ---------------------------------

function mostrarResultados(lista) {
    resultadosClientes.replaceChildren();

    if (lista.length === 0) {
        resultadosClientes.textContent =
            "No se encontraron clientes.";
        return;
    }

    lista.forEach(cliente => {
        const boton = document.createElement("button");

        boton.type = "button";
        boton.className = "resultado-cliente";

        boton.textContent =
            `${cliente.cuit} - ${cliente.razonSocial}`;

        boton.addEventListener("click", () => {
            cargarCliente(cliente);
            dialogo.close();
        });

        resultadosClientes.appendChild(boton);
    });
}

// ---------------------------------
// GUARDAR O ACTUALIZAR CLIENTE
// ---------------------------------

formulario.addEventListener("submit", async evento => {
    evento.preventDefault();

    const cuit = campoCuit.value.trim();
    const celular = document.getElementById("celular").value.trim();
    const email = document.getElementById("email").value.trim();

    const prefijosValidos = ["20", "23", "27", "30"];

    // VALIDAR CUIT

    if (
        !/^\d{11}$/.test(cuit) ||
        !prefijosValidos.includes(cuit.substring(0, 2))
    ) {
        mostrarError("CUIT inválido.", campoCuit);
        return;
    }

    // VALIDAR CUIT DUPLICADO
    // Se permite conservar el CUIT original.
    // Si se cambia, el nuevo no debe pertenecer
    // a otro cliente.

    if (clientes.some(cliente =>
        cliente.cuit === cuit &&
        cliente.cuit !== cuitEnEdicion
    )) {
        mostrarError(
            "Ya existe un cliente con ese CUIT.",
            campoCuit
        );
        return;
    }

    // VALIDAR CONTACTO OBLIGATORIO

    if (celular === "" && email === "") {
        mostrarError(
            "Debe ingresar un celular o un email.",
            document.getElementById("celular")
        );
        return;
    }

    // VALIDAR CELULAR

    if (
        celular !== "" &&
        !/^54\d{10}$/.test(celular)
    ) {
        mostrarError(
            "Celular inválido. Debe comenzar con 54 y tener 12 dígitos.",
            document.getElementById("celular")
        );
        return;
    }

    // VALIDAR EMAIL

    if (
        email !== "" &&
        !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)
    ) {
        mostrarError(
            "Email inválido.",
            document.getElementById("email")
        );
        return;
    }

    // valida el codigo postal antes de pedir confirmacion
    const campoCP = document.getElementById("cp");
    const cp = campoCP.value.trim().toUpperCase();

    if (!/^(?:[0-9]{4}|[A-Z][0-9]{4}(?:[A-Z]{3})?)$/.test(cp)) {
        mostrarError(
            "Código postal inválido. Ejemplos: 6700, B6700 o B6700ABC.",
            campoCP
        );
        return;
    }

    campoCP.value = cp;
    // CONSTRUIR OBJETO CLIENTE

    const cliente = {
        cuit: cuit,
        razonSocial: document.getElementById("razonSocial").value.trim(),
        domicilio: document.getElementById("domicilio").value.trim(),
        localidad: document.getElementById("localidad").value.trim(),
        cp: cp,
        celular: celular === "" ? null : celular,
        email: email === "" ? null : email
    };

    const editando = cuitEnEdicion !== null;
    const accion = editando ? "modificación" : "alta";

    // CONFIRMACIÓN

    let textoConfirmacion =
        `¿Confirma la ${accion} del cliente ${cliente.razonSocial}?`;

    if (editando && cuitEnEdicion !== cuit) {
        textoConfirmacion +=
            `\n\nCUIT anterior: ${cuitEnEdicion}` +
            `\nCUIT nuevo: ${cuit}`;
    }

    if (!confirm(textoConfirmacion)) {
        return;
    }

    // DETERMINAR ENDPOINT Y MÉTODO

    const url = editando
        ? `/clientes/${encodeURIComponent(cuitEnEdicion)}`
        : "/clientes";

    const metodo = editando ? "PUT" : "POST";

    // ENVIAR AL SERVIDOR

    try {
        // evita nuevos envios mientras se espera la respuesta del servidor
        botonGuardar.disabled = true;

        const respuesta = await fetch(url, {
            method: metodo,
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(cliente)
        });

        if (!respuesta.ok) {
            let detalle = "";

            try {
                detalle = await respuesta.text();
            } catch {
                detalle = "";
            }

            console.error(
                "Error HTTP:",
                respuesta.status,
                detalle
            );

            mostrarError(
                respuesta.status === 409
                    ? "El CUIT ya pertenece a otro cliente."
                    : "Error al guardar los datos del cliente."
            );

            return;
        }

        // ACTUALIZAR LISTA DE CLIENTES

        await cargarClientesExistentes();

        // LIMPIAR FORMULARIO

        nuevoCliente();

        // MOSTRAR RESULTADO

        mostrarExito(
            editando
                ? "Cliente actualizado correctamente."
                : "Cliente guardado correctamente."
        );

    } catch (error) {
        console.error(error);

        mostrarError(
            "Error de comunicación con el servidor."
        );

    } finally {
        // vuelve a habilitar el guardado tanto si hubo exito como si ocurrio un error
        botonGuardar.disabled = false;
    }
});
