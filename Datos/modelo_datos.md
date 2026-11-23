CLIENTE

CUIT NUMBER(11)
RazonSocial VARCHAR(60)
Domicilio VARCHAR(60)
Localidad VARCHAR(40)
CP VARCHAR(10) -- HASTA ACÁ LOS OBLIGATORIOS
Celular NUMBER(12) -- Uno de
Email VARCHAR(100) -- estos es obligatorio
Fecha_Alta DATETIME
Fecha_Baja DATETIME DEFAULT NULL

UI Cliente
- Datos obligatorios
- CapsUp a todo lo ingresado

CUIT consta de:
- 2 números al principio (20,23,27,30)
- 8 números en medio
- Un dígito al final

Celular:
- 54 adelante
- 10 dígitos del número

Mail:
- Debe tener al menos un "@" y un "." algo.

Para los DATETIME:
- Debe capturar la fecha del servidor.

PRODUCTOS

Codigo N(... quizas codigo de barras)
Descripcion V(50)
Stock N(10,2decimales)
Stock_Minimo N(10,2)
Unidad VARCHAR(10) (KG, MTS, LITROS, UNIDADES)
Precio N(10,2)
Fecha_Alta DATE -- Hasta acá los obligatorios
Fecha_Baja DATE DEFAULT NULL

UI PRODUCTOS

- Datos obligatorios
- CapsUp los datos ingresados
- Stock_Minimo = 0
- Los DATETIME deben capturar la hora del sistema.

PEDIDOS_CABECERA

ID_PEDIDO NUMBER(6)
Fecha/Hora DATETIME
CUIT FK CLIENTE
Operador VARCHAR(20)
Estado VARCHAR(20) (PENDIENTE, ACEPTADO, CONFIRMADO, RECHAZADO, ANULADO)

PEDIDOS_DETALLE

ID_PEDIDO FK
Codigo_Producto FK
Cantidad
Unidad - TRAER
Precio - SOLO

UI PEDIDOS

-Datos obligatorios
-Mayuscula
-StockMin 0
-Fecha del servidor

IMPORTANTE el ID del pedido y la fecha se adjudican una vez se
apriete el boton de GRABAR pedido, no antes...
