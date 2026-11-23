CREATE TABLE Cliente (
    CUIT          TEXT PRIMARY KEY,
    RazonSocial   TEXT NOT NULL,
    Domicilio     TEXT NOT NULL,
    Localidad     TEXT NOT NULL,
    CP            TEXT NOT NULL,
    Celular       TEXT,
    Email         TEXT,
    Fecha_Alta    DATETIME NOT NULL,
    Fecha_Baja    DATETIME DEFAULT NULL,

    CHECK (Celular IS NOT NULL OR Email IS NOT NULL)
);

CREATE TABLE Producto (
    Codigo          TEXT PRIMARY KEY,
    Descripcion     TEXT NOT NULL,
    Stock           REAL NOT NULL,
    Stock_Minimo    REAL NOT NULL DEFAULT 0,
    Unidad          TEXT NOT NULL,
    Precio          REAL NOT NULL,
    Fecha_Alta      DATETIME NOT NULL,
    Fecha_Baja      DATETIME DEFAULT NULL,

    CHECK (Unidad IN ('KG', 'MTS', 'LITROS', 'UNIDADES'))
);

CREATE TABLE Pedidos_Cabecera (
    ID_Pedido    INTEGER PRIMARY KEY,
    Fecha_Hora   DATETIME NOT NULL,
    CUIT         TEXT NOT NULL,
    Operador     TEXT NOT NULL,
    Estado 	 TEXT NOT NULL DEFAULT 'PENDIENTE',
    FOREIGN KEY (CUIT) REFERENCES Cliente(CUIT),

    CHECK (Estado IN (
        'PENDIENTE',
        'ACEPTADO',
        'CONFIRMADO',
        'RECHAZADO',
        'ANULADO'
    ))
);

CREATE TABLE Pedidos_Detalle (
    ID_Pedido         INTEGER NOT NULL,
    Codigo_Producto   TEXT NOT NULL,
    Cantidad          REAL NOT NULL,
    Unidad            TEXT NOT NULL,
    Precio            REAL NOT NULL,

    PRIMARY KEY (ID_Pedido, Codigo_Producto),

    FOREIGN KEY (ID_Pedido)
        REFERENCES Pedidos_Cabecera(ID_Pedido),

    FOREIGN KEY (Codigo_Producto)
        REFERENCES Producto(Codigo)
);
