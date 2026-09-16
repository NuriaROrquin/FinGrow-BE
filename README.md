# FinGrow · Backend

API REST de FinGrow, construida en .NET 10 con Clean Architecture.

Es uno de los tres componentes de la plataforma:

| Repositorio | Stack | Responsabilidad |
|---|---|---|
| [FinGrow-FE](https://github.com/NuriaROrquin/FinGrow-FE) | React · Next.js | Interfaz de usuario |
| **FinGrow-BE** | **.NET 10 · PostgreSQL** | **Reglas de negocio, datos y autenticación** |
| [FinGrow-AI](https://github.com/NuriaROrquin/FinGrow-AI) | Python | OCR de tickets y procesamiento de lenguaje natural |

El backend consume la API de IA por HTTP; el frontend nunca la llama directo.

---

## Requisitos

- [.NET SDK 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (opcional, para levantar PostgreSQL)

## Puesta en marcha

Con Docker, que levanta la base y la API juntas:

```bash
docker compose up --build
```

En local, contra una PostgreSQL ya disponible:

```bash
dotnet run --project src/FinGrow.Api
```

| Recurso | URL |
|---|---|
| API | http://localhost:8080 |
| Swagger UI (solo en Development) | http://localhost:8080/swagger |
| Health check | http://localhost:8080/health |

## Configuración

Los valores por defecto están en `src/FinGrow.Api/appsettings.json` y se pueden pisar con
variables de entorno usando `__` como separador de sección.

| Clave | Variable de entorno | Descripción |
|---|---|---|
| `ConnectionStrings:Database` | `ConnectionStrings__Database` | Cadena de conexión a PostgreSQL |
| `Database:MigrateOnStartup` | `Database__MigrateOnStartup` | Aplica las migraciones pendientes al arrancar (default `true`). Poner en `false` si las migraciones se corren desde un paso de deploy separado |
| `AiService:BaseUrl` | `AiService__BaseUrl` | URL base de FinGrow-AI |
| `AiService:ApiKey` | `AiService__ApiKey` | Secreto compartido con FinGrow-AI; viaja en el header `X-API-Key` y tiene que ser el mismo valor que `API_KEY` en ese servicio |
| `AiService:TimeoutSeconds` | `AiService__TimeoutSeconds` | Timeout de las llamadas a IA (default 30) |
| `Twilio:AccountSid` | `Twilio__AccountSid` | Account SID de la cuenta de Twilio; autentica la descarga de adjuntos |
| `Twilio:AuthToken` | `Twilio__AuthToken` | Auth Token de Twilio; valida la firma de cada webhook y autentica la descarga de adjuntos |
| `Twilio:PublicBaseUrl` | `Twilio__PublicBaseUrl` | URL pública de la API tal como está cargada en Twilio (ej. `https://api.fingrow.app`). Solo hace falta detrás de un proxy o un túnel; en local se deja vacía |
| `Telegram:BotToken` | `Telegram__BotToken` | Token del bot que entrega @BotFather; autentica las respuestas que la API manda por la Bot API |
| `Telegram:WebhookSecret` | `Telegram__WebhookSecret` | Secreto elegido por nosotros al registrar el webhook (1 a 256 caracteres: letras, números, `_` y `-`); Telegram lo devuelve en cada update y la API rechaza con 403 el que no coincida |
| `Telegram:TimeoutSeconds` | `Telegram__TimeoutSeconds` | Timeout de las llamadas a la Bot API (default 30) |
| `MercadoPago:ClientId` | `MercadoPago__ClientId` | Client ID de la aplicación creada en [Mercado Pago Developers](https://www.mercadopago.com.ar/developers/panel/app) |
| `MercadoPago:ClientSecret` | `MercadoPago__ClientSecret` | Client Secret de esa aplicación; autentica el canje y la renovación de tokens OAuth |
| `MercadoPago:RedirectUri` | `MercadoPago__RedirectUri` | URL pública de `GET /api/integrations/mercadopago/oauth/callback`, idéntica a la cargada en la aplicación de Mercado Pago. MP no acepta `localhost`: en local se usa un túnel (ngrok) |
| `MercadoPago:FrontendReturnUrl` | `MercadoPago__FrontendReturnUrl` | Página del frontend a la que vuelve el navegador al terminar la vinculación; recibe `?mercadopago=linked` o `?mercadopago=error&reason=...` (default `http://localhost:3000/dashboard/settings`) |
| `MercadoPago:TimeoutSeconds` | `MercadoPago__TimeoutSeconds` | Timeout de las llamadas a la API de Mercado Pago (default 30) |
| `MercadoPago:SyncIntervalMinutes` | `MercadoPago__SyncIntervalMinutes` | Cada cuántos minutos el job recorre las cuentas vinculadas y trae los movimientos nuevos (default 60). Mercado Pago no avisa por webhook lo que un usuario paga: solo lo que cobra, por eso se consulta |
| `MercadoPago:SyncInitialDelaySeconds` | `MercadoPago__SyncInitialDelaySeconds` | Espera antes de la primera sincronización al arrancar (default 30) |
| `TokenEncryption:Key` | `TokenEncryption__Key` | Clave AES-256 en base64 (`openssl rand -base64 32`) con la que se cifran en la base los tokens OAuth de las integraciones. Cambiarla deja ilegibles los tokens ya guardados |
| `Cors:AllowedOrigins` | `Cors__AllowedOrigins__0` | Orígenes habilitados para el frontend |

Los secretos no se commitean. En desarrollo local:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "<cadena>" --project src/FinGrow.Api
dotnet user-secrets set "Jwt:SecretKey" "<clave de al menos 32 caracteres>" --project src/FinGrow.Api
dotnet user-secrets set "AiService:ApiKey" "<secreto compartido con FinGrow-AI>" --project src/FinGrow.Api
dotnet user-secrets set "Twilio:AccountSid" "<Account SID de Twilio>" --project src/FinGrow.Api
dotnet user-secrets set "Twilio:AuthToken" "<Auth Token de Twilio>" --project src/FinGrow.Api
dotnet user-secrets set "Telegram:BotToken" "<token del bot>" --project src/FinGrow.Api
dotnet user-secrets set "Telegram:WebhookSecret" "<secreto inventado para el webhook>" --project src/FinGrow.Api
```

Si `Jwt:SecretKey`, `AiService:ApiKey` o las credenciales de Twilio o Telegram faltan, la API no arranca y
el log dice cuál es. En
desarrollo `AiService:ApiKey` puede ser cualquier texto: FinGrow-AI con `API_KEY` vacía no lo
valida. En producción los dos servicios tienen que compartir el mismo valor.

### WhatsApp (Twilio)

Los mensajes de WhatsApp entran por `POST /api/webhooks/whatsapp`. El endpoint es público
—lo llama Twilio, no el frontend— y rechaza con 403 cualquier request cuya cabecera
`X-Twilio-Signature` no coincida con el Auth Token configurado.

Para probar con el sandbox de WhatsApp de Twilio (no hace falta número aprobado):

1. En la consola de Twilio, *Messaging → Try it out → Send a WhatsApp message*, mandá desde tu
   teléfono el `join <palabra>` que muestra el sandbox al número `+1 415 523 8886`.
2. En *Sandbox settings*, cargá en **"When a message comes in"** la URL pública de la API más
   `/api/webhooks/whatsapp`, método `POST`. Los demás campos quedan vacíos. En local, `ngrok http
   8080` (o el puerto que uses) te da esa URL; ponela también en `Twilio:PublicBaseUrl` si la
   API no ve el mismo host que llamó Twilio.
3. Copiá el Account SID y el Auth Token de la consola a la configuración (ver arriba).

Un número tiene que vincularse antes de que sus mensajes cuenten: el empleado logueado pide un
código con `POST /api/integrations/whatsapp/link-code`, lo manda por el chat dentro de los 10
minutos y la API le contesta que quedó vinculado. Desde ahí cada mensaje de ese número se
resuelve a ese empleado. Un número sin vincular solo recibe las instrucciones para hacerlo.

### Telegram (Bot API)

Los mensajes de Telegram entran por `POST /api/webhooks/telegram`. También es público y rechaza
con 403 cualquier request cuya cabecera `X-Telegram-Bot-Api-Secret-Token` no coincida con
`Telegram:WebhookSecret`. A diferencia de Twilio, la respuesta no va en el cuerpo del HTTP: la
API contesta `200` enseguida y le escribe al chat llamando a `sendMessage` de la Bot API.

Para probar en local:

1. En Telegram, hablale a **@BotFather**, `/newbot`, elegí nombre y usuario (tiene que terminar
   en `bot`). Copiá el token a `Telegram:BotToken` y el usuario a `NEXT_PUBLIC_TELEGRAM_BOT_USERNAME`
   del frontend.
2. Levantá la API y exponela con un túnel HTTPS: `ngrok http 8080` (o el puerto que uses).
3. Registrá el webhook una sola vez, con el secreto que hayas puesto en `Telegram:WebhookSecret`:

   ```bash
   curl "https://api.telegram.org/bot<token>/setWebhook"      -d "url=https://<subdominio>.ngrok-free.app/api/webhooks/telegram"      -d "secret_token=<secreto>"      -d "allowed_updates=[\"message\"]"
   ```

   Cada vez que ngrok cambie de URL hay que repetir este paso. `getWebhookInfo` en la misma
   URL base muestra el estado y el último error si Telegram no logra entregar.

El flujo de vinculación es el mismo que WhatsApp: el empleado pide un código con
`POST /api/integrations/telegram/link-code` y se lo manda al bot. El frontend arma el link
`https://t.me/<bot>?start=<código>`, así que basta tocar "Abrir" y "Iniciar": Telegram manda
`/start <código>` y el chat queda vinculado. Solo se procesan chats privados; un mensaje en un
grupo se ignora.

---

## Arquitectura

Cuatro capas con las dependencias apuntando siempre hacia adentro: cada capa conoce a las
interiores y nunca a las exteriores.

```
FinGrow.Api  ──►  FinGrow.Application  ──►  FinGrow.Domain
     │                     ▲
     └──► FinGrow.Infrastructure ──┘
```

| Proyecto | Depende de | Contiene |
|---|---|---|
| `FinGrow.Domain` | — | Entidades, value objects, interfaces de repositorio |
| `FinGrow.Application` | Domain | Casos de uso, DTOs, validadores, contratos hacia servicios externos |
| `FinGrow.Infrastructure` | Application | EF Core, PostgreSQL, JWT, cliente de FinGrow-AI, integraciones |
| `FinGrow.Api` | Application · Infrastructure | Controllers, middlewares, configuración de arranque |

`FinGrow.Domain` no referencia ningún paquete NuGet ni ningún otro proyecto. Es la
restricción que mantiene las reglas de negocio independientes de la base de datos y del
transporte.

**La API referencia Infrastructure únicamente para registrar las implementaciones en el
contenedor de dependencias** (`Program.cs` → `AddInfrastructure()`). Los controllers hablan
solo con Application; hay un test que lo verifica.

### Estructura de carpetas

```
src/
├── FinGrow.Domain/
│   ├── Common/             Entity, AggregateRoot, ValueObject
│   ├── Entities/           Company, Department, Employee, Transaction, Budget, Goal, Investment
│   ├── ValueObjects/       Money, Email, TaxId
│   ├── Enums/
│   ├── Errors/
│   └── Repositories/       Interfaces, no implementaciones
├── FinGrow.Application/
│   ├── Common/             Result, Error y el ValidationBehavior de MediatR
│   ├── Interfaces/         IUnitOfWork, ICurrentUser, IAiService, IDateTimeProvider, ITwilio*, ITelegram*
│   ├── Features/           Un subdirectorio por funcionalidad (Integrations/{GenerateLinkCode,GetIntegration,UnlinkIntegration,Linking,WhatsApp,Telegram})
│   ├── DTOs/
│   └── Validators/
├── FinGrow.Infrastructure/
│   ├── Persistence/        DbContext, Configurations, Repositories, Migrations
│   ├── Identity/           Resolución del usuario autenticado
│   ├── Ai/                 Cliente HTTP hacia FinGrow-AI
│   ├── Integrations/       Twilio (firma de webhooks y descarga de adjuntos) y Telegram (secreto del webhook y Bot API); Gmail después
│   └── Services/
└── FinGrow.Api/
    ├── Controllers/
    ├── Twilio/             Filtro de firma, parseo del form y respuesta TwiML del webhook
    ├── Telegram/           Filtro del secreto y parseo del update JSON del webhook
    ├── Middleware/         Manejo global de errores → ProblemDetails
    ├── Extensions/
    └── Program.cs

tests/
├── FinGrow.Domain.UnitTests/
├── FinGrow.Application.UnitTests/
├── FinGrow.Api.UnitTests/
└── FinGrow.ArchitectureTests/
```

Las carpetas que siguen vacías con un `.gitkeep` corresponden a tareas todavía no hechas:
repositorios, casos de uso, DTOs, validadores, controllers e integraciones.

---

## Modelo de dominio

Siete entidades y tres value objects. El esquema se crea con la migración `InitialCreate`.

| Tabla | Raíz de agregado | Qué guarda |
|---|---|---|
| `companies` | Company | Empresa contratante: razón social, CUIT, credenciales, moneda por defecto |
| `departments` | (parte de Company) | Departamentos de la empresa; se desactivan, no se borran |
| `employees` | Employee | La persona que usa la app; pertenece a una empresa y opcionalmente a un departamento |
| `transactions` | Transaction | Ingresos y gastos, con importe siempre positivo y el signo dado por `type` |
| `budgets` | Budget | Tope por categoría y período |
| `goals` | Goal | Metas de ahorro con objetivo, avance y fecha límite |
| `investments` | Investment | Posiciones del portafolio: capital invertido y valuación actual |

Decisiones que conviene conocer antes de tocar el modelo:

- **Un importe nunca viaja solo.** `Money` es un value object de monto + moneda que se guarda
  como dos columnas (`amount` / `currency`). Combinar importes de monedas distintas lanza
  `DomainException`.
- **Lo gastado de un presupuesto no se persiste.** Se calcula sumando las transacciones del
  período. Un contador guardado se desincroniza en cuanto alguien edita o borra un movimiento.
- **Un empleado se relaciona con su departamento por id**, no por nombre como en los mocks del
  frontend. Renombrar un departamento no toca a nadie.
- **`ExpenseCategory` es un contrato compartido con FinGrow-AI.** Los diez valores viven también
  en `FinGrow-AI/app/domain/enums.py` y se guardan en la base con el mismo texto
  (`ahorro_inversion`, no `AhorroInversion`). Hay un test que falla si las dos listas se separan.
- **Las reglas críticas están además en la base.** Un gasto con categoría de ingreso, un importe
  cero o una meta con objetivo y progreso en monedas distintas los rechaza un `CHECK`, no solo el
  código C#.
- **Nombres en snake_case.** Se aplican de una sola vez en `OnModelCreating`
  (`SnakeCaseNamingExtensions`), para poder consultar en psql sin comillas dobles.

---

## Desarrollo

```bash
dotnet build FinGrow.sln
```

```bash
dotnet test FinGrow.sln
```

Migraciones de base de datos. La API aplica las pendientes al arrancar
(`Database:MigrateOnStartup`), así que `database update` solo hace falta para migrar sin
levantar la API:

```bash
dotnet ef migrations add <Nombre> --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api
```

```bash
dotnet ef database update --project src/FinGrow.Infrastructure --startup-project src/FinGrow.Api
```

### Tests de arquitectura

`tests/FinGrow.ArchitectureTests` verifica en cada build que las reglas de dependencia se
cumplan:

- Domain no depende de ninguna otra capa.
- Domain no depende de EF Core ni de Npgsql.
- Application no depende de Infrastructure ni de Api.
- Infrastructure no depende de Api.
- Ningún controller usa tipos de Infrastructure.
- Los handlers de Application (`*Handler`) son `internal` y viven bajo `Application.Features`.

Si alguien las rompe, el build falla. GitHub Actions los ejecuta en cada push y pull request.

### Convenciones

- Identificadores en inglés; documentación y mensajes de error en castellano.
- Namespaces file-scoped y `var` solo para tipos no evidentes (ver `.editorconfig`).
- Los warnings se tratan como errores (`Directory.Build.props`).
- Las versiones de paquetes se declaran una sola vez, en `Directory.Packages.props`. Los
  `.csproj` referencian paquetes sin versión.
- Reglas de negocio con desenlace esperable devuelven `Result`; las excepciones quedan para
  fallas técnicas.
