# Resumen del proyecto — App de gestión para bodega

Resumen de todo lo trabajado hasta ahora, para continuar en otro chat o retomar el contexto más adelante.

## Contexto / objetivo del proyecto

App web para un negocio real (bodega del amigo de mi padre) — todavía sin nombre definitivo. Debe ser **responsive** (celular y laptop). Reutiliza como base un proyecto universitario ("Qullqa", del curso, en un GitHub de un compañero) pero con un enfoque más profesional y con tecnologías/features nuevas.

Nombre real de la bodega: **pendiente** (a preguntar al padre). Mientras tanto, todo el proyecto usa el nombre temporal **"Bodega Platform"** / **"bodega-platform"** / **"bodega-webapp"**.

## Alcance funcional acordado

- **Niveles de uso**: administrador / usuario / operario. **Diseño de permisos detallado: pendiente**, se define más adelante (no es prioridad ahora, pero queda como idea a retomar).
- **Registro de productos**: manual al inicio, o por lectura de código de barras. El sistema "aprende" qué código corresponde a qué producto; al escanear un código ya conocido, debe mostrar un mensaje de confirmación del producto antes de registrar el movimiento.
- **Entradas/salidas de producto** (almacenamiento y ventas).
- **Ventas / crédito a clientes**: se registra si el pago fue efectuado o quedó pendiente. Se agregó la idea de manejar **cuotas** (en cuántas cuotas está pagando el cliente y cuántas ya pagó), pero **sin tocar la lógica de transacciones** más allá de eso — mantenerlo simple.
- **Proveedores y lotes**: trazabilidad de lote. **Todos los productos manejan fecha de vencimiento**, por lo tanto el sistema debe generar **alertas de vencimiento** (esto ya existía en el proyecto universitario — "Alerts" — pero hay que profesionalizarlo).
- **Eliminada la función de delivery/tracking** — no aplica al negocio real.
- **Exportación de reportes en PDF** de entradas/salidas, con 3 filtros combinables a elección del usuario: por rango de fechas, por producto, por proveedor.
- **Sin módulo de suscripción/SaaS** — es una app para un solo negocio, no una plataforma multi-cliente (el proyecto original sí tenía esto, se excluyó).

## Lector de código de barras (YHD-1100CB)

Ya configurado y documentado aparte en `resumen_config_lector_YHD-1100CB.md` (mismo directorio). Resumen clave:
- Modo **Bluetooth HID** (funciona como teclado, sin drivers, compatible laptop y celular).
- Terminador **CR (Enter)** al final de cada lectura.
- **Instant upload mode** (envía cada lectura al instante).
- Pendiente de implementar en la app: un `<input>` siempre enfocado que escuche `keydown`, acumule hasta el Enter, y compare el código contra la base de productos (si existe → confirmar y seleccionar producto; si no existe → flujo de registro nuevo).

## Stack tecnológico (heredado del proyecto original, validado como vigente)

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), arquitectura DDD modular por bounded context, CQRS vía `Cortex.Mediator` |
| Datos | Entity Framework Core + MySQL |
| Auth | JWT + BCrypt (módulo `Iam` propio, no ASP.NET Identity) |
| Docs API | Swashbuckle (Swagger) |
| Frontend | Vue 3 + Vite |
| UI | PrimeVue + PrimeFlex |
| Estado | Pinia |
| Routing / i18n | Vue Router / Vue I18n |
| HTTP | Axios |
| Nuevo: PDF | QuestPDF (generación de reportes del lado del servidor) |
| Nuevo: validación | FluentValidation |
| Nuevo: logging | Serilog |
| Nuevo: contenedores | Docker + Docker Compose (backend) |

## Proyectos y repositorios

**Proyecto universitario original** (referencia, sin tocar):
- Backend: `C:\Users\fguer\RiderProjects\qullqa-platform` → GitHub del curso (`upc-pre-202610-1asi0730-17953-flowbit/qullqa-platform`)
- Frontend: `C:\Users\fguer\WebstormProjects\qullqa-webapp-repository` → GitHub del curso (`.../qullqa-webapp`)
- **Nota histórica**: el `appsettings.Production.json` de este repo tenía credenciales reales hardcodeadas (Azure MySQL + secreto JWT). No se migraron al proyecto nuevo — se usan placeholders en su lugar. Es un repo compartido de la universidad, fuera del alcance de este trabajo.

**Proyecto nuevo** (activo, público en GitHub, cuenta `FerG17`):
- Backend: `C:\Users\fguer\RiderProjects\bodega-platform` → https://github.com/FerG17/bodega-platform
- Frontend: `C:\Users\fguer\WebstormProjects\bodega-webapp` → https://github.com/FerG17/bodega-webapp
- Ambos repos: públicos (decisión: no hay riesgo de que otros los modifiquen — eso depende de colaboradores invitados, no de la visibilidad — y así aparece como proyecto propio en el perfil). Se puede renombrar el repo más adelante sin problema cuando se tenga el nombre real de la bodega.

## Qué se hizo, en orden

1. **Análisis inicial** del alcance funcional y del `.md` de configuración del lector de código de barras.
2. **Exploración del proyecto universitario** (backend .NET + frontend Vue) para entender stack y arquitectura antes de decidir cómo migrar.
3. Primer intento: duplicar carpetas tal cual (`robocopy`, sin `.git`) — se descartó porque WebStorm/Rider arrastraban estado cacheado del `.idea` copiado (la terminal seguía mostrando la ruta del proyecto viejo). Esas copias se **eliminaron**.
4. **Proyectos nuevos creados desde cero** con los wizards de Rider (ASP.NET Core Web API, .NET 10, con Docker + Docker Compose) y WebStorm (`npm create vite -- --template vue`), con git local inicializado en ambos.
5. **NuGets agregados al backend**: los del proyecto original (EF Core, MySQL, JWT, BCrypt, Cortex.Mediator, Swashbuckle, Humanizer) + nuevos (QuestPDF, FluentValidation, Serilog). Se quitó `Microsoft.AspNetCore.OpenApi` (redundante e inseguro) y se reemplazó por configuración de Swashbuckle con esquema Bearer/JWT. `dotnet build` verificado limpio.
6. **Migración de la estructura de dominio** (bounded contexts) de ambos proyectos, con namespace renombrado `Flowbit.Qullqa.Platform` → `Bodega.Platform`:
   - Portado: `Iam`, `Products`, `Sales`, `Suppliers`, `Alerts`, `Dashboard`, `Shared` (342 archivos backend, ~87 frontend).
   - Excluido a propósito: `Deliveries` y `Subscription` (fuera de alcance).
   - Se cortó el acoplamiento cruzado que quedaba roto por la exclusión (FK `Business.PlanId` hacia Subscription, UI de tracking de envíos en la lista de órdenes de compra de Suppliers, referencias de navegación/i18n a "tracking" y "plan").
   - Credenciales reales del proyecto original **no se migraron** — el nuevo proyecto usa placeholders (`%BODEGA_DB_PASSWORD%`, `%BODEGA_JWT_SECRET%`, etc.).
   - Ambos proyectos compilan limpio (`dotnet build` / `npm run build`).
7. **Publicación en GitHub**: se instaló GitHub CLI (no estaba instalado), login como `FerG17`, se crearon ambos repos como públicos y se hizo push del primer commit.
8. **Corrección del mensaje del primer commit** (se sacó la mención al proyecto universitario, se hizo `amend` + `push --force-with-lease` en ambos repos — seguro porque eran repos recién creados de un solo commit, sin colaboradores).

## Pendiente / próximos pasos

1. ~~**Modelo de dominio**: ajustar `Products`/lotes para fecha de vencimiento + alertas "profesionales", y `Sales` para el manejo de cuotas de crédito.~~ **Hecho** (Fases B1, B2, B3 — ver `plan_desarrollo_bodega.md`).
2. ~~**Migraciones de EF Core**: generar de cero.~~ **Hecho** (Fase B0, más las incrementales de cada fase posterior).
3. **Integración del lector de código de barras** en el frontend (input enfocado + listener de Enter + confirmación de producto).
4. ~~**Roles y permisos** (admin/usuario/operario): mapear permisos exactos por módulo.~~ **Hecho en el backend** (Fase B4: matriz completa sobre 19 controllers, con roles reales `ADMIN`/`CASHIER`/`WAREHOUSE`). Falta reflejarlo en la UI (Fase F6).
5. **Diseño del frontend**: hay cambios de diseño pendientes de discutir (el usuario los mencionó pero se pospuso hasta avanzar el dominio del backend).
6. **Nombre real de la bodega**: cuando se tenga, renombrar namespaces (`Bodega.Platform` → nombre final), `package.json`, nombre de base de datos, política CORS, y — importante — reemplazar el logo (`qullqa_logo.jpeg` todavía tiene "QULLQA" incrustado en la imagen).
7. ~~Decidir si conviene branch protection u otras reglas en los repos públicos nuevos.~~ **Hecho (2026-08-10)**: se instaló y autenticó GitHub CLI, se creó la rama `develop` en `bodega-platform` (a partir de `main`), y se activó branch protection (PR obligatorio antes de mergear, sin force-push, sin borrado) en `main` y `develop` de `bodega-platform`, y en `main` de `bodega-webapp` (su `develop` se crea cuando arranque la fase de frontend).
8. **Seguridad**: aplicar un enfoque estricto en capas (autenticación, aislamiento multi-tenant, hardening de API, frontend) — detallado como fases dedicadas en `plan_desarrollo_bodega.md`. **Backend hecho** (Fase B0.5 + las correcciones de la auditoría, PRs #10–#16); falta la parte del frontend (Fase F0.5).

## Estado actual (2026-08-11)

**Backend completo** (Fases B0 a B6) y **auditado de forma independiente** por tres agentes sin contexto previo del desarrollo, que encontraron defectos críticos reales — todos corregidos en los PRs #10 a #16. Detalle completo en la sección "2.bis Auditoría independiente" de `plan_desarrollo_bodega.md`.

Lo más grave que encontró la auditoría: el secreto JWT nunca se expandía desde su variable de entorno, así que en producción la clave de firma habría sido una cadena pública del repositorio — cualquiera podía haber forjado un token de administrador. No llegó a ser explotable porque el proyecto todavía no está desplegado.

Hay una **red de seguridad de 24 tests de integración** (`dotnet test` con el MySQL de Docker levantado) contra la API HTTP real. Cada test que cubre un bug de la auditoría fue verificado en rojo contra el código previo antes de aceptarse.

**Frontend sin empezar** — Fases F0 a F7 pendientes.

## Flujo de trabajo Git (bodega-platform)

- `main`: solo recibe merges desde `develop`, cuando un conjunto de tareas está probado y estable.
- `develop`: rama de integración, recibe el merge de cada tarea terminada.
- `feature/xxx`, `fix/xxx`, `chore/xxx`, etc.: una por tarea, creada desde `develop`. Se completa, se verifica que compile y no rompa nada existente, se abre PR y se mergea a `develop` (branch protection exige PR, no se puede pushear directo a `main` ni `develop`).
- Cuando se acumulen tareas suficientes y estén verificadas, PR de `develop` → `main`.

Plan de ejecución detallado (fases, ramas, orden de trabajo backend→frontend): ver `plan_desarrollo_bodega.md` (mismo directorio).

---

## Cómo funciona el proyecto universitario original (Qullqa)

Documentación de referencia sobre el funcionamiento real de `qullqa-platform` (backend) y `qullqa-webapp-repository` (frontend), para entender exactamente sobre qué base se construyó el proyecto nuevo y qué se dejó fuera.

### Backend (`qullqa-platform`) — ASP.NET Core / DDD modular

**Módulos (bounded contexts)**: `Iam`, `Products`, `Sales`, `Suppliers`, `Deliveries`, `Alerts`, `Dashboard`, `Subscription`, `Shared`. Es un monolito modular: un solo proyecto/ejecutable y un solo `AppDbContext`, donde cada módulo aporta su propia configuración EF Core al modelo central.

**Capas dentro de cada módulo** (patrón DDD/CQRS consistente en todos):
- **Domain**: agregados y entidades (`Model/Aggregates`, `Model/Entities`), comandos/queries como objetos puros (`Model/Commands`, `Model/Queries`), eventos de dominio (`Model/Events`), e interfaces de repositorio (`Repositories`). Sin dependencias hacia afuera.
- **Application**: orquesta casos de uso (`CommandServices`/`QueryServices` + implementaciones internas), maneja eventos de otros módulos (`Internal/EventHandlers`), y expone el **ACL** — fachadas (`IProductContextFacade`, `ISalesContextFacade`, etc.) que son el único canal permitido para que un módulo consulte datos de otro, evitando acoplarse a sus internals.
- **Infrastructure**: implementación técnica — repositorios EF Core, configuración del modelo (`ApplyXConfiguration`), piezas específicas (JWT en Iam, BCrypt en Iam, el job de barrido de vencimientos en Alerts).
- **Interfaces**: controladores REST, DTOs de request/response (`Resources`) y assemblers que convierten Resource ↔ Command/Query ↔ Entidad.

Flujo típico de una petición: Controller → assembler arma el Command/Query → *Service (Application) → Repository (Infrastructure) → `AppDbContext`. Cortex.Mediator se usa para publicar/consumir **eventos de dominio entre módulos** (no para las llamadas Command/Query normales, que van por inyección de dependencias directa).

**Módulos principales, resumidos**:
- **Iam**: `Business` (tenant) + `User` + `Role` (catálogo fijo ADMIN/CASHIER/WAREHOUSE). Sign-up crea `User`+`Business` atómicamente y devuelve JWT (auto-login). Multi-tenant: todo módulo filtra por `BusinessId` vía `ICurrentUserAccessor`.
- **Products**: `Product`, `Warehouse`, `Batch` (lote con vencimiento y precio de compra), `StockMovement`, inventario por almacén. Emite eventos (`StockLevelChangedEvent`, `BatchRegisteredEvent`) que Alerts consume. Expone facade para que Sales/Suppliers descuenten o ingresen stock.
- **Sales**: `Sale` (con líneas embebidas), `Customer`. Total siempre calculado server-side. Al vender, descuenta stock vía el facade de Products.
- **Suppliers**: `Supplier`, `PurchaseOrder` (con líneas). Marcar una orden como `RECEIVED` dispara automáticamente el ingreso de stock correspondiente.
- **Dashboard**: sin agregados propios (salvo `Report`, que solo guarda metadatos); todos los KPIs/reportes se calculan **en vivo** componiendo los facades de Sales y Products. Exportación existente: **solo CSV**, no hay generación de PDF en el backend actual.
- **Alerts**: motor de alertas con dos mecanismos — reactivo (escucha eventos de Products vía Cortex.Mediator y genera/actualiza alertas al instante) y programado (`AlertExpirationSweepJob`, background service que recorre lotes cada N horas para detectar vencimientos por simple paso del tiempo). Tipos: `LOW_STOCK`, `OUT_OF_STOCK`, `EXPIRATION`, `EXPIRED`. Reglas configurables por negocio (`AlertRule`, con umbral de días para vencimiento). El modelo tiene campos preparados para notificar (`Notified`/`NotifiedAt`) pero **no hay dispatcher real de push/email** — es solo el modelo de datos.

**Autenticación**: JWT propio (no ASP.NET Identity), claims con `userId`, `email`, `business_id`, `Role`. Middleware custom (`RequestAuthorizationMiddleware`) exige token válido salvo `[AllowAnonymous]`. Los roles viajan en el token pero **no hay autorización granular por rol implementada** — solo valida "autenticado o no", el control fino por rol queda pendiente (coincide con lo que el usuario pidió dejar como idea a futuro).

**Relaciones cruzadas clave**: casi todo cruce entre módulos pasa por un facade ACL; las únicas FKs reales documentadas son `User.BusinessId`, `Business.PlanId` (→ Subscription) y `Delivery.PurchaseDetailId` (→ Suppliers).

### Frontend (`qullqa-webapp-repository`) — Vue 3

**Estructura**: `src/` organizado por bounded context (`iam`, `product`, `sales`, `suppliers`, `delivery`, `subscription`, `alerts`, `dashboard`, `shared`), cada uno con subcapas `domain/model` (entidades JS con reglas de negocio), `application` (store Pinia), `infrastructure` (`*.api.js` con Axios + `*.assembler.js`), y `presentation` (`*.routes.js`, `views/`, `components/`).

**Puntos clave**:
- **Routing**: rutas públicas (`sign-in`, `sign-up`, `forgot-password`) fuera del layout; el resto vive bajo `/app` con un layout de sidebar. Guard global (`authenticationGuard`) redirige a `sign-in` si no hay sesión. No hay guard de roles a nivel de ruta.
- **Estado (Pinia)**: un store por bounded context, todos con Composition API. No existe un store de "carrito" separado — el estado del POS vive dentro de `sales.store.js`.
- **Auth**: sesión (usuario) en `localStorage` (`qullqa.session`), token separado (`qullqa.token`). Axios interceptor añade el Bearer automáticamente; al recibir 401 dispara un evento global que `iam.store.js` escucha para cerrar sesión (desacopla `shared` de `iam`). Logout hace **reload completo del navegador** para limpiar todos los stores en memoria.
- **i18n**: español (default) e inglés (fallback), dos archivos planos de traducciones.
- **UI**: PrimeVue 4 + PrimeFlex, estilos mayormente inline con paleta hardcodeada por componente (no hay design tokens centralizados).
- **Lector de código de barras**: **no existe ningún código relacionado** en este repo (ni librería, ni componente) — habrá que construirlo desde cero en el proyecto nuevo, tal como estaba previsto.
- **Exportación PDF**: **no existe** en el frontend tampoco; la única exportación implementada es CSV (client-side, desde Dashboard).
- **Alerts (frontend)**: dashboard con tabs "Activas" (filtros por estado/tipo, banner de alertas críticas con animación) y "Reglas" (activar/desactivar por tipo, umbral de días para vencimiento). El store ya no evalúa reglas en cliente — solo consume lo que genera el backend. El sidebar muestra un badge con el conteo de alertas activas.

### Comparación con lo que se busca para la bodega

| Aspecto | Proyecto universitario (Qullqa) | Proyecto nuevo (Bodega) |
|---|---|---|
| Multi-tenant (varios negocios en un mismo sistema) | Sí (`Business`, pensado para SaaS multi-cliente) | No aplica — es para un solo negocio; el concepto `Business` se mantiene en el código pero ya no tiene sentido de "múltiples clientes" |
| Suscripción/planes | Sí (`Subscription`, catálogo de planes, sin billing real) | **Eliminado** — no aplica a un negocio único |
| Tracking de envíos | Sí (`Deliveries`, GPS/waypoints simulados) | **Eliminado** — no aplica al negocio real |
| Roles/permisos | Roles existen en el modelo (ADMIN/CASHIER/WAREHOUSE) pero sin autorización granular real | Se mantiene la idea, pendiente de diseñar permisos reales por módulo (administrador/usuario/operario) |
| Alertas de vencimiento | Ya implementado (reactivo + job programado), pero simple | Se mantiene y se busca "profesionalizar" (posiblemente: notificaciones reales, mejor UX, más granularidad) |
| Cuotas de crédito en ventas | No existe — `Sale` no maneja cuotas, solo la venta en sí | Por agregar: número de cuotas y cuántas se pagaron, sin tocar la lógica transaccional existente |
| Exportación de reportes | Solo CSV, sin filtros combinables documentados más allá del rango del reporte | Por construir: PDF (QuestPDF) con 3 filtros combinables (fecha, producto, proveedor) |
| Lector de código de barras | No implementado | Por construir — el hardware ya está configurado (YHD-1100CB) |
| Backend | ASP.NET Core, DDD modular, CQRS-ligero vía Cortex.Mediator, JWT+BCrypt propio, MySQL | Mismo enfoque arquitectónico, mismo stack — heredado y ya portado |
| Frontend | Vue 3 + PrimeVue/PrimeFlex + Pinia | Mismo stack — heredado y ya portado |

### Funcionalidades eliminadas al migrar (detalle técnico)

**Backend — `Deliveries`**: agregado `Delivery` (tracking number, proveedor, origen/destino, conductor/vehículo, estado `REGISTERED→IN_TRANSIT→AT_DESTINATION→COMPLETED`/`CANCELLED`, coordenadas GPS, progreso de ruta) + entidad `Waypoint` (checkpoints geolocalizados). Vinculado opcionalmente a una línea de orden de compra (`PurchaseDetailId`, FK real). El código lo marcaba como feature "Premium" pero sin gating real implementado.

**Backend — `Subscription`**: agregado `Plan` (nombre, precio, periodicidad, features en texto libre) + FK real `Business.PlanId`. Sin billing real (no hay Stripe ni ningún procesador de pagos) y sin feature-gating (las "features" del plan eran solo texto de marketing, no restringían nada en código).

**Frontend — `delivery/`**: vista `delivery-list.vue` (dashboard de seguimiento con tarjetas en vivo y barra de progreso), `delivery-detail-modal.vue` (stepper de waypoints con simulación de actualización GPS/IoT), `delivery-form-modal.vue` (registro de envío vinculado a una orden de compra). Se perdió también el botón "Crear envío"/"Ver seguimiento" que aparecía en la lista de órdenes de compra de Suppliers.

**Frontend — `subscription/`**: sin vistas ni rutas propias — vivía integrado en la tab "Plan" de Ajustes (`settings.vue`), mostrando catálogo de planes y permitiendo cambiar el plan del negocio. Se perdió esa tab y el flujo de upgrade de plan.

En ambos casos, al excluir estos módulos del proyecto nuevo se cortó también el acoplamiento cruzado que dejaban: la FK `Business.PlanId` hacia Subscription, y las referencias de navegación/i18n hacia "tracking" y "plan" dentro de Suppliers e Iam respectivamente (ya resuelto durante la migración, según el punto 6 de "Qué se hizo, en orden").
