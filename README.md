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

Con Docker, que levanta la base, la API, FinGrow-AI y un S3 local para los comprobantes:

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
| `AiService:RecommendationsCacheHours` | `AiService__RecommendationsCacheHours` | Horas que la API reutiliza las recomendaciones del reporte para un mismo resumen financiero antes de volver a pedirlas a la IA (default 6; `0` las pide siempre) |
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
| `Gmail:ClientId` | `Gmail__ClientId` | Client ID del cliente OAuth "Aplicación web" creado en [Google Cloud Console](https://console.cloud.google.com/auth/clients) del proyecto FinGrow, con la Gmail API habilitada |
| `Gmail:ClientSecret` | `Gmail__ClientSecret` | Client Secret de ese cliente; autentica el canje, la renovación y la revocación de tokens |
| `Gmail:RedirectUri` | `Gmail__RedirectUri` | URL de `GET /api/integrations/gmail/oauth/callback`, idéntica a una de las cargadas en el cliente OAuth. Google acepta `localhost`: en desarrollo ya viene configurada para el puerto 5210 |
| `Gmail:FrontendReturnUrl` | `Gmail__FrontendReturnUrl` | Página del frontend a la que vuelve el navegador al terminar la vinculación; recibe `?gmail=linked` o `?gmail=error&reason=...` (default `http://localhost:3000/dashboard/settings`) |
| `Gmail:TimeoutSeconds` | `Gmail__TimeoutSeconds` | Timeout de las llamadas a Google (default 30) |
| `Iol:ApiBaseUrl` | `Iol__ApiBaseUrl` | URL base de la API de InvertirOnline (default `https://api.invertironline.com/`). Todo lo que se hace por la API impacta en la cuenta real |
| `Iol:TimeoutSeconds` | `Iol__TimeoutSeconds` | Timeout de las llamadas a la API de IOL (default 30) |
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
| `Email:ResendApiKey` | `Email__ResendApiKey` | API key de [Resend](https://resend.com/api-keys), el proveedor de los correos transaccionales (por ejemplo, las credenciales del alta de un empleado) |
| `Email:From` | `Email__From` | Remitente de los correos. Tiene que ser una dirección del dominio verificado en Resend (default `FinGrow <no-reply@fingrow.com.ar>`) |
| `Email:RedirectAllTo` | `Email__RedirectAllTo` | Si tiene valor, todos los correos van a esa dirección en lugar de a su destinatario, que queda en el asunto (`[Para ana@empresa.com] ...`). Por defecto va vacía en todos los entornos, `dev-be` incluido. El `docker-compose.yml` local la pone en `delivered@resend.dev`, una casilla de prueba de Resend que acepta el correo sin entregarlo a nadie: para verlo en tu bandeja, poné tu dirección en `EMAIL_REDIRECT_ALL_TO` del `.env` |
| `Email:TimeoutSeconds` | `Email__TimeoutSeconds` | Timeout de cada envío a Resend (default 30) |
| `Storage:ServiceUrl` | `Storage__ServiceUrl` | URL del almacenamiento compatible con S3 donde viven los comprobantes. En Cloudflare R2 es `https://<account-id>.r2.cloudflarestorage.com`; en desarrollo, el S3Mock de compose (`http://localhost:9090`). Ver [Comprobantes](#comprobantes-cloudflare-r2) |
| `Storage:BucketName` | `Storage__BucketName` | Bucket de los comprobantes. Uno por entorno, siempre privado |
| `Storage:AccessKey` | `Storage__AccessKey` | Access Key ID del token de API de R2 |
| `Storage:SecretKey` | `Storage__SecretKey` | Secret Access Key de ese token |
| `Storage:Region` | `Storage__Region` | Región del bucket. En R2 es `auto` (default) |
| `Storage:ForcePathStyle` | `Storage__ForcePathStyle` | Arma las URLs como `<host>/<bucket>` en lugar de `<bucket>.<host>` (default `true`, que es lo que necesitan R2 y S3Mock) |
| `Storage:TimeoutSeconds` | `Storage__TimeoutSeconds` | Timeout de cada operación contra el almacenamiento (default 30) |
| `ReceiptsCleanup:GraceDays` | `ReceiptsCleanup__GraceDays` | Días que espera `receipts-cleanup` antes de borrar un comprobante suelto, contados desde que se subió (default 7, entre 1 y 365) |

Los secretos no se commitean. En desarrollo local:

```bash
dotnet user-secrets set "ConnectionStrings:Database" "<cadena>" --project src/FinGrow.Api
dotnet user-secrets set "Jwt:SecretKey" "<clave de al menos 32 caracteres>" --project src/FinGrow.Api
dotnet user-secrets set "AiService:ApiKey" "<secreto compartido con FinGrow-AI>" --project src/FinGrow.Api
dotnet user-secrets set "Twilio:AccountSid" "<Account SID de Twilio>" --project src/FinGrow.Api
dotnet user-secrets set "Twilio:AuthToken" "<Auth Token de Twilio>" --project src/FinGrow.Api
dotnet user-secrets set "Telegram:BotToken" "<token del bot>" --project src/FinGrow.Api
dotnet user-secrets set "Telegram:WebhookSecret" "<secreto inventado para el webhook>" --project src/FinGrow.Api
dotnet user-secrets set "Gmail:ClientId" "<Client ID del cliente OAuth de Google>" --project src/FinGrow.Api
dotnet user-secrets set "Gmail:ClientSecret" "<Client Secret de ese cliente>" --project src/FinGrow.Api
dotnet user-secrets set "TokenEncryption:Key" "$(openssl rand -base64 32)" --project src/FinGrow.Api
dotnet user-secrets set "Jobs:ApiKey" "$(openssl rand -base64 24)" --project src/FinGrow.Api
dotnet user-secrets set "Email:ResendApiKey" "<API key de Resend>" --project src/FinGrow.Api
```

Si `Jwt:SecretKey`, `AiService:ApiKey`, las credenciales de Twilio, Telegram o Gmail, o la API key de Resend faltan, la API no arranca y
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
  formulario y, si la fuente no tiene precio o no responde, usa ese último precio. Las inversiones
  importadas de IOL no pasan por acá: las valúa `iol-sync` con lo que informa IOL.
- **Lo que no cotiza en ningún mercado.** Los plazos fijos, las cauciones y las cuentas
  remuneradas no tienen precio: se valúan con la TNA que carga el empleado (obligatoria en esos
  tres tipos, con el plazo en días en plazos fijos y cauciones). Plazos fijos y cauciones devengan
  interés simple hasta el vencimiento, `capital × (1 + TNA × días / 365)` con los días topeados en
  el plazo; las cuentas remuneradas capitalizan todos los días, `capital × (1 + TNA / 365)^días`.
  El alta y la edición ya dejan la valuación del día y `investment-accruals` la registra cada día
  con origen `Accrued`; una vencida queda valuada a la fecha de vencimiento. Para el rendimiento
  cuenta como cotizada, y como se deriva de capital, fecha, TNA y plazo, se recalcula cuando cambia
  cualquiera de ellos.
- **La evolución del portafolio (HU-33).** `GET /api/investments/evolution?currency=ARS&range=ThreeMonths`
  (`OneMonth`, `ThreeMonths`, `SixMonths`, `OneYear` o `All`) devuelve, para una moneda, el valor y
  el capital invertido de cada día del rango, o de cada semana si el rango pasa de un año. Empieza
  como mucho en la primera compra y termina hoy, así que el último punto coincide con el valor
  total del resumen. No consulta ninguna fuente: en cada fecha, cada inversión vale su última
  valuación conocida, y las que tienen TNA, lo devengado ese día.

### Comprobantes (Cloudflare R2)

Los archivos que suben los empleados (fotos y PDF de comprobantes, HU-12) no se guardan en
PostgreSQL: van a un almacenamiento compatible con S3 y la tabla `transaction_receipts` guarda
solo su clave. El código habla el protocolo S3 (`IFileStorage` → `S3FileStorage`), así que el
mismo código funciona contra Cloudflare R2 en los entornos desplegados y contra
[S3Mock](https://github.com/adobe/S3Mock) en desarrollo; lo único que cambia es la sección
`Storage`.

| Endpoint | Qué hace |
|---|---|
| `POST /api/receipts` | Sube un comprobante (`multipart/form-data`, campo `file`). Acepta JPG, PNG y PDF de hasta 10 MB, y contrasta el tipo declarado con los primeros bytes del archivo |
| `GET /api/receipts/{id}/file` | Devuelve el archivo. Responde 403 si el comprobante es de otro empleado |
| `POST /api/transactions` con `receiptId` | Crea el movimiento y le asocia el comprobante en la misma operación: o se guardan los dos, o ninguno. 404 si el comprobante no existe, 403 si es de otro empleado |
| `PUT /api/transactions/{id}` con `receiptId` | Igual que al crear, pero sobre un movimiento existente. Si ya tenía uno, lo reemplaza. Sin `receiptId`, conserva el que tenía |
| `DELETE /api/transactions/{id}/receipt` | Quita el comprobante del movimiento, que queda sin ninguno |
| `POST /api/receipts/{id}/scan` | Escanea un comprobante ya subido (HU-12) y devuelve el movimiento propuesto, pendiente, con el comprobante asociado. Ver [Escaneo de comprobantes](#escaneo-de-comprobantes-hu-12) |

**Un movimiento tiene como máximo un comprobante** (índice único en `transaction_receipts.transaction_id`): asociarle otro reemplaza al anterior, que queda suelto. Las respuestas de los movimientos (listado, detalle, crear, editar, pendientes y confirmar) informan su `receiptId`, o `null` si no tiene.

Un comprobante que deja de usarse (se reemplaza, se quita o se borra su movimiento) **no se borra en el momento**: queda suelto, sin movimiento, y lo limpia el trabajo `receipts-cleanup` (ver [Trabajos programados](#trabajos-programados-dokploy)) cuando pasan `ReceiptsCleanup:GraceDays` días desde que se subió. La espera protege la foto que un empleado acaba de subir y todavía no guardó; un comprobante viejo que se suelta hoy se borra en la corrida siguiente.

Un comprobante nace sin movimiento: si el OCR falla, la imagen ya quedó guardada y se asocia
después. Si el almacenamiento no responde, la subida devuelve 503 y no se crea nada en la base.

#### Escaneo de comprobantes (HU-12)

`POST /api/receipts/{id}/scan` lee el archivo del almacenamiento, se lo manda a FinGrow-AI
(`POST /api/v1/receipt-scanning/receipts`, Amazon Textract más Claude para el comercio y la
categoría) y registra un gasto **pendiente** con fuente `ReceiptScan`: el empleado lo revisa y lo
confirma o descarta como cualquier otra propuesta (HU-15).

- **Lo que se precarga**: monto, moneda (pesos si el ticket no dice otra), fecha, comercio (como
  descripción), categoría y medio de pago. Sin medio de pago queda Efectivo; sin fecha, o con una
  fecha futura, queda la de hoy; sin comercio, la descripción es «Comprobante escaneado».
- **La confianza** es la de la lectura (la menor entre monto, fecha y comercio) y se guarda en
  `ai_confidence`, con `ai_model` = OCR + modelo. Un campo que no se pudo leer baja la confianza a
  media.
- **No se escanea dos veces**: si el comprobante ya tiene una propuesta pendiente, la devuelve; si
  está asociado a un movimiento ya registrado, responde 409. `external_reference` es
  `receipt:<id>`.
- **Cuando no se puede leer**, responde 400 con un código propio y no crea nada:
  `Receipt.Unreadable`, `Receipt.MissingAmount`, `Receipt.UnsupportedForScan` (un PDF de más de
  una página) o `Receipt.TooLargeToScan` (más de 5 MB, el máximo que Textract acepta en bytes; el
  frontend recomprime las fotos antes de subirlas). Si FinGrow-AI no responde, 503
  `Receipt.ScanUnavailable`. En todos los casos el comprobante sigue guardado y el empleado lo
  puede asociar a un movimiento cargado a mano (`POST /api/transactions` con `receiptId`).
- **Descartar una propuesta suelta su comprobante** (evento `TransactionDiscarded`), así
  `receipts-cleanup` lo borra pasado el período de gracia.

**En local** no hay que configurar nada: `docker compose up` levanta S3Mock con el bucket
`fingrow-receipts` ya creado y `appsettings.Development.json` apunta a él. Nada de lo que se sube
en desarrollo sale de tu máquina.

**En un entorno desplegado** (una sola vez por entorno):

1. En el panel de Cloudflare, *R2 Object Storage* → **Create bucket**. Nombre por entorno
   (`fingrow-receipts-dev`, `fingrow-receipts-prod`), ubicación automática. No habilites el acceso
   público: los archivos solo se sirven a través de la API, que controla de quién es cada uno.
2. *R2 Object Storage* → **Manage API tokens** → **Create API token**, con permiso
   **Object Read & Write** limitado a ese bucket. Cloudflare muestra el *Access Key ID*, el
   *Secret Access Key* y el endpoint S3 (`https://<account-id>.r2.cloudflarestorage.com`). El
   secreto se ve una sola vez.
3. En Dokploy, en las variables de entorno del servicio de la API: `Storage__ServiceUrl`,
   `Storage__BucketName`, `Storage__AccessKey` y `Storage__SecretKey`.

**La API no arranca sin la sección `Storage` completa** (se valida al iniciar), así que las
variables tienen que estar cargadas antes de desplegar esta versión.

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

Con el número vinculado, el empleado registra movimientos escribiendo o mandando una nota de voz
(HU-14). Una nota de voz se descarga de Twilio, se transcribe con FinGrow-AI
(`POST /api/v1/transcription/audio`, que necesita `TRANSCRIPTION_PROVIDER=openai` en ese
servicio) y se descarta: el audio no se guarda. El texto, escrito o transcripto, sigue el mismo
camino que en Telegram: queda pendiente con fuente `WhatsApp` y la respuesta muestra la propuesta,
precedida de «🎙️ Entendí: …» si fue un audio. Como TwiML no tiene botones, el empleado contesta
**SÍ** o **NO**, o el **número** del medio de pago cuando el mensaje no lo dijo (elegir uno
confirma). La respuesta aplica a la última propuesta de WhatsApp pendiente de las últimas 24
horas; si no hay ninguna, el mensaje se interpreta como un movimiento nuevo. Twilio espera la
respuesta del webhook 15 segundos como mucho: si la descarga, la transcripción y la
interpretación no terminan en 12, el empleado recibe el aviso de que lo intente de nuevo.

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
   curl "https://api.telegram.org/bot<token>/setWebhook"      -d "url=https://<subdominio>.ngrok-free.app/api/webhooks/telegram"      -d "secret_token=<secreto>"      -d "allowed_updates=[\"message\",\"callback_query\"]"
   ```

   Cada vez que ngrok cambie de URL hay que repetir este paso. `getWebhookInfo` en la misma
   URL base muestra el estado y el último error si Telegram no logra entregar.

El flujo de vinculación es el mismo que WhatsApp: el empleado pide un código con
`POST /api/integrations/telegram/link-code` y se lo manda al bot. El frontend arma el link
`https://t.me/<bot>?start=<código>`, así que basta tocar "Abrir" y "Iniciar": Telegram manda
`/start <código>` y el chat queda vinculado. Solo se procesan chats privados; un mensaje en un
grupo se ignora.

Con el chat vinculado, el empleado registra movimientos escribiéndole al bot en lenguaje natural
(HU-13): "Gasté $500 en el súper", "Cobré $10000 por un freelance". La API le pide a FinGrow-AI
que interprete el texto (`POST /api/v1/message-parsing/transactions`), guarda el movimiento como
pendiente con fuente `Telegram` y contesta con lo que entendió y botones para confirmarlo o
descartarlo. Si el mensaje no dice el medio de pago, los botones son los medios de pago y tocar
uno confirma con ese medio; si el empleado no contesta, queda pendiente en la bandeja de la app
con Efectivo. La fecha es la que dice el mensaje ("Ayer gasté $500 en el súper"): la API le manda
a FinGrow-AI la fecha de hoy en Argentina para que resuelva "ayer" o "el lunes", y si el mensaje
no dice ninguna o la que dice cae en el futuro, registra la de hoy. Con un período vago ("la
semana pasada", "hace unos días") FinGrow-AI estima la fecha y la propuesta la muestra como
«(estimada)»: el empleado la confirma o la descarta con los mismos botones. Si falta el monto, el bot
repregunta y no registra nada. Los toques de botón llegan
como `callback_query`, por eso el webhook tiene que registrarse con ese tipo en `allowed_updates`.

Una nota de voz (HU-71) sigue el mismo camino: la API la baja con `getFile` del Bot API, la
transcribe con FinGrow-AI (`POST /api/v1/transcription/audio`, que necesita
`TRANSCRIPTION_PROVIDER=openai` en ese servicio) y propone el movimiento con los mismos botones,
precedido de «🎙️ Entendí: …». El audio no se guarda. Las notas de voz llegan como `message`,
así que no hace falta tocar `allowed_updates`.

Una foto de un ticket, o un PDF de una página mandado como archivo (HU-12), se baja con
`getFile`, se guarda como comprobante igual que uno subido desde la app y se escanea con
FinGrow-AI. La propuesta llega con fuente `ReceiptScan`, precedida de «🧾 Esto leí en el ticket:»
y con los mismos botones; si el ticket no dice el medio de pago, los botones son los medios de
pago. Si el ticket no se puede leer o no tiene total, el bot lo dice y no registra nada: el
comprobante queda suelto y lo limpia `receipts-cleanup`. Un reintento del mismo update no lo
vuelve a guardar ni a escanear (`external_reference` = chat + mensaje).

### InvertirOnline (IOL)

IOL no tiene una pantalla de consentimiento como Mercado Pago: autentica con usuario y
contraseña (`POST /token`, `grant_type=password`) y devuelve un bearer de 15 minutos y un
refresh token. Por eso la contraseña pasa por la API de FinGrow: el empleado la manda a
`POST /api/integrations/iol/link`, la API la canjea en el momento y guarda solo los tokens,
cifrados. La contraseña no se persiste, no se loguea y no vuelve en la respuesta.

Cada empleado tiene que tener las APIs activadas en su cuenta de IOL (menú de usuario >
*Configuración API*, aceptando los términos); si no, IOL rechaza el login como si la contraseña
fuera incorrecta.

Antes de cada llamada a IOL, `IolAccessTokenProvider` renueva el bearer si le quedan menos de
2 minutos (`grant_type=refresh_token`).

**El bearer dura 15 minutos y el refresh token, 20.** IOL los informa en `.issued`, `.expires` y
`.refreshexpires`; `expires_in` dice 1200 segundos, pero el bearer muere a los 15, así que
`IolAuthClient` toma la duración de las fechas. Como el refresh token no sobrevive 20 minutos sin
uso, el trabajo `iol-token-refresh` renueva cada 5 minutos, las 24 horas, las autorizaciones a las
que les quedan menos de 10 minutos: si no corre, cada empleado tendría que volver a poner su
contraseña después de cualquier pausa. Si IOL rechaza el refresh token, la integración queda en
estado `Error` con el motivo, que `GET /api/integrations/iol` devuelve en `errorReason`, y el
empleado tiene que volver a vincular. Una caída de IOL no marca error: se reintenta en la próxima
llamada. `DELETE /api/integrations/iol` desvincula y borra los tokens.

**Importar el portafolio (T-35).** El trabajo `iol-sync` y `POST /api/integrations/iol/sync` (a
demanda, para el empleado logueado) leen `GET /api/v2/portafolio/argentina` y
`/estados_Unidos` y dejan cada posición como una inversión con `source = Iol` y
`external_reference` = mercado + símbolo (`bcba:GGAL`), única por empleado:

| Tipo de IOL | `InvestmentType` |
|---|---|
| `ACCIONES` | `Stock` |
| `CEDEARS` | `Cedear` |
| `TitulosPublicos` | `Bond` |
| `ObligacionesNegociables` | `CorporateBond` |
| `Letras` | `TreasuryBill` |
| `FondoComundeInversion` | `MutualFund` |
| `ETF` | `Etf` |

Opciones, cauciones y cualquier otro tipo o moneda se omiten y se cuentan como `ignored` en el
resumen. El capital es cantidad × `ppc` en la moneda del título; la fecha de compra, la de la
primera compra terminada del símbolo en `GET /api/v2/operaciones` (o el día de hoy si no aparece o
IOL no las devuelve); la primera valuación, `valorizado` con origen `Feed` y fecha del día. En las
corridas siguientes una cantidad distinta ajusta el capital y un valor distinto registra la
valuación del día (dos corridas el mismo día corrigen la misma). Una posición que deja de venir
no se borra ni se sigue valuando: conserva su última valuación (`noLongerHeld` en el resumen). Si
IOL no devuelve alguno de los dos portafolios no se toca ninguna inversión y la corrida queda
fallida. Una inversión manual con el mismo símbolo no se pisa: conviven y se distinguen por
`source`.

### Trabajos programados (Dokploy)

La API no tiene un scheduler propio. Lo que corre "solo cada tanto" son **trabajos** registrados
en código (`IScheduledJob`) que se disparan desde afuera, y en los entornos desplegados el que
los dispara es el cron de Dokploy (*Schedules*). Cada corrida queda registrada en la tabla
`job_runs` con inicio, fin, resultado y, si falló, el error; una corrida fallida nunca impide la
siguiente.

| Trabajo | Cron sugerido (UTC) | Qué hace |
|---|---|---|
| `metrics-snapshot` | `0 4 1 * *` (el 1 de cada mes, 01:00 de Argentina) | Genera las fotos mensuales de métricas por empresa y por departamento (`company_metrics_snapshots` y `department_metrics_snapshots`) del último mes cerrado, y completa las de los meses anteriores que falten desde el alta de cada empresa. En la misma pasada calcula el puntaje de salud financiera de cada empleado (ver más abajo). Es idempotente: correrlo de nuevo actualiza la foto del último mes cerrado en lugar de duplicarla. Si un mes necesita el dólar MEP y DolarApi no responde, esa foto queda pendiente y la corrida, fallida |
| `company-alerts` | `0 5 1 * *` (el 1 de cada mes, después de `metrics-snapshot`) | Evalúa la configuración de alertas de cada empresa sobre `company_metrics_snapshots`. Avisa si el bienestar financiero promedio o la participación quedaron por debajo del umbral configurado, y si la plantilla activa aumentó o disminuyó respecto de la foto mensual anterior. Solo expone períodos, porcentajes y cantidades agregadas |
| `mercadopago-sync` | `0 * * * *` (cada hora) | Recorre las cuentas de Mercado Pago vinculadas y trae los movimientos nuevos como pendientes de revisión. Mercado Pago no avisa por webhook lo que un usuario paga, por eso se consulta |
| `iol-token-refresh` | `*/5 * * * *` (cada 5 minutos, todo el día) | Renueva la autorización de cada cuenta de IOL vinculada a la que le quedan menos de 10 minutos, para que el refresh token (que dura 20) no venza. Si IOL la rechaza (el empleado cambió la contraseña o revocó el acceso), la integración queda en error; si IOL no responde, la corrida queda fallida y la siguiente reintenta sin perder la vinculación. **Tiene que estar programado**: sin él, las cuentas de IOL se desvinculan solas |
| `iol-sync` | `*/30 14-21 * * 1-5` (cada media hora de 11 a 18:30 de Argentina, días hábiles) | Importa el portafolio de cada cuenta de IOL vinculada como inversiones (ver *InvertirOnline (IOL)*). Si IOL no responde para una cuenta, esa no se toca, la corrida queda fallida con el motivo y la siguiente la reintenta. Una integración en error no se visita |
| `gmail-sync` | `0 13 * * *` (todos los días, 10:00 de Argentina: las facturas llegan una vez por mes) | Recorre los Gmail vinculados y activos, busca los mails que parecen facturas por su contenido (no por remitente, así un reenvío también entra) y le pide a FinGrow-AI que decida si lo son (`POST /api/v1/email-parsing/bills`). Cada factura entra como gasto pendiente con fuente `Gmail`, identificada por empresa + vencimiento + importe para que la original y su reenvío no se propongan dos veces. Si Google responde `invalid_grant` (el empleado quitó el permiso o venció), la integración queda en `Failed` y se le avisa (`IntegrationAccessLost`); deja de sincronizarse hasta que vuelva a autorizar. Si FinGrow-AI no responde, la próxima corrida retoma desde el último mail leído. Lee hasta 50 mails por corrida; si hay más, siguen al día siguiente. Solo importa facturas de servicios: un viaje (Cabify, Uber) o una compra no entra aunque traiga un total. El empleado también puede sincronizar a mano desde la card de Gmail (`POST /api/integrations/gmail/sync`), que corre lo mismo solo para su cuenta
| `investment-quotes` | `30 21 * * 1-5` (días hábiles, 18:30 de Argentina) | Guarda en `security_prices` el último precio de cada símbolo de BYMA, cada fondo de ArgentinaDatos y cada cripto de CoinGecko (aunque nadie tenga inversiones con símbolo) y cotiza con esos precios las inversiones cargadas con símbolo y cantidad, registrándoles la valuación de mercado del día. Correrlo de nuevo el mismo día actualiza esa valuación en lugar de duplicarla. Si una fuente no responde, cotiza con las otras y la corrida queda fallida con el motivo; si no responde ninguna, no toca nada |
| `investment-accruals` | `0 6 * * *` (todos los días, 03:00 de Argentina: el interés corre también los fines de semana) | Devenga con su TNA los plazos fijos, las cauciones y las cuentas remuneradas y les registra la valuación del día (ver *Lo que no cotiza en ningún mercado*). No llama a ninguna fuente externa. Correrlo de nuevo el mismo día actualiza esa valuación en lugar de duplicarla, y un plazo fijo vencido no suma valuaciones nuevas. Si falla con un empleado, sigue con el resto y la corrida queda fallida con el motivo |
| `receipts-cleanup` | `0 7 * * *` (todos los días, 04:00 de Argentina) | Borra los comprobantes sueltos (sin movimiento) subidos hace más de `ReceiptsCleanup:GraceDays` días (default 7): primero el archivo de R2 y después la fila de `transaction_receipts`. Si R2 falla para uno, sigue con el resto, la corrida queda fallida con el motivo y ese comprobante se reintenta en la siguiente. Nunca toca un comprobante asociado a un movimiento |
| `goal-progress-alerts` | `0 12 * * *` (todos los días, 09:00 de Argentina) | Recorre las metas activas y avisa al empleado cuando una lleva 30 días sin aportes (`GoalStalled`, como máximo uno cada 30 días mientras siga quieta) o cuando se acerca su fecha límite con saldo pendiente (`GoalDeadlineApproaching`, una vez a los 30, a los 7 y a 1 día, con cuánto falta y cuántos días quedan). Si la fecha límite está cerca solo manda esa, que ya dice lo mismo. Correrlo de nuevo el mismo día no repite avisos |
| `investment-alerts` | `30 12 * * *` (todos los días, 09:30 de Argentina, después de la cotización y de la sincronización de IOL del día anterior) | Avisa cuando el valor del portafolio de un empleado, por moneda, cambia 5 % o más entre sus dos últimas fechas de valuación (`InvestmentValueChanged`; lo comprado después de la fecha anterior no cuenta, y un portafolio sin valuaciones de los últimos 3 días no se evalúa) y cuando a un plazo fijo o una caución le faltan 7 o 1 día para vencer (`InvestmentMaturing`, con la fecha y lo que va a cobrar). No repite la misma fecha de valuación ni el mismo hito de un vencimiento |
| `email-delivery` | `*/5 * * * *` (cada 5 minutos) | Reintenta los correos de `outbound_emails` que no salieron en el momento (`Failed`, o `Pending` que su primer intento dejó colgado hace más de un minuto), hasta 50 por corrida. Cada fallo espera más para el siguiente intento (5 min, 15 min, 1 h, 4 h) y al quinto el correo queda `Abandoned`. Si alguno no sale, la corrida queda fallida con el motivo de cada uno |
| `achievements-evaluation` | `0 9 * * *` (todos los días, 06:00 de Argentina) | Otorga los logros de mes cerrado (`ConsistentThreeMonths`, `BudgetKept`, `Saver`) del último mes cerrado y avisa al empleado. Además es la red de seguridad de los logros instantáneos: la primera corrida exitosa otorga los que ya se cumplían antes de que existieran los logros (el front los celebra juntos), y las siguientes registran sin celebrar los que no se pudieron otorgar en el momento. Corre todos los días aunque el mes no cambie: un movimiento confirmado tarde puede completar el mes cerrado. Si falta el dólar MEP para medir el ahorro de alguien con movimientos en dólares, la corrida queda fallida y la siguiente lo reintenta (ver *Logros*) |

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

Para las alertas empresariales, el schedule mensual ejecuta el mismo comando con
`company-alerts`:

```bash
curl -fsS -X POST "http://localhost:8080/api/jobs/company-alerts/run" -H "X-Jobs-Key: $Jobs__ApiKey"
```

Si el schedule corre en el servidor en lugar de dentro del contenedor, es la misma llamada contra
el host público con la clave pegada. Y para probar a mano desde tu máquina:

```bash
curl -fsS -X POST "https://dev-be.fingrow.com.ar/api/jobs/metrics-snapshot/run" -H "X-Jobs-Key: <Jobs:ApiKey del entorno>"
```

La respuesta es la corrida registrada; si `status` es `Failed`, `error` dice por qué y el log de
la API tiene el detalle completo. `GET /api/jobs` sirve para verificar de un vistazo que cada
cron esté corriendo: muestra la última corrida de cada trabajo.

### Puntaje de salud financiera

Cada empleado con actividad suficiente recibe por mes un puntaje de 0 a 100
(`financial_health_scores`), que calcula `metrics-snapshot` al fotografiar el mes. Es un
indicador para la empresa, que solo lo ve agregado (promedio de la plantilla y por departamento:
HU-57, HU-60 y HU-62). El empleado no ve el suyo.

**Quién recibe puntaje.** Solo quien tuvo en el mes al menos 10 movimientos confirmados y algún
ingreso. Sin eso no se guarda nada, ni siquiera un cero: un mes casi vacío no dice nada de la
salud financiera de la persona, y un cero es otra cosa (alguien con actividad al que le va mal).

**Factores.** Cada uno va de 0 a 100 y es lineal entre los dos extremos.

| Factor | Peso | Qué mide | 0 | 100 |
|---|---|---|---|---|
| Ahorro | 40 % | (ingresos − gastos) / ingresos del mes. Lo gastado en `ahorro_inversion` cuenta como ahorro, no como gasto | ahorro de 0 % o menos | ahorro del 20 % o más |
| Presupuesto | 25 % | De las categorías con tope en el presupuesto mensual, cuántas no se pasaron. Pasarse es gastar el tope o más; estar en alerta (80 %) no resta | todas pasadas | ninguna pasada |
| Metas | 20 % | Promedio, entre las metas con cuota ese mes, de lo aportado en el mes sobre la cuota mensual (tope 100 % por meta). La meta que se alcanza ese mes cuenta completa | nada aportado | cuota cubierta |
| Colchón | 15 % | Saldo acumulado al cierre del mes (ingresos − gastos de toda la historia, sin restar `ahorro_inversion`) sobre el gasto mensual promedio de los últimos 3 meses, o de los que haya desde el primer movimiento | saldo de 0 o negativo | 3 meses de gasto cubiertos |

**Si no hay presupuesto del mes o ninguna meta con cuota, ese factor no cuenta y su peso se
reparte entre los demás**: el puntaje mide las finanzas, no cuántas pantallas usa la persona.
Ahorro y colchón están siempre.

```
puntaje = Σ (factor × peso) / Σ pesos de los factores presentes      (redondeado al entero)
```

| Banda | Puntaje |
|---|---|
| Crítica | 0 a 39 |
| Frágil | 40 a 59 |
| Estable | 60 a 79 |
| Saludable | 80 a 100 |

**Monedas.** Ahorro y colchón suman todo en pesos: lo que está en dólares se convierte al MEP
de venta de DolarApi del momento del cálculo, y lo que está en EUR o BRL no cuenta (igual que en
el dashboard). Presupuesto y metas comparan cada tope y cada meta en su propia moneda. Si hace
falta el MEP y DolarApi no responde, la foto de ese mes de esa empresa no se genera y la corrida
queda fallida; como la foto falta, la corrida siguiente la genera. Un mes viejo que se fotografía
por primera vez usa el MEP del día de la corrida, no el de ese mes.

**Cuándo se recalcula.** En cada corrida de `metrics-snapshot` el último mes cerrado se rehace
entero (factores y puntaje), así que un movimiento confirmado tarde lo actualiza, y si alguien
deja de tener actividad suficiente su puntaje se borra. Los meses que ya tenían foto antes de
este cambio quedan sin puntaje.

Las fotos guardan `employees_with_score` y `financial_health_score_sum`; el promedio se deriva
(`PeriodMetrics.AverageFinancialHealthScore`) y el umbral mínimo de anonimización de T-11 se
aplica al mostrarlo. La fórmula vive en `FinancialHealthFactors` (`Domain/ValueObjects/`): pesos
y umbrales son constantes de esa clase.

### Reportes: evolución mensual (HU-41), hitos y recomendaciones (HU-44)

`GET /api/reports/monthly-summary?currency&from&to` devuelve, mes por mes, ingresos, gastos, ahorro
y gasto por categoría de los movimientos confirmados de una moneda. Es la base que reutilizan los
reportes del empleado (HU-41 y HU-42). El ahorro cuenta lo destinado a `ahorro_inversion` como
ahorro, igual que el puntaje de salud financiera.

La serie no se rellena con ceros: quedan afuera los meses anteriores al primer movimiento del empleado
en la moneda (en todo su historial, no solo en el período pedido) y el mes en curso (o uno futuro)
mientras no tenga movimientos. Cualquier otro mes vacío aparece en cero, aunque esté al principio del
período. Sobre esos meses la respuesta trae los totales, el ahorro mensual promedio
(`averageMonthlySavings`, `null` si no hay meses) y `highlights`: el mes con mayor ingreso, el de mayor
gasto y el mejor mes de ahorro. Solo cuenta un monto positivo y, ante un empate, gana el mes más
reciente. No hay umbral de historial: con un solo mes con movimientos ya hay serie e indicadores.

`GET /api/reports/insights?currency&from&to` arma las dos tarjetas de la pestaña Resumen:

| Parte | De dónde sale |
|---|---|
| Habilitación | Al menos 2 meses con actividad suficiente (10 movimientos confirmados y algún ingreso) en todo el historial, sin importar el filtro. Vale el mes en curso y no tienen que ser seguidos. Si no alcanza, la respuesta trae cuántos meses activos hay y cómo va el mes en curso, y los logros del período igual: lo que se habilita son hitos y recomendaciones |
| Hitos | Calculados con reglas fijas sobre el período: totales, tasa de ahorro, mes con mayor ingreso, mes con mayor gasto y mejor mes de ahorro (las mismas reglas que `monthly-summary`), más los logros ganados en el período. El front muestra de acá solo los logros: los tres meses los toma de `monthly-summary`, que no tiene umbral |
| Recomendaciones | Tres, generadas por FinGrow-AI. Recibe los meses del período, los 3 meses anteriores como referencia, los topes del último mes contra lo gastado y las metas de la moneda con su nombre, y responde en el idioma de las preferencias del empleado |

Si la IA no responde, los hitos salen igual y `recommendationsAvailable` viene en `false`. Un período sin
movimientos en la moneda no llama a la IA. El mismo resumen se contesta desde una caché en memoria
durante `AiService:RecommendationsCacheHours`: la clave es el contenido completo del pedido, así que
cualquier movimiento nuevo genera recomendaciones nuevas. La aclaración de que no es asesoramiento
financiero certificado la muestra el front.

### Logros

El catálogo (`achievements`) es fijo: lo define `AchievementCatalog` (`Domain/Entities/`) y lo
carga la migración. Nadie lo edita desde la aplicación; cambiar un texto o sumar un logro es
cambiar el catálogo y generar la migración. Cada logro tiene su condición de otorgamiento escrita
(`condition`) y se otorga una sola vez por empleado (`employee_achievements`, índice único por
empleado y código). **Un logro no se revoca**, aunque después se borre lo que lo originó.

| Código | Logro | Se otorga | Celebración |
|---|---|---|---|
| `FirstTransaction` | Primer paso | Al confirmar el primer movimiento | Toast |
| `ReceiptScanned` | Sin papeles | Al confirmar un movimiento que entró por foto del comprobante | Toast |
| `ChannelLinked` | Conectado | Al vincular la primera integración | Toast |
| `FirstBudget` | Con plan | Al poner el primer tope por categoría | Toast |
| `FirstGoal` | Con objetivo | Al crear la primera meta | Toast |
| `GoalAchieved` | Meta cumplida | Al alcanzar una meta | Central |
| `ThreeGoalsAchieved` | Imparable | Al alcanzar la tercera meta | Central |
| `FirstInvestment` | Inversor | Al registrar o importar la primera inversión | Toast |
| `FirstCourse` | Aprendiz | Al completar todas las lecciones de un curso | Toast |
| `ConsistentThreeMonths` | Constancia | Tres meses cerrados seguidos con actividad suficiente | Central |
| `BudgetKept` | Dentro del plan | Un mes cerrado con actividad suficiente sin ninguna categoría por encima del tope | Central |
| `Saver` | Ahorrador | Un mes cerrado con actividad suficiente ahorrando el 10 % o más de los ingresos | Central |

"Actividad suficiente" es el mismo umbral del puntaje de salud financiera: 10 movimientos
confirmados y algún ingreso en el mes. Ningún logro usa un monto fijo, porque un umbral en pesos
queda viejo con la inflación y no se puede comparar entre monedas.

**Cómo se otorgan.** Los instantáneos los otorga `UnlockInstantAchievementsHandler` al escuchar los
eventos de dominio (`TransactionConfirmed`, `GoalCreated`, `GoalAchieved`, `BudgetLimitSet`,
`IntegrationLinked`, `InvestmentAdded`, `LessonCompleted`); el evento solo dice qué logros pudo
destrabar, y si se cumplen lo decide lo que hay en la base. Los de mes cerrado los otorga
`achievements-evaluation`. En los dos casos se levanta `AchievementUnlocked`, que manda la
notificación `AchievementUnlocked` (bandeja, tiempo real y Telegram).

**Cómo los celebra el front.** La notificación es solo la señal: el front pide
`GET /api/achievements/uncelebrated`, muestra lo que haya (un logro chico como toast, uno
importante en el centro, varios juntos en un solo toast con todas sus medallas) y después
confirma con `POST /api/achievements/celebrate` qué códigos mostró. Lo mismo hace al abrir la app,
así que un logro ganado desde Telegram o WhatsApp se celebra la próxima vez que el empleado entra.
La empresa no ve logros, ni siquiera agregados.

| Método y ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/achievements` | Empleado | El catálogo completo, con `achievedAt` en los que ya ganó (null en el resto). `code` es lo que elige la medalla |
| `GET /api/achievements/uncelebrated` | Empleado | Los ganados que el front todavía no celebró, del más viejo al más nuevo |
| `POST /api/achievements/celebrate` | Empleado | Marca como celebrados los códigos indicados: `{ "codes": ["FirstGoal"] }`. Ignora los que ya estaban celebrados o no son del empleado |

### Alertas y notificaciones

Las alertas salen de un único motor (`Notifier`, en `Application/Features/Notifications`) que
comparten presupuestos, metas, integraciones y el panel de empresa. Cada alerta pasa por tres
pasos, siempre en este orden:

1. **Deduplicación.** Toda alerta declara un criterio (`deduplication_key`, por ejemplo
   `transactions-to-review:MercadoPago`) y una ventana. Si al mismo destinatario ya se le avisó
   el mismo tipo con el mismo criterio dentro de la ventana, no se vuelve a avisar.
2. **Persistencia.** La notificación queda en la tabla `notifications`, que es la bandeja de la
   app y el historial de todo lo que se avisó. Por eso la bandeja no se puede apagar.
3. **Entrega.** Se empuja al instante a las pestañas abiertas del destinatario (SignalR, ver más
   abajo) y se manda además por cada canal externo que el empleado no haya desactivado. Telegram
   requiere tener el chat vinculado; las alertas `BudgetWarning` y `BudgetExceeded` también se
   encolan como email en `outbound_emails` para que las entregue el job de correos con reintentos.
   Si un canal falla, se loguea y la notificación queda igual en la bandeja.

El destinatario puede ser un empleado o la empresa (`recipient_type`), así que la misma bandeja
sirve para las alertas de bienestar del panel de empresa. Los canales externos son solo para
empleados. La empresa configura los cuatro tipos empresariales de forma independiente:
`FinancialWellbeingLow` y `ParticipationLow` se evalúan mensualmente sobre fotos agregadas y cada
empresa puede fijar su umbral entre 1 y 100. `EmployeeAdded` y `EmployeeRemoved` comparan la
cantidad de empleados activos entre las dos últimas fotos mensuales y avisan, respectivamente,
un aumento o una reducción netos de la plantilla. Los cuatro tipos empresariales se evalúan
mensualmente y solo incluyen períodos y cantidades agregadas; no registran ni exponen eventos o
datos personales de empleados.

#### Alertas empresariales (HU-50)

Las cuatro preferencias pertenecen a la empresa, empiezan desactivadas y se habilitan de forma
independiente desde `PUT /api/notifications/types/{tipo}`. La frecuencia es siempre
`Monthly`: el job `company-alerts` corre después de `metrics-snapshot`, por lo que analiza el
último mes cerrado y no cambios en tiempo real.

| Tipo | Regla evaluada sobre `company_metrics_snapshots` | Configuración |
|---|---|---|
| `FinancialWellbeingLow` | El promedio de puntaje de salud financiera del último snapshot visible es menor que el umbral | Umbral opcional entre 1 y 100; por defecto, 60 |
| `ParticipationLow` | La tasa de participación del último snapshot visible es menor que el umbral | Umbral opcional entre 1 y 100; por defecto, 50 |
| `EmployeeAdded` | Los empleados activos del último snapshot son más que los del snapshot mensual anterior | No lleva umbral |
| `EmployeeRemoved` | Los empleados activos del último snapshot son menos que los del snapshot mensual anterior | No lleva umbral |

La participación cuenta a un empleado activo que, durante el mes, confirmó al menos un
movimiento o hizo un aporte a una meta. El bienestar financiero es el promedio de los puntajes
de salud financiera que pudieron calcularse para el período; un puntaje requiere actividad
suficiente y algún ingreso. Ambos respetan el umbral de anonimización del panel empresarial: si
el snapshot no puede revelarse, no generan alerta.

Las alertas de plantilla describen un **cambio neto**, no eventos individuales de contratación,
eliminación o desactivación. Si no existe un snapshot mensual anterior, no se puede comparar y
no se emite una alerta. Así, las notificaciones contienen solo el período actual, el anterior y
las cantidades agregadas de empleados activos.

Lo que dispara una alerta es un **evento de dominio**: un agregado lo levanta (`Raise`) cuando
pasa algo, y recién cuando `SaveChanges` confirma el cambio se publica por MediatR, cada evento
en un scope propio. Si una reacción falla, se loguea y no rompe la operación que la originó,
porque esa ya quedó guardada. La primera alerta es `TransactionsToReview`: cuando Mercado Pago o
Gmail traen movimientos pendientes, el empleado recibe un aviso para revisarlos, como máximo uno
cada 12 horas por origen (un sync de 30 movimientos genera una sola alerta). Lo que carga el
empleado a mano, por foto o por chat no avisa: ya está mirando la app.
`IntegrationAccessLost` avisa cuando la sincronización pierde el acceso a una cuenta vinculada (Gmail o IOL), una
sola vez hasta que el empleado la vuelva a autorizar.
`NewEducationContent` avisa a todos los empleados con acceso cuando un curso o un artículo se publica por primera vez (`Course.Publish` y `Article.Publish` levantan `EducationContentPublished`, y solo la primera publicación; una segunda no avisa). Cada empleado se avisa en su propio scope y la deduplicación es por contenido, así que un evento reprocesado no repite el aviso.

| Método y ruta | Quién | Qué hace |
|---|---|---|
| `GET /api/notifications?unreadOnly=false&pageNumber=1&pageSize=20` | Empleado y empresa | La bandeja del usuario logueado, la más reciente primero (`pageSize` entre 1 y 100) |
| `GET /api/notifications/unread-count` | Empleado y empresa | Cuántas no leídas tiene, para el contador de la campanita |
| `POST /api/notifications/{id}/read` | Empleado y empresa | Marca una como leída. `404` si no es del usuario |
| `POST /api/notifications/read-all` | Empleado y empresa | Marca todas como leídas y devuelve cuántas marcó |
| `GET /api/notifications/channels` | Empleado | Cada canal con `isEnabled`, `isConfigurable` y `isConnected` (Telegram vinculado) |
| `PUT /api/notifications/channels/{canal}` | Empleado | Activa o desactiva un canal externo: `{ "isEnabled": false }`. `400` para `InApp` |
| `GET /api/notifications/types` | Empleado y empresa | Cada tipo habilitable del rol con `isEnabled`; para empresa agrega `cadence` (`Monthly`) y, cuando corresponde, `threshold` |
| `PUT /api/notifications/types/{tipo}` | Empleado y empresa | Activa o desactiva un tipo. La empresa siempre envía `cadence: "Monthly"`; bienestar y participación pueden incluir `threshold`, mientras que las alertas de plantilla no lo admiten |

Ejemplos de configuración empresarial:

```json
{ "isEnabled": true, "cadence": "Monthly", "threshold": 70 }
```

para `FinancialWellbeingLow` o `ParticipationLow`, y:

```json
{ "isEnabled": true, "cadence": "Monthly" }
```

para `EmployeeAdded` o `EmployeeRemoved`.

Un canal sin preferencia guardada está activo: quien vincula Telegram empieza a recibir alertas
por ahí hasta que lo apague.

Las preferencias por tipo y por canal conviven: el tipo decide si la alerta existe y el canal
decide por dónde viaja. Un tipo desactivado (HU-05) corta en `Notifier` antes de crear la
notificación, así que no queda en la bandeja ni sale por Telegram o correo. Para empleados, un
tipo sin preferencia guardada está activo. Para empresa, los cuatro tipos de HU-50 nacen
desactivados: no se genera ninguna alerta hasta que la empresa la habilita explícitamente.

**Tiempo real.** El frontend escucha el hub de SignalR `/hubs/notifications`
(`Api/Notifications/`) en lugar de consultar el contador cada tanto. Cada notificación nueva
llega con el evento `NotificationReceived` y el mismo JSON que un ítem de
`GET /api/notifications`, solo a las conexiones de su destinatario (empleado o empresa) y a
todas sus pestañas. El hub se autentica con la misma cookie de sesión que la API y cierra la
conexión cuando vence el access token: el front renueva la sesión y se reconecta. Traefik
(Dokploy) deja pasar WebSockets sin configuración extra; si un proxy los bloquea, SignalR cae a
Server-Sent Events o long polling. Funciona con una sola réplica de la API: con más de una hace
falta un backplane (Redis) para que el aviso llegue a la conexión que está en otra réplica.

Para sumar una alerta nueva: un tipo en `NotificationType`, el evento de dominio que la dispara
(levantado por el agregado con `Raise`) y una clase `internal` bajo
`Application/Features/Notifications/Alerts/` que implementa
`INotificationHandler<DomainEventEnvelope<TuEvento>>` y llama a `Notifier.NotifyAsync` con el
criterio y la ventana de deduplicación. Una alerta que depende del paso del tiempo (una meta sin
aportes) se evalúa desde un trabajo programado y usa el mismo `Notifier`, como
`goal-progress-alerts` (`Alerts/GoalProgress/`).

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
│   ├── Integrations/       Twilio (firma de webhooks y descarga de adjuntos) y Telegram (secreto del webhook y Bot API), Mercado Pago y Gmail (OAuth y lectura de mails)
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
| `companies` | Company | Empresa contratante: razón social, CUIT, credenciales (contraseña y secreto del doble factor, cifrado), moneda por defecto |
| `departments` | (parte de Company) | Departamentos de la empresa; se desactivan, no se borran |
| `employees` | Employee | La persona que usa la app; pertenece a una empresa y opcionalmente a un departamento |
| `transactions` | Transaction | Ingresos y gastos, con importe siempre positivo y el signo dado por `type` |
| `budgets` | Budget | Tope por categoría y período |
| `goals` | Goal | Metas de ahorro con objetivo, avance y fecha límite |
| `investments` | Investment | Posiciones del portafolio: capital invertido y valuación actual. `symbol` es el ticker (hasta 20 caracteres) o, en un fondo común, su nombre completo (hasta 150); `quantity` admite 10 decimales para fracciones de cripto. `source` dice si la cargó el empleado (`Manual`) o la importó IOL (`Iol`); las importadas llevan `external_reference` (mercado + símbolo), única por empleado y origen. `annual_rate` (TNA en porcentaje, dos decimales) y `term_days` valúan plazos fijos, cauciones y cuentas remuneradas |
| `security_prices` | SecurityPrice | El último precio por unidad de cada mercado, símbolo y moneda (BYMA, fondos de ArgentinaDatos y cripto de CoinGecko); una fila por mercado, símbolo y moneda que `investment-quotes` pisa en cada corrida. En los fondos el símbolo es el nombre del fondo. Es lo que cotiza el formulario de inversiones cuando la fuente no publica precios (BYMA los fines de semana y feriados) o no responde |
| `job_runs` | JobRun | Registro de cada corrida de un trabajo programado: inicio, fin, resultado y error |
| `company_metrics_snapshots` | CompanyMetricsSnapshot | Foto mensual de métricas agregadas de una empresa: empleados activos, cuántos participaron, movimientos confirmados, presupuestos, metas, integraciones, y cuántos tienen puntaje de salud financiera con la suma de esos puntajes |
| `financial_health_scores` | FinancialHealthScore | El puntaje de salud financiera de cada empleado por mes, con sus cuatro factores y la fecha de cómputo. Solo existe para los meses con actividad suficiente |
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
