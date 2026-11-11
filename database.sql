-- AD01 - Aplicaciones Distribuidas - Comercio Multibodega
-- SQL Server 2019+; ejecutar con permisos CREATE DATABASE en una instancia de desarrollo.
-- Cada microservicio es propietario exclusivo de su base. Las referencias a IdProducto
-- desde InventarioDB y PedidosDB son logicas: se validan mediante Catalogo.Api y no
-- se implementan como FK entre bases/servicios.

USE master;
GO
IF DB_ID(N'CatalogoDB') IS NULL EXEC(N'CREATE DATABASE CatalogoDB');
GO
IF DB_ID(N'InventarioDB') IS NULL EXEC(N'CREATE DATABASE InventarioDB');
GO
IF DB_ID(N'PedidosDB') IS NULL EXEC(N'CREATE DATABASE PedidosDB');
GO

USE CatalogoDB;
GO
CREATE TABLE dbo.Categoria (
    IdCategoria BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categoria PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    Descripcion NVARCHAR(300) NULL,
    CONSTRAINT UQ_Categoria_Nombre UNIQUE (Nombre)
);
GO
CREATE TABLE dbo.Marca (
    IdMarca BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Marca PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    PaisOrigen NVARCHAR(80) NULL,
    CONSTRAINT UQ_Marca_Nombre UNIQUE (Nombre)
);
GO
CREATE TABLE dbo.Producto (
    IdProducto BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Producto PRIMARY KEY,
    IdCategoria BIGINT NOT NULL,
    IdMarca BIGINT NOT NULL,
    Nombre NVARCHAR(160) NOT NULL,
    SKU NVARCHAR(40) NOT NULL,
    Precio DECIMAL(12,2) NOT NULL,
    Estado NVARCHAR(15) NOT NULL CONSTRAINT DF_Producto_Estado DEFAULT N'ACTIVO',
    FechaCreacion DATETIME2(0) NOT NULL CONSTRAINT DF_Producto_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Producto_SKU UNIQUE (SKU),
    CONSTRAINT CK_Producto_Precio CHECK (Precio >= 0),
    CONSTRAINT CK_Producto_Estado CHECK (Estado IN (N'ACTIVO',N'INACTIVO')),
    CONSTRAINT FK_Producto_Categoria FOREIGN KEY (IdCategoria) REFERENCES dbo.Categoria(IdCategoria),
    CONSTRAINT FK_Producto_Marca FOREIGN KEY (IdMarca) REFERENCES dbo.Marca(IdMarca)
);
GO
CREATE INDEX IX_Producto_Categoria ON dbo.Producto(IdCategoria);
CREATE INDEX IX_Producto_Marca ON dbo.Producto(IdMarca);
GO

USE InventarioDB;
GO
CREATE TABLE dbo.Bodega (
    IdBodega BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Bodega PRIMARY KEY,
    Nombre NVARCHAR(120) NOT NULL,
    Ciudad NVARCHAR(100) NOT NULL,
    Direccion NVARCHAR(240) NOT NULL,
    Estado NVARCHAR(15) NOT NULL CONSTRAINT DF_Bodega_Estado DEFAULT N'ACTIVA',
    CONSTRAINT UQ_Bodega_Nombre_Ciudad UNIQUE (Nombre,Ciudad),
    CONSTRAINT CK_Bodega_Estado CHECK (Estado IN (N'ACTIVA',N'INACTIVA'))
);
GO
CREATE TABLE dbo.Existencia (
    IdExistencia BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Existencia PRIMARY KEY,
    IdProducto BIGINT NOT NULL, -- referencia logica a Catalogo.Api; validar por API
    IdBodega BIGINT NOT NULL,
    Stock INT NOT NULL CONSTRAINT DF_Existencia_Stock DEFAULT 0,
    FechaActualizacion DATETIME2(0) NOT NULL CONSTRAINT DF_Existencia_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_Existencia_Producto_Bodega UNIQUE (IdProducto,IdBodega),
    CONSTRAINT CK_Existencia_Stock CHECK (Stock >= 0),
    CONSTRAINT FK_Existencia_Bodega FOREIGN KEY (IdBodega) REFERENCES dbo.Bodega(IdBodega)
);
GO
CREATE INDEX IX_Existencia_IdProducto ON dbo.Existencia(IdProducto);
GO
CREATE TABLE dbo.MovimientoInventario (
    IdMovimiento BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_MovimientoInventario PRIMARY KEY,
    IdProducto BIGINT NOT NULL, -- referencia logica a Catalogo.Api
    IdBodega BIGINT NOT NULL,
    Tipo NVARCHAR(12) NOT NULL,
    Cantidad INT NOT NULL,
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Movimiento_Fecha DEFAULT SYSUTCDATETIME(),
    Observacion NVARCHAR(250) NULL,
    CONSTRAINT CK_Movimiento_Tipo CHECK (Tipo IN (N'ENTRADA',N'SALIDA',N'AJUSTE')),
    CONSTRAINT CK_Movimiento_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT FK_Movimiento_Bodega FOREIGN KEY (IdBodega) REFERENCES dbo.Bodega(IdBodega)
);
GO
CREATE INDEX IX_Movimiento_Producto_Fecha ON dbo.MovimientoInventario(IdProducto,Fecha DESC);
CREATE INDEX IX_Movimiento_Bodega_Fecha ON dbo.MovimientoInventario(IdBodega,Fecha DESC);
GO
CREATE TABLE dbo.ReservaStock (
    IdReserva BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ReservaStock PRIMARY KEY,
    IdProducto BIGINT NOT NULL, -- referencia logica a Catalogo.Api
    IdBodega BIGINT NOT NULL,
    Cantidad INT NOT NULL,
    Estado NVARCHAR(15) NOT NULL CONSTRAINT DF_Reserva_Estado DEFAULT N'ACTIVA',
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Reserva_Fecha DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Reserva_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Reserva_Estado CHECK (Estado IN (N'ACTIVA',N'LIBERADA',N'CONSUMIDA',N'CANCELADA')),
    CONSTRAINT FK_Reserva_Bodega FOREIGN KEY (IdBodega) REFERENCES dbo.Bodega(IdBodega)
);
GO
CREATE INDEX IX_Reserva_Producto_Bodega_Estado ON dbo.ReservaStock(IdProducto,IdBodega,Estado);
GO

USE PedidosDB;
GO
CREATE TABLE dbo.Cliente (
    IdCliente BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cliente PRIMARY KEY,
    Nombres NVARCHAR(160) NOT NULL,
    Cedula NVARCHAR(20) NOT NULL,
    Correo NVARCHAR(254) NOT NULL,
    Estado NVARCHAR(15) NOT NULL CONSTRAINT DF_Cliente_Estado DEFAULT N'ACTIVO',
    CONSTRAINT UQ_Cliente_Cedula UNIQUE (Cedula),
    CONSTRAINT UQ_Cliente_Correo UNIQUE (Correo),
    CONSTRAINT CK_Cliente_Estado CHECK (Estado IN (N'ACTIVO',N'INACTIVO'))
);
GO
CREATE TABLE dbo.Pedido (
    IdPedido BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pedido PRIMARY KEY,
    IdCliente BIGINT NOT NULL,
    Fecha DATETIME2(0) NOT NULL CONSTRAINT DF_Pedido_Fecha DEFAULT SYSUTCDATETIME(),
    Total DECIMAL(14,2) NOT NULL CONSTRAINT DF_Pedido_Total DEFAULT 0,
    Estado NVARCHAR(15) NOT NULL CONSTRAINT DF_Pedido_Estado DEFAULT N'PENDIENTE',
    CONSTRAINT CK_Pedido_Total CHECK (Total >= 0),
    CONSTRAINT CK_Pedido_Estado CHECK (Estado IN (N'PENDIENTE',N'CONFIRMADO',N'RESERVADO',N'DESPACHADO',N'CANCELADO')),
    CONSTRAINT FK_Pedido_Cliente FOREIGN KEY (IdCliente) REFERENCES dbo.Cliente(IdCliente)
);
GO
CREATE INDEX IX_Pedido_Cliente_Fecha ON dbo.Pedido(IdCliente,Fecha DESC);
GO
CREATE TABLE dbo.DetallePedido (
    IdDetalle BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DetallePedido PRIMARY KEY,
    IdPedido BIGINT NOT NULL,
    IdProducto BIGINT NOT NULL, -- referencia logica a Catalogo.Api; validar por API
    Cantidad INT NOT NULL,
    PrecioUnitario DECIMAL(12,2) NOT NULL,
    CONSTRAINT CK_Detalle_Cantidad CHECK (Cantidad > 0),
    CONSTRAINT CK_Detalle_Precio CHECK (PrecioUnitario >= 0),
    CONSTRAINT FK_Detalle_Pedido FOREIGN KEY (IdPedido) REFERENCES dbo.Pedido(IdPedido)
);
GO
CREATE INDEX IX_Detalle_Pedido ON dbo.DetallePedido(IdPedido);
CREATE INDEX IX_Detalle_Producto ON dbo.DetallePedido(IdProducto);
GO

-- Datos de demostracion ficticios (ejecutar una sola vez en una base recien creada).
USE CatalogoDB;
GO
INSERT dbo.Categoria(Nombre,Descripcion) VALUES
(N'Computacion',N'Equipos y accesorios informaticos'),
(N'Redes',N'Equipos para conectividad');
INSERT dbo.Marca(Nombre,PaisOrigen) VALUES
(N'AndesTech',N'Ecuador'),
(N'NetSur',N'Ecuador');
INSERT dbo.Producto(IdCategoria,IdMarca,Nombre,SKU,Precio,Estado) VALUES
(1,1,N'Laptop Andina 14',N'AT-LAP-014',729.99,N'ACTIVO'),
(2,2,N'Router NetSur AX',N'NS-ROU-AX1',89.50,N'ACTIVO');
GO
USE InventarioDB;
GO
INSERT dbo.Bodega(Nombre,Ciudad,Direccion) VALUES
(N'Bodega Quito','Quito',N'Av. 10 de Agosto y Naciones Unidas'),
(N'Bodega Guayaquil','Guayaquil',N'Av. Juan Tanca Marengo');
INSERT dbo.Existencia(IdProducto,IdBodega,Stock) VALUES (1,1,25),(1,2,12),(2,1,40);
INSERT dbo.MovimientoInventario(IdProducto,IdBodega,Tipo,Cantidad,Observacion) VALUES
(1,1,N'ENTRADA',25,N'Saldo inicial de demostracion'),
(1,2,N'ENTRADA',12,N'Saldo inicial de demostracion'),
(2,1,N'ENTRADA',40,N'Saldo inicial de demostracion');
GO
USE PedidosDB;
GO
INSERT dbo.Cliente(Nombres,Cedula,Correo) VALUES
(N'Cliente de Prueba',N'0000000000',N'cliente.prueba@example.test');
INSERT dbo.Pedido(IdCliente,Total,Estado) VALUES (1,819.49,N'PENDIENTE');
INSERT dbo.DetallePedido(IdPedido,IdProducto,Cantidad,PrecioUnitario) VALUES
(1,1,1,729.99),(1,2,1,89.50);
GO
-- Reglas de implementacion: actualizar Existencia y registrar Movimiento dentro
-- de una transaccion en Inventario.Api. Reservar stock debe comprobar Stock
-- disponible bajo concurrencia. Pedidos.Api valida SKU/estado/precio consultando
-- Catalogo.Api y coordina ReservaStock; no crear FK entre bases de microservicios.


