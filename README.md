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

Con Docker, que levanta la base, la API y FinGrow-AI juntas:

```bash
docker compose up --build
```

El servicio `ai` se construye desde `../FinGrow-AI`, así que los dos repos tienen que estar
clonados uno al lado del otro en la misma carpeta:

```
FinGrow/
├── FinGrow-BE/
└── FinGrow-AI/
```

FinGrow-AI además tiene que tener su propio `.env`, porque compose lo lee al levantar la IA (sin
ese archivo `docker compose up` falla). Se crea desde la plantilla de ese repo:

```bash
cp ../FinGrow-AI/.env.example ../FinGrow-AI/.env
```

Con la plantilla tal cual, la IA usa el proveedor `stub` (respuestas de prueba, sin llamar a
Anthropic). Para usar Claude, completar ahí `LLM_PROVIDER=anthropic` y `LLM_API_KEY`.

El secreto compartido entre la API y la IA va en un `.env` en la raíz de este repo, que no se
commitea:

```bash
echo "API_KEY=$(openssl rand -hex 32)" >> .env
```

Compose se lo pasa a la API como `AiService__ApiKey` y a la IA como `API_KEY`, pisando el
`API_KEY` del `.env` de FinGrow-AI, así que los dos siempre coinciden. Sin ese `.env` ambos
reciben el mismo valor de relleno y la comunicación funciona igual.

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
| `Database:MigrateOnStartup` | `Database__MigrateOnStartup` | Aplica las migraciones pendientes y carga el catálogo educativo al arrancar (default `true`). Poner en `false` si las migraciones se corren desde un paso de deploy separado: en ese caso el catálogo tampoco se carga |
| `Database:SeedOnStartup` | `Database__SeedOnStartup` | Carga la empresa de demo con sus departamentos y empleados (default `false`). Solo para entornos de prueba: los empleados de demo comparten una contraseña conocida |
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
| `TokenEncryption:Key` | `TokenEncryption__Key` | Clave AES-256 en base64 (`openssl rand -base64 32`) con la que se cifran en la base los tokens OAuth de las integraciones. Cambiarla deja ilegibles los tokens ya guardados |
| `Jobs:ApiKey` | `Jobs__ApiKey` | Clave (mínimo 16 caracteres, `openssl rand -base64 24`) que tiene que traer la cabecera `X-Jobs-Key` para disparar o consultar los trabajos programados en `/api/jobs`. Es la que usa el cron de Dokploy; ver [Trabajos programados](#trabajos-programados-dokploy) |
| `DolarApi:BaseUrl` | `DolarApi__BaseUrl` | URL base de [DolarApi](https://dolarapi.com), de donde sale la cotización del dólar MEP que usa la pantalla de Inversiones (default `https://dolarapi.com/`). Es pública y no pide credenciales |
| `DolarApi:TimeoutSeconds` | `DolarApi__TimeoutSeconds` | Timeout de la consulta de la cotización (default 10) |
| `DolarApi:CacheMinutes` | `DolarApi__CacheMinutes` | Minutos que la API reutiliza la última cotización antes de volver a pedirla (default 5) |
| `Byma:BaseUrl` | `Byma__BaseUrl` | Datos públicos del sitio de BYMA de los que salen los precios de cierre del trabajo `investment-quotes` (default `https://open.bymadata.com.ar/vanoms-be-core/rest/api/bymadata/free/`). No piden credenciales |
| `Byma:TimeoutSeconds` | `Byma__TimeoutSeconds` | Timeout de cada consulta a BYMA (default 30) |
| `Byma:MaxPagesPerPanel` | `Byma__MaxPagesPerPanel` | Tope de páginas que se leen de cada panel, de a 189 títulos (default 20) |
| `Byma:CacheMinutes` | `Byma__CacheMinutes` | Minutos que la API reutiliza los precios bajados de BYMA antes de volver a pedirlos, para que cotizar un símbolo desde el formulario no descargue los paneles en cada consulta (default 5) |
| `ArgentinaDatos:BaseUrl` | `ArgentinaDatos__BaseUrl` | API de [ArgentinaDatos](https://argentinadatos.com) de la que sale la cuotaparte de los fondos comunes (default `https://api.argentinadatos.com/`). Es pública y no pide credenciales |
| `ArgentinaDatos:TimeoutSeconds` | `ArgentinaDatos__TimeoutSeconds` | Timeout de cada consulta (default 15) |
| `ArgentinaDatos:CacheMinutes` | `ArgentinaDatos__CacheMinutes` | Minutos que se reutilizan las cuotapartes bajadas; se publican una vez por día (default 60) |
| `ArgentinaDatos:StaleAfterDays` | `ArgentinaDatos__StaleAfterDays` | Días de atraso respecto de la cuotaparte más nueva a partir de los cuales un fondo se descarta por no existir más (default 30) |
| `CoinGecko:BaseUrl` | `CoinGecko__BaseUrl` | API de [CoinGecko](https://www.coingecko.com) de la que sale el precio de las criptomonedas (default `https://api.coingecko.com/api/v3/`) |
| `CoinGecko:ApiKey` | `CoinGecko__ApiKey` | Clave demo opcional (gratuita) que viaja en `x-cg-demo-api-key`; sin ella CoinGecko corta con `429` después de pocos pedidos por minuto |
| `CoinGecko:TimeoutSeconds` | `CoinGecko__TimeoutSeconds` | Timeout de cada consulta (default 15) |
| `CoinGecko:CacheMinutes` | `CoinGecko__CacheMinutes` | Minutos que se reutilizan los precios bajados (default 10) |
| `CoinGecko:TopCoins` | `CoinGecko__TopCoins` | Cuántas criptomonedas, de mayor a menor capitalización, se cotizan (default y máximo 250) |
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
dotnet user-secrets set "TokenEncryption:Key" "$(openssl rand -base64 32)" --project src/FinGrow.Api
dotnet user-secrets set "Jobs:ApiKey" "$(openssl rand -base64 24)" --project src/FinGrow.Api
```

Si `Jwt:SecretKey`, `AiService:ApiKey` o las credenciales de Twilio o Telegram faltan, la API no arranca y
el log dice cuál es. Con `dotnet run` contra una FinGrow-AI levantada con `uvicorn` y `API_KEY`
vacía, `AiService:ApiKey` puede ser cualquier texto porque la IA no lo valida. Con Docker y en
producción los dos servicios tienen que compartir el mismo valor.

`/health` informa `Degraded` (no `Unhealthy`) si FinGrow-AI no responde: la API sigue atendiendo
todo lo que no depende de la IA.

`GET /api/exchange-rates/mep` devuelve la cotización del dólar MEP (compra, venta y hora de
actualización) que la pantalla de Inversiones usa para mostrar el portafolio en una sola moneda.
Si DolarApi no responde o devuelve algo que no se puede leer, el endpoint contesta `503` y el
frontend muestra cada moneda por separado; una respuesta fallida nunca queda guardada en la caché.

Las inversiones cargadas con símbolo y cantidad se cotizan con tres fuentes públicas, una por
mercado: los datos del sitio de BYMA para lo que cotiza en bolsa (acciones, CEDEAR, ETF, bonos,
obligaciones negociables y letras del Tesoro), [ArgentinaDatos](https://argentinadatos.com) para
los fondos comunes y [CoinGecko](https://www.coingecko.com) para las criptomonedas. Los plazos
fijos, las cauciones y las cuentas remuneradas no tienen fuente y quedan valuados al costo. En
BYMA el símbolo tiene que ser la variante de la moneda de la inversión (AL30 en pesos, AL30D en
dólares) y los bonos, las ONs y las letras cotizan cada 100 nominales. Los datos de BYMA no son su
API contratada ni tienen garantía de servicio: antes de producción con empleados reales se
reemplazan por la API EOD de BYMA (contrato con marketdata@byma.com.ar), implementando otro
`IMarketPriceProvider`.

`GET /api/security-prices?symbol=AL30&currency=ARS&type=Bond` cotiza un símbolo mientras se carga
el formulario de inversiones: devuelve el precio por unidad (por nominal en bonos, ONs y letras; por
cuotaparte en fondos), el nombre si la fuente lo publica, la fecha del precio y la fuente. El
`type` elige el mercado, así que un mismo símbolo en dos mercados (COIN como CEDEAR y como cripto)
no se confunde. Primero busca en la fuente del mercado, que la API reutiliza unos minutos; si no
tiene precio para ese símbolo (los fines de semana y feriados el feed público de BYMA viene todo en
0) o no responde en 10 segundos, usa el último precio que guardó `investment-quotes` en
`security_prices` con su fecha. Sin precio en ninguno de los dos contesta `404` si la fuente
respondió y `503` si no. BYMA y CoinGecko se cotizan en `ARS` o `USD`; un fondo, en la moneda de
la inversión.

`GET /api/security-prices/search?type=MutualFund&query=balanz` busca hasta 20 símbolos o nombres
del mercado que contengan el texto, sin distinguir mayúsculas ni tildes, primero los que empiezan
con él. Es lo que usa el formulario para elegir un fondo, porque un fondo se identifica por su
nombre completo tal como lo publica ArgentinaDatos ("1810 Ahorro", "Balanz Capital Money Market -
Clase A"), no por un símbolo.

### Fuente de cotización de cada instrumento

| Instrumento | `InvestmentType` | Fuente | De dónde sale el precio | Precio por |
|---|---|---|---|---|
| Acción | `Stock` | BYMA, datos públicos | Paneles `leading-equity` y `general-equity` | Acción |
| CEDEAR | `Cedear` | BYMA, datos públicos | Panel `cedears` | CEDEAR |
| ETF | `Etf` | BYMA, datos públicos | Panel `cedears`: los ETF del exterior (SPY, QQQ) se operan como CEDEAR | Unidad |
| Bono | `Bond` | BYMA, datos públicos | Panel `public-bonds` | Nominal (BYMA cotiza cada 100) |
| Obligación negociable | `CorporateBond` | BYMA, datos públicos | Panel `negociable-obligations` | Nominal (BYMA cotiza cada 100) |
| Letra del Tesoro | `TreasuryBill` | BYMA, datos públicos | Panel `lebacs` (LECAP y BONCAP) | Nominal (BYMA cotiza cada 100) |
| Fondo común | `MutualFund` | ArgentinaDatos | `GET v1/finanzas/fci/{categoria}/ultimo` de `mercadoDinero`, `rentaFija`, `rentaMixta`, `rentaVariable` y `retornoTotal` | Cuotaparte (ArgentinaDatos publica el valor de 1000) |
| Criptomoneda | `Crypto` | CoinGecko | `GET coins/markets` en `usd` y `ars`, las 250 de mayor capitalización | Unidad |
| Plazo fijo | `FixedTermDeposit` | Ninguna | Se valúa al costo | — |
| Caución | `Repo` | Ninguna | Se valúa al costo; BYMA publica una tasa, no un precio | — |
| Cuenta remunerada | `RemuneratedAccount` | Ninguna | Se valúa al costo | — |
| Dólar MEP | — | [DolarApi](https://dolarapi.com) | `GET v1/dolares/bolsa`; convierte el portafolio a una sola moneda, no valúa inversiones | Dólar |

- **Un proveedor por mercado.** Cada fuente implementa `IMarketPriceProvider` y declara su
  `PriceMarket` (`Exchange`, `MutualFund` o `Crypto`); el tipo de activo de la inversión elige el
  mercado (`InvestmentType.QuotedOn()`) y dentro del mercado el precio se busca por símbolo y
  moneda. Cada proveedor baja todo de una vez y lo reutiliza en memoria (`CacheMinutes`).
- **BYMA.** `POST {Byma:BaseUrl}{panel}` con liquidación a 24 hs. La moneda elige la variante:
  AL30 en pesos, AL30D en dólares; las variantes en dólar cable (AL30C) se descartan.
- **ArgentinaDatos.** No informa la moneda de cada fondo: un fondo se busca por nombre en
  cualquier moneda y se valúa en la de la inversión. La moneda que devuelve la API se deduce del
  nombre ("Dólares", "USD", salvo "Dólar Linked", que es en pesos) y el formulario la usa para
  avisar si no coincide. Se descartan los fondos cuya última cuotaparte tiene más de
  `ArgentinaDatos:StaleAfterDays` días que la más nueva: son fondos que ya no existen.
- **CoinGecko.** Sin clave permite pocos pedidos por minuto y corta con `429`; cada descarga hace
  dos (dólares y pesos) y se reutiliza `CoinGecko:CacheMinutes`. Con una clave demo gratuita en
  `CoinGecko:ApiKey` el límite sube a 30 por minuto. Si un símbolo se repite, queda el de mayor
  capitalización; los que no son letras y números se descartan.
- **Quién la usa.** `investment-quotes` valúa una vez por día hábil las inversiones con símbolo y
  guarda el último precio en `security_prices`; si una fuente no responde, cotiza con las otras y
  la corrida queda fallida con el motivo. `GET /api/security-prices` cotiza mientras se carga el
  formulario y, si la fuente no tiene precio o no responde, usa ese último precio. Las cuentas
  vinculadas a IOL se cotizan aparte con T-19.

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

### Trabajos programados (Dokploy)

La API no tiene un scheduler propio. Lo que corre "solo cada tanto" son **trabajos** registrados
en código (`IScheduledJob`) que se disparan desde afuera, y en los entornos desplegados el que
los dispara es el cron de Dokploy (*Schedules*). Cada corrida queda registrada en la tabla
`job_runs` con inicio, fin, resultado y, si falló, el error; una corrida fallida nunca impide la
siguiente.

| Trabajo | Cron sugerido (UTC) | Qué hace |
|---|---|---|
| `metrics-snapshot` | `0 4 1 * *` (el 1 de cada mes, 01:00 de Argentina) | Genera las fotos mensuales de métricas por empresa y por departamento (`company_metrics_snapshots` y `department_metrics_snapshots`) del último mes cerrado, y completa las de los meses anteriores que falten desde el alta de cada empresa. Es idempotente: correrlo de nuevo actualiza la foto del último mes cerrado en lugar de duplicarla |
| `mercadopago-sync` | `0 * * * *` (cada hora) | Recorre las cuentas de Mercado Pago vinculadas y trae los movimientos nuevos como pendientes de revisión. Mercado Pago no avisa por webhook lo que un usuario paga, por eso se consulta |
| `investment-quotes` | `30 21 * * 1-5` (días hábiles, 18:30 de Argentina) | Guarda en `security_prices` el último precio de cada símbolo de BYMA, cada fondo de ArgentinaDatos y cada cripto de CoinGecko (aunque nadie tenga inversiones con símbolo) y cotiza con esos precios las inversiones cargadas con símbolo y cantidad, registrándoles la valuación de mercado del día. Correrlo de nuevo el mismo día actualiza esa valuación en lugar de duplicarla. Si una fuente no responde, cotiza con las otras y la corrida queda fallida con el motivo; si no responde ninguna, no toca nada |

Los endpoints viven bajo `/api/jobs` y se protegen con la cabecera `X-Jobs-Key`, que tiene que
coincidir con `Jobs:ApiKey`. No usan JWT: los llama un cron, no una persona logueada.

| Método y ruta | Qué hace |
|---|---|
| `GET /api/jobs` | Lista los trabajos registrados con su cron sugerido y su última corrida |
| `POST /api/jobs/{nombre}/run` | Ejecuta el trabajo en el momento y responde cuando termina con la corrida registrada (`status`, `summary`, `error`, `durationSeconds`). `404` si el nombre no existe, `409` si ese mismo trabajo ya está corriendo |
| `GET /api/jobs/{nombre}/runs?take=20` | Las últimas corridas, la más reciente primero (`take` entre 1 y 100) |

En Dokploy, dentro del servicio de la API, pestaña **Schedules → Create Schedule**: un schedule
por trabajo, con el cron de la tabla y este comando. El comando corre dentro del contenedor, que
ya trae `curl` y tiene `Jobs__ApiKey` en su entorno, así que la clave no se copia a ningún lado:

```bash
curl -fsS -X POST "http://localhost:8080/api/jobs/metrics-snapshot/run" -H "X-Jobs-Key: $Jobs__ApiKey"
```

Si el schedule corre en el servidor en lugar de dentro del contenedor, es la misma llamada contra
el host público con la clave pegada. Y para probar a mano desde tu máquina:

```bash
curl -fsS -X POST "https://dev-be.fingrow.com.ar/api/jobs/metrics-snapshot/run" -H "X-Jobs-Key: <Jobs:ApiKey del entorno>"
```

La respuesta es la corrida registrada; si `status` es `Failed`, `error` dice por qué y el log de
la API tiene el detalle completo. `GET /api/jobs` sirve para verificar de un vistazo que cada
cron esté corriendo: muestra la última corrida de cada trabajo.

### Alertas y notificaciones

Las alertas salen de un único motor (`Notifier`, en `Application/Features/Notifications`) que
comparten presupuestos, metas, integraciones y el panel de empresa. Cada alerta pasa por tres
pasos, siempre en este orden:

1. **Deduplicación.** Toda alerta declara un criterio (`deduplication_key`, por ejemplo
   `transactions-to-review:MercadoPago`) y una ventana. Si al mismo destinatario ya se le avisó
   el mismo tipo con el mismo criterio dentro de la ventana, no se vuelve a avisar.
2. **Persistencia.** La notificación queda en la tabla `notifications`, que es la bandeja de la
   app y el historial de todo lo que se avisó. Por eso la bandeja no se puede apagar.
3. **Entrega.** Se manda además por cada canal externo que el empleado no haya desactivado. Hoy
   el único es Telegram, y solo si el empleado tiene el chat vinculado. Si un canal falla, se
   loguea y la notificación queda igual en la bandeja.

El destinatario puede ser un empleado o la empresa (`recipient_type`), así que la misma bandeja
sirve para las alertas de bienestar del panel de empresa. Los canales externos son solo para
empleados.

Lo que dispara una alerta es un **evento de dominio**: un agregado lo levanta (`Raise`) cuando
pasa algo, y recién cuando `SaveChanges` confirma el cambio se publica por MediatR, cada evento
en un scope propio. Si una reacción falla, se loguea y no rompe la operación que la originó,
porque esa ya quedó guardada. La primera alerta es `TransactionsToReview`: cuando Mercado Pago o
Gmail traen movimientos pendientes, el empleado recibe un aviso para revisarlos, como máximo uno
cada 12 horas por origen (un sync de 30 movimientos genera una sola alerta). Lo que carga el
empleado a mano, por foto o por chat no avisa: ya está mirando la app.

| Método y ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/notifications?unreadOnly=false&pageNumber=1&pageSize=20` | Empleado y empresa | La bandeja del usuario logueado, la más reciente primero (`pageSize` entre 1 y 100) |
| `GET /api/notifications/unread-count` | Empleado y empresa | Cuántas no leídas tiene, para el contador de la campanita |
| `POST /api/notifications/{id}/read` | Empleado y empresa | Marca una como leída. `404` si no es del usuario |
| `POST /api/notifications/read-all` | Empleado y empresa | Marca todas como leídas y devuelve cuántas marcó |
| `GET /api/notifications/channels` | Empleado | Cada canal con `isEnabled`, `isConfigurable` y `isConnected` (Telegram vinculado) |
| `PUT /api/notifications/channels/{canal}` | Empleado | Activa o desactiva un canal externo: `{ "isEnabled": false }`. `400` para `InApp` |

Un canal sin preferencia guardada está activo: quien vincula Telegram empieza a recibir alertas
por ahí hasta que lo apague.

Para sumar una alerta nueva: un tipo en `NotificationType`, el evento de dominio que la dispara
(levantado por el agregado con `Raise`) y una clase `internal` bajo
`Application/Features/Notifications/Alerts/` que implementa
`INotificationHandler<DomainEventEnvelope<TuEvento>>` y llama a `Notifier.NotifyAsync` con el
criterio y la ventana de deduplicación. Una alerta que depende del paso del tiempo (una meta sin
aportes) se evalúa desde un trabajo programado y usa el mismo `Notifier`.

Pendiente fuera de T-07: WhatsApp no es canal todavía (Twilio exige plantillas aprobadas para
escribirle a alguien fuera de la ventana de 24 horas), no hay mail, no se reintenta una entrega
fallida y no hay política de retención para `notifications`.

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
│   ├── Jobs/               IScheduledJob, JobResult y el JobRunner que registra cada corrida
│   ├── Features/           Un subdirectorio por funcionalidad (Integrations/{GenerateLinkCode,GetIntegration,UnlinkIntegration,Linking,WhatsApp,Telegram}, Jobs, Metrics)
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
    ├── Jobs/               Filtro de la clave X-Jobs-Key con la que el cron dispara los trabajos
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
| `investments` | Investment | Posiciones del portafolio: capital invertido y valuación actual. `symbol` es el ticker (hasta 20 caracteres) o, en un fondo común, su nombre completo (hasta 150); `quantity` admite 10 decimales para fracciones de cripto |
| `security_prices` | SecurityPrice | El último precio por unidad de cada mercado, símbolo y moneda (BYMA, fondos de ArgentinaDatos y cripto de CoinGecko); una fila por mercado, símbolo y moneda que `investment-quotes` pisa en cada corrida. En los fondos el símbolo es el nombre del fondo. Es lo que cotiza el formulario de inversiones cuando la fuente no publica precios (BYMA los fines de semana y feriados) o no responde |
| `job_runs` | JobRun | Registro de cada corrida de un trabajo programado: inicio, fin, resultado y error |
| `company_metrics_snapshots` | CompanyMetricsSnapshot | Foto mensual de métricas agregadas de una empresa: empleados activos, cuántos participaron, movimientos confirmados, presupuestos, metas e integraciones |
| `department_metrics_snapshots` | DepartmentMetricsSnapshot | La misma foto, por departamento |
| `courses` | Course | Cursos del catálogo de educación financiera: nivel, categoría y tipo de activo relacionado; la duración se deriva de las lecciones |
| `lessons` | (parte de Course) | Lecciones de un curso, ordenadas por `position`, con su duración y el video |
| `articles` | Article | Artículos del catálogo: resumen, cuerpo en Markdown, categoría y tiempo de lectura |
| `lesson_completions` | LessonCompletion | Qué lecciones terminó cada empleado; el progreso de un curso se deriva de acá |
| `course_ratings` | CourseRating | La calificación de 1 a 5 que cada empleado le da a un curso que terminó; el promedio del curso se deriva de acá |

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
- **El catálogo educativo es contenido de la plataforma, no de una empresa.** Lo carga
  `EducationCatalogSeeder` después de migrar, desde `EducationCatalog`. La carga es idempotente
  por `slug`: suma lo que falta y no pisa lo que ya existe, así que corregir un curso ya cargado
  se hace en la base o con una migración, y cambiar un slug lo carga como contenido nuevo.
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
