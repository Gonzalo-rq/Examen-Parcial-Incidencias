# Examen Parcial: Plataforma de Incidencias Operativas (EcoBici)

Plataforma integral para la gestión y monitoreo de averías en estaciones de bicicletas compartidas, desarrollada en **ASP.NET Core MVC** con **Entity Framework Core**, **SQLite**, **Algolia Search**, **Redis Cache**, **PieHost WebSockets** y control de versiones profesional en **GitHub**.

---

## 📋 Información General

- **Repositorio en GitHub**: [https://github.com/Gonzalo-rq/Examen-Parcial-Incidencias](https://github.com/Gonzalo-rq/Examen-Parcial-Incidencias)
- **Commit Desplegado en main**: `59f5b1b` (Merge pull request #3)
- **Commit Ancestro Común**: `1f7a8a6`
- **Usuario Supervisor de Prueba**:
  - **Email**: `supervisor@bicis.com`
  - **Contraseña**: `Supervisor123!`

---

## 🌿 Tabla de Ramas y Pull Requests Independientes

Todas las ramas fueron creadas de forma independiente a partir del **commit inicial de `main`** (`1f7a8a6`), implementando cada requerimiento por separado antes de iniciar el proceso de integración:

| Pregunta | Rama | Pull Request | Estado | Descripción |
| :--- | :--- | :--- | :---: | :--- |
| **Pregunta 1** | `feature/busqueda-algolia` | [PR #1](https://github.com/Gonzalo-rq/Examen-Parcial-Incidencias/pull/1) | **MERGED** | Búsqueda por texto en servidor con Algolia filtrando sólo incidencias abiertas existentes en base de datos. Título: `Incidencias abiertas encontradas`. |
| **Pregunta 2** | `feature/cache-redis` | [PR #2](https://github.com/Gonzalo-rq/Examen-Parcial-Incidencias/pull/2) | **MERGED** | Caché distribuida de 60s en Redis para el listado general, logs estructurados de Cache Hit/Miss e invalidación al cerrar. Título: `Incidencias abiertas con consulta rápida`. |
| **Pregunta 3** | `feature/websocket-piehost` | [PR #3](https://github.com/Gonzalo-rq/Examen-Parcial-Incidencias/pull/3) | **MERGED** | Actualización en tiempo real vía WebSockets con PieHost (`IncidenciaActualizada`), eliminación de filas sin recarga y sincronización de estado vigente al reconectar. Título: `Incidencias abiertas en tiempo real`. |

---

## 🌳 Historial de Ramas y Fusiones (`git log --graph --oneline --all`)

El siguiente diagrama de árbol demuestra que las tres ramas nacen del mismo ancestro común y que las fusiones se realizaron en orden estricto (A, luego B con resolución, luego C con resolución) conservando todos los commits:

```text
*   59f5b1b Merge pull request #3 from feature/websocket-piehost
|\  
| *   b4beb95 merge: resolver conflicto 2 integrando WebSocket PieHost con busqueda Algolia y cache Redis
| |\  
| |/  
|/|   
* |   ac440b9 Merge pull request #2 from feature/cache-redis
|\ \  
| * \   71666c7 merge: resolver conflicto 1 integrando busqueda Algolia con cache Redis en feature/cache-redis
| |\ \  
| |/ /  
|/| |   
* | |   9f57b52 Merge pull request #1 from feature/busqueda-algolia
|\ \ \  
| * | | ea3e223 feat(algolia): Pregunta 1 - Búsqueda con Algolia en servidor y filtro de incidencias abiertas
|/ / /  
| * / 9569a78 feat(redis): Pregunta 2 - Caché por 60 segundos con Redis e invalidación al cerrar incidencias
|/ /  
| * e5c98b1 feat(piehost): Pregunta 3 - Actualización en tiempo real con WebSockets PieHost y reconexión
|/  
* 1f7a8a6 feat: initial commit - Plataforma base de Incidencias con /Operaciones/Incidencias
```

---

## ⚔️ Explicación de la Resolución de Conflictos

### Conflicto 1: Integración de `main` (Algolia) en `feature/cache-redis` (Redis)
- **Commit de resolución**: `71666c7`
- **Archivos en conflicto**:
  - `Program.cs`: Se registraron ambos servicios en el contenedor de dependencias (`IRedisCacheService` e `IAlgoliaSearchService`).
  - `Controllers/OperacionesController.cs`: Se inyectaron ambos servicios. Se configuró que si el usuario ingresa un término de búsqueda (`q`), se consulte directamente Algolia sin usar la caché; mientras que si la consulta es vacía (listado general), se aplique la caché distribuida de 60 segundos de Redis.
  - `Views/Operaciones/Incidencias.cshtml`: Se preservó el buscador de Algolia junto al indicador de origen de datos de Redis (`⚡ Redis Cache` / `⚡ Base de Datos SQLite`).

### Conflicto 2: Integración de `main` (Algolia + Redis) en `feature/websocket-piehost` (PieHost)
- **Commit de resolución**: `b4beb95`
- **Archivos en conflicto**:
  - `Program.cs`: Se unificó el registro de `IRedisCacheService`, `IAlgoliaSearchService` e `IPieHostWebSocketService`.
  - `Controllers/OperacionesController.cs`: Se aseguró la **secuencia estricta exigida por la rúbrica al cerrar una incidencia**:
    1. **Persistencia en Base**: `incidencia.Estado = "Cerrada"; await _context.SaveChangesAsync();`
    2. **Invalidación en Redis**: `await _cacheService.InvalidateIncidenciasAbiertasCacheAsync();`
    3. **Publicación en PieHost**: `await _webSocketService.PublicarIncidenciaActualizadaAsync(id, "Cerrada");`
    - Además se mantuvo el endpoint `GET /Operaciones/EstadoVigente` para responder a la reconexión de clientes.
  - `Views/Operaciones/Incidencias.cshtml`: Se unificó la vista para incluir el buscador por Algolia, el badge de velocidad de Redis, el badge de conectividad en tiempo real de PieHost (`🟢 Conectado`), y el script de WebSocket que elimina la fila con animación visual al recibir el evento sin recargar la página.

---

## 🚀 Despliegue en Render.com (Guía Paso a Paso)

El proyecto incluye `Dockerfile` optimizado con .NET 9 y `render.yaml`. Para desplegarlo manualmente:

1. Ingresa a [dashboard.render.com](https://dashboard.render.com/) y haz clic en **New +** -> **Web Service**.
2. Conecta tu cuenta de GitHub y selecciona el repositorio:
   `https://github.com/Gonzalo-rq/Examen-Parcial-Incidencias`
3. Configuración del servicio:
   - **Name**: `incidencias-app`
   - **Branch**: `main`
   - **Runtime**: `Docker` (seleccionará automáticamente el `Dockerfile` del repositorio).
   - **Instance Type**: `Free`.
4. En **Environment Variables**, agrega las siguientes claves (sin comillas):
   - `ASPNETCORE_ENVIRONMENT` = `Production`
   - `ConnectionStrings__DefaultConnection` = `Data Source=/data/app.db;Cache=Shared`
   - `Redis__ConnectionString` = `superglossy-hobbies-tangerine-38114.db.redis.io:13512,password=T2bE8h7rXhekGlsrqFxHv2CYMMrKf0M7,user=default,ssl=False,abortConnect=false`
   - `Algolia__ApplicationId` = `CBBGMIXT8X`
   - `Algolia__SearchApiKey` = `ed33f483bd657736bc421f094c2d5ee1`
   - `Algolia__WriteApiKey` = `31dc11e700a57fe749cc311b0abdca87`
   - `Algolia__IndexName` = `incidencias`
   - `PieHost__ClusterId` = `free.blr2`
   - `PieHost__ApiKey` = `f2X1bR8OHdxfU5blSBwspk25q3otbhOTtfbHAWMh`
   - `PieHost__ApiSecret` = `fYqXwNjGQfOGnuevoOMmNFgKDkSU6Qg`
   - `PieHost__ChannelId` = `1`
5. Haz clic en **Create Web Service**.
6. Render construirá la imagen Docker e iniciará la aplicación en el puerto asignado dinámicamente (`PORT`).

---

## 🧪 Guía de Pruebas y Demostración

### 1. Búsqueda con Algolia
1. Ingresa a `/Operaciones/Incidencias`.
2. En la caja de búsqueda escribe `Frenos` y presiona **Buscar (Algolia)**:
   - El sistema consulta Algolia en el servidor y filtra mostrando únicamente las incidencias abiertas que coincidan.
3. Deja el campo vacío o haz clic en **Limpiar**:
   - Se muestra el listado general completo.
4. Inspecciona el código fuente de la página: confirma que no existe ninguna clave de administración expuesta en el HTML/JavaScript del cliente.

### 2. Caché con Redis
1. Carga `/Operaciones/Incidencias`:
   - En la primera petición el badge mostrará `⚡ Base de Datos SQLite (Guardada en Caché)` y los logs registrarán `[DATABASE HIT / CACHE MISS]`.
2. Actualiza la página antes de que pasen 60 segundos:
   - El badge indicará `⚡ Redis Cache (Rápida - 60s)` y los logs registrarán `[REDIS HIT]`.

### 3. Tiempo Real con PieHost
1. Abre dos navegadores o una ventana normal y otra en incógnito en `/Operaciones/Incidencias`.
2. Observa el badge: `🟢 PieHost WebSocket: Conectado`.
3. En la primera ventana, haz clic en **Cerrar Incidencia** sobre cualquier avería:
   - En la segunda ventana, **sin recargar la página**, la fila de la incidencia se resaltará y desaparecerá automáticamente, acompañada de una notificación emergente en tiempo real.
4. Si se interrumpe la conexión, el sistema intentará reconectar y al restablecerse invocará `/Operaciones/EstadoVigente` para sincronizar los datos al instante.
