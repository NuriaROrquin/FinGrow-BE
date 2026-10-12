# FinGrow · Backend

API REST en .NET 10 con Clean Architecture y PostgreSQL. Es uno de tres repos: FinGrow-FE
(Next.js), **FinGrow-BE** (este) y FinGrow-AI (Python, OCR y lenguaje natural). El backend
consume la API de IA por HTTP; el frontend nunca la llama directo.

## Comandos

```bash
dotnet build FinGrow.sln
dotnet test FinGrow.sln
docker compose up -d db          # PostgreSQL 16 en localhost:5432
dotnet ef migrations add <Nombre> --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api
```

La herramienta `dotnet-ef` tiene que ser 10.x: una versión anterior a la de EF Core no puede
leer el modelo.

## Dónde está el modelo de datos

| Documento | Qué contiene |
|---|---|
| [docs/database-schema.md](docs/database-schema.md) | **El esquema que existe hoy**: 7 tablas, sus restricciones y su comportamiento al borrar |
| [docs/database-schema-target.md](docs/database-schema-target.md) | El DER completo de las 65 historias: 31 tablas en 7 módulos, y las divergencias con lo construido |
| [docs/backlog-huecos.md](docs/backlog-huecos.md) | Huecos del backlog detectados al modelar |

**Regla: toda migración nueva actualiza `docs/database-schema.md` en el mismo commit.** La
fuente de verdad es la migración; el documento es una foto que se desactualiza sola si nadie la
mueve, y un DER que miente es peor que no tener ninguno.

## Reglas del dominio que se violan sin querer

Estas son las que no se deducen leyendo el código, y por las que conviene preguntar antes de
"arreglar" algo que parece faltar.

**Un total no se persiste: se deriva de sus partes.** El gastado de un presupuesto se calcula
sumando las transacciones del período, el acumulado de una meta es la suma de
`goal_contributions` y el valor actual de una inversión es la última fila de
`investment_valuations`; ninguno existe como columna. La tentación de agregar un `spent` o un
`current_amount` para "evitar el cálculo" es exactamente el bug: un total guardado y sus partes
se desincronizan en cuanto alguien edita o borra una parte, y después nadie sabe cuál de los dos
números es el bueno. Por eso las colecciones hijas (`Contributions`, `Valuations`, `Limits`)
se cargan siempre con su agregado (`AutoInclude`).

**`ExpenseCategory` es un contrato con FinGrow-AI.** Los diez valores viven también en
`FinGrow-AI/app/domain/enums.py` y se guardan en la base con el texto que usa la IA
(`ahorro_inversion`, no `AhorroInversion`). Hay un test que rompe el build si las dos listas se
separan: si falla, no lo ajustes sin cambiar los dos lados.
Desde HU-13 pasa lo mismo con `IncomeCategory` (`IncomeCategoryTests`): la IA también clasifica
ingresos al interpretar un mensaje del bot.

**Un importe nunca viaja solo.** `Money` es un value object de monto + moneda que se guarda como
dos columnas. Combinar importes de monedas distintas lanza `DomainException`, a propósito.

**Un movimiento propuesto no cuenta hasta que lo confirman.** Lo que entra por Gmail, por el bot
o por el OCR nace con `status = Pending`. Saldo, presupuestos y reportes miran solo los
confirmados. El empleado lo revisa en la bandeja (`GET /api/transactions/pending`) y lo
confirma con sus correcciones (`POST .../{id}/confirm`) o lo descarta (`POST .../{id}/discard`).
Descartar no borra la fila: queda en `Discarded` porque su `external_reference` es lo que impide
que la integración vuelva a proponerlo; por eso `ListExistingExternalReferencesAsync` no filtra
por estado. `Discarded` es "nunca fue un movimiento"; `Eliminated` es "existió y lo borraron".

**Las reglas críticas están además en la base.** Hay `CHECK` que rechazan un gasto con categoría
de ingreso, un importe cero y una meta con objetivo y progreso en monedas distintas. La
duplicación con el dominio es deliberada: el código protege del bug, el `CHECK` protege de la
carga masiva y del `UPDATE` hecho a mano.

## Arquitectura

Cuatro capas con las dependencias hacia adentro: `Api` → `Application` → `Domain`, e
`Infrastructure` → `Application`. `FinGrow.Domain` no referencia ningún paquete NuGet ni ningún
otro proyecto: es lo que mantiene las reglas de negocio independientes de la base y del
transporte.

`tests/FinGrow.ArchitectureTests` verifica esas reglas en cada build y falla si alguien las
rompe. Si uno de esos tests falla, el problema es la dependencia nueva, no el test.

## Patrón de handlers en Application

`AddApplication()` (`src/FinGrow.Application/DependencyInjection.cs`) escanea su propio
ensamblado y registra automáticamente validadores de FluentValidation y handlers de MediatR
(fijado en 12.5.0 — la última versión con licencia libre; desde la 13.x es de pago). Un feature
nuevo bajo `Application/Features/<Feature>/<Caso>/` queda disponible por inyección de
dependencias sin tocar `Program.cs` ni `AddApplication()`.

- **Los validadores tienen que ser `public`.** `AddValidatorsFromAssembly` no incluye tipos
  `internal` por defecto: un validador `internal` compila sin error pero nunca se ejecuta.
- **Los handlers tienen que ser `internal` y vivir bajo `Application.Features`.** Dos reglas en
  `tests/FinGrow.ArchitectureTests/NamingRules.cs` lo verifican y rompen el build si no se
  cumple.
- **La validación corta antes de llegar al handler.** `ValidationBehavior`
  (`Application/Common/Behaviors/`) es un `IPipelineBehavior` de MediatR que, si FluentValidation
  encuentra errores, devuelve `Result.Failure`/`Result<T>.Failure` con `ErrorType.Validation` sin
  invocar al handler.
- **El mapeo a HTTP vive en Api, no en Application**, para no acoplar Application a ASP.NET
  Core. `ResultExtensions.ToActionResult()` (`Api/Extensions/`) traduce `Error.Type` a status
  code: `Validation`→400, `NotFound`→404, `Conflict`→409, `Forbidden`→403, `Unavailable`→503
  (un servicio externo que no responde, como DolarApi), cualquier otro (`Failure`)→500. Un controller nuevo solo necesita
  `return (await sender.Send(command, ct)).ToActionResult();`.
- Esto es distinto de `ExceptionHandlingMiddleware`: ese middleware sigue cubriendo únicamente
  lo inesperado (excepciones no manejadas); `Result.Failure` es el camino para fallos de negocio
  esperables y nunca debería llegar como excepción.

El primer feature real es `Application/Features/Integrations/WhatsApp` (T-25): sirve de
referencia de cómo queda un caso de uso completo, con repositorios en `Domain/Repositories`
implementados en `Infrastructure/Persistence/Repositories`. Lo que comparten los canales de
chat vive fuera de la carpeta del proveedor: `Integrations/GenerateLinkCode`, `GetIntegration`
y `UnlinkIntegration` reciben el `IntegrationProvider` como parámetro (la ruta es
`api/integrations/{provider}`), y `Integrations/Linking/LinkCodeRedeemer` canjea el código
para cualquiera de los dos webhooks. Un handler nuevo por proveedor solo debería contener lo
que es distinto de ese proveedor. `tests/FinGrow.Application.UnitTests`
prueba los handlers con fakes en memoria (`Fakes/InMemoryFakes.cs`) y `tests/FinGrow.Api.UnitTests`
el borde HTTP reemplazando los repositorios en `ConfigureTestServices`.

**Los webhooks son públicos y se protegen con el secreto de cada proveedor**, no con JWT.
`ValidateTwilioSignatureAttribute` (`Api/Twilio/`) y `ValidateTelegramSecretAttribute`
(`Api/Telegram/`) cortan antes del handler; los controllers no tienen lógica: parsear la
entrada es `TwilioInboundMessage` / `TelegramInboundMessage` y el formato de transporte
(`whatsapp:+549…`, el JSON del update) se queda en `Api`, Application recibe datos limpios.
WhatsApp responde inline con `TwiMlResult`; Telegram responde `200` y manda el texto por
`ITelegramBotClient`, y si esa llamada falla el handler lo loguea sin fallar el request, porque
Telegram reintenta cualquier respuesta que no sea 2xx y un código ya canjeado se rechazaría en
el reintento. Una cuenta que no está en `employee_integrations` solo puede mandar su código de
vinculación.

**Registrar movimientos desde un chat (HU-13)** vive en `Integrations/ChatTransactions`, que no
sabe de qué canal viene el texto: `ChatTransactionProposer` le pide a FinGrow-AI que lo
interprete y crea el movimiento como `Pending` (con `external_reference` = canal + id del
mensaje, para que un reintento del webhook no lo duplique), y `ChatTransactionReviewer` lo
confirma o descarta. Telegram lo muestra con botones (`TelegramTransactionCallback` arma y lee el
`callback_data`, limitado a 64 bytes) y `AnswerTelegramCallback` procesa el toque. WhatsApp
(HU-14) no tiene botones en TwiML: `WhatsAppProposalReviewer` aplica un SÍ, un NO o el número
del medio de pago (`WhatsAppReviewReply`) a la última propuesta de WhatsApp pendiente de las
últimas 24 horas, y cualquier otro texto es un movimiento nuevo. Las notas de voz de los dos
canales (HU-14 y HU-71) las transcribe `ChatVoiceNoteTranscriber`; cada canal le pasa cómo
bajar el audio (Twilio o `getFile` del Bot API). Todo el procesamiento de un mensaje de WhatsApp
corre con un tope de 12 segundos (`ProcessingBudget`) porque la respuesta va en el cuerpo del webhook
y Twilio corta a los 15: no lo subas ni lo saques sin mirar ese límite.

## Aislamiento de datos (T-10)

**Entre empresas, lo garantiza la base de datos de EF**: `FinGrowDbContext.Tenancy.cs` registra un
filtro global con nombre (`Tenant`) en cada entidad, acotado al `ICurrentUser.CompanyId`. Lo que
cuelga de un empleado (movimientos, metas, etc.) lo hereda preguntando si el dueño es visible.
Una entidad nueva tiene que aplicar el filtro o figurar en `GlobalEntityTypes`
(`TenantIsolationRules` rompe el build si no).

- **Sin sesión no hay empresa**: webhooks, trabajos, login y seeders corren sin filtro. Con sesión
  siempre aplica: un token sin `company_id` no ve ninguna fila.
- **Lo que cruza empresas a propósito** usa `IgnoreQueryFilters(new[] { FinGrowDbContext.TenantFilter })`
  y lo justifica con un comentario (el email único de `GetByEmailAsync`, el promedio de votos de un curso).
- **Un recurso de otra empresa responde `NotFound`**: el filtro hace que ni se vea.

**Entre empleados de la misma empresa el filtro no alcanza**: cada caso de uso compara el dueño con
`ICurrentUser.UserId`. `Ownership.Require` (`Application/Common/`) resuelve el par: inexistente →
`NotFound`, de otro empleado → `Forbidden` (403). Usalo en los handlers que cargan un recurso por id.

## Panel de la empresa

`api/company/departments` y `api/company/employees` (`[Authorize(Roles = Rol.Empresa)]`)
administran la plantilla de la empresa del token. El filtro global de T-10 ya acota las consultas a
esa empresa, pero **cada handler sigue filtrando por `ICurrentUser.CompanyId`** como segunda barrera y responde `NotFound` (no `Forbidden`)
ante un recurso de otra empresa, para no confirmar que existe.

- **El alta de un empleado devuelve una contraseña temporal una sola vez**
  (`CreatedEmployeeResponse`); en la base queda solo el hash. Además se le manda por correo
  (`WelcomeEmail`), encolado en el mismo `SaveChanges` que el empleado.
- **El email de un empleado es único en toda la plataforma**, no por empresa: es su credencial
  de login.
- **Eliminar un empleado es definitivo.** La cascada se lleva sus movimientos, metas,
  presupuestos, inversiones e integraciones; el handler borra además sus notificaciones (no
  tienen FK porque el destinatario es polimórfico) y los archivos de sus comprobantes. Si el
  almacenamiento no responde, la baja se mantiene y se loguea. Para cortar el acceso
  conservando los datos está la desactivación (HU-55).
- **Desactivar corta el acceso sin borrar nada** (`PUT .../{id}/access`): revoca los refresh
  tokens y el login responde `Auth.AccesoDesactivado` (403), recién después de verificar la
  contraseña para no revelar qué cuentas existen. El access token emitido sigue valiendo hasta
  que expira, igual que al cambiar la contraseña.
- **El departamento de un empleado tiene historial** (`employee_department_assignments`, hija
  del agregado `Employee`): `AssignToDepartment` cierra la asignación vigente y abre otra, y un
  índice único parcial impide dos abiertas a la vez. `metrics-snapshot` toma el departamento que
  el empleado tenía al cierre de cada mes, así que rehacer la foto de un mes cerrado no mueve a
  nadie de departamento hacia atrás. No leas `Employee.DepartmentId` para un período pasado.
- **Un departamento se desactiva, no se borra, cuando hay historia** (`PUT .../{id}/status`,
  HU-53). Al desactivarlo sus empleados salen en la misma operación hacia `moveEmployeesTo`
  (otro departamento activo) o quedan sin departamento: nunca queda gente en uno inactivo, y
  el alta y el cambio de departamento rechazan los inactivos. `DeactivatedAt` es lo que permite
  que `metrics-snapshot` lo siga fotografiando en los meses en que estuvo activo
  (`Department.WasActiveDuring`); reactivarlo lo limpia.
- **El responsable de un departamento es un empleado** (`departments.manager_id`, HU-52), no un
  texto. Al asignarlo tiene que ser un empleado activo de la misma empresa
  (`DepartmentManagerEligibility`); si después pierde el acceso sigue figurando como responsable
  hasta que lo cambien, y si lo eliminan la FK lo deja en `NULL`. Como empleados y departamentos
  se apuntan entre sí, un alta de los dos a la vez necesita dos `SaveChanges` (ver el seeder).
  Las respuestas de departamentos se arman en `DepartmentResponseBuilder`, que trae el nombre
  del responsable y la cantidad de empleados.

## Contenido educativo e inversiones

Artículos y cursos se vinculan a un tipo de activo con `RelatedInvestmentType`, y
`/api/articles` y `/api/courses` filtran por él (`?investmentType=Cedear`, HU-35): así la
pantalla de inversiones trae lo que corresponde a cada activo. `EducationCatalogTests` rompe
el build si un `InvestmentType` nuevo queda sin contenido relacionado. Qué cuenta como
contenido de inversiones lo decide el dominio (`IsInvestmentContent`: categoría `Investments` o
un tipo de activo vinculado) y viaja en la respuesta. El front muestra la aclaración de que es
material educativo y no asesoramiento financiero certificado: no la saques ni dupliques el
criterio en otro lado.

## Archivos subidos

Un archivo nunca se guarda en PostgreSQL: va a `IFileStorage` (`Infrastructure/Storage/S3FileStorage`,
protocolo S3: Cloudflare R2 desplegado, S3Mock en compose) y la base guarda solo la clave. Los
comprobantes son `TransactionReceipt` (`Application/Features/Receipts`): nacen sin movimiento
para que un OCR fallido no pierda la imagen, y la clave la arma el dominio a partir del empleado
y del id del comprobante. Lo que sume otro tipo de archivo reutiliza `IFileStorage`. Las notas
de voz de WhatsApp y Telegram (HU-14 y HU-71) no se guardan, por decisión de producto: se
transcriben y se descartan. Un almacenamiento que no responde llega como
`FileStorageUnavailableException`
y el caso de uso lo traduce a `Unavailable` (503). Los handlers la atrapan; no dejes que
escape como 500. Un movimiento tiene un solo comprobante, y lo que deja de usar uno lo suelta
con `Detach()`, no lo borra: el borrado real es exclusivo de `receipts-cleanup`
(`Application/Features/Receipts/Cleanup/`), que borra los sueltos pasado el período de gracia
(`ReceiptsCleanup:GraceDays`, medido desde `UploadedAt`). Primero borra el archivo y después la
fila, y guarda todas las filas juntas al final: si se corta a mitad de camino, quedan filas cuyo
archivo ya no existe, y la corrida siguiente las limpia porque borrar un archivo inexistente no
falla. Al revés quedaría un archivo sin fila que nadie volvería a encontrar.

## Trabajos programados

No hay scheduler dentro del proceso. Los trabajos implementan `IScheduledJob`
(`Application/Jobs/`), se registran en `AddApplication()` y los dispara desde afuera el cron de
Dokploy llamando a `POST /api/jobs/{nombre}/run` con la cabecera `X-Jobs-Key`
(`ValidateJobsKeyAttribute`, `Api/Jobs/`; la clave es `Jobs:ApiKey` y sin ella la API no
arranca). `JobRunner` persiste cada corrida en `job_runs`, ejecuta el trabajo en un scope de DI
propio (DbContext aparte, así lo que un trabajo dejó a medias no se guarda junto con el registro
de la corrida) y no deja escapar excepciones: quedan como corrida fallida con el error y la
siguiente corre igual. Un mismo trabajo no corre dos veces a la vez (`RunningJobs` responde
`Conflict`). El detalle operativo (crons, comando de Dokploy) está en el README.

- **Un trabajo nuevo** es una clase `internal` bajo `Application/Features/<Feature>/` que
  implementa `IScheduledJob`, más una línea en `AddApplication()` y su schedule en Dokploy.
  `Name` va en kebab-case y tiene que ser único; `Schedule` es el cron sugerido en UTC que se
  copia a Dokploy: acá es documentación, no lo ejecuta nadie.
- **Devolvé `JobResult`, no excepciones, para los desenlaces esperables.**
  `JobResult.Failure(error, resumen)` es para cuando parte de la corrida no salió aunque el
  resto sí (`mercadopago-sync` lo hace por cuenta).
- **Lo que recorre muchos empleados abre un scope por unidad** (`IServiceScopeFactory`), como
  `MercadoPagoSyncJob`: si a uno le falla `SaveChanges`, el DbContext queda con cambios rotos y
  arrastraría a los que siguen.
- **`metrics-snapshot` es idempotente y se autocompleta**: siempre rehace la foto del último mes
  cerrado (por si se confirmaron movimientos tarde) y genera las de los meses anteriores que
  falten desde el alta de la empresa. Las fotos guardan conteos, no porcentajes: la tasa de
  participación se deriva (`PeriodMetrics.ParticipationRate`), fiel a "un total no se persiste".

## Panel empresarial y anonimización

La empresa nunca ve datos de una persona identificable. La anonimización se garantiza en
Application, antes de serializar la respuesta, y no escondiendo columnas en el frontend.

- **Las métricas empresariales siempre son agregadas.** Los endpoints
  `GET /api/company/metrics` y `GET /api/company/departments/{departmentId}/metrics` leen
  `CompanyMetricsSnapshot` o `DepartmentMetricsSnapshot`; no consultan ni proyectan empleados,
  movimientos, metas o inversiones individuales.
- **El umbral mínimo es configurable por alcance y por defecto es de tres empleados activos.**
  `MetricsAnonymizationPolicy` (`Application/Features/Metrics/`) lee
  `Metrics:CompanyMinimumEmployees` para métricas de empresa y
  `Metrics:DepartmentMinimumEmployees` para métricas de departamento. Se configuran por
  variables de entorno como `Metrics__CompanyMinimumEmployees` y
  `Metrics__DepartmentMinimumEmployees`. En Docker Compose se pasan desde
  `METRICS_COMPANY_MINIMUM_EMPLOYEES` y `METRICS_DEPARTMENT_MINIMUM_EMPLOYEES`, ambos con
  valor predeterminado 3. Cada handler rechaza la métrica si el período tiene menos empleados
  que su umbral. No devuelvas el dato parcialmente ni completes el resultado con ceros para
  ocultarlo.
- **Las respuestas usan DTOs exclusivos del panel.** `CompanyMetricsResponse` y
  `DepartmentMetricsResponse` solo pueden contener períodos, conteos, tasas y promedios
  agregados. No agregues `EmployeeId`, `UserId`, nombres, emails, `TransactionId`, `GoalId`,
  `InvestmentId` ni colecciones de entidades personales.
- **El mapeo al DTO es explícito.** No serialices snapshots, entidades ni `PeriodMetrics`
  directamente desde un controller o handler. Si se agrega un campo al modelo, debe evaluarse
  expresamente si puede formar parte del contrato anonimizado.
- **El alcance de empresa se verifica siempre.** El handler usa el `CompanyId` de
  `ICurrentUser`; el acceso a un departamento de otra empresa se trata como inexistente y no
  confirma que el recurso existe.
- **Toda modificación del contrato debe tener una prueba.** Los tests deben comprobar el umbral
  de tres empleados, el aislamiento entre empresas y que la respuesta serializada no expone
  identificadores, montos, saldos ni movimientos individuales. Un test arquitectónico debe
  fallar si los DTOs incorporan propiedades identificables.

## Puntaje de salud financiera

La fórmula está en `FinancialHealthFactors` (`Domain/ValueObjects/`) y explicada en el README;
la aplica `FinancialHealthScorer` (`Application/Features/Metrics/FinancialHealth/`) dentro de
`metrics-snapshot`. Lo que no se deduce leyendo:

- **Sin actividad suficiente no hay puntaje, y eso no es un cero** (`HasEnoughActivity`: 10
  movimientos confirmados y algún ingreso en el mes). Un cero es alguien con actividad al que le
  va mal; la ausencia de fila es "no se puede medir". No completes con ceros.
- **El puntaje sí se persiste**, a diferencia de los totales: es el resultado de una fórmula en
  una fecha (`computed_at`), no la suma de partes editables. Factores y puntaje se escriben
  juntos (`Compute`/`Refresh`), nunca uno sin el otro.
- **El empleado nunca ve su puntaje y la empresa solo lo ve agregado.** Las fotos guardan cuántos
  empleados tienen puntaje y la suma, y el promedio se deriva. No expongas
  `financial_health_scores` por empleado en ningún endpoint.
- **`ahorro_inversion` es ahorro, no gasto**, en el factor de ahorro y en el colchón.
- **Un factor que falta no vale cero:** sin presupuesto del mes o sin metas con cuota, su peso se
  reparte entre los demás.
- **Sin MEP no se fotografía el mes** de una empresa con movimientos en dólares: la corrida queda
  fallida y la siguiente genera la foto que falta. No conviertas con una cotización inventada ni
  guardes una foto con parte de la plantilla sin puntuar.

## Correos

Un correo nunca se manda directo desde un handler: se encola con `OutboundEmail.Queue` en
`outbound_emails` dentro del mismo `SaveChanges` que el cambio que lo origina, así una falla de
Resend no deshace ese cambio. El evento `OutboundEmailQueued` dispara el primer intento apenas se
guarda, y `OutboundEmailDispatcher` (`Application/Features/OutboundEmails/`) registra el resultado:
`Sent`, `Failed` con el próximo intento, o `Abandoned` después de `OutboundEmail.MaxAttempts`.
Los reintentos los hace el trabajo `email-delivery`. Un correo recién encolado le pertenece a su
primer intento durante `OutboundEmail.FirstAttemptGrace`: así el trabajo no lo manda a la vez.

- **El cuerpo va cifrado y se borra cuando el correo deja de esperar envío**: puede llevar
  credenciales. Un `CHECK` impide que un correo enviado o abandonado conserve el cuerpo.
- **En `Development` todo correo va a `Email:RedirectAllTo`**, no a su destinatario: lo resuelve
  `ResendEmailSender`, no quien encola.
- **El cliente de Resend no reintenta**: repetir el `POST` cuando Resend ya lo recibió duplica el
  correo. Los reintentos son de `outbound_emails`.

## Eventos de dominio y alertas

Un agregado levanta un evento con `Raise` y `DomainEventsInterceptor` (`Infrastructure/Persistence/`)
lo publica por MediatR, envuelto en `DomainEventEnvelope<T>`, recién después de que
`SaveChanges` confirma el cambio y cada uno en un scope de DI propio. Una reacción que falla se
loguea y no llega a quien guardó, porque su cambio ya está en la base. Como el `DbContext`
necesita `IDomainEventDispatcher`, `AddInfrastructure()` solo arranca junto a `AddApplication()`.

- **Toda alerta pasa por `Notifier`** (`Application/Features/Notifications/`): deduplica por
  criterio y ventana, persiste en `notifications` y entrega por los canales externos que el
  empleado no apagó. No crees un `Notification` a mano ni mandes un Telegram desde un handler.
- **Una alerta nueva** es un valor en `NotificationType` más una clase `internal` bajo
  `Notifications/Alerts/` que implementa `INotificationHandler<DomainEventEnvelope<TEvento>>`.
  El criterio de deduplicación es lo que hace que "la misma condición" no se repita: incluí en
  la clave lo que distingue una alerta de otra (categoría, período, meta).
- **La bandeja de la app no es un canal configurable**: es el historial de las alertas. Hay un
  `CHECK` que impide guardar una preferencia para `InApp`.
- **El tiempo real es el sender de `InApp`.** `SignalRNotificationSender` (`Api/Notifications/`)
  empuja cada notificación ya guardada al hub `/hubs/notifications`; como `InApp` no se puede
  apagar, corre siempre. `NotificationRecipientUserIdProvider` identifica cada conexión como
  `{tipo}:{id}` a partir del rol y el id del token, porque el destinatario es el par y no solo
  el id. El hub no expone métodos al cliente: solo emite `NotificationReceived`.

## Convenciones

- Identificadores en inglés; documentación, comentarios y mensajes de error en castellano. La
  excepción son los enums de categorías, en castellano por ser contrato con FinGrow-AI.
- Los warnings se tratan como errores. Las migraciones están exentas de las reglas de estilo en
  `.editorconfig` porque son código generado.
- Las versiones de paquetes se declaran una sola vez en `Directory.Packages.props`; los `.csproj`
  referencian sin versión.
- Los nombres en la base son snake_case, aplicados de una sola vez en `OnModelCreating`
  (`SnakeCaseNamingExtensions`). No hace falta `HasColumnName` salvo para columnas de value
  objects.
- Reglas de negocio con desenlace esperable devuelven `Result`; las excepciones quedan para
  fallas técnicas.
- **Los tests van íntegramente en inglés**: nombres de métodos (frases separadas por guion
  bajo, ver `[tests/**/*.cs]` en `.editorconfig`, que por eso desactiva `CA1707`), clases,
  fakes, helpers y también las variables locales (`balance`, no `saldo`). Las únicas cosas en
  castellano dentro de un test son los datos de prueba (`"juan@empresa.com"`) y los valores
  del contrato con FinGrow-AI (`"alimentos"`). Los nombres de test no siguen las convenciones
  de nombres de producción.
- Las claves primarias son `Guid.CreateVersion7()`, no `Guid.NewGuid()`: son ordenables por
  tiempo y no fragmentan el índice.
- **Las colecciones de un agregado** siguen el patrón de `Company.Departments`: un campo
  `private readonly List<T> _xxx` inicializado con `= new()`, expuesto como
  `IReadOnlyCollection<T>` y mapeado con `PropertyAccessMode.Field`. La entidad hija tiene
  `internal static Create` para que solo el agregado pueda instanciarla.
- **Sin *collection expressions*** (`[]`, `[a, b]`, `[typeof(X)]`) en ningún lado: `new()`,
  `new[] { … }`, `Array.Empty<T>()`. Compilan, pero Rider (que es lo que usa el equipo) no las
  entiende: marca campos como "never assigned" y deja de resolver los métodos encadenados
  después de una. Los enums con código ISO (`Currency.ARS`) llevan `[SuppressMessage]` para la
  inspección de nombres de Rider, porque el valor se guarda tal cual en la base.
- **Comentarios solo donde el código no alcanza.** Un comentario justifica una decisión que no
  se deduce leyendo (por qué existe una valuación inicial, por qué el índice incluye `period`);
  no narra lo que hace la línea de abajo ni repite lo que ya dice este archivo o `docs/`. Si
  al releer un comentario se puede borrar sin perder información, se borra.

## Trabajo pendiente que afecta al modelo

Queda una divergencia entre lo construido y el DER objetivo: la tabla `users` para
credenciales, 2FA y preferencias (HU-01, HU-04, HU-46, HU-49). Arrastra una migración sobre
tablas ya construidas: revisá [docs/database-schema-target.md](docs/database-schema-target.md)
antes de tomar esas historias.

`ai_confidence`, `ai_model` y el estado `Discarded` de `transactions` ya existen (HU-15). HU-12
reutiliza `ai_confidence` para la confianza del OCR: es un dato del movimiento propuesto, no de
la categoría.
