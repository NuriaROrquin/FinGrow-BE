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

**Un importe nunca viaja solo.** `Money` es un value object de monto + moneda que se guarda como
dos columnas. Combinar importes de monedas distintas lanza `DomainException`, a propósito.

**Un movimiento propuesto no cuenta hasta que lo confirman.** Lo que entra por Gmail, por el bot
o por el OCR nace con `status = Pending`. Saldo, presupuestos y reportes miran solo los
confirmados.

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
  code: `Validation`→400, `NotFound`→404, `Conflict`→409, `Forbidden`→403, cualquier otro
  (`Failure`)→500. Un controller nuevo solo necesita
  `return (await sender.Send(command, ct)).ToActionResult();`.
- Esto es distinto de `ExceptionHandlingMiddleware`: ese middleware sigue cubriendo únicamente
  lo inesperado (excepciones no manejadas); `Result.Failure` es el camino para fallos de negocio
  esperables y nunca debería llegar como excepción.

El primer feature real es `Application/Features/Integrations/WhatsApp` (T-25): sirve de
referencia de cómo queda un caso de uso completo, con repositorios en `Domain/Repositories`
implementados en `Infrastructure/Persistence/Repositories`. `tests/FinGrow.Application.UnitTests`
prueba los handlers con fakes en memoria (`Fakes/InMemoryFakes.cs`) y `tests/FinGrow.Api.UnitTests`
el borde HTTP reemplazando los repositorios en `ConfigureTestServices`.

**El webhook de WhatsApp es público y se protege con la firma de Twilio**, no con JWT.
`ValidateTwilioSignatureAttribute` (`Api/Twilio/`) corta antes del handler; el controller no
tiene lógica: parsear el form es `TwilioInboundMessage` y responder es `TwiMlResult`. Un número
que no está en `employee_integrations` solo puede mandar su código de vinculación.

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

Quedan tres divergencias entre lo construido y el DER objetivo: el responsable de un
departamento como FK (HU-52), la tabla `users` para credenciales, 2FA y preferencias (HU-01,
HU-04, HU-46, HU-49) y `ai_confidence` más el estado `Discarded` en `transactions` (HU-12,
HU-15). Cada una arrastra una migración sobre tablas ya construidas: revisá
[docs/database-schema-target.md](docs/database-schema-target.md) antes de tomar esas historias.
