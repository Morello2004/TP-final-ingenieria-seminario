PRAGMA foreign_keys = ON;

-- =====================================
-- CLIENTES
-- =====================================

INSERT INTO Cliente
(CUIT, RazonSocial, Domicilio, Localidad, CP, Celular, Email, Fecha_Alta)
VALUES
('23111222333', 'DISTRIBUIDORA DEL OESTE', 'SAN MARTIN 450', 'LUJAN', '6700', '541123456789', 'Ventas@Distribuidora.com', datetime('now')),
('23222333444', 'COMERCIAL NORTE', 'RIVADAVIA 120', 'MERCEDES', '6600', '541134567890', 'contacto@ComercialNorte.com', datetime('now')),
('20333444555', 'JUAN PEREZ', 'BELGRANO 780', 'LUJAN', '6700', '541145678901', 'Juan.Perez@gmail.com', datetime('now')),
('27444555666', 'MARIA GONZALEZ', 'MITRE 250', 'GENERAL RODRIGUEZ', '1748', NULL, 'Maria.Gonzalez@hotmail.com', datetime('now')),
('30555666777', 'FERRETERIA CENTRAL', 'MORENO 990', 'LUJAN', '6700', '541156789012', NULL, datetime('now'));

-- =====================================
-- PRODUCTOS
-- =====================================

INSERT INTO Producto
(Codigo, Descripcion, Stock, Stock_Minimo, Unidad, Precio, Fecha_Alta)
VALUES
('P001', 'CEMENTO', 150, 20, 'KG', 8500, datetime('now')),
('P002', 'CABLE ELECTRICO', 500, 50, 'MTS', 1200, datetime('now')),
('P003', 'PINTURA BLANCA', 80, 10, 'LITROS', 15000, datetime('now')),
('P004', 'TORNILLOS', 2000, 100, 'UNIDADES', 150, datetime('now')),
('P005', 'ARENA', 300, 30, 'KG', 2500, datetime('now'));

-- =====================================
-- PEDIDOS - CABECERA
-- =====================================

INSERT INTO Pedidos_Cabecera
(ID_Pedido, Fecha_Hora, CUIT, Operador, Estado)
VALUES
(1, datetime('now'), '23111222333', 'ADMIN', 'PENDIENTE'),
(2, datetime('now'), '23111222333', 'ADMIN', 'CONFIRMADO'),
(3, datetime('now'), '20333444555', 'ADMIN', 'ACEPTADO');

-- =====================================
-- PEDIDOS - DETALLE
-- =====================================

INSERT INTO Pedidos_Detalle
(ID_Pedido, Codigo_Producto, Cantidad, Unidad, Precio)
VALUES
(1, 'P001', 10, 'KG', 8500),
(1, 'P002', 25, 'MTS', 1200),
(2, 'P003', 5, 'LITROS', 15000),
(3, 'P004', 100, 'UNIDADES', 150);
