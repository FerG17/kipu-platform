# Plan de desarrollo — App de gestión para bodega

Hoja de ruta para avanzar con la implementación, ordenada en fases y tareas por rama. Complementa a `resumen_proyecto_bodega.md` (contexto y decisiones ya tomadas) — este documento es el **plan de ejecución** a partir de ahí.

Orden general acordado: **backend completo primero → frontend después → ajustes transversales al final** (rebranding, despliegue, diseño visual). La razón de este orden: el backend define el contrato (API) que el frontend va a consumir; avanzar en el frontend antes de cerrar el modelo de dominio implicaría rehacer pantallas cuando cambien los datos.

---

## 1. Flujo de trabajo en GitHub

### 1.1 Estrategia de ramas

- **`main`**: protegido, solo recibe merges desde `develop`, y solo cuando un conjunto de tareas está completo, probado y estable (nunca una tarea suelta a medio probar).
- **`develop`**: protegida también, es la rama de integración donde vive el trabajo en curso. Cada tarea terminada se mergea acá primero.
- **`feature/...`, `fix/...`, `chore/...`, etc.**: una rama por tarea, creada siempre desde `develop` actualizada, con alcance acotado (idealmente completable y probable en una sesión de trabajo). Al terminar la tarea y confirmar que compila y no rompe nada, PR hacia `develop` (no hacia `main`).
- Convención de nombres:

| Prefijo | Uso |
|---|---|
| `feature/...` | Funcionalidad nueva |
| `fix/...` | Corrección de un bug |
| `chore/...` | Tareas de infraestructura/mantenimiento (docker, migraciones, config) |
| `refactor/...` | Reestructuración sin cambiar comportamiento |
| `docs/...` | Documentación |

Ejemplos ya definidos más abajo: `feature/sales-credit-installments`, `feature/products-batch-expiration`, `chore/ef-core-baseline-migrations`, etc.

### 1.2 Commits

Usar [Conventional Commits](https://www.conventionalcommits.org/): `feat: ...`, `fix: ...`, `chore: ...`, `refactor: ...`, `docs: ...`. Facilita leer el historial y, más adelante, generar changelog automático si se quiere.

### 1.3 Pull Requests

Aunque el equipo sea de una persona (+ Claude), abrir PR de cada rama de tarea hacia `develop` en vez de hacer merge directo:
- Permite revisar el diff completo antes de integrar (evita que algo se cuele sin querer).
- Deja registro de qué se hizo y por qué (descripción del PR).
- Facilita revertir una tarea puntual si algo sale mal, sin afectar el resto.

Plantilla sugerida de descripción de PR:
```
## Qué cambia
## Por qué
## Cómo probarlo
```

Merge recomendado:
- **`feature/*` → `develop`**: merge normal con commit (no squash), para conservar el historial de cada tarea dentro de `develop`.
- **`develop` → `main`**: cuando se cierra un conjunto de tareas verificado y estable, PR de `develop` a `main`.

Borrar la rama de tarea después de mergearla a `develop`.

**Branch protection activa en GitHub** (`main` y `develop`, en `bodega-platform` y en `main` de `bodega-webapp` — su `develop` se crea al iniciar la fase de frontend): PR obligatorio antes de mergear, sin force-push, sin borrado de rama. Configurado el 2026-08-10 vía `gh api`. (Más adelante, si se agrega CI, sumar "Require status checks to pass".)

### 1.4 Issues y tablero (opcional, recomendado)

Crear un Issue por tarea (uno por cada rama listada en este plan) en cada repo, y un GitHub Project (tablero Kanban: `Backlog` / `En progreso` / `Review` / `Done`) para visualizar avance. Vincular el PR al issue con `Closes #N` en la descripción para que se cierre automáticamente al mergear.

### 1.5 Definición de "hecho" (Definition of Done) por tarea

- [ ] Compila sin errores ni warnings nuevos (`dotnet build` / `npm run build`).
- [ ] Backend: si hay cambios de modelo, migración generada (`dotnet ef migrations add ...`) y probada localmente contra MySQL (`dotnet ef database update`).
- [ ] Backend: endpoints nuevos/modificados probados manualmente en Swagger.
- [ ] Frontend: funcionalidad probada en navegador, en viewport de laptop **y** de celular.
- [ ] No se rompió nada de lo ya portado (smoke test rápido de las pantallas/endpoints relacionados).
- [ ] **Seguridad**: cualquier endpoint nuevo valida autenticación, rol, y que el dato pertenece al `BusinessId` del usuario autenticado (no se puede acceder a datos de otro negocio cambiando un ID). Cualquier input nuevo pasa por validación server-side (FluentValidation), nunca se confía solo en la validación del frontend. No se agregan secretos, tokens ni credenciales al código ni a los logs.
- [ ] PR abierto, revisado y mergeado a `develop`; rama borrada.
- [ ] Si el cambio afecta el alcance o las decisiones documentadas, actualizar `resumen_proyecto_bodega.md`.

### 1.6 Ritual de sesión de trabajo (con Claude)

1. `git checkout develop && git pull`
2. `git checkout -b <nombre-de-rama>` (según la tarea que toque del plan)
3. Trabajar la tarea con Claude.
4. Verificar build/tests localmente.
5. Commit(s) siguiendo Conventional Commits.
6. `git push -u origin <nombre-de-rama>`
7. `gh pr create --base develop` (revisar el diff antes de mergear).
8. Merge (con commit, no squash) → `git checkout develop && git pull && git branch -d <nombre-de-rama>`.
9. Pasar a la siguiente tarea del plan.
10. Cuando un conjunto de tareas de `develop` esté completo y verificado: `gh pr create --base main --head develop` → revisar → merge a `main`.

---

## 2. Fases — Backend (`bodega-platform`)

### Fase B0 — Preparación de infraestructura

Objetivo: tener una base de datos real corriendo localmente antes de tocar el modelo de dominio (las migraciones viejas no se portaron a propósito).

- [ ] `chore/docker-compose-mysql` — agregar el servicio de MySQL a `docker-compose.yaml` (por ahora solo tiene el API), con volumen persistente y variables de entorno acordes a `appsettings.Development.json`.
- [x] ~~`chore/ef-core-baseline-migrations` — generar la migración inicial por módulo (Initial_<Modulo>)~~ — **hecho (2026-08-10), ajustado en la ejecución**: `AppDbContext` es un único DbContext compartido por todos los bounded contexts (una sola base física, todos los módulos ya cableados en `OnModelCreating`), así que no tiene sentido una migración incremental por módulo sin historial previo que preservar. Se generó una única migración baseline (`InitialCreate`) con el modelo completo y se aplicó (`dotnet ef database update`) contra el MySQL de Docker. Confirmado: 17 tablas creadas (una por cada agregado de Iam/Products/Sales/Suppliers/Alerts/Dashboard/Shared), API arranca sin errores y Swagger responde 200. De acá en adelante, cada cambio real de modelo genera su propia migración incremental normal.
- [x] ~~Confirmar branch protection en `bodega-platform`~~ — hecho el 2026-08-10 (ver 1.3).

*Depende de*: nada (es el punto de partida).

### Fase B0.5 — Seguridad base del backend

Objetivo: dejar la base de la API endurecida **antes** de construir funcionalidad nueva encima, para no tener que rehacer nada de seguridad más adelante. Esta app maneja datos sensibles reales (ventas, créditos y datos de clientes de un negocio real), así que se trata en serio desde el inicio, no como un ajuste de último momento.

- [x] `feature/backend-security-baseline` — **hecho (2026-08-10)**, cubre:
  - **Aislamiento multi-tenant (crítico)**: implementado como **filtro global de EF Core** (`AppDbContext.OnModelCreating` → `ApplyBusinessScopedQueryFilters`), no como checks manuales sueltos por controller. Se aplica automáticamente a las 13 entidades con `BusinessId` propio (vía reflection, `HasQueryFilter`) más `Business` (filtrada por `Id`). Diseño **fail-closed**: si no hay negocio autenticado (`CurrentBusinessId == null`), el filtro no deja ver nada — nunca "ve todo" por accidente. Los pocos flujos que sí necesitan cruzar negocios legítimamente (búsqueda de email en login/registro, el barrido de alertas en background) usan `.IgnoreQueryFilters()` explícito y documentado en el propio método del repositorio, nunca por defecto. `BaseRepository.FindByIdAsync` se cambió de `FindAsync` a una query LINQ explícita (`FindAsync` no garantiza pasar por los filtros globales). Además se agregaron validaciones de pertenencia de `ProductId`/`WarehouseId` al negocio actual en `InventoryCommandService`, `SaleCommandService` y `PurchaseOrderCommandService` (antes se podía crear una venta/compra/inventario referenciando un producto de otro negocio). **Probado empíricamente** con dos negocios reales (sign-up A y B): lectura cruzada de products/users/businesses por id → 404; listados cruzados → vacíos; venta de B referenciando producto de A → 404 `ProductNotFound`; el SQL generado confirma que el filtro se re-evalúa por request (`@ef_filter__CurrentBusinessId` distinto por token), no queda "congelado" con el primer negocio que arrancó la app.
  - **JWT más estricto**: expiración bajada de 7 días a 1 día (`appsettings.json`/`appsettings.Production.json`; se dejó en 7 en `appsettings.Development.json` por comodidad local). Revocación real vía `User.TokenVersion`: se incrementa al cambiar contraseña (`UpdatePasswordHash`) o desactivar el usuario (`Deactivate`), se incluye como claim del JWT, y `RequestAuthorizationMiddleware` lo revalida contra la base en cada request junto con `Status == "ACTIVE"` — un cambio de contraseña invalida todos los tokens viejos al instante, no hay que esperar a que expiren. Probado: cambiar contraseña con un token → ese mismo token pasa a devolver 401 inmediatamente.
  - **Rate limiting**: `Microsoft.AspNetCore.RateLimiting` — límite global de 120 req/min por IP para toda la API, y política `"auth"` más estricta (10 req/min por IP) aplicada a `AuthenticationController` completo (sign-in y sign-up). Probado con 15 intentos seguidos de login: los primeros pasan, el resto devuelve 429.
  - **Política de contraseñas**: FluentValidation (ya estaba referenciado en el `.csproj` pero sin usar — ahora conectado). Mínimo 8 caracteres + al menos una letra + un número, validado en sign-up, invitar usuario, y cambio de contraseña (`SignUpCommandValidator`, `InviteUserCommandValidator`, `ChangePasswordCommandValidator`). Deliberadamente no se exige símbolos/mayúsculas — el objetivo es proteger al personal real de la bodega, no bloquearlos con una política que no van a recordar.
  - **CORS estricto**: confirmado, ya estaba bien — `AllowedOrigins` vacío por defecto en producción (nunca `*`), solo `localhost:5173` en desarrollo.
  - **Headers de seguridad**: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin` en toda respuesta; `Server` header de Kestrel deshabilitado (`AddServerHeader = false`); `Content-Security-Policy: default-src 'none'` activa solo en Producción (en dev/staging Swagger UI necesita cargar sus propios scripts, así que ahí no aplica).
  - **HTTPS/HSTS**: `app.UseHsts()` activo fuera de `Development`. Como efecto colateral se aprovechó para además ocultar Swagger UI en Producción (antes quedaba expuesto sin querer).
  - **Auditoría de acciones sensibles**: se conectó Serilog (estaba en el `.csproj` sin usar) con `UseSerilogRequestLogging`, una línea estructurada por request enriquecida con `UserId`/`BusinessId` del usuario autenticado — trazabilidad de "quién hizo qué endpoint y cuándo" a nivel de logs. **Nota de alcance**: esto es logging estructurado, no una tabla de auditoría con historial de cambios por entidad (quién cambió qué campo). Si más adelante se necesita eso (ej. para disputas de ventas/cuotas), es una fase aparte — `AuditableEntityInterceptor` por ahora solo sigue sellando `CreatedAt`/`UpdatedAt`, no un actor. Confirmado que ningún sink loggea contraseñas/tokens (Serilog no captura bodies; el único lugar donde se ve un hash de password es el log de comandos SQL de EF Core, y solo en Development con `EnableSensitiveDataLogging` explícitamente activado para debug — nunca en Producción).
  - Migración nueva: `AddUserTokenVersion` (columna `token_version` en `users`).
  - **Archivos tocados** (4 commits en `feature/backend-security-baseline`, mergeado a `develop` en PR #3):
    - *Aislamiento multi-tenant*: `Shared/Infrastructure/Persistence/EntityFrameworkCore/Configuration/AppDbContext.cs`, `Shared/.../Repositories/BaseRepository.cs`, `Iam/Infrastructure/Persistence/EntityFrameworkCore/Repositories/UserRepository.cs`, `Products/.../Repositories/BatchRepository.cs`, `Alerts/.../Repositories/AlertRepository.cs`, `Alerts/.../Repositories/AlertRuleRepository.cs`, `Products/Application/Internal/CommandServices/InventoryCommandService.cs`, `Sales/Application/Internal/CommandServices/SaleCommandService.cs` + `Sales/Domain/Model/Errors/SalesError.cs` + `Sales/Interfaces/Rest/Transform/SalesActionResultAssembler.cs` + `Sales/Resources/SalesMessages(.es).resx`, `Suppliers/Application/Internal/CommandServices/PurchaseOrderCommandService.cs` + `Suppliers/Domain/Model/Errors/SuppliersError.cs` + `Suppliers/Interfaces/Rest/Transform/SuppliersActionResultAssembler.cs` + `Suppliers/Resources/SuppliersMessages(.es).resx`.
    - *JWT + revocación*: `Iam/Domain/Model/Aggregates/User.cs`, `Iam/Domain/Repositories/IUserRepository.cs`, `Iam/Infrastructure/Tokens/Jwt/Services/TokenService.cs`, `Iam/Infrastructure/Pipeline/Middleware/Components/RequestAuthorizationMiddleware.cs`, `appsettings.json`, `appsettings.Production.json`, migración `Shared/.../Migrations/20260810211623_AddUserTokenVersion(.Designer).cs`.
    - *Política de contraseñas*: `Iam/Domain/Model/Commands/Validation/PasswordRuleExtensions.cs` (nuevo), `SignUpCommandValidator.cs` (nuevo), `InviteUserCommandValidator.cs` (nuevo), `ChangePasswordCommandValidator.cs` (nuevo), `Iam/Domain/Model/Errors/IamError.cs`, `Iam/Application/Internal/CommandServices/UserCommandService.cs`, `Iam/Interfaces/Rest/Transform/IamActionResultAssembler.cs`, `Iam/Resources/IamMessages(.es).resx`.
    - *Rate limiting, headers, HSTS, Serilog*: `Program.cs`, `Iam/Interfaces/Rest/AuthenticationController.cs`.
    - *Infraestructura previa (Fase B0, PRs #1 y #2)*: `compose.yaml`, `Bodega.Platform/appsettings.Development.json`, migración baseline `Shared/.../Migrations/20260810205154_InitialCreate(.Designer).cs` + `AppDbContextModelSnapshot.cs`.

*Depende de*: Fase B0.

### Fase B1 — Products: lotes y vencimientos "profesionales"

- [x] `feature/products-batch-expiration` — **hecho (2026-08-10)**:
  - **IDOR encontrado y corregido**: `InventoryCommandService.Handle(CreateOrUpdateBatchCommand)` no validaba que el `ProductId` perteneciera al negocio actual antes de crear/actualizar el lote — mismo patrón que Inventory/Sale/PurchaseOrder en Fase B0.5, se me había pasado en esa fase porque `CreateOrUpdateBatchCommand` vive en el mismo command service pero no se tocó entonces. Ahora valida con `productRepository.FindByIdAsync` (filtro global de negocio) antes de escribir → 404 `ProductNotFound` si el producto no es del negocio autenticado. Probado con dos negocios reales.
  - **Fecha de vencimiento pasada rechazada**: nuevo `ProductError.InvalidExpirationDate` (400) si `Expiration` es anterior a hoy (UTC). Probado.
  - **"Días restantes" expuesto de forma consistente**: `BatchResource` ahora incluye `DaysToExpiry`, `IsExpired`, `IsExpiringSoon`, calculados en el servidor (`BatchResourceFromEntityAssembler`, reusando `Batch`/`ExpirationRules` — las mismas reglas que usa Alerts) en vez de que cada cliente haga su propia matemática de fechas. Antes el frontend hubiera tenido que calcularlo él mismo a partir de `Expiration` (riesgo de desincronía/timezone con lo que realmente dispara las alertas).
  - **`BatchRegisteredEvent`/`StockLevelChangedEvent` confirmados funcionando**: revisado el código (ya estaban bien portados, no requirió cambios) y probado end-to-end — crear un lote con vencimiento a 3 días generó automáticamente una `Alert` tipo `EXPIRATION` con el mensaje y `daysToExpiry` correctos vía `BatchRegisteredEventHandler`.
  - **Archivos tocados**: `Products/Application/Internal/CommandServices/InventoryCommandService.cs`, `Products/Domain/Model/Errors/ProductError.cs`, `Products/Interfaces/Rest/Resources/BatchResource.cs`, `Products/Interfaces/Rest/Transform/BatchResourceFromEntityAssembler.cs`, `Products/Interfaces/Rest/Transform/ProductActionResultAssembler.cs`, `Products/Resources/ProductMessages(.es).resx`.

*Depende de*: Fase B0.

### Fase B2 — Alerts: motor de alertas profesional

- [x] `feature/alerts-engine` — **hecho (2026-08-10)**:
  - **Bug real encontrado y corregido (severidad media)**: `AlertExpirationSweepJob` (el barrido programado) generaba alertas de vencimiento con el **nombre del producto vacío** (`" vence en 3 día(s)."` en vez de `"Arroz Costeno vence en 3 día(s)."`). Causa: `ProductContextFacade.GetAllActiveBatchesForExpirationSweep` resolvía los nombres con `productRepository.ListAsync()`, el método genérico que ahora pasa por el filtro global de negocio (Fase B0.5) — como el sweep corre en background sin negocio autenticado, el filtro fail-closed devolvía cero productos. Se agregó `IProductRepository.ListIgnoringTenantAsync()` (mismo patrón que `FindByIdIgnoringTenantAsync` de Iam) y se usó ahí. Confirmado con `grep` que no hay otros casos del mismo patrón en el resto del código (el único otro `.ListAsync()` es sobre `Role`, que no tiene `BusinessId` y por lo tanto nunca recibió el filtro).
  - **`AlertRule` (umbral configurable)**: ya estaba bien implementado (upsert por `(BusinessId, AlertType)`, defaults sensatos cuando no existe regla). Se le agregó la única validación que faltaba: `ThresholdValue` no puede ser negativo (`AlertsError.InvalidThreshold`, 400) — antes se podía guardar un umbral negativo sin error.
  - **Alertas duplicadas**: confirmado que ya estaba bien resuelto — `FindActiveByProductAndTypeAsync` busca por `Status != Resolved` (no solo `ACTIVE`), así que un alerta `ACKNOWLEDGED` se actualiza in-place (`RefreshStockInfo`) en vez de crear una segunda. Probado explícitamente: reconocer una alerta y volver a disparar el mismo evento no duplica, solo refresca.
  - **Flujo `ACTIVE → ACKNOWLEDGED/RESOLVED`**: probado end-to-end — reconocer no la oculta de la lista de "activas" (sigue visible, solo cambia de estado), resolver la mueve a historial, resolver dos veces devuelve 409 (inmutabilidad de alertas resueltas).
  - **Punto de extensión para notificaciones**: nuevo `IAlertNotificationDispatcher` (interfaz) + `NoOpAlertNotificationDispatcher` (implementación por defecto, no hace nada — deliberadamente no marca `Notified`, ya que no se envió nada de verdad). Conectado en los dos event handlers reactivos y en el sweep job, pero **solo se llama cuando se crea una alerta nueva**, no en cada refresh de una ya activa (evitaría re-notificar en cada venta/entrada que toque un producto que ya está bajo). Swapping a un dispatcher real (email/push) más adelante es un cambio de una línea en `Program.cs`, sin tocar nada de la lógica de alertas.
  - **Archivos tocados**: `Alerts/Application/Internal/CommandServices/AlertRuleCommandService.cs`, `Alerts/Application/Internal/EventHandlers/BatchRegisteredEventHandler.cs`, `Alerts/Application/Internal/EventHandlers/StockLevelChangedEventHandler.cs`, `Alerts/Application/Internal/OutboundServices/IAlertNotificationDispatcher.cs` (nuevo), `Alerts/Infrastructure/Notifications/NoOpAlertNotificationDispatcher.cs` (nuevo), `Alerts/Domain/Model/Errors/AlertsError.cs`, `Alerts/Infrastructure/Pipeline/BackgroundServices/AlertExpirationSweepJob.cs`, `Alerts/Interfaces/Rest/Transform/AlertsActionResultAssembler.cs`, `Alerts/Resources/AlertsMessages(.es).resx`, `Products/Application/Acl/ProductContextFacade.cs`, `Products/Domain/Repositories/IProductRepository.cs`, `Products/Infrastructure/Persistence/EntityFrameworkCore/Repositories/ProductRepository.cs`, `Program.cs`.

*Depende de*: Fase B1 (usa los eventos de Products).

### Fase B3 — Sales: cuotas de crédito

- [x] `feature/sales-credit-installments` — **hecho (2026-08-10)**:
  - **Descubrimiento importante**: `Sale` no tenía NINGÚN concepto de crédito/pago pendiente todavía — `SaleStatus` solo tiene `PAID`/`CANCELLED` (toda venta se crea como pagada de inmediato; `PaymentMethod` es un string libre sin validar, ej. `"CASH"`/`"CREDIT"` son ambos valores válidos pero sin significado especial en el backend). Lo de "cuotas" es 100% nuevo, no una mejora de algo existente.
  - **Diseño deliberadamente simple** (según lo pedido — "mantenerlo simple"): nueva entidad `PaymentPlan` (tabla `payment_plans`, 1:1 con `Sale` vía `SaleId` único) con solo `TotalInstallments`/`PaidInstallments` — sin fechas de vencimiento ni montos por cuota. Se adjunta a una venta ya existente mediante un comando separado (`CreatePaymentPlanCommand`), nunca como parte de `CreateSaleCommand`. **`Sale.cs`, `SaleCommandService.cs` y `CreateSaleCommand` no se tocaron** — verificado en el diff final.
  - **Comando para pagar una cuota**: `RegisterInstallmentPaymentCommand` — incrementa `PaidInstallments`, rechaza con 409 si ya está completamente pagado.
  - **Queries "cuotas pendientes por cliente/venta"**: `GET /payment-plans/by-sale/{saleId}` (estado de una venta puntual) y `GET /payment-plans/pending?customerId=` (pendientes de un cliente, join contra `Sale.CustomerId`) / `GET /payment-plans/pending` (todas las pendientes del negocio).
  - Nuevo controller `PaymentPlansController`, separado de `SalesController` a propósito.
  - **Probado end-to-end** contra el MySQL de Docker local: cuota inválida (0) → 400; negocio B no puede crear plan sobre venta de A → 404 (el filtro global de tenant ya protege `Sale.FindByIdAsync`); un segundo plan sobre la misma venta → 409; pagar 3 cuotas de 3 → `isFullyPaid: true`; pagar una 4ª → 409; aparece/desaparece correctamente de "pendientes" según el estado; B no puede registrar pago sobre el plan de A → 404.
  - **Archivos tocados**: nuevos — `Sales/Domain/Model/Entities/PaymentPlan.cs`, `Sales/Domain/Model/Commands/CreatePaymentPlanCommand.cs`, `Sales/Domain/Model/Commands/RegisterInstallmentPaymentCommand.cs`, `Sales/Domain/Model/Queries/GetPaymentPlanBySaleIdQuery.cs`, `Sales/Domain/Model/Queries/GetPendingPaymentPlansByBusinessIdQuery.cs`, `Sales/Domain/Model/Queries/GetPendingPaymentPlansByCustomerIdQuery.cs`, `Sales/Domain/Repositories/IPaymentPlanRepository.cs`, `Sales/Infrastructure/Persistence/EntityFrameworkCore/Repositories/PaymentPlanRepository.cs`, `Sales/Application/CommandServices/IPaymentPlanCommandService.cs`, `Sales/Application/Internal/CommandServices/PaymentPlanCommandService.cs`, `Sales/Application/QueryServices/IPaymentPlanQueryService.cs`, `Sales/Application/Internal/QueryServices/PaymentPlanQueryService.cs`, `Sales/Interfaces/Rest/PaymentPlansController.cs`, `Sales/Interfaces/Rest/Resources/CreatePaymentPlanResource.cs`, `Sales/Interfaces/Rest/Resources/PaymentPlanResource.cs`, `Sales/Interfaces/Rest/Transform/CreatePaymentPlanCommandFromResourceAssembler.cs`, `Sales/Interfaces/Rest/Transform/PaymentPlanResourceFromEntityAssembler.cs`, migración `AddPaymentPlans`; modificados — `Sales/Domain/Model/Errors/SalesError.cs`, `Sales/Infrastructure/Persistence/EntityFrameworkCore/Configuration/Extensions/ModelBuilderExtensions.cs`, `Sales/Interfaces/Rest/Transform/SalesActionResultAssembler.cs`, `Sales/Resources/SalesMessages(.es).resx`, `Program.cs`.

*Depende de*: Fase B0 (necesita el modelo `Sale` ya migrado).

### Fase B4 — Roles y permisos

- [x] `feature/iam-roles-permissions` — **hecho (2026-08-10)**:
  - **Confirmado**: el `[Authorize]` existente era 100% decorativo — no aplicaba ningún control de rol, solo "¿hay token válido?". `ICurrentUserAccessor.CurrentUserRole` existía pero no se usaba en ningún lado. `RoleId` viajaba en el JWT como número (`"1"`), sin nombre.
  - **Diseño elegido**: en vez de migrar a las políticas nativas de ASP.NET Core (`AddAuthentication`/`AddAuthorization`, que este proyecto deliberadamente no usa en ningún lado — todo el pipeline de auth es custom), se extendió el `[Authorize]` propio para aceptar roles: `[Authorize(RoleNames.Admin, RoleNames.Warehouse)]`. Sin roles especificados = cualquier rol autenticado (comportamiento previo, compatible). `RequestAuthorizationMiddleware` ahora valida el rol del JWT contra la lista antes de dejar pasar la request, devolviendo 403 (no 401) si el rol no alcanza.
  - **El JWT ahora lleva el nombre del rol** (`"ADMIN"`/`"CASHIER"`/`"WAREHOUSE"`), no el `RoleId` numérico — `TokenService.GenerateToken` recibe el nombre ya resuelto (`UserCommandService` lo busca vía `IRoleRepository` antes de generar el token, en sign-in y sign-up). **Efecto colateral esperado**: todos los tokens ya emitidos antes de este cambio dejan de servir para chequeos de rol (siguen pasando la validación de firma, pero comparan `"1"` contra `"ADMIN"` y fallan) — hay que re-loguearse. Aceptable, todavía no hay usuarios reales.
  - **Matriz de permisos aplicada** (19 controllers):

    | Módulo | Lectura | Escritura |
    |---|---|---|
    | Iam / Users | admin (equipo) / self-o-admin (perfil propio) | admin (invitar/eliminar); self-o-admin (editar perfil, cambiar contraseña) |
    | Iam / Businesses | cualquier rol | admin |
    | Iam / Roles | cualquier rol (catálogo) | — |
    | Products, Warehouses, Inventories, Batches | cualquier rol | admin + almacén |
    | Products / Stock movements (auditoría) | admin + almacén | — |
    | Sales, Customers, Sale details, Payment plans | admin + cajero | admin + cajero |
    | Suppliers, Purchases, Purchase details | admin + almacén | admin + almacén |
    | Alerts (leer / reconocer / resolver) | cualquier rol | reconocer/resolver: admin + almacén; creación manual: admin |
    | Alert rules (umbrales) | cualquier rol | admin |
    | Dashboard / Reports | admin | admin |

  - **"Self-o-admin"**: `GetUserById`/`UpdateProfile`/`ChangePassword` no se pueden expresar como lista estática de roles (necesitan "soy yo, o soy admin"), así que quedaron con `[Authorize]` sin roles + un chequeo explícito en el método (`IsSelfOrAdmin`) — un cajero puede cambiar su propia contraseña pero no la de otro.
  - **Bug evitado antes de llegar a producción**: casi uso `Forbid()` de ASP.NET Core para el 403 del self-or-admin check — ese método dispara el mecanismo de authentication challenge del framework, que este proyecto no tiene registrado (`AddAuthentication()` no se llama en ningún lado). Se cambió a `StatusCode(StatusCodes.Status403Forbidden)` manual, consistente con como el middleware ya devuelve 403.
  - **Probado end-to-end**: creado un cajero y un operario de almacén reales (vía `InviteUser`) además del admin de las pruebas anteriores. Verificado: cajero crea venta (201) pero no puede crear producto (403) ni listar usuarios (403) ni ver dashboard (403); cajero puede cambiar su propia contraseña (204) pero no la de otro (403); almacén crea producto y proveedor (201) pero no puede crear venta (403); cualquier rol puede leer `alert-rules` pero solo admin puede escribirlas (403 para almacén); admin sigue teniendo acceso a todo.
  - **Archivos tocados**: nuevo — `Iam/Domain/Model/Entities/RoleNames.cs`; modificados — `Iam/Infrastructure/Pipeline/Middleware/Attributes/AuthorizeAttribute.cs`, `Iam/Infrastructure/Pipeline/Middleware/Components/RequestAuthorizationMiddleware.cs`, `Iam/Application/Internal/OutboundServices/ITokenService.cs`, `Iam/Infrastructure/Tokens/Jwt/Services/TokenService.cs`, `Iam/Application/Internal/CommandServices/UserCommandService.cs`, y los 19 controllers: `Iam/Interfaces/Rest/{UsersController,BusinessesController}.cs`, `Products/Interfaces/Rest/{ProductsController,WarehousesController,InventoriesController,BatchesController,StockMovementsController}.cs`, `Sales/Interfaces/Rest/{SalesController,CustomersController,SaleDetailsController,PaymentPlansController}.cs`, `Suppliers/Interfaces/Rest/{SuppliersController,PurchasesController,PurchaseDetailsController}.cs`, `Alerts/Interfaces/Rest/{AlertsController,AlertRulesController}.cs`, `Dashboard/Interfaces/Rest/{DashboardController,ReportsController}.cs`.

*Depende de*: ninguna de las anteriores estrictamente, pero conviene hacerla después de B1-B3 para tener claro sobre qué acciones aplican los permisos.

### Fase B5 — Reportes PDF con filtros combinables

- [x] `feature/reports-pdf-export` — **hecho (2026-08-10)**:
  - **Diseño**: se reutilizó toda la infraestructura de `Report`/`GenerateReport`/historial ya existente (persistía metadata de reporte, recalcula en vivo al exportar) en vez de armar un mecanismo paralelo — se agregó un tercer `ReportType.StockMovements` ("entradas/salidas") junto a los ya existentes `SALES`/`INVENTORY`. `Report` ahora tiene `ProductId`/`SupplierId` opcionales (columnas planas, sin FK — un reporte histórico debe seguir siendo válido aunque el producto/proveedor que filtró se borre después). Nuevo endpoint dedicado `GET /reports/{id}/export/pdf` junto al `/export` (CSV) que ya existía.
  - **Bug de dispatch corregido de paso**: el switch de `ExportReportAsCsv` original era `Type == Inventory ? Inventory : Sales` — con el tipo nuevo, cualquier reporte `STOCK_MOVEMENTS` hubiera caído silenciosamente en la rama de Sales. Se cambió a un `switch` explícito con los 3 tipos.
  - **Los 3 filtros combinables**: rango de fechas y producto se resuelven en `IProductContextFacade.GetStockMovementsForReport` (nuevo método, reutiliza `StockMovementRepository` con un nuevo `FindFilteredByBusinessIdAsync`). El proveedor es más particular: `StockMovement.Supplier` es un campo de texto libre (no hay FK real a `Supplier` en este movimiento — se llena con el nombre al recibir una orden de compra), así que el filtro por `SupplierId` se resuelve vía `ISupplierContextFacade.GetSupplierName` (nuevo método) y se hace matching por nombre. Documentado explícitamente en el código para que no se lea como un descuido.
  - **PDF real vía QuestPDF**: `StockMovementsPdfGenerator` (nuevo, en `Dashboard/Infrastructure/Reporting`) — tabla con fecha/producto/tipo/cantidad/proveedor/nota, encabezado con resumen de los filtros aplicados, paginación. Se agregó `QuestPDF.Settings.License = LicenseType.Community` en `Program.cs` (requerido por la librería antes de generar cualquier documento, si no tira excepción al primer uso). PDF solo soportado para `STOCK_MOVEMENTS` — pedir PDF de un reporte `SALES`/`INVENTORY` da 400 `UnsupportedReportTypeForPdf` (esos dos tipos se quedan con CSV, que es lo único que se pidió para ellos).
  - **Probado end-to-end**: reporte sin filtros (exporta todos los movimientos), reporte con `productId` + `supplierId` combinados (CSV y PDF solo muestran la fila que matchea ambos), PDF verificado como documento real (`file` confirma "PDF document, version 1.4"), 400 al pedir PDF de un reporte `SALES`, 404 al intentar exportar el reporte de otro negocio (protegido automáticamente por el filtro global de tenant, sin código adicional).
  - **Archivos tocados**: nuevo — `Dashboard/Infrastructure/Reporting/StockMovementsPdfGenerator.cs`, migración `AddReportFilters`; modificados — `Dashboard/Domain/Model/Entities/Report.cs`, `Dashboard/Domain/Model/Commands/GenerateReportCommand.cs`, `Dashboard/Domain/Model/Errors/DashboardError.cs`, `Dashboard/Application/{CommandServices/ReportCommandService,QueryServices/ReportQueryService,QueryServices/IReportQueryService}.cs`, `Dashboard/Infrastructure/Persistence/EntityFrameworkCore/Configuration/Extensions/ModelBuilderExtensions.cs`, `Dashboard/Interfaces/Rest/ReportsController.cs`, `Dashboard/Interfaces/Rest/Resources/{GenerateReportResource,ReportResource}.cs`, `Dashboard/Interfaces/Rest/Transform/{DashboardActionResultAssembler,GenerateReportCommandFromResourceAssembler,ReportResourceFromEntityAssembler}.cs`, `Dashboard/Resources/DashboardMessages(.es).resx`, `Products/Interfaces/Acl/IProductContextFacade.cs`, `Products/Application/Acl/ProductContextFacade.cs`, `Products/Domain/Repositories/IStockMovementRepository.cs`, `Products/Infrastructure/Persistence/EntityFrameworkCore/Repositories/StockMovementRepository.cs`, `Suppliers/Interfaces/Acl/ISupplierContextFacade.cs`, `Suppliers/Application/Acl/SupplierContextFacade.cs`, `Program.cs`.

*Depende de*: Fase B1 (datos de producto/lote) y de Suppliers ya portado (sin cambios adicionales necesarios ahí).

### Fase B6 — Limpieza y endurecimiento backend

- [x] `chore/backend-hardening` — **hecho (2026-08-10)**:
  - **FluentValidation en los 5 comandos nuevos de B1-B5** (antes tenían checks manuales sueltos con `if` dentro del command service, o directamente no validaban nada):
    - `CreateOrUpdateBatchCommand` (B1): `PurchasePrice >= 0` (nuevo, no existía antes) → `ProductError.InvalidPurchasePrice`.
    - `CreateOrUpdateAlertRuleCommand` (B2): `AlertType` no vacío (nuevo) + `ThresholdValue >= 0` (migrado del `if` manual que ya existía).
    - `CreateAlertCommand` (B2, creación manual/técnica): `ProductName`/`Type`/`Severity`/`Message` no vacíos, `CurrentStock`/`MinStock >= 0` — antes no validaba absolutamente nada.
    - `CreatePaymentPlanCommand` (B3): `TotalInstallments >= 1` (migrado del `if` manual).
    - `GenerateReportCommand` (B5): `Type` no vacío (nuevo) + `DateFrom <= DateTo` cuando ambas están presentes (**gap real que no existía** — se podía generar un reporte con rango de fechas invertido sin error).
  - **Logging estructurado en los 3 flujos clave**, más allá de la línea genérica por-request que ya existía desde B0.5:
    - **Venta**: `SaleCommandService` ahora loggea la venta registrada (id, negocio, líneas, total, moneda, método de pago) y la cancelación. También se corrigió un vacío real: el `catch (Exception)` alrededor de la transacción de venta no loggeaba nada — un fallo inesperado quedaba sin rastro más allá del 500 genérico al cliente. Ahora usa `logger.LogError` con la excepción completa.
    - **Alerta**: los dos event handlers reactivos (`StockLevelChangedEventHandler`, `BatchRegisteredEventHandler`) y el `AlertExpirationSweepJob` loggean cuando se crea una alerta **nueva** (no en cada refresh de una ya activa, mismo criterio que la notificación de B2).
    - **Exportación**: `ReportQueryService` loggea cada exportación (CSV y PDF) con el id/tipo de reporte, negocio, y para PDF además cantidad de líneas y tamaño en bytes.
  - **Tests de integración**: se dejaron pendientes a propósito — no existe ningún proyecto de tests en la solución todavía (ni xUnit, ni WebApplicationFactory, ni una estrategia de base de datos para tests), y el plan los marca como "opcionalmente". Armar esa infraestructura desde cero es una tarea con entidad propia, no algo para sumar de paso a esta fase ya de por sí extensa.
  - **Probado end-to-end**: precio de compra negativo en lote (400), 0 cuotas en plan de pago (400), tipo de alerta vacío en regla (400), alerta manual con mensaje vacío (400), reporte con `dateFrom` posterior a `dateTo` (400), reporte válido sigue funcionando (200). Confirmado en el log real: `"Sale 3 registered for business 1: 1 line(s), total 5.5 PEN, payment method CASH"` y `"Report 5 (SALES) exported as CSV for business 1"`.
  - **Archivos tocados**: nuevos — `Products/Domain/Model/Commands/Validation/CreateOrUpdateBatchCommandValidator.cs`, `Sales/Domain/Model/Commands/Validation/CreatePaymentPlanCommandValidator.cs`, `Alerts/Domain/Model/Commands/Validation/{CreateOrUpdateAlertRuleCommandValidator,CreateAlertCommandValidator}.cs`, `Dashboard/Domain/Model/Commands/Validation/GenerateReportCommandValidator.cs`; modificados — `Products/{Application/Internal/CommandServices/InventoryCommandService,Domain/Model/Errors/ProductError,Interfaces/Rest/Transform/ProductActionResultAssembler,Resources/ProductMessages(.es)}.cs/.resx`, `Sales/Application/Internal/CommandServices/{PaymentPlanCommandService,SaleCommandService}.cs`, `Alerts/{Application/Internal/CommandServices/AlertCommandService,Application/Internal/CommandServices/AlertRuleCommandService,Application/Internal/EventHandlers/BatchRegisteredEventHandler,Application/Internal/EventHandlers/StockLevelChangedEventHandler,Infrastructure/Pipeline/BackgroundServices/AlertExpirationSweepJob,Domain/Model/Errors/AlertsError,Interfaces/Rest/Transform/AlertsActionResultAssembler,Resources/AlertsMessages(.es)}.cs/.resx`, `Dashboard/{Application/Internal/CommandServices/ReportCommandService,Application/Internal/QueryServices/ReportQueryService,Domain/Model/Errors/DashboardError,Interfaces/Rest/Transform/DashboardActionResultAssembler,Resources/DashboardMessages(.es)}.cs/.resx`.

*Depende de*: B1-B5 completas.

---

## 3. Fases — Frontend (`bodega-webapp`)

### Fase F0 — Base y autenticación

- [ ] `feature/frontend-auth-shell` — portar layout (sidebar responsive), routing, guard de autenticación, i18n base, y las vistas de sign-in/sign-up conectadas al backend nuevo. Verificar CORS contra `bodega-platform`.

*Depende de*: Fase B0 (backend con datos reales para probar login), Fase B0.5 (para no construir el flujo de auth del frontend sobre una base sin endurecer).

### Fase F0.5 — Seguridad base del frontend

Objetivo: decidir la estrategia de manejo de sesión **antes** de portar más pantallas, porque cambiarla después implica tocar todo lo ya construido.

- [ ] `feature/frontend-security-baseline` — cubre, como mínimo:
  - **Almacenamiento del token de sesión**: el proyecto original guardaba el JWT en `localStorage`, accesible desde cualquier script — si hay una vulnerabilidad XSS, el token se puede robar directamente. Evaluar migrar a una cookie `httpOnly` + `Secure` + `SameSite=Strict/Lax` gestionada por el backend (el JS del navegador nunca llega a tocar el token). Si se mantiene `localStorage` por simplicidad, dejarlo documentado como decisión consciente, no como default heredado sin revisar.
  - **CSRF**: si se migra a cookies, agregar protección CSRF correspondiente (con `SameSite` bien configurado suele bastar para una SPA + API separada, pero hay que validarlo).
  - **XSS**: evitar `v-html` con contenido que no sea estrictamente confiable; Vue ya escapa por defecto en templates normales, mantenerlo así.
  - **Validación en cliente + revalidación en servidor**: cualquier validación de formulario en el frontend es solo UX — la validación real (Fase B0.5/FluentValidation) vive en el backend.
  - **Dependencias**: `npm audit` como parte del checklist recurrente, igual que `dotnet list package --vulnerable` en el backend.
  - Confirmar que no haya secretos ni claves hardcodeadas en el bundle del frontend (los `.env` actuales solo tienen URLs públicas — mantenerlo así).

*Depende de*: Fase F0, Fase B0.5.

### Fase F1 — Productos, inventario y lector de código de barras

- [ ] `feature/frontend-products-inventory` — portar vista de productos/inventario/movimientos de stock.
- [ ] `feature/frontend-barcode-scanner` — construir el input de escaneo (input enfocado, listener `keydown`, detección de Enter/CR según la config del YHD-1100CB), comparación contra la base de productos, mensaje de confirmación si el código ya existe, flujo de alta si es un código nuevo.

*Depende de*: Fase F0, Fase B1 (backend de lotes/vencimiento ya ajustado).

### Fase F2 — Ventas / POS y cuotas

- [ ] `feature/frontend-sales-pos` — portar pantalla POS, carrito, historial de ventas y clientes.
- [ ] `feature/frontend-credit-installments-ui` — UI para registrar/consultar cuotas de una venta a crédito (consume Fase B3).

*Depende de*: Fase F1 (el POS necesita productos), Fase B3.

### Fase F3 — Proveedores y compras

- [ ] `feature/frontend-suppliers` — portar listado de proveedores y órdenes de compra, **sin** nada de tracking/delivery (ya excluido en el backend).

*Depende de*: Fase F0.

### Fase F4 — Alertas

- [ ] `feature/frontend-alerts` — portar el dashboard de alertas activas + reglas, conectado al motor mejorado de Fase B2.

*Depende de*: Fase B2, Fase F0.

### Fase F5 — Dashboard, reportes y exportación PDF

- [ ] `feature/frontend-dashboard-reports` — portar KPIs y filtros de reporte, agregar la UI para disparar la exportación PDF con los 3 filtros combinables (consume Fase B5).

*Depende de*: Fase B5, Fase F0.

### Fase F6 — Roles y permisos en la UI

- [ ] `feature/frontend-roles-ui` — ocultar/deshabilitar acciones según el rol del usuario autenticado (consume Fase B4).

*Depende de*: Fase B4, y haber portado ya las pantallas relevantes (F1-F5).

### Fase F7 — Rediseño visual / UX

- [ ] Rama(s) a definir según el alcance que se acuerde en su momento. Este es el punto donde retomamos explícitamente la conversación de diseño que quedó pospuesta hasta tener el dominio backend avanzado.

*Depende de*: F0-F6 completas (para tener todas las pantallas reales sobre las cuales decidir el rediseño, en vez de diseñar en el vacío).

---

## 4. Fases finales — transversales

### Fase X1 — QA responsive

- [ ] Probar cada pantalla portada (F0-F6) en viewport mobile y laptop; ajustar breakpoints de PrimeFlex donde falle.

### Fase X2 — Rebranding (cuando se tenga el nombre real de la bodega)

- [ ] `chore/rebrand-backend` — renombrar namespace `Bodega.Platform` → nombre final, nombre de base de datos, política CORS.
- [ ] `chore/rebrand-frontend` — `package.json`, textos de marca, logo nuevo (el actual `qullqa_logo.jpeg` todavía tiene "QULLQA" incrustado en la imagen).
- [ ] Opcional: renombrar los repos en GitHub (`FerG17/bodega-platform` → nombre final) — GitHub deja el redirect automático desde el nombre viejo.

### Fase X3 — Auditoría de seguridad final y cierre

- [ ] `chore/security-audit` — barrido final antes de considerar el proyecto "en producción": escaneo de dependencias vulnerables (`dotnet list package --vulnerable`, `npm audit`), revisión de que ningún secreto quede hardcodeado o loggeado, checklist OWASP-lite aplicado a los flujos críticos (login, ventas, cuotas, alertas), confirmación de backups de base de datos configurados.
- [ ] Confirmar que el branch protection sigue activo en ambos repos nuevos.
- [ ] Decidir estrategia de despliegue/hosting (fuera de alcance de este plan hasta que se defina) y confirmar que queda con HTTPS/HSTS activo.

---

## 5. Orden de ejecución sugerido (checklist único)

1. Fase B0 — infraestructura backend
2. Fase B0.5 — seguridad base backend
3. Fase B1 — Products / lotes / vencimiento
4. Fase B2 — Alerts
5. Fase B3 — Sales / cuotas
6. Fase B4 — Roles y permisos (backend)
7. Fase B5 — Reportes PDF
8. Fase B6 — Endurecimiento backend
9. Fase F0 — Frontend: base y auth
10. Fase F0.5 — seguridad base frontend
11. Fase F1 — Frontend: productos + lector de código de barras
12. Fase F2 — Frontend: ventas/POS + cuotas
13. Fase F3 — Frontend: proveedores
14. Fase F4 — Frontend: alertas
15. Fase F5 — Frontend: dashboard + PDF
16. Fase F6 — Frontend: roles en UI
17. Fase F7 — Rediseño visual
18. Fase X1 — QA responsive
19. Fase X2 — Rebranding (cuando se sepa el nombre)
20. Fase X3 — Auditoría de seguridad final y cierre

Este orden es una guía, no una camisa de fuerza: si en el camino conviene adelantar o reordenar una tarea puntual (por ejemplo, resolver algo de UI menor mientras se prueba un endpoint), no pasa nada — la idea es tener siempre claro qué sigue y no perder de vista las dependencias reales (por ejemplo, no tiene sentido hacer F1 antes que B1, porque el frontend de lotes necesita que el backend de lotes ya esté ajustado).
