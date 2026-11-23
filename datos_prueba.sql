PRAGMA foreign_keys = ON;

-- =====================================
-- CLIENTES
-- =====================================

INSERT INTO Cliente
(CUIT, RazonSocial, Domicilio, Localidad, CP, Celular, Email, Fecha_Alta)
VALUES
('00000000001', 'CLIENTE DE PRUEBA 1', 'DOMICILIO DE PRUEBA 1', 'LOCALIDAD DE PRUEBA', '0000', NULL, 'cliente1@example.com', datetime('now')),
('00000000002', 'CLIENTE DE PRUEBA 2', 'DOMICILIO DE PRUEBA 2', 'LOCALIDAD DE PRUEBA', '0000', NULL, 'cliente2@example.com', datetime('now')),
('00000000003', 'CLIENTE DE PRUEBA 3', 'DOMICILIO DE PRUEBA 3', 'LOCALIDAD DE PRUEBA', '0000', NULL, 'cliente3@example.com', datetime('now')),
('00000000004', 'CLIENTE DE PRUEBA 4', 'DOMICILIO DE PRUEBA 4', 'LOCALIDAD DE PRUEBA', '0000', NULL, 'cliente4@example.com', datetime('now')),
('00000000005', 'CLIENTE DE PRUEBA 5', 'DOMICILIO DE PRUEBA 5', 'LOCALIDAD DE PRUEBA', '0000', NULL, 'cliente5@example.com', datetime('now'));

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
(1, datetime('now'), '00000000001', 'ADMIN', 'PENDIENTE'),
(2, datetime('now'), '00000000001', 'ADMIN', 'CONFIRMADO'),
(3, datetime('now'), '00000000003', 'ADMIN', 'ACEPTADO');

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
