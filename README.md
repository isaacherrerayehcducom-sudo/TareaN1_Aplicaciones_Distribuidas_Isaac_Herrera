# Comercio Multibodega

Solucion educativa AD01 con tres microservicios REST ASP.NET Core y bases de datos separadas por dominio:

| Servicio | Base de datos | Responsabilidad |
|---|---|---|
| `Catalogo.Api` | `CatalogoDB` | Categorias, marcas y productos |
| `Inventario.Api` | `InventarioDB` | Bodegas, existencias, movimientos y reservas |
| `Pedidos.Api` | `PedidosDB` | Clientes y pedidos con sus detalles |

Los servicios comparten identificadores de producto como referencias logicas. Las claves foraneas fisicas relacionan tablas dentro de una misma base. Inventario y pedidos consultan `Catalogo.Api` para validar que el producto exista y este activo. Los pedidos guardan el precio vigente como precio unitario.

## Requisitos

- .NET SDK 10.0
- SQL Server 2019 o posterior para persistencia SQL; el script tambien se puede ejecutar en SQL Server Express/Developer compatible.
- Postman para importar y ejecutar `postman/AD01_Comercio_Multibodega.postman_collection.json`.

## Ejecutar el modo de demostracion

El modo predeterminado usa bases en memoria con datos ficticios para que las APIs puedan probarse sin instalar SQL Server. Desde la carpeta del proyecto, abre tres terminales y ejecuta un servicio en cada una:

```powershell
$env:ASPNETCORE_URLS = 'http://localhost:5081'
dotnet run --no-launch-profile --project .\Catalogo.Api
```

```powershell
$env:ASPNETCORE_URLS = 'http://localhost:5082'
$env:Services__CatalogApi = 'http://localhost:5081/'
dotnet run --no-launch-profile --project .\Inventario.Api
```

```powershell
$env:ASPNETCORE_URLS = 'http://localhost:5083'
$env:Services__CatalogApi = 'http://localhost:5081/'
dotnet run --no-launch-profile --project .\Pedidos.Api
```

Confirma que cada servicio responde en `/health`. Los datos en memoria se reinician cuando se detiene el proceso.

## Usar SQL Server

1. Ejecuta `database.sql` una sola vez desde SSMS o desde una terminal con `sqlcmd`:

   ```powershell
   sqlcmd -S localhost -E -i .\database.sql -b
   ```

   Ajusta `-S` y la autenticacion segun tu instalacion. El script crea las tres bases, las tablas, restricciones e informacion ficticia inicial.

2. En la terminal de cada API configura su proveedor y cadena de conexion. Por ejemplo, usando autenticacion integrada de Windows:

   ```powershell
   $env:Storage__Provider = 'SqlServer'
   $env:ConnectionStrings__Default = 'Server=localhost;Database=CatalogoDB;Trusted_Connection=True;TrustServerCertificate=True'
   ```

   Para `Inventario.Api` cambia `Database=InventarioDB`; para `Pedidos.Api`, `Database=PedidosDB`. Si usas una instancia con autenticacion SQL, guarda la cadena de conexion en variables locales de entorno y no la publiques en Git.

3. Ejecuta cada servicio con las mismas direcciones indicadas arriba. El servicio requiere que las tablas ya existan; no modifica ni migra bases automaticamente.

## Rutas disponibles

Todas las rutas de recursos usan JSON. Los controladores CRUD devuelven `200`, `201`, `204`, `400`, `404` y `409` segun el resultado.

| Servicio | Rutas |
|---|---|
| Catalogo | `GET/POST /api/categorias`, `GET/PUT/DELETE /api/categorias/{id}`; las mismas operaciones en `/api/marcas` y `/api/productos`; `GET /api/productos/buscar/{sku}` |
| Inventario | CRUD en `/api/bodegas`; `GET /api/existencias?idProducto=&idBodega=`; `GET/POST /api/movimientos` y `GET /api/movimientos/{id}`; `GET/POST /api/reservas`, `GET /api/reservas/{id}` y `PATCH /api/reservas/{id}/liberar` |
| Pedidos | CRUD en `/api/clientes`; `GET /api/pedidos`, `GET /api/pedidos/{id}`, `POST /api/pedidos` |

Los movimientos de inventario son inmutables y actualizan la existencia dentro de una transaccion. Las reservas activas se descuentan del stock disponible y se pueden liberar. Un pedido valida cliente y productos, captura los precios consultados en el catalogo y registra el total.

## Compilar

```powershell
dotnet restore .\ComercioMultibodega.slnx
dotnet build .\ComercioMultibodega.slnx
```

En `database.sql` se encuentran las restricciones de SQL Server. `bin/` y `obj/` se excluyen de Git.
